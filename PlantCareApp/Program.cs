using Microsoft.EntityFrameworkCore;
using PlantCareApp.Components;
using PlantCareApp.Data;
using PlantCareApp.Options;
using PlantCareApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=plants.db"));

builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));

builder.Services.AddScoped<PlantService>();
builder.Services.AddScoped<ImageStorageService>();
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<PhotoAnalysisService>();
builder.Services.AddScoped<RecommendationService>();
builder.Services.AddScoped<PlantTimelineService>();
builder.Services.AddScoped<CareAutomationService>();
builder.Services.AddHttpClient<PlantIdentificationService>();
builder.Services.AddHttpClient<LocationLookupService>();
builder.Services.AddSingleton<HomeZoneService>();
builder.Services.AddSingleton<UserSettingsService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
