FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos TODO el proyecto (porque el contexto ya es PlantCareApp)
COPY . .

# Restauramos el proyecto principal
RUN dotnet restore ./PlantCareApp.csproj

# Publicamos en modo Release
RUN dotnet publish ./PlantCareApp.csproj -c Release -o /app/publish

# Imagen de runtime (producción)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiamos lo publicado desde la etapa build
COPY --from=build /app/publish .

# La app escucha en el puerto 8080 (para Dokploy/Traefik)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PlantCareApp.dll"]
