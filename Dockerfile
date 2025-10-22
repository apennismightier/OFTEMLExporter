# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out ./

# ✅ Vercel sets PORT (defaults to 3000). Listen on it.
ENV PORT=3000
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE 3000

ENTRYPOINT ["dotnet", "OftEmlExporter.dll"]
