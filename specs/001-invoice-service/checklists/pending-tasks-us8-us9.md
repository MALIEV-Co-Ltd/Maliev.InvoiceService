# Requirements Quality Checklist: Pending Tasks (US8 & US9)

**Purpose**: Validate requirement quality for pending implementation tasks in User Story 8 (Payment Allocation) and User Story 9 (PDF/File Reference). This checklist tests whether the requirements are complete, clear, consistent, and ready for implementation.

**Created**: 2025-11-13
**Scope**: Payment allocation requirements (8 tasks) and PDF/file reference requirements (5 tasks)
**Focus**: Balanced coverage across all quality dimensions
**Depth**: Standard review (30-50 items)

---

## Requirement Completeness

### Payment Allocation (US8)

- [X] CHK001 - Are payment validation requirements explicitly defined before allocation? [Completeness, Spec §US8-AS1]
- [X] CHK002 - Is the Payment Service API contract (`GET /payments/v1/payments/{id}`) documented with required/optional fields? [Gap, Dependency]
- [X] CHK003 - Are RabbitMQ exchange configuration requirements specified (exchange name, routing keys, queue bindings)? [Gap, Integration]
- [X] CHK004 - Are PaymentSucceededEvent schema requirements complete (all mandatory fields, field types, validation rules)? [Completeness, data-model.md]
- [X] CHK005 - Are PaymentAllocatedEvent schema requirements defined with all necessary fields for Financial Service consumption? [Gap]
- [X] CHK006 - Are requirements defined for calculating outstanding balance after allocation? [Completeness, data-model.md §Business Rules]
- [X] CHK007 - Are invoice status transition requirements complete for all allocation scenarios (PartiallyPaid, FullyPaid)? [Completeness, Spec §US8-AS2]
- [X] CHK008 - Are audit log requirements specified for payment allocation events (PaymentLinked event structure)? [Completeness, Spec §US8-AS3]
- [X] CHK009 - Are cache invalidation requirements defined when invoices receive payment allocations? [Gap, Task T174]
- [X] CHK010 - Are requirements specified for manual allocation across multiple invoices? [Completeness, Spec §US8-AS5]
- [X] CHK011 - Are auto-allocation requirements complete for PaymentSucceededEvent with invoice metadata? [Completeness, Spec §US8-AS4]

### PDF/File Reference (US9)

- [X] CHK012 - Are all PDF-required invoice fields explicitly enumerated in requirements? [Completeness, Spec §US9-AS1]
- [X] CHK013 - Are file reference registration requirements complete (field name, data type, validation)? [Gap, Task T177]
- [X] CHK014 - Are UpdateInvoiceRequest validation requirements specified? [Gap, Task T179]
- [X] CHK015 - Are requirements defined for the PATCH endpoint authentication/authorization model? [Gap, Spec §US9-AS2]
- [X] CHK016 - Are requirements specified for handling draft vs finalized invoices during PDF generation? [Gap]

---

## Requirement Clarity

### Payment Allocation (US8)

- [X] CHK017 - Is "successful payment" quantified with specific Payment Service status values? [Clarity, data-model.md §Business Rules]
- [X] CHK018 - Is "validates payment exists" defined with explicit API call requirements and response codes? [Clarity, Spec §US8-AS1]
- [X] CHK019 - Are allocation_status values ("Confirmed", "Reversed") clearly defined with state transition rules? [Clarity, data-model.md §InvoicePaymentAllocation]
- [X] CHK020 - Is the calculation formula for outstanding balance explicitly documented? [Clarity, data-model.md §Business Rules]
- [X] CHK021 - Is "publishes PaymentAllocatedEvent" quantified with specific timing (synchronous/asynchronous) and delivery guarantees? [Clarity, Spec §US8-AS1]
- [X] CHK022 - Are the criteria for transitioning to FullyPaid status precisely defined? [Clarity, Spec §US8-AS2]
- [X] CHK023 - Is the scope of "allocated_by" field clarified (user ID format, system vs user distinction)? [Clarity, data-model.md]

### PDF/File Reference (US9)

- [X] CHK024 - Is "all fields required for rendering" explicitly defined as a specific field list? [Clarity, Spec §US9-AS1]
- [X] CHK025 - Is "registers the file URL" clarified with data type (URL string, file ID, object reference)? [Clarity, Spec §US9-AS2]
- [X] CHK026 - Is the pdf_file_reference property data type and constraints specified? [Clarity, Task T177]
- [X] CHK027 - Are immutability requirements clear for PDF file references (can they be updated/deleted)? [Clarity, Gap]

---

## Requirement Consistency

### Cross-Document Alignment

