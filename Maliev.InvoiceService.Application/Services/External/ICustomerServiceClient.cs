using Maliev.InvoiceService.Application.Models.Customers;

namespace Maliev.InvoiceService.Application.Services.External;

/// <summary>
/// Client interface for interacting with the Customer Service API
/// </summary>
public interface ICustomerServiceClient
{
    /// <summary>
    /// Retrieves a customer by their unique identifier
    /// </summary>
    /// <param name="customerId">The unique customer identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer response with details, or null if not found</returns>
    Task<CustomerResponse?> GetCustomerByIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}
