# Feature Specification: Invoice Management Service

**Feature Branch**: `001-invoice-service`
**Created**: 2025-11-11
**Status**: Draft
**Input**: User description: "Create an Invoice WebAPI service that manages all data aspects of invoices issued by the company, serving as the single authoritative source of truth for invoice records and related financial data."

## Clarifications

### Session 2025-11-11

- Q: How should the system guarantee invoice number uniqueness under high concurrency? → A: Database sequence or identity column with guaranteed atomic increment
- Q: What timeout and retry strategy should be applied for external service calls? → A: Medium timeout (5 seconds), 3 retries with exponential backoff, circuit breaker pattern
- Q: How long must audit logs be retained? → A: Retain for 7 years
- Q: What level of access control granularity should the system implement? → A: Role-based with operation-level permissions (e.g., create, edit, finalize, cancel, view audit logs)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create Invoice from Quotation (Priority: P1)

A financial administrator receives an approved quotation and needs to generate an invoice to bill the customer. They access the system, reference the quotation number, review the pre-populated invoice data (customer details, line items, totals), make any necessary adjustments (such as changing currency or customer information), and finalize the invoice. The system assigns an invoice number, locks the record, and makes it available for PDF generation and delivery.

**Why this priority**: This is the primary workflow for invoice creation in a quotation-driven business. Without this capability, the service cannot fulfill its core purpose.

**Independent Test**: Can be fully tested by creating a quotation in the system, then generating an invoice from it, and verifying all fields are correctly populated and the invoice is finalized with an immutable record.

**Acceptance Scenarios**:

1. **Given** an approved quotation exists in the system, **When** the user creates an invoice from that quotation, **Then** the invoice is pre-populated with customer details, line items, currency, and totals from the quotation
2. **Given** the user is reviewing a draft invoice created from a quotation, **When** the user overrides the customer name or billing address, **Then** the system saves the custom values and does not revert to quotation data
3. **Given** the quotation is in USD but the invoice needs to be in THB, **When** the user changes the currency during invoice creation, **Then** the system queries the Currency Service for the exchange rate and stores it with the invoice
4. **Given** a draft invoice with all mandatory fields completed, **When** the user finalizes the invoice, **Then** the system assigns a unique invoice number, locks the record as immutable, and logs the finalization event with timestamp and user identity
5. **Given** a finalized invoice, **When** the user attempts to edit any field, **Then** the system rejects the change and informs the user that modifications require creating a revision or credit note

---

### User Story 2 - Create Invoice Manually (Priority: P1)

A financial administrator needs to create an invoice without a quotation reference—for example, for recurring services, ad-hoc charges, or corrections. They manually enter all required information: customer details, line items (code, description, quantity, price, tax category), payment terms, PO number, and withholding tax details. The system validates all mandatory fields, computes totals and taxes, and allows the user to finalize the invoice.

**Why this priority**: Many invoices do not originate from quotations. This is a critical alternate path that must work independently to support all invoice creation scenarios.

**Independent Test**: Can be fully tested by creating an invoice from scratch without any quotation reference, entering all fields manually, and verifying validation, calculation, and finalization work correctly.

**Acceptance Scenarios**:

1. **Given** the user starts creating a new invoice without a quotation reference, **When** they send a request to create/update the draft with customer details and line items, **Then** the system persists the draft and accepts further update requests
2. **Given** a draft invoice with incomplete mandatory fields, **When** the user attempts to finalize, **Then** the system displays validation errors identifying missing fields (customer tax ID, line item descriptions, payment terms, etc.)
3. **Given** the user enters line items with quantities, unit prices, and tax categories, **When** the system calculates totals, **Then** line-level subtotals, tax amounts, and invoice totals are computed deterministically and stored explicitly
4. **Given** withholding tax is applicable, **When** the user specifies the withholding tax type and percentage, **Then** the system calculates the withholding amount and includes it in the invoice total calculation
5. **Given** all mandatory fields are completed and totals validated, **When** the user finalizes the invoice, **Then** the system assigns an invoice number, locks the record, and creates an audit log entry

