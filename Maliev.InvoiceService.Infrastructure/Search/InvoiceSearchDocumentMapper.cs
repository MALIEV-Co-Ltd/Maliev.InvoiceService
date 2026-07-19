using System.Globalization;
using Maliev.InvoiceService.Application.Authorization;
using Maliev.InvoiceService.Domain.Entities;
using Maliev.MessagingContracts.Contracts.Search;
using Maliev.MessagingContracts.Contracts.Shared;

namespace Maliev.InvoiceService.Infrastructure.Search;

/// <summary>
/// Maps invoice records to centralized global search documents.
/// </summary>
public static class InvoiceSearchDocumentMapper
{
    private const string SourceService = "InvoiceService";
    private const string ResourceType = "invoice";

    /// <summary>
    /// Creates a search upsert event for an invoice.
    /// </summary>
    /// <param name="invoice">Invoice to index.</param>
    /// <param name="occurredAtUtc">Timestamp for the source change.</param>
    /// <returns>A centralized search upsert event.</returns>
    public static SearchDocumentUpsertedEvent ToUpsertEvent(Invoice invoice, DateTimeOffset occurredAtUtc)
    {
        var title = string.IsNullOrWhiteSpace(invoice.InvoiceNumber)
            ? $"Draft invoice {invoice.Id}"
            : invoice.InvoiceNumber;

        var lineKeywords = invoice.Lines
            .SelectMany(line => CompactKeywords(line.ItemCode, line.Description))
            .ToArray();

        var summary = string.Join(" ",
            invoice.DocumentType.ToString(),
            invoice.Currency,
            invoice.GrandTotal.ToString("N2", CultureInfo.InvariantCulture),
            invoice.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            invoice.QuotationReference,
            invoice.PoNumber)
            .Trim();

        var keywords = CompactKeywords(
                invoice.Id.ToString(),
                invoice.InvoiceNumber,
                invoice.CustomerId.ToString(),
                invoice.CustomerName,
                invoice.CustomerTaxId,
                invoice.PoNumber,
                invoice.QuotationReference,
                invoice.Currency,
                invoice.Status,
                invoice.DocumentType.ToString())
            .Concat(lineKeywords)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new SearchDocumentUpsertedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(SearchDocumentUpsertedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: SourceService,
            ConsumedBy: ["SearchService"],
            CorrelationId: invoice.Id,
            CausationId: null,
            OccurredAtUtc: occurredAtUtc,
            IsPublic: false,
            Payload: new SearchDocumentUpsertedEventPayload(
                SourceService: SourceService,
                ResourceType: ResourceType,
                ResourceId: invoice.Id.ToString(),
                Title: title,
                Subtitle: invoice.CustomerName,
                Summary: string.IsNullOrWhiteSpace(summary) ? null : summary,
                Keywords: keywords,
                Status: invoice.Status,
                RequiredPermission: InvoicePermissions.InvoiceRead,
                OccurredAtUtc: occurredAtUtc));
    }

    /// <summary>
    /// Creates a search delete event for an invoice.
    /// </summary>
    /// <param name="invoiceId">Invoice identifier.</param>
    /// <param name="occurredAtUtc">Timestamp for the source change.</param>
    /// <returns>A centralized search delete event.</returns>
    public static SearchDocumentDeletedEvent ToDeletedEvent(Guid invoiceId, DateTimeOffset occurredAtUtc)
    {
        return new SearchDocumentDeletedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(SearchDocumentDeletedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: SourceService,
            ConsumedBy: ["SearchService"],
            CorrelationId: invoiceId,
            CausationId: null,
            OccurredAtUtc: occurredAtUtc,
            IsPublic: false,
            Payload: new SearchDocumentDeletedEventPayload(
                SourceService: SourceService,
                ResourceType: ResourceType,
                ResourceId: invoiceId.ToString(),
                OccurredAtUtc: occurredAtUtc));
    }

    private static IReadOnlyList<string> CompactKeywords(params string?[] values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
