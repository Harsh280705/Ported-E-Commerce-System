# Production build for the .NET 10 Minimal API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/Ecommerce.Core/Ecommerce.Core.csproj backend/Ecommerce.Core/
COPY backend/Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj backend/Ecommerce.Infrastructure/
COPY backend/Ecommerce.Api/Ecommerce.Api.csproj backend/Ecommerce.Api/
RUN dotnet restore backend/Ecommerce.Api/Ecommerce.Api.csproj
COPY backend/ backend/
WORKDIR /src/backend/Ecommerce.Api
RUN dotnet publish Ecommerce.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ecommerce.Api.dll"]