---

### User Story 3 - Split Invoice into Multiple Child Invoices (Priority: P2)

A financial administrator has a large project or quotation that needs to be billed in stages—such as 40% upfront, 30% midway, and 30% on completion. They select the parent invoice (or quotation reference) and specify the split criteria: percentages, fixed amounts, or custom rules. The system creates child invoices that reference the parent, each with its proportional share of line items and taxes. The system enforces reconciliation: the sum of all child invoice totals must equal the parent total or configured retained value.

**Why this priority**: Splitting invoices is critical for multi-stage billing but can be developed and tested after core invoice creation is operational. It builds on the finalized invoice functionality.

**Independent Test**: Can be fully tested by creating a finalized invoice, splitting it into two or more child invoices using percentage or fixed-amount rules, and verifying that totals reconcile and each child references the parent correctly.

**Acceptance Scenarios**:

1. **Given** a finalized parent invoice with a total of 100,000 THB, **When** the user splits it into two child invoices at 40% and 60%, **Then** the system creates two child invoices with totals of 40,000 and 60,000 THB, each referencing the parent invoice
2. **Given** the parent invoice has five line items, **When** the user splits by percentage, **Then** each child invoice contains proportional quantities and amounts for each line item, maintaining the same unit prices and tax rates
3. **Given** a parent invoice in USD and child invoices are also in USD, **When** the split is created, **Then** all child invoices use the same currency and exchange rate (if any) as the parent
4. **Given** the parent invoice used a specific exchange rate from the Currency Service, **When** child invoices are generated, **Then** the same exchange rate is stored in each child for audit consistency
5. **Given** three child invoices created from a parent, **When** the system validates reconciliation, **Then** the sum of child invoice totals equals the parent total, or an error is raised if they do not match

---

### User Story 4 - Search and Retrieve Invoices (Priority: P2)

A user needs to find a specific invoice or a set of invoices for reporting, auditing, or customer inquiries. They access the search interface and apply filters such as customer name, quotation reference, PO number, invoice number, status (draft, finalized, cancelled), currency, issue date range, or due date. The system returns matching invoices efficiently with pagination, allowing the user to view details, export results, or perform bulk operations.

**Why this priority**: Search and retrieval are essential for operational use but depend on having invoices already created. This is a supporting feature that enhances usability once core creation workflows are in place.

**Independent Test**: Can be fully tested by creating several invoices with varying attributes, then performing searches with different filter combinations and verifying that results are accurate, complete, and performant.

**Acceptance Scenarios**:

1. **Given** multiple invoices exist in the system, **When** the user searches by customer name, **Then** the system returns all invoices associated with that customer, ordered by issue date descending
2. **Given** invoices exist with different statuses, **When** the user filters by status "finalized", **Then** only finalized invoices are returned
3. **Given** invoices issued in multiple currencies, **When** the user filters by currency "USD", **Then** only USD invoices are returned
4. **Given** a large result set, **When** the user requests the first page of 50 results, **Then** the system returns 50 invoices with pagination metadata (total count, current page, next page link)
5. **Given** the user wants to export search results, **When** they request a bulk export, **Then** the system provides all matching invoices in a structured format (e.g., CSV, JSON) suitable for financial reporting or BI systems

---

### User Story 5 - Audit Trail and Immutability Enforcement (Priority: P2)

A financial administrator or auditor needs to review the complete history of an invoice—who created it, when it was finalized, any attempts to modify it, and any related actions such as cancellations or revisions. They access the audit log for a specific invoice and see a timestamped, actor-identified record of all events. The system prevents any modification to finalized invoices except through explicit revision or linked document creation (credit notes, amendments), ensuring data integrity for regulatory compliance.

**Why this priority**: Audit trails are legally required for financial systems, but they can be tested once core invoice operations are functional. This feature supports compliance and trust.

**Independent Test**: Can be fully tested by creating, editing, and finalizing invoices, then attempting unauthorized modifications and reviewing the audit log to verify all actions are captured with correct timestamps and actor identities.

