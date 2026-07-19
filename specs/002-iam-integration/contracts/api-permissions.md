# API Permission Contracts

This document specifies the required permissions for each API endpoint in the InvoiceService.

## InvoicesController

| Endpoint | Method | Required Permission |
|----------|--------|----------------------|
| `/invoices` | POST | `invoice.invoices.create` |
| `/invoices` | GET | `invoice.invoices.read` |
| `/invoices/{id}` | GET | `invoice.invoices.read` |
| `/invoices/{id}` | PUT | `invoice.invoices.update` |
| `/invoices/{id}` | DELETE | `invoice.invoices.delete` |
| `/invoices/{id}/finalize` | POST | `invoice.invoices.finalize` |
| `/invoices/{id}/approve` | POST | `invoice.invoices.approve` |
| `/invoices/{id}/void` | POST | `invoice.invoices.void` |
| `/invoices/export` | POST | `invoice.invoices.export` |
| `/invoices/{id}/send` | POST | `invoice.invoices.send` |
| `/invoices/pdf/register` | POST | `invoice.files.register` |

## InvoiceSegmentsController

| Endpoint | Method | Required Permission |
|----------|--------|----------------------|
| `/segments` | POST | `invoice.segments.create` |
| `/segments/{id}` | GET | `invoice.segments.read` |
| `/segments/{id}` | PUT | `invoice.segments.update` |
| `/segments/{id}` | DELETE | `invoice.segments.delete` |

## Reports/AuditController

| Endpoint | Method | Required Permission |
|----------|--------|----------------------|
| `/reports/currency` | GET | `invoice.reports.currency` |
| `/reports/analytics` | GET | `invoice.reports.analytics` |
| `/reports/export` | POST | `invoice.reports.export` |
| `/audit` | GET | `invoice.invoices.read` (or specific audit permission) |
