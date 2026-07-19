# Quickstart: Permission-Based Authorization

## Enabling Permission-Based Auth

To enable the new authorization model in your local development environment:

1. Update `appsettings.Development.json`:
   ```json
   {
     "Features": {
       "PermissionBasedAuthEnabled": true
     },
     "ExternalServices": {
       "IAM": {
         "BaseUrl": "http://localhost:5100",
         "ServiceAccountToken": "dev-token"
       }
     }
   }
   ```

2. Ensure the IAM Service is running.

3. Start the InvoiceService. It will automatically register permissions and roles.

## Testing with Postman/Insomnia

1. Obtain a JWT from the identity provider.
2. If your token contains roles like `Manager`, the system will map them to permissions.
3. If your token contains a `permissions` claim (e.g., `["invoice.invoices.create"]`), it will take precedence.

## Verification

Check the service logs for:
`[Information] IAM: Registered 21 permissions for InvoiceService`
`[Information] IAM: Registered 5 predefined roles for InvoiceService`