**Acceptance Scenarios**:

1. **Given** a draft invoice is created, **When** the user views the audit log, **Then** the log shows the creation timestamp, user identity, and initial field values
2. **Given** a draft invoice is edited multiple times, **When** the user finalizes it, **Then** the audit log captures each edit with changed fields, timestamps, and the finalization event
3. **Given** a finalized invoice, **When** a user attempts to modify any field directly, **Then** the system rejects the change and logs the attempted modification with user identity and reason
4. **Given** a finalized invoice needs correction, **When** the user creates a credit note or amendment, **Then** the system creates a new linked document and logs the creation event referencing the original invoice
5. **Given** an auditor reviews an invoice, **When** they access the audit log, **Then** all events (creation, edits, finalization, cancellation, linked documents) are visible with full traceability

---

### User Story 6 - Invoice Cancellation (Priority: P3)

A financial administrator discovers that a finalized invoice was issued in error and needs to cancel it. They access the invoice, initiate a cancellation action, provide a reason, and confirm. The system marks the invoice as cancelled, logs the cancellation event with timestamp and actor, and ensures the cancelled invoice does not appear in receivables or financial reports unless specifically filtered.

**Why this priority**: Cancellation is an important operational feature but is used less frequently than creation and search. It can be developed after core workflows are stable.

**Independent Test**: Can be fully tested by finalizing an invoice, then cancelling it with a reason, and verifying the invoice status changes, the audit log is updated, and the invoice is excluded from active financial queries.

**Acceptance Scenarios**:

1. **Given** a finalized invoice, **When** the user initiates cancellation and provides a reason, **Then** the system marks the invoice as cancelled and logs the event
2. **Given** a cancelled invoice, **When** the user searches for active invoices, **Then** the cancelled invoice does not appear unless the user explicitly includes cancelled invoices in the filter
3. **Given** a cancelled invoice, **When** an external service (e.g., PDF Service) requests invoice data, **Then** the response includes the cancelled status to prevent accidental processing
4. **Given** a cancelled invoice in the audit log, **When** an auditor reviews it, **Then** the cancellation reason, timestamp, and actor identity are clearly visible

---

### User Story 7 - Currency Conversion and Rate Storage (Priority: P3)

A financial administrator creates an invoice in a currency different from the quotation or base currency. The system queries the Currency Service for the current or historical exchange rate at the time of invoice creation, applies the conversion if needed for reporting purposes, and stores the exact rate used. This ensures that even if exchange rates change later, the invoice totals remain reproducible and auditable.

**Why this priority**: Currency conversion is a supporting feature that enhances the primary workflows. It can be implemented after core invoice creation and depends on integration with the Currency Service.

**Independent Test**: Can be fully tested by creating invoices in multiple currencies, verifying the exchange rate is fetched and stored, and confirming that totals are calculated consistently using the stored rate.

**Acceptance Scenarios**:

1. **Given** a quotation in USD and the user creates an invoice in THB, **When** the invoice is saved, **Then** the system queries the Currency Service for the USD-to-THB rate and stores it with the invoice
2. **Given** an invoice with a stored exchange rate, **When** the exchange rate in the Currency Service changes, **Then** the invoice totals remain unchanged and continue to use the originally stored rate
3. **Given** an invoice created in a foreign currency, **When** the financial team generates reports, **Then** they can view both the original currency amount and the converted amount using the stored rate
4. **Given** the Currency Service is temporarily unavailable, **When** the user creates an invoice requiring conversion, **Then** the system either uses a fallback rate or prompts the user to enter a manual rate, logging the source

---

### User Story 8 - Allocate Payments to Invoices (Priority: P3)

A financial administrator receives notification that a customer payment has been successfully processed through the Payment Service (via Stripe, bank transfer, etc.). They need to allocate this payment to one or more outstanding invoices. They access the Invoice Service, reference the payment ID from the Payment Service, specify the allocation amount for each invoice, and mark invoices as partially or fully paid. The system validates the payment exists and is successful, creates allocation records, updates invoice status, recalculates outstanding balance, publishes events for the Financial Service, and logs all actions in the audit trail.

