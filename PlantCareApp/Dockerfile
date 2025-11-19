# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos solo la carpeta del proyecto desde el contexto (que es la raíz del repo)
COPY PlantCareApp/ ./PlantCareApp/

# Nos movemos a la carpeta del proyecto
WORKDIR /src/PlantCareApp

# Restauramos dependencias
RUN dotnet restore ./PlantCareApp.csproj

# Publicamos en modo Release
RUN dotnet publish ./PlantCareApp.csproj -c Release -o /app/publish

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiamos lo publicado desde la etapa de build
COPY --from=build /app/publish .

# La app escucha en el puerto 8080 (Dokploy/Traefik)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PlantCareApp.dll"]
