using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PlantCareApp.Models;
using PlantCareApp.Options;

namespace PlantCareApp.Services;

public class PlantIdentificationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;
    private readonly ILogger<PlantIdentificationService> _logger;

    public PlantIdentificationService(HttpClient httpClient, IOptions<OpenAIOptions> options, ILogger<PlantIdentificationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PlantIdentificationResult>> IdentifyAsync(Stream photoStream, CancellationToken cancellationToken = default)
    {
        var apiKey = _options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                     Environment.GetEnvironmentVariable("PLANTCARE_OPENAI_APIKEY");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is missing. Configure OpenAI:ApiKey in appsettings or environment variables.");
        }

        var base64Image = await ConvertToBase64Async(photoStream, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = BuildRequestPayload(base64Image);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, SerializerOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var completion = await JsonSerializer.DeserializeAsync<ChatCompletionResponse>(stream, SerializerOptions, cancellationToken);
        if (completion?.Choices is null || completion.Choices.Length == 0)
        {
            return Array.Empty<PlantIdentificationResult>();
        }

        try
        {
            var content = completion.Choices[0].Message.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                return Array.Empty<PlantIdentificationResult>();
            }

            var parsed = JsonSerializer.Deserialize<PlantIdentificationEnvelope>(content, SerializerOptions);
            return parsed?.Candidates ?? Array.Empty<PlantIdentificationResult>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse OpenAI response for plant identification. Raw: {Raw}", completion.Choices[0].Message.Content);
            throw;
        }
    }

    private static async Task<string> ConvertToBase64Async(Stream stream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return Convert.ToBase64String(ms.ToArray());
    }

    private object BuildRequestPayload(string base64Image)
    {
        return new
        {
            model = string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4o-mini" : _options.Model,
            temperature = 0.2,
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "plant_identification",
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            candidates = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        commonName = new { type = "string" },
                                        scientificName = new { type = "string" },
                                        confidence = new { type = "number" },
                                        summary = new { type = "string" },
                                        referencePhotos = new
                                        {
                                            type = "array",
                                            items = new { type = "string" }
                                        }
                                    },
                                    required = new[] { "commonName", "scientificName", "confidence" }
                                }
                            }
                        },
                        required = new[] { "candidates" }
                    }
                }
            },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "Eres un botánico especializado en ornamentales latinoamericanas y variedades variegadas. Al analizar una foto debes fijarte en forma de hoja, patrón de nervaduras, colores, manchas y contexto. Prioriza especies que respeten los detalles observados (por ejemplo, si ves hojas variegadas blanco/verde, evita sugerir cultivares completamente verdes). Devuelve siempre nombres comunes y científicos junto con una nota corta de cuidados clave."
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "Analiza la imagen y devuelve las 3 especies más probables en español neutro. Explica por qué cada opción coincide con la foto, mencionando si se trata de una versión variegada o cultivar especial. Reduce la confianza para especies genéricas (poto, filodendro) cuando haya patrones distintivos como manchas blancas, bordes claros o formas muy específicas tipo Syngonium podophyllum albo variegatum. Incluye solo datos verificables de cultivo." },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = $"data:image/jpeg;base64,{base64Image}"
                            }
                        }
                    }
                }
            }
        };
    }
}

public record PlantIdentificationResult(
    string CommonName,
    string ScientificName,
    double Confidence,
    string? Summary,
    IReadOnlyList<string>? ReferencePhotos);

file record PlantIdentificationEnvelope(IReadOnlyList<PlantIdentificationResult> Candidates);

file record ChatCompletionResponse(ChatChoice[] Choices);

file record ChatChoice(ChatMessage Message);

file record ChatMessage(string Role, string Content);
