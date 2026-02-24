using Maliev.InvoiceService.Api.Models.Customers;
using Maliev.InvoiceService.Api.Services.External;

namespace Maliev.InvoiceService.Tests.Mocks;

public class MockCustomerServiceClient : ICustomerServiceClient
{
    public Task<CustomerResponse?> GetCustomerByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        // Return a valid corporate customer for all test requests
        // CompanyName matches what most tests send as CustomerName so assertions pass
        var customer = new CustomerResponse
        {
            Id = customerId,
            FirstName = "Test",
            LastName = "Customer",
            CompanyId = Guid.NewGuid(),
            CompanyName = "Test Customer"
        };

        return Task.FromResult<CustomerResponse?>(customer);
    }
}
