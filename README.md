# Maliev.InvoiceService

This project has undergone a comprehensive migration and API refinement to align with modern .NET development standards, focusing on improved maintainability, testability, security, and consistency with other services.

## Project Structure

The solution is now composed of three main projects:

*   **`Maliev.InvoiceService.Api`**: The ASP.NET Core Web API project, responsible for handling HTTP requests, defining API contracts (using DTOs), and orchestrating business logic through a service layer. It now utilizes the minimal API hosting model (`Program.cs`) for configuration.
*   **`Maliev.InvoiceService.Data`**: A .NET Standard library project, responsible for data access, containing the Entity Framework Core `DbContext` and entity models.
*   **`Maliev.InvoiceService.Tests`**: A project containing unit and integration tests for the service and API layers.

## API Endpoints

The API follows a RESTful design, with clear, hierarchical routes. The base address for this API is `https://api.maliev.com`.

### Invoices

*   **GET /invoices**: Retrieve a paginated collection of all invoices.
    *   Query Parameters: `pageNumber`, `pageSize`
    *   Example: `GET https://api.maliev.com/invoices?pageNumber=1&pageSize=10`
*   **GET /api/invoices/{id}**: Retrieve a specific invoice by its unique ID.
*   **GET /api/invoices/by-number/{number}**: Retrieve a specific invoice by its unique invoice number.
*   **POST /api/invoices**: Create a new invoice.
*   **PUT /api/invoices/{id}**: Fully update an existing invoice by its ID.
*   **DELETE /api/invoices/{id}**: Delete a specific invoice by its ID.

### Invoice Files

*   **GET /api/invoices/{invoiceId}/file**: Retrieve the invoice file (PDF) associated with a specific invoice.
*   **POST /api/invoices/{invoiceId}/file**: Upload/create the invoice file for a specific invoice.
*   **PUT /api/invoices/{invoiceId}/file**: Update the invoice file for a specific invoice.
*   **DELETE /invoices/{invoiceId}/file**: Delete the invoice file associated with a specific invoice.

### Order Items

*   **GET /api/invoices/{invoiceId}/order-items**: Retrieve all order items belonging to a specific invoice.
*   **GET /api/invoices/{invoiceId}/order-items/{orderItemId}**: Retrieve a specific order item for a specific invoice.
*   **POST /invoices/{invoiceId}/order-items**: Create a new order item for a specific invoice.
*   **PUT /api/invoices/{invoiceId}/order-items/{orderItemId}**: Fully update a specific order item for a specific invoice.
*   **DELETE /invoices/{invoiceId}/order-items/{orderItemId}**: Delete a specific order item for a specific invoice.

## Build Instructions

To build the entire solution, navigate to the root directory (`R:\maliev\Maliev.InvoiceService`) and run:

```bash
dotnet build
```

## Run Instructions

To run the API project locally, navigate to the `Maliev.InvoiceService.Api` directory (`R:\maliev\Maliev.InvoiceService\Maliev.InvoiceService.Api`) and run:

```bash
dotnet run
```

This will start the API, and if configured, it will automatically open your browser to the Swagger UI. You can access the Swagger UI at `https://localhost:7000/invoices/swagger` (or your configured port).

## Test Instructions

To run the tests, navigate to the root directory (`R:\maliev\Maliev.InvoiceService`) and run:

```bash
dotnet test Maliev.InvoiceService.Tests/Maliev.InvoiceService.Tests.csproj
```

## Secret Management

Sensitive information such as database connection strings and JWT keys are managed externally for both production and local development environments.

### Production Secrets (Google Secret Manager)

For production deployments, secrets are retrieved from Google Secret Manager. Ensure the following secrets are configured in your Google Cloud project (`maliev-website`):

*   `ConnectionStrings-InvoiceServiceDbContext`: Your production database connection string.
*   `JwtSecurityKey`: Your JWT security key.

These secrets are accessed via Kubernetes `SecretProviderClass` during deployment.

### Local Development Secrets (User Secrets)

For local development, sensitive information is managed using .NET User Secrets. This keeps your secrets out of source control.

To configure your local secrets:

1.  Right-click on the `Maliev.InvoiceService.Api` project in Visual Studio.
2.  Select "Manage User Secrets".
3.  A `secrets.json` file will open. Paste the following structure into it and replace the placeholder values with your actual local development secrets:

    ```json
    {
      "JwtSecurityKey": "YOUR_JWT_SECURITY_KEY",
      "Jwt:Issuer": "YOUR_JWT_ISSUER",
      "Jwt:Audience": "YOUR_JWT_AUDIENCE",
      "ConnectionStrings:InvoiceServiceDbContext": "YOUR_LOCAL_DATABASE_CONNECTION_STRING"
    }
    ```
    **Note:** The `JwtSecurityKey` is currently hardcoded in `Program.cs` for demonstration purposes. **It is crucial to replace this hardcoded key with a secure key loaded from configuration (e.g., User Secrets for local development, environment variables, or Google Secret Manager for production) before deploying to a production environment.**

## Deployment

The project is configured for deployment to Kubernetes using Google Kubernetes Engine (GKE).

### Prerequisites

*   `gcloud` CLI installed and configured.
*   `kubectl` CLI installed and configured.
*   Access to the `maliev-website` Google Cloud project.
*   Docker installed.

### Deployment Steps

1.  **Build and Push Docker Image:**
    Navigate to the project root (`R:\maliev\Maliev.InvoiceService`) and run the PowerShell script:
    ```powershell
    .\deploy.ps1
    ```
    This script will:
    *   Replace the `##VERSION##` placeholder in `deployment.yaml` with a generated tag.
    *   Authenticate Docker with Google Artifact Registry.
    *   Build the Docker image for `Maliev.InvoiceService.Api`.
    *   Push the Docker image to Google Artifact Registry.
    *   Apply the `deployment.yaml` to your Kubernetes cluster.
    *   Display pod and service information.
    *   Revert the `##VERSION##` placeholder in `deployment.yaml`.

2.  **Apply Kubernetes Service:**
    To apply or update the Kubernetes Service, run the PowerShell script:
    ```powershell
    .\deploy-service.ps1
    ```
    This script will apply the `service.yaml` to your Kubernetes cluster.