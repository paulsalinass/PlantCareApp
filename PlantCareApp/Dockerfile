FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos el csproj y restauramos dependencias
COPY *.csproj .
RUN dotnet restore ./PlantCareApp.csproj

# Copiamos el resto del código
COPY . .

# Publicamos en modo Release
RUN dotnet publish ./PlantCareApp.csproj -c Release -o /app/publish

# Imagen de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiamos lo publicado desde la etapa build
COPY --from=build /app/publish .

# La app escucha en el puerto 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PlantCareApp.dll"]