**Architecture Context**: This service does not process payments or interact with payment gateways. Payment processing is owned by the Payment Service. This service only tracks allocation of payment IDs to invoices. The Payment Service publishes events (PaymentSucceededEvent) via RabbitMQ that can trigger automatic allocation workflows.

**Why this priority**: Payment allocation is important for financial operations but is downstream from invoice creation and depends on integration with Payment Service. It can be developed after core invoice features are functional.

**Independent Test**: Can be fully tested by mocking Payment Service API and RabbitMQ events, creating finalized invoices, allocating payment references, and verifying that invoice status updates, balances are recalculated correctly, events are published, and audit history is complete.

**Acceptance Scenarios**:

1. **Given** the Payment Service has processed a successful payment of 50,000 THB (payment ID: pay-123), **When** the user allocates 20,000 THB from payment pay-123 to Invoice-001, **Then** the system validates payment exists via Payment Service API, creates allocation record, updates Invoice-001 status to "PartiallyPaid", outstanding balance is 30,000 THB, and publishes PaymentAllocatedEvent
2. **Given** Invoice-001 has outstanding balance of 30,000 THB, **When** the user allocates an additional payment of 30,000 THB, **Then** the invoice status changes to "FullyPaid", outstanding balance is 0, paid_at timestamp is set, and PaymentAllocatedEvent is published with status "FullyPaid"
3. **Given** a payment allocation is created, **When** the user views the audit log, **Then** the PaymentLinked event is recorded with payment ID, allocated amount, invoice status change, and actor identity
4. **Given** the Payment Service publishes PaymentSucceededEvent with metadata containing invoice IDs, **When** the Invoice Service receives the event via RabbitMQ, **Then** it automatically allocates the payment to the specified invoices and publishes corresponding PaymentAllocatedEvent for each allocation
5. **Given** multiple invoices from the same customer, **When** the user allocates a single payment across three invoices with specified amounts, **Then** the system creates three allocation records, updates each invoice's status independently, and publishes three PaymentAllocatedEvent messages

---

### User Story 9 - Provide Data for PDF Generation and File Storage (Priority: P3)

The PDF Service needs to generate a printable invoice document. It queries the Invoice Service for invoice details (invoice number, customer name, billing address, line items, totals, tax details, payment terms, PO number). The Invoice Service returns all required data in a structured format. Once the PDF is generated, the Upload Service stores the file and registers the file reference back with the Invoice Service, linking the invoice record to its PDF URL or file ID.

**Why this priority**: This is an integration point rather than a user-facing feature. It depends on the core invoice data being correct and complete. It enables downstream services but can be developed after the primary invoice workflows are stable.

**Independent Test**: Can be fully tested by mocking the PDF Service and Upload Service, querying the Invoice Service for invoice data, verifying the response format, and simulating file reference registration.

**Acceptance Scenarios**:

1. **Given** a finalized invoice, **When** the PDF Service requests invoice data via API, **Then** the Invoice Service returns all fields required for rendering (invoice number, customer details, line items, totals, taxes, payment terms)
2. **Given** the PDF Service has generated a PDF, **When** the Upload Service registers the file URL with the Invoice Service, **Then** the invoice record is updated with the PDF file reference
3. **Given** an invoice with a registered PDF file, **When** a user views the invoice details, **Then** the system provides a link or reference to the PDF document
4. **Given** the PDF Service requests data for a cancelled invoice, **When** the Invoice Service responds, **Then** the response includes the cancelled status to prevent accidental PDF generation or delivery

---

### Edge Cases