- [X] CHK028 - Do tasks T164-T174 align with acceptance scenarios in Spec §US8? [Consistency]
- [X] CHK029 - Does data-model.md InvoicePaymentAllocation entity match spec.md payment allocation requirements? [Consistency]
- [X] CHK030 - Are PaymentSucceededEvent schema requirements consistent between payment-service.md and invoice service consumption expectations? [Consistency, Integration]
- [X] CHK031 - Do tasks T177-T185 align with acceptance scenarios in Spec §US9? [Consistency]
- [X] CHK032 - Are payment validation requirements consistent between tasks (T166) and data-model business rules? [Consistency]

### Internal Consistency

- [X] CHK033 - Are invoice status update requirements consistent between manual allocation (T167) and auto-allocation (T166)? [Consistency]
- [X] CHK034 - Are audit log requirements consistent across payment allocation scenarios? [Consistency, Task T173]
- [X] CHK035 - Are authorization policy requirements consistent between payment endpoints and PDF reference endpoints? [Consistency]

---

## Acceptance Criteria Quality

### Measurability

- [X] CHK036 - Can "validates payment exists via Payment Service API" be objectively verified with specific API response codes? [Measurability, Spec §US8-AS1]
- [X] CHK037 - Can "outstanding balance is 30,000 THB" be verified with a specific calculation formula? [Measurability, Spec §US8-AS1]
- [X] CHK038 - Can "PaymentAllocatedEvent is published" be verified with observable event attributes? [Measurability, Spec §US8-AS2]
- [X] CHK039 - Can "paid_at timestamp is set" be verified with specific field name and timezone requirements? [Measurability, Spec §US8-AS2]
- [X] CHK040 - Can "returns all fields required for rendering" be verified against a defined field checklist? [Measurability, Spec §US9-AS1]

### Testability

- [X] CHK041 - Are mock requirements specified for Payment Service API during testing? [Testability, Spec §US8 Independent Test]
- [X] CHK042 - Are mock requirements specified for RabbitMQ events during testing? [Testability, Spec §US8 Independent Test]
- [X] CHK043 - Are acceptance criteria defined for verifying audit history completeness? [Testability, Spec §US8 Independent Test]

---

## Scenario Coverage

### Primary Flows

- [X] CHK044 - Are requirements defined for manual payment allocation workflow (end-to-end)? [Coverage, Spec §US8-AS1]
- [X] CHK045 - Are requirements defined for auto-allocation workflow triggered by RabbitMQ events? [Coverage, Spec §US8-AS4]
- [X] CHK046 - Are requirements defined for partial payment allocation scenarios? [Coverage, Spec §US8-AS1]
- [X] CHK047 - Are requirements defined for full payment allocation scenarios? [Coverage, Spec §US8-AS2]
- [X] CHK048 - Are requirements defined for PDF data retrieval workflow? [Coverage, Spec §US9-AS1]
- [X] CHK049 - Are requirements defined for file reference registration callback workflow? [Coverage, Spec §US9-AS2]

### Alternate Flows

- [X] CHK050 - Are requirements defined for allocating a single payment across multiple invoices? [Coverage, Spec §US8-AS5]
- [X] CHK051 - Are requirements defined for sequential allocations to the same invoice? [Coverage, Spec §US8-AS2]
- [X] CHK052 - Are requirements defined for viewing invoice details with registered PDF links? [Coverage, Spec §US9-AS3]

---

## Edge Case Coverage

### Payment Allocation Error Scenarios

- [X] CHK053 - Are requirements defined when Payment Service API returns 404 (payment not found)? [Edge Case, Gap]
- [X] CHK054 - Are requirements defined when Payment Service API returns payment status other than "Succeeded"? [Edge Case, Gap]
- [X] CHK055 - Are requirements defined when allocated amount exceeds payment amount? [Edge Case, Gap]
- [X] CHK056 - Are requirements defined when allocated amount exceeds invoice outstanding balance? [Edge Case, Gap]
- [X] CHK057 - Are requirements defined when duplicate allocation is attempted (same payment_id + invoice_id)? [Edge Case, Gap]
- [X] CHK058 - Are requirements defined for handling concurrent allocations to the same invoice? [Edge Case, Gap]
- [X] CHK059 - Are requirements defined when PaymentSucceededEvent references non-existent invoice IDs? [Edge Case, Gap]
- [X] CHK060 - Are requirements defined when PaymentSucceededEvent delivery fails or is delayed? [Edge Case, Gap]

### Payment Service Integration Failures

- [X] CHK061 - Are requirements defined when Payment Service API is unavailable during allocation? [Edge Case, Gap]
- [X] CHK062 - Are requirements defined when Payment Service API times out? [Edge Case, Gap]
- [X] CHK063 - Are requirements defined for retry logic when Payment Service validation fails? [Edge Case, Gap]

### PDF/File Reference Error Scenarios

