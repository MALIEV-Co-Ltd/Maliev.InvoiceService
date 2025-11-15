# Multi-stage Docker build for Maliev.InvoiceService.Api
# Based on MALIEV Co. Ltd. standard .NET 10 microservice template

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Maliev.InvoiceService.sln ./
COPY Maliev.InvoiceService.Api/Maliev.InvoiceService.Api.csproj Maliev.InvoiceService.Api/
COPY Maliev.InvoiceService.Data/Maliev.InvoiceService.Data.csproj Maliev.InvoiceService.Data/
COPY Maliev.InvoiceService.Tests/Maliev.InvoiceService.Tests.csproj Maliev.InvoiceService.Tests/

# Restore dependencies
RUN dotnet restore Maliev.InvoiceService.sln

# Copy all source code
COPY . .

# Build and publish
WORKDIR /src/Maliev.InvoiceService.Api
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application
COPY --from=build /app/publish .

# Create directory for Google Secret Manager secrets
RUN mkdir -p /mnt/secrets && chown -R appuser:appuser /mnt/secrets

# Switch to non-root user
USER appuser

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