- What happens when the Currency Service is unavailable at invoice creation time? (System retries up to 3 times with exponential backoff per FR-055; if still unavailable, uses fallback default rate, prompts for manual entry, or defers finalization until the service is available per FR-024)
- How does the system handle concurrent updates to a draft invoice from multiple client requests? (Implement optimistic locking with version fields or last-write-wins with conflict detection)
- What happens if a user attempts to split an already-split child invoice? (System should either prevent nested splits or clearly track the hierarchy)
- How does the system validate that all child invoices have been issued before marking a parent as fully reconciled? (Enforce business rules and validation checks)
- What happens if a line item has zero quantity or zero price? (System should validate and either reject or flag for review)
- How does the system handle rounding differences when splitting invoices? (Apply rounding adjustments to the last child invoice to ensure totals reconcile exactly)
- What happens when a finalized invoice is linked to a payment but then needs to be cancelled? (System should validate payment status and prevent cancellation if payments are linked, or reverse payments first)
- How does the system handle invoices with multiple tax categories on different line items? (Calculate taxes per line and aggregate correctly, storing each category separately)
- What happens if the user tries to finalize an invoice with missing mandatory fields? (System must reject finalization and provide clear validation errors)
- How does the system handle invoices issued before the compliance validation rules were updated? (Grandfather old invoices or apply validation only to new invoices, documenting the policy)

---

## Requirements *(mandatory)*

### Functional Requirements

#### Invoice Creation and Management

- **FR-001**: System MUST allow users to create invoices from existing quotation references, pre-populating all invoice fields with quotation data
- **FR-002**: System MUST allow users to create invoices manually without quotation references, entering all fields directly
- **FR-003**: System MUST allow users to override pre-populated fields (customer information, line items, currency) when creating invoices from quotations
- **FR-004**: System MUST accept and persist invoices in "draft" status, allowing unlimited updates via API calls until finalized
- **FR-004a**: System MUST handle concurrent update requests to draft invoices using optimistic locking (version fields) or last-write-wins strategy with conflict detection
- **FR-005**: System MUST assign a unique, sequential invoice number upon finalization using database sequence or identity column for atomic, guaranteed-unique generation
- **FR-006**: System MUST mark finalized invoices as immutable, preventing any direct field modifications
- **FR-007**: System MUST allow users to cancel finalized invoices with a mandatory reason, logging the cancellation event
- **FR-008**: System MUST support creating revisions or linked documents (credit notes, amendments) for correcting finalized invoices

#### Invoice Data Model

- **FR-009**: System MUST store customer details including legal name, tax ID, billing address, and contact information
- **FR-010**: System MUST store line items with item code, description, quantity, unit price, discounts, tax category, and computed subtotal
- **FR-011**: System MUST store withholding tax details including type, percentage, and calculated amount
- **FR-012**: System MUST store payment terms including due date, allowed payment methods, and late fee rules
- **FR-013**: System MUST store PO numbers, quotation references, and custom metadata fields
- **FR-014**: System MUST compute and store line-level taxes, subtotals, tax-inclusive totals, withholding amounts, and rounding adjustments explicitly
- **FR-015**: System MUST store invoice currency and exchange rate (if applicable) at the time of creation

#### Invoice Splitting and Reconciliation

- **FR-016**: System MUST allow users to split a parent invoice into multiple child invoices using percentage, fixed amount, or custom rules
- **FR-017**: System MUST create child invoices that reference the parent invoice ID
- **FR-018**: System MUST distribute line items proportionally across child invoices, maintaining unit prices and tax rates
- **FR-019**: System MUST enforce reconciliation: the sum of all child invoice totals MUST equal the parent invoice total or configured retained value
- **FR-020**: System MUST preserve currency consistency across parent and child invoices or explicitly store separate exchange rates if currencies differ

#### Currency Conversion and Exchange Rates

- **FR-021**: System MUST query the Currency Service for exchange rates when an invoice is created in a currency different from the quotation or base currency, using 5-second timeout with 3 retries (exponential backoff) and circuit breaker pattern
- **FR-022**: System MUST store the exact exchange rate used at invoice creation time for audit and reproducibility
- **FR-023**: System MUST continue to use the stored exchange rate for all calculations related to that invoice, even if current rates change
- **FR-024**: System MUST handle Currency Service unavailability by using a fallback rate, prompting manual entry, or deferring finalization

