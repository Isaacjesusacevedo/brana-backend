# ── Stage 1: build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restaurar dependencias (layer cacheada si .csproj no cambia)
COPY Backend.csproj .
RUN dotnet restore

# Copiar el resto del código y publicar en modo Release
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Stage 2: runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copiar artefactos de la etapa de build
COPY --from=build /app/publish .

# Render inyecta PORT automáticamente; el default en Program.cs es 8080
EXPOSE 8080

# Variables de entorno que DEBEN configurarse en Render Dashboard:
#   DATABASE_URL       → Supabase PostgreSQL connection string
#   CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET
#   FRONTEND_URL       → URL del frontend en Netlify (para CORS)
#   ASPNETCORE_ENVIRONMENT → Production

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Backend.dll"]
