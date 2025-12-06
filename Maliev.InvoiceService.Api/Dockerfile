# syntax=docker/dockerfile:1.4
# Multi-stage Docker build for Maliev.InvoiceService.Api
# Based on MALIEV Co. Ltd. standard .NET 10 microservice template

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy nuget.config and project files
COPY nuget.config ./
COPY Maliev.InvoiceService.Api/Maliev.InvoiceService.Api.csproj Maliev.InvoiceService.Api/
COPY Maliev.InvoiceService.Data/Maliev.InvoiceService.Data.csproj Maliev.InvoiceService.Data/

# Restore with GitHub Packages authentication using BuildKit secrets
RUN --mount=type=secret,id=nuget_username \
  --mount=type=secret,id=nuget_password \
  NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
  NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
  dotnet restore Maliev.InvoiceService.Api/Maliev.InvoiceService.Api.csproj

# Copy all source code
COPY . .

# Build and publish
WORKDIR /src/Maliev.InvoiceService.Api
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create directory for Google Secret Manager secrets and ensure 'app' owns the workdir (app user already exists in ASP.NET runtime image)
RUN mkdir -p /mnt/secrets && chown -R app:app /app /mnt/secrets

# Switch to non-root user
USER app

# Copy published application (now owned by app)
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set ASP.NET Core environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/invoices/liveness || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Maliev.InvoiceService.Api.dll"]