#### Validation and Compliance

- **FR-025**: System MUST validate that all mandatory fields are present before allowing invoice finalization
- **FR-026**: System MUST validate customer tax ID format and presence according to regulatory requirements
- **FR-027**: System MUST validate withholding tax and VAT calculations before finalization
- **FR-028**: System MUST validate that line items have positive quantities and non-zero prices (or explicitly flag exceptions)
- **FR-029**: System MUST ensure computed totals (subtotals, taxes, grand totals) are deterministic and reproducible

#### Audit Trail and Immutability

- **FR-030**: System MUST log all invoice creation, edit, finalization, cancellation, and revision events with timestamp and actor identity, capturing changed field values in the audit log for each edit action
- **FR-032**: System MUST log reasons for cancellations, revisions, or amendments
- **FR-033**: System MUST prevent deletion of finalized or cancelled invoices
- **FR-034**: System MUST provide read-only access to audit logs for authorized users and auditors
- **FR-056**: System MUST retain audit logs for a minimum of 7 years to meet financial and tax compliance requirements

#### Authorization and Access Control

- **FR-057**: System MUST implement role-based access control (RBAC) with operation-level permissions including: create invoices, edit draft invoices, finalize invoices, cancel invoices, view audit logs, record payments, split invoices, and export data
- **FR-058**: System MUST enforce permission checks before allowing any operation, rejecting unauthorized actions with appropriate error messages
- **FR-059**: System MUST support roles such as Invoice Creator, Invoice Approver, Financial Administrator, and Auditor with distinct permission sets

#### Search, Retrieval, and Filtering

- **FR-035**: System MUST allow users to search invoices by customer name, tax ID, quotation reference, PO number, invoice number, status, currency, issue date range, and due date range
- **FR-036**: System MUST return search results with pagination (configurable page size)
- **FR-037**: System MUST support sorting search results by issue date, due date, invoice number, or total amount
- **FR-038**: System MUST provide bulk export functionality for search results in structured formats (CSV, JSON, Excel)
- **FR-039**: System MUST exclude cancelled invoices from default search results unless explicitly filtered

#### Payment Linking

- **FR-040**: System MUST allow users to record payments against invoices, storing amount, date, and payment method
- **FR-041**: System MUST update invoice status to "partially paid" or "fully paid" based on payment amounts
- **FR-042**: System MUST calculate and display outstanding balance for each invoice
- **FR-043**: System MUST log payment events in the audit trail with actor identity and timestamp
- **FR-044**: System MUST support allocating a single payment across multiple invoices

#### Integration with External Services

- **FR-045**: System MUST provide a structured API endpoint for the PDF Service to retrieve invoice data for rendering
- **FR-046**: System MUST provide a structured API endpoint for the Upload Service to register PDF file references or URLs with invoice records
- **FR-047**: System MUST include invoice status (draft, finalized, cancelled) in all API responses to prevent processing of invalid invoices
- **FR-048**: System MUST provide metadata endpoints for analytics and BI systems, exposing invoice counts by status, total invoiced amounts, withholding tax totals, receivable aging, and payment delays
- **FR-055**: System MUST apply 5-second timeout with 3 retries (exponential backoff: 1s, 2s, 4s) and circuit breaker pattern to all outbound calls to external services (Currency, Quotation, PDF, Upload)

#### Caching and Performance

- **FR-049**: System MUST cache frequently accessed invoice data (by invoice number, customer, quotation reference) with long TTLs
- **FR-050**: System MUST invalidate cached data when invoices are created, updated, finalized, or cancelled
- **FR-051**: System MUST respond to invoice lookup requests in under 200 milliseconds for 95% of cached queries
- **FR-051a**: System MUST respond to invoice lookup requests (cache miss, database retrieval) in under 500 milliseconds for 95% of queries
- **FR-052**: System MUST handle at least 500 concurrent read requests without performance degradation (defined as p95 latency increase >50% or error rate >1%)