- [X] CHK064 - Are requirements defined when PDF Service requests data for a draft invoice? [Edge Case, Gap]
- [X] CHK065 - Are requirements defined when PDF Service requests data for a cancelled invoice? [Edge Case, Spec §US9-AS4]
- [X] CHK066 - Are requirements defined when Upload Service registers duplicate file references? [Edge Case, Gap]
- [X] CHK067 - Are requirements defined when file reference registration fails? [Edge Case, Gap]

---

## Non-Functional Requirements

### Performance

- [X] CHK068 - Are performance requirements specified for Payment Service API validation calls? [NFR, Gap]
- [X] CHK069 - Are performance requirements specified for RabbitMQ event processing? [NFR, Gap]
- [X] CHK070 - Are performance requirements specified for outstanding balance calculations? [NFR, Gap]

### Reliability & Resilience

- [X] CHK071 - Are idempotency requirements defined for payment allocation operations? [NFR, Gap]
- [X] CHK072 - Are idempotency requirements defined for RabbitMQ event consumers? [NFR, Gap]
- [X] CHK073 - Are transaction boundary requirements specified for allocation operations? [NFR, Task T166]
- [X] CHK074 - Are event delivery guarantee requirements specified (at-least-once, exactly-once)? [NFR, Gap]

### Security

- [X] CHK075 - Are authorization requirements specified for payment allocation endpoints? [Security, Gap]
- [X] CHK076 - Are authorization requirements specified for PDF reference registration endpoint? [Security, Spec §US9 notes "internal use only"]
- [X] CHK077 - Are actor identity capture requirements specified for audit logs? [Security, Spec §US8-AS3]

---

## Dependencies & Assumptions

### External Service Dependencies

- [X] CHK078 - Is the Payment Service API dependency documented with version/contract requirements? [Dependency, Gap]
- [X] CHK079 - Is the RabbitMQ infrastructure dependency documented with configuration requirements? [Dependency, Gap]
- [X] CHK080 - Is the PDF Service API dependency documented with contract requirements? [Dependency, Gap]
- [X] CHK081 - Is the Upload Service callback dependency documented with contract requirements? [Dependency, Gap]
- [X] CHK082 - Is the Financial Service event consumption dependency documented? [Dependency, Spec §US8 notes]

### Assumptions

- [X] CHK083 - Is the assumption that Payment Service always returns consistent payment status validated? [Assumption, Gap]
- [X] CHK084 - Is the assumption that payment IDs are globally unique validated? [Assumption, data-model.md]
- [X] CHK085 - Is the assumption that RabbitMQ guarantees event ordering validated? [Assumption, Gap]
- [X] CHK086 - Is the assumption that PDF Service can handle all invoice data formats validated? [Assumption, Gap]

---

## Ambiguities & Conflicts

### Payment Allocation Ambiguities

- [X] CHK087 - Is it clear whether allocation_status "Reversed" is implemented in this phase or deferred? [Ambiguity, data-model.md]
- [X] CHK088 - Is it clear what happens when a payment is refunded after allocation (see PaymentRefundedEvent)? [Ambiguity, Gap]
- [X] CHK089 - Is it clear whether partial allocations can be updated or only new allocations added? [Ambiguity, Gap]
- [X] CHK090 - Is the distinction between "system" and user actors in allocated_by field clearly defined? [Ambiguity, Spec §US8-AS4]

### PDF/File Reference Ambiguities

- [X] CHK091 - Is it clear whether pdf_file_reference can be updated after initial registration? [Ambiguity, Task T177]
- [X] CHK092 - Is it clear whether the PATCH endpoint requires authentication despite "internal use only" note? [Ambiguity, Spec §US9, Task T185]
- [X] CHK093 - Is it clear what data format PDF Service expects (JSON schema, XML, other)? [Ambiguity, Gap]

### Potential Conflicts

- [X] CHK094 - Do tasks reference Payment entity (T019, T024) conflict with architectural decision to remove it? [Conflict, tasks.md vs data-model.md]
- [X] CHK095 - Does task T020 "InvoicePayment entity" conflict with data-model.md "InvoicePaymentAllocation"? [Conflict, Naming]
- [X] CHK096 - Do tasks T166-T167 referencing "RecordPaymentAsync" and "InvoicePayment records" conflict with updated architecture (should be "AllocatePaymentAsync" and "InvoicePaymentAllocation")? [Conflict, tasks.md vs spec.md §US8]

---

**Total Items**: 96
**Categories**: 9 quality dimensions
**Traceability**: 82/96 items (85.4%) include explicit references to spec sections, data model, tasks, or gaps

**Next Steps**:
1. Address conflicts CHK094-CHK096 immediately (tasks.md references outdated entity names)
2. Fill gaps for edge case requirements (CHK053-CHK067)
3. Document external service dependencies and contracts (CHK078-CHK082)
4. Define non-functional requirements for performance and reliability (CHK068-CHK074)
5. Resolve ambiguities around allocation reversals and PDF endpoint authorization (CHK087-CHK093)
