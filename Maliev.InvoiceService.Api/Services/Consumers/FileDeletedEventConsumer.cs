using Maliev.InvoiceService.Data.Data;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Uploads;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.InvoiceService.Api.Services.Consumers;

/// <summary>
/// Consumes FileDeletedEvent to clean up local invoice file references.
/// </summary>
public partial class FileDeletedEventConsumer : IConsumer<FileDeletedEvent>
{
    private readonly InvoiceDbContext _dbContext;
    private readonly ILogger<FileDeletedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDeletedEventConsumer"/> class.
    /// </summary>
    public FileDeletedEventConsumer(InvoiceDbContext dbContext, ILogger<FileDeletedEventConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Consumes the FileDeletedEvent.
    /// </summary>
    public async Task Consume(ConsumeContext<FileDeletedEvent> context)
    {
        var @event = context.Message;
        var payload = @event.Payload;

        if (payload.ServiceId != "invoice-service")
        {
            // Not for this service
            return;
        }

        Log.ConsumingFileDeletedEvent(_logger, payload.FileId, payload.StoragePath);

        // Find file references associated with this file
        // We check both FileUrl (which might contain the path) and potentially other fields
        var references = await _dbContext.FileReferences
            .Where(f => f.FileUrl.Contains(payload.FileId.ToString()) || f.FileUrl.Contains(payload.StoragePath))
            .ToListAsync(context.CancellationToken);

        if (references.Count > 0)
        {
            _dbContext.FileReferences.RemoveRange(references);
            await _dbContext.SaveChangesAsync(context.CancellationToken);
            Log.FileReferencesRemoved(_logger, references.Count);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Consuming FileDeletedEvent for FileId: {FileId}, StoragePath: {StoragePath}")]
        public static partial void ConsumingFileDeletedEvent(ILogger logger, string fileId, string storagePath);

        [LoggerMessage(Level = LogLevel.Information, Message = "Removed {Count} invoice file references")]
        public static partial void FileReferencesRemoved(ILogger logger, int count);
    }
}