#### Event Notifications (Optional)

- **FR-053**: System MAY emit lightweight notifications (e.g., invoice.finalized, invoice.cancelled) for internal synchronization with other services
- **FR-054**: Notifications MUST include invoice ID, status, and timestamp, but MUST NOT include full invoice payloads

---

### Key Entities

- **Invoice**: Represents a billing document issued to a customer. Contains invoice number, issue date, due date, status (draft, finalized, cancelled), customer details, line items, totals (subtotal, tax, withholding, grand total), currency, exchange rate, quotation reference, PO number, payment terms, and custom metadata. Related to zero or one parent invoice (for splits) and zero or more child invoices.

- **InvoiceLine**: Represents a single line item on an invoice. Contains item code, description, quantity, unit price, discount percentage, tax category, line subtotal (computed), and tax amount (computed). Many-to-one relationship with Invoice.

- **Customer**: Represents the billing party. Contains legal name, tax ID, billing address, shipping address (optional), contact person, email, phone. Referenced by Invoice but may be stored redundantly within each invoice for immutability.

- **WithholdingTax**: Represents withholding tax applied to an invoice. Contains type (e.g., "WHT 3%", "WHT 5%"), percentage, calculated amount. One-to-one or many-to-one relationship with Invoice.

- **PaymentTerm**: Represents payment conditions. Contains due date, allowed payment methods (e.g., bank transfer, check, credit card), late fee percentage, early payment discount. Embedded within Invoice.

- **Payment**: Represents a payment received against an invoice. Contains payment amount, payment date, payment method, reference number, and linked invoice ID(s). Many-to-one or many-to-many relationship with Invoice (for bulk payments).

- **ExchangeRate**: Represents the currency conversion rate used at invoice creation. Contains source currency, target currency, rate, and timestamp. Embedded within Invoice or referenced by ID.

- **AuditLog**: Represents a single event in the invoice lifecycle. Contains invoice ID, event type (created, edited, finalized, cancelled, payment linked), timestamp, actor identity (user ID or system), changed fields (for edits), and reason (for cancellations/revisions).

- **QuotationReference**: Represents a link to a quotation system. Contains quotation ID, quotation number, quotation date. Referenced by Invoice.

- **UserRole**: Represents an authorization role with specific operation permissions. Contains role name (e.g., Invoice Creator, Invoice Approver, Financial Administrator, Auditor) and permitted operations (create, edit, finalize, cancel, view audit logs, record payments, split invoices, export data). Used for RBAC enforcement.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create a new invoice from a quotation in under 3 minutes, including review and finalization
- **SC-002**: Users can create a manual invoice with 10 line items in under 5 minutes
- **SC-003**: System completes invoice finalization (validation, number assignment, locking) in under 2 seconds
- **SC-004**: Invoice search results are returned in under 1 second for queries with up to 10,000 matching records (paginated)
- **SC-005**: System handles at least 500 concurrent invoice lookup requests without response time exceeding 500 milliseconds
- **SC-006**: 95% of invoice lookups by invoice number are served from cache in under 100 milliseconds
- **SC-007**: Invoice splitting and reconciliation completes in under 5 seconds for invoices with up to 50 line items
- **SC-008**: Audit log retrieval for a single invoice completes in under 1 second regardless of event count
- **SC-009**: Bulk export of up to 1,000 invoices completes in under 30 seconds
- **SC-010**: 100% of finalized invoices remain immutable and any modification attempts are logged and rejected
- **SC-011**: 100% of mandatory tax and compliance fields are validated before finalization, with zero invalid invoices finalized
- **SC-012**: System maintains 99.9% uptime during business hours, measured monthly
- **SC-013**: Zero data loss events: all invoice data, audit logs, and payment records are persisted with database-level consistency; audit logs retained for minimum 7 years
- **SC-014**: External services (PDF Service, Upload Service) successfully retrieve invoice data with 99.5% success rate (excluding client errors)
- **SC-015**: Financial reports generated from invoice data match manual audit calculations with 100% accuracy
- **SC-016**: User satisfaction: 90% of users rate invoice creation workflows as "easy" or "very easy" in post-deployment surveys
- **SC-017**: Support tickets related to invoice data errors or inconsistencies are reduced by 80% compared to legacy systems

