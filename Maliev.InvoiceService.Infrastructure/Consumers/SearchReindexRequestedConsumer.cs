using Maliev.InvoiceService.Infrastructure.Persistence;
using Maliev.InvoiceService.Infrastructure.Search;
using Maliev.MessagingContracts.Contracts.Search;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maliev.InvoiceService.Infrastructure.Consumers;

/// <summary>
/// Republishes invoice search documents when SearchService requests a reindex.
/// </summary>
public class SearchReindexRequestedConsumer : IConsumer<SearchReindexRequestedCommand>
{
    private const string SourceService = "InvoiceService";
    private readonly InvoiceDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SearchReindexRequestedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchReindexRequestedConsumer"/> class.
    /// </summary>
    /// <param name="context">Invoice database context.</param>
    /// <param name="publishEndpoint">MassTransit publish endpoint.</param>
    /// <param name="logger">Logger instance.</param>
    public SearchReindexRequestedConsumer(
        InvoiceDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<SearchReindexRequestedConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<SearchReindexRequestedCommand> context)
    {
        if (!ShouldHandle(context.Message.Payload.SourceService))
        {
            return;
        }

        var count = 0;
        var occurredAtUtc = DateTimeOffset.UtcNow;

        await foreach (var invoice in _context.Invoices
            .AsNoTracking()
            .Include(item => item.Lines)
            .Where(item => !item.IsDeleted)
            .AsAsyncEnumerable()
            .WithCancellation(context.CancellationToken))
        {
            await _publishEndpoint.Publish(
                InvoiceSearchDocumentMapper.ToUpsertEvent(invoice, occurredAtUtc),
                context.CancellationToken);
            count++;
        }

        _logger.LogInformation("Republished {Count} invoice search documents", count);
    }

    private static bool ShouldHandle(string? sourceService)
    {
        return string.IsNullOrWhiteSpace(sourceService) ||
            string.Equals(sourceService, SourceService, StringComparison.OrdinalIgnoreCase);
    }
}