---

## Assumptions and Constraints

### Assumptions

- This is a backend API service; frontend client applications are responsible for UI/UX concerns including form validation timing, auto-save behavior, and user interaction flows
- The Currency Service is available and provides reliable exchange rates; fallback rates or manual entry are acceptable for rare downtime scenarios
- Quotation references are valid and exist in a quotation system accessible to users
- Users have appropriate permissions and authentication managed by an external authentication service; the Invoice Service implements role-based access control (RBAC) with operation-level permissions for authorization
- The PDF Service and Upload Service will be developed or integrated separately and will consume structured API responses from the Invoice Service
- Invoice numbers follow a sequential, organization-defined format (e.g., "INV-2025-00001")
- Tax regulations and compliance requirements are based on Thai tax law; international variations are out of scope for the initial version
- Payment processing and reconciliation are manual processes; automated payment gateway integration is out of scope initially

### Constraints

- The service must not perform PDF rendering or file storage directly; these are delegated to separate services
- Finalized invoices are immutable; corrections require creating linked documents (credit notes, amendments)
- The system must support multi-currency invoicing but does not need to support real-time currency fluctuation tracking (rates are fixed at creation time)
- Event publishing is optional and lightweight; no complex event-driven workflows are required
- The service must operate in a Kubernetes environment with standard health checks, metrics, and logging
- The service must comply with MALIEV Co. Ltd. microservices architecture standards, including PostgreSQL database, Serilog logging, and JWT authentication

---

## Out of Scope

- PDF generation and rendering of invoice documents
- File storage and management of invoice PDFs or attachments
- Real-time payment gateway integration or automated payment processing
- Customer Relationship Management (CRM) functionality
- Quotation creation or management (handled by a separate Quotation Service)
- Inventory management or stock deduction based on invoiced items
- Multi-tenant or white-label invoice customization (single organization only)
- Advanced tax calculation engines for international jurisdictions (focus on Thai tax compliance)
- Workflow automation or approval chains for invoice finalization (manual user action required)

---

## Dependencies

- **Currency Service**: Provides exchange rates for multi-currency invoicing
- **PDF Service**: Consumes invoice data to generate printable PDF documents
- **Upload Service**: Stores generated PDFs and registers file references back to the Invoice Service
- **Authentication Service**: Provides user identity and JWT tokens for access control
- **Quotation Service**: Provides quotation data for invoice creation (indirect dependency via API)
- **PostgreSQL Database**: Provides persistent storage for invoice data, audit logs, and payments
- **Kubernetes Cluster**: Provides runtime environment, health checks, and service discovery

---

## Technical Notes (Non-Normative)

These notes are informational and do not constitute requirements. Implementation decisions will be made during the planning phase.

- Consider using database triggers or application-level audit logging frameworks for comprehensive audit trails; implement archival strategy for 7-year retention (see FR-056)
- Caching strategy should balance TTL duration with data freshness requirements; consider event-driven cache invalidation
- Invoice number generation uses database sequence/identity for atomic operation (see FR-005)
- Rounding adjustments for currency conversions should follow standard accounting practices (e.g., round to 2 decimal places, adjust last line item)
- Consider using optimistic locking (e.g., version fields) to prevent concurrent edit conflicts on draft invoices
- API responses should follow RESTful conventions and include HATEOAS links where appropriate
- Metrics endpoints should expose Prometheus-compatible metrics for observability
- Consider rate limiting on public-facing API endpoints to prevent abuse

---

## Revision History

| Version | Date       | Author      | Changes                  |
|---------|------------|-------------|--------------------------|
| 1.0     | 2025-11-11 | Claude Code | Initial specification    |
