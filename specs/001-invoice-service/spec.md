# InvoiceService Specification - Permission-Based Authorization Migration

## Overview
Migrate InvoiceService from policy-based authorization (`[Authorize(Policy = "...")]`) to fine-grained permission-based authorization using the IAM service.

## Current State
- Uses policy-based authorization with hardcoded policies:
  - `EmployeeOrHigher` - For create, export, file registration, currency reports
  - `Manager` - For finalize, split operations, analytics
- Policies defined in Program.cs using role-based checks
- No fine-grained permission control
- Cannot dynamically grant/revoke specific permissions
- Authorization checks: `[Authorize(Policy = "Manager")]`

## Target State
- Permission-based authorization with format: `invoice.{resource}.{action}`
- Fine-grained permissions for all operations
- Dynamic permission assignment via IAM service
- Authorization checks: `[RequirePermission("invoice.invoices.create")]`
- Service registers permissions with IAM on startup
- Supports resource-scoped permissions (future enhancement)

## Permissions to Define

### Invoice Operations
```
invoice.invoices.create          - Create new invoices
invoice.invoices.read            - Read invoice details
invoice.invoices.update          - Update invoice information
invoice.invoices.delete          - Delete invoices
invoice.invoices.finalize        - Finalize invoices (lock for editing)
invoice.invoices.approve         - Approve invoices
invoice.invoices.void            - Void/cancel invoices
invoice.invoices.export          - Export invoices to various formats
invoice.invoices.send            - Send invoices to customers
```

### Segment Operations
```
invoice.segments.create          - Create invoice segments
invoice.segments.read            - Read segment details
invoice.segments.update          - Update segments
invoice.segments.delete          - Delete segments
```

### Split Invoice Operations
```
invoice.splits.create            - Create split invoices
invoice.splits.manage            - Manage split invoice relationships
```

### File Operations
```
invoice.files.upload             - Upload files to invoices
invoice.files.read               - Read/download invoice files
invoice.files.delete             - Delete invoice files
invoice.files.register           - Register PDF files (service-to-service)
```

### Reporting Operations
```
invoice.reports.currency         - View currency exchange reports
invoice.reports.analytics        - Access invoice analytics
invoice.reports.export           - Export reports
```

## Predefined Roles

### invoice-admin
**Description**: Full administrative access to all invoice operations
**Permissions**:
- All invoice.* permissions

### invoice-manager
**Description**: Can create, update, finalize, and approve invoices
**Permissions**:
- invoice.invoices.create
- invoice.invoices.read
- invoice.invoices.update
- invoice.invoices.finalize
- invoice.invoices.approve
- invoice.invoices.send
- invoice.segments.create
- invoice.segments.read
- invoice.segments.update
- invoice.files.upload
- invoice.files.read
- invoice.reports.currency
- invoice.reports.analytics

### invoice-creator
**Description**: Can create and manage own invoices
**Permissions**:
- invoice.invoices.create
- invoice.invoices.read
- invoice.invoices.update
- invoice.segments.create
- invoice.segments.read
- invoice.segments.update
- invoice.files.upload
- invoice.files.read

### invoice-viewer
**Description**: Read-only access to invoices
**Permissions**:
- invoice.invoices.read
- invoice.segments.read
- invoice.files.read
- invoice.reports.currency

### invoice-accountant
**Description**: Can approve, void, and manage financial aspects
**Permissions**:
- invoice.invoices.read
- invoice.invoices.approve
- invoice.invoices.void
- invoice.invoices.finalize
- invoice.segments.read
- invoice.reports.currency
- invoice.reports.analytics
- invoice.reports.export

## Controller Changes

### InvoicesController.cs

**Before**:
```csharp
[HttpPost]
[Authorize(Policy = "EmployeeOrHigher")]
public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
{
    // ...
}

[HttpPost("{id}/finalize")]
[Authorize(Policy = "Manager")]
public async Task<IActionResult> FinalizeInvoice(Guid id, [FromBody] FinalizeInvoiceRequest request)
{
    // ...
}

[HttpPost("export")]
[Authorize(Policy = "EmployeeOrHigher")]
public async Task<IActionResult> ExportInvoices([FromBody] ExportInvoicesRequest request)
{
    // ...
}
```

**After**:
```csharp
[HttpPost]
[RequirePermission("invoice.invoices.create")]
public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
{
    // ...
}

[HttpPost("{id}/finalize")]
[RequirePermission("invoice.invoices.finalize")]
public async Task<IActionResult> FinalizeInvoice(Guid id, [FromBody] FinalizeInvoiceRequest request)
{
    // ...
}

[HttpPost("export")]
[RequirePermission("invoice.invoices.export")]
public async Task<IActionResult> ExportInvoices([FromBody] ExportInvoicesRequest request)
{
    // ...
}

[HttpPost("{id}/approve")]
[RequirePermission("invoice.invoices.approve")]
public async Task<IActionResult> ApproveInvoice(Guid id)
{
    // ...
}

[HttpGet("{id}")]
[RequirePermission("invoice.invoices.read")]
public async Task<IActionResult> GetInvoice(Guid id)
{
    // ...
}
```

### InvoiceSegmentsController.cs

**Before**:
```csharp
[Authorize(Policy = "EmployeeOrHigher")]
```

**After**:
```csharp
[HttpPost]
[RequirePermission("invoice.segments.create")]
public async Task<IActionResult> CreateSegment(...)

[HttpGet("{id}")]
[RequirePermission("invoice.segments.read")]
public async Task<IActionResult> GetSegment(...)

[HttpPut("{id}")]
[RequirePermission("invoice.segments.update")]
public async Task<IActionResult> UpdateSegment(...)

[HttpDelete("{id}")]
[RequirePermission("invoice.segments.delete")]
public async Task<IActionResult> DeleteSegment(...)
```

## New Files to Create

### 1. InvoicePermissions.cs

**Location**: `Maliev.InvoiceService.Api/Authorization/InvoicePermissions.cs`

**Content**:
```csharp
namespace Maliev.InvoiceService.Api.Authorization;

public static class InvoicePermissions
{
    // Invoice operations
    public const string InvoicesCreate = "invoice.invoices.create";
    public const string InvoicesRead = "invoice.invoices.read";
    public const string InvoicesUpdate = "invoice.invoices.update";
    public const string InvoicesDelete = "invoice.invoices.delete";
    public const string InvoicesFinalize = "invoice.invoices.finalize";
    public const string InvoicesApprove = "invoice.invoices.approve";
    public const string InvoicesVoid = "invoice.invoices.void";
    public const string InvoicesExport = "invoice.invoices.export";
    public const string InvoicesSend = "invoice.invoices.send";

    // Segment operations
    public const string SegmentsCreate = "invoice.segments.create";
    public const string SegmentsRead = "invoice.segments.read";
    public const string SegmentsUpdate = "invoice.segments.update";
    public const string SegmentsDelete = "invoice.segments.delete";

    // Split operations
    public const string SplitsCreate = "invoice.splits.create";
    public const string SplitsManage = "invoice.splits.manage";

    // File operations
    public const string FilesUpload = "invoice.files.upload";
    public const string FilesRead = "invoice.files.read";
    public const string FilesDelete = "invoice.files.delete";
    public const string FilesRegister = "invoice.files.register";

    // Reporting operations
    public const string ReportsCurrency = "invoice.reports.currency";
    public const string ReportsAnalytics = "invoice.reports.analytics";
    public const string ReportsExport = "invoice.reports.export";

    public static readonly string[] All = new[]
    {
        InvoicesCreate,
        InvoicesRead,
        InvoicesUpdate,
        InvoicesDelete,
        InvoicesFinalize,
        InvoicesApprove,
        InvoicesVoid,
        InvoicesExport,
        InvoicesSend,
        SegmentsCreate,
        SegmentsRead,
        SegmentsUpdate,
        SegmentsDelete,
        SplitsCreate,
        SplitsManage,
        FilesUpload,
        FilesRead,
        FilesDelete,
        FilesRegister,
        ReportsCurrency,
        ReportsAnalytics,
        ReportsExport
    };
}
```

### 2. InvoicePredefinedRoles.cs

**Location**: `Maliev.InvoiceService.Api/Authorization/InvoicePredefinedRoles.cs`

**Content**:
```csharp
using Maliev.Aspire.ServiceDefaults.IAM;

namespace Maliev.InvoiceService.Api.Authorization;

public static class InvoicePredefinedRoles
{
    public static readonly RoleRegistration Admin = new()
    {
        RoleId = "invoice-admin",
        RoleName = "Invoice Administrator",
        Description = "Full administrative access to all invoice operations",
        Permissions = InvoicePermissions.All
    };

    public static readonly RoleRegistration Manager = new()
    {
        RoleId = "invoice-manager",
        RoleName = "Invoice Manager",
        Description = "Can create, update, finalize, and approve invoices",
        Permissions = new[]
        {
            InvoicePermissions.InvoicesCreate,
            InvoicePermissions.InvoicesRead,
            InvoicePermissions.InvoicesUpdate,
            InvoicePermissions.InvoicesFinalize,
            InvoicePermissions.InvoicesApprove,
            InvoicePermissions.InvoicesSend,
            InvoicePermissions.SegmentsCreate,
            InvoicePermissions.SegmentsRead,
            InvoicePermissions.SegmentsUpdate,
            InvoicePermissions.FilesUpload,
            InvoicePermissions.FilesRead,
            InvoicePermissions.ReportsCurrency,
            InvoicePermissions.ReportsAnalytics
        }
    };

    public static readonly RoleRegistration Creator = new()
    {
        RoleId = "invoice-creator",
        RoleName = "Invoice Creator",
        Description = "Can create and manage own invoices",
        Permissions = new[]
        {
            InvoicePermissions.InvoicesCreate,
            InvoicePermissions.InvoicesRead,
            InvoicePermissions.InvoicesUpdate,
            InvoicePermissions.SegmentsCreate,
            InvoicePermissions.SegmentsRead,
            InvoicePermissions.SegmentsUpdate,
            InvoicePermissions.FilesUpload,
            InvoicePermissions.FilesRead
        }
    };

    public static readonly RoleRegistration Viewer = new()
    {
        RoleId = "invoice-viewer",
        RoleName = "Invoice Viewer",
        Description = "Read-only access to invoices",
        Permissions = new[]
        {
            InvoicePermissions.InvoicesRead,
            InvoicePermissions.SegmentsRead,
            InvoicePermissions.FilesRead,
            InvoicePermissions.ReportsCurrency
        }
    };

    public static readonly RoleRegistration Accountant = new()
    {
        RoleId = "invoice-accountant",
        RoleName = "Invoice Accountant",
        Description = "Can approve, void, and manage financial aspects",
        Permissions = new[]
        {
            InvoicePermissions.InvoicesRead,
            InvoicePermissions.InvoicesApprove,
            InvoicePermissions.InvoicesVoid,
            InvoicePermissions.InvoicesFinalize,
            InvoicePermissions.SegmentsRead,
            InvoicePermissions.ReportsCurrency,
            InvoicePermissions.ReportsAnalytics,
            InvoicePermissions.ReportsExport
        }
    };

    public static readonly RoleRegistration[] All = new[]
    {
        Admin,
        Manager,
        Creator,
        Viewer,
        Accountant
    };
}
```

### 3. InvoiceIAMRegistrationService.cs

**Location**: `Maliev.InvoiceService.Api/Services/InvoiceIAMRegistrationService.cs`

**Content**:
```csharp
using Maliev.Aspire.ServiceDefaults.IAM;

namespace Maliev.InvoiceService.Api.Services;

public class InvoiceIAMRegistrationService : IAMRegistrationService
{
    public InvoiceIAMRegistrationService(
        IHttpClientFactory httpClientFactory,
        ILogger<InvoiceIAMRegistrationService> logger)
        : base(httpClientFactory, logger, "InvoiceService")
    {
    }

    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return new[]
        {
            // Invoice operations
            new PermissionRegistration
            {
                PermissionId = InvoicePermissions.InvoicesCreate,
                ResourceType = "invoices",
                Action = "create",
                Description = "Create new invoices",
                IsCritical = false
            },
            new PermissionRegistration
            {
                PermissionId = InvoicePermissions.InvoicesRead,
                ResourceType = "invoices",
                Action = "read",
                Description = "Read invoice details",
                IsCritical = false
            },
            new PermissionRegistration
            {
                PermissionId = InvoicePermissions.InvoicesUpdate,
                ResourceType = "invoices",
                Action = "update",
                Description = "Update invoice information",
                IsCritical = false
            },
            new PermissionRegistration
            {
                PermissionId = InvoicePermissions.InvoicesFinalize,
                ResourceType = "invoices",
                Action = "finalize",
                Description = "Finalize invoices (lock for editing)",
                IsCritical = true  // Critical operation
            },
            new PermissionRegistration
            {
                PermissionId = InvoicePermissions.InvoicesApprove,
                ResourceType = "invoices",
                Action = "approve",
                Description = "Approve invoices",
                IsCritical = true  // Critical operation
            },
            new PermissionRegistration
            {
                PermissionId = InvoicePermissions.InvoicesVoid,
                ResourceType = "invoices",
                Action = "void",
                Description = "Void/cancel invoices",
                IsCritical = true  // Critical operation
            },
            // ... (add all other permissions)
        };
    }

    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return InvoicePredefinedRoles.All;
    }
}
```

## Program.cs Changes

### Remove Old Authorization Policies

**Before**:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EmployeeOrHigher", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("Manager", policy =>
        policy.RequireRole("Manager", "manager", "admin"));
});
```

**After**:
```csharp
// Remove all policy-based authorization
// Authorization is now handled by [RequirePermission] attribute
```

### Add IAM Client and Registration

**Add**:
```csharp
// Add IAM client
builder.Services.AddIAMClient(builder.Configuration, "InvoiceService");

// Register permissions/roles on startup
builder.Services.AddHostedService<InvoiceIAMRegistrationService>();
```

### Add Feature Flag Configuration

**appsettings.json**:
```json
{
  "Features": {
    "PermissionBasedAuthEnabled": false
  },
  "ExternalServices": {
    "IAM": {
      "BaseUrl": "http://iam-service:8080",
      "ServiceAccountToken": "<secret-from-vault>",
      "Timeout": 5000,
      "RetryCount": 2,
      "CircuitBreakerThreshold": 5
    }
  }
}
```

## Service-to-Service Operations

Some endpoints are called by other services (e.g., PDF registration from UploadService). These should use service account permissions:

**Before**:
```csharp
[HttpPost("pdf/register")]
[AllowAnonymous]  // Service-to-service
public async Task<IActionResult> RegisterPDF(...)
```

**After**:
```csharp
[HttpPost("pdf/register")]
[RequirePermission("invoice.files.register")]  // Service account has this permission
public async Task<IActionResult> RegisterPDF(...)
```

UploadService's service account should be granted the `invoice.files.register` permission.

## Testing Requirements

### Unit Tests
- Verify permission constants are correctly formatted
- Test IAMRegistrationService returns correct permissions/roles
- Mock RequirePermission attribute behavior

### Integration Tests
**New Tests**:
```csharp
[Fact]
public async Task CreateInvoice_WithCreatePermission_ReturnsCreated()
{
    // Arrange
    var client = _factory.CreateClient()
        .WithTestAuth("invoice.invoices.create");

    // Act
    var response = await client.PostAsJsonAsync("/invoices", new CreateInvoiceRequest { ... });

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}

[Fact]
public async Task CreateInvoice_WithoutCreatePermission_ReturnsForbidden()
{
    // Arrange
    var client = _factory.CreateClient()
        .WithTestAuth("invoice.invoices.read");  // Only read permission

    // Act
    var response = await client.PostAsJsonAsync("/invoices", new CreateInvoiceRequest { ... });

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}

[Fact]
public async Task FinalizeInvoice_WithoutFinalizePermission_ReturnsForbidden()
{
    // Arrange
    var client = _factory.CreateClient()
        .WithTestAuth("invoice.invoices.create", "invoice.invoices.read");

    // Act
    var response = await client.PostAsync($"/invoices/{invoiceId}/finalize", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

## Migration Checklist

- [ ] Create InvoicePermissions.cs with all permission constants
- [ ] Create InvoicePredefinedRoles.cs with role definitions
- [ ] Create InvoiceIAMRegistrationService.cs
- [ ] Update all controller methods with [RequirePermission]
- [ ] Remove [Authorize(Policy)] attributes
- [ ] Remove policy configurations from Program.cs
- [ ] Add IAM client configuration
- [ ] Add InvoiceIAMRegistrationService to Program.cs
- [ ] Add configuration for IAM service
- [ ] Update all integration tests
- [ ] Write new permission-based tests
- [ ] Test with real IAM service in dev
- [ ] Document permission requirements in OpenAPI
- [ ] Deploy with feature flag OFF
- [ ] Enable feature flag in staging
- [ ] Enable feature flag in production

## Success Criteria

- [ ] All endpoints have [RequirePermission] attributes
- [ ] No [Authorize(Policy)] attributes remain
- [ ] Service registers 21+ permissions on startup
- [ ] Service registers 5 predefined roles
- [ ] All integration tests pass with permission-based auth
- [ ] Permission checks work correctly (allow/deny)
- [ ] Service-to-service calls work with service accounts
- [ ] OpenAPI documentation shows permission requirements
- [ ] No regressions in functionality

## Rollback Plan

### Immediate Rollback
- Set feature flag `PermissionBasedAuthEnabled=false`
- Service reverts to old policy-based authorization
- No code changes needed

### Code Rollback
- Deploy previous version
- All functionality restored

## Benefits of Permission-Based Authorization

1. **Fine-Grained Control**: Separate permissions for create, finalize, approve, void
2. **Dynamic Assignment**: Grant/revoke permissions without code changes
3. **Audit Trail**: IAM tracks all permission grants/revocations
4. **Resource Scoping**: Future support for invoice-specific permissions
5. **Consistency**: Same authorization pattern across all services
6. **Testability**: Easy to test with different permission combinations

## Future Enhancements (Out of Scope)

- Resource-scoped permissions: `invoice.invoices.approve:INV-123`
- Conditional permissions: Only approve invoices < $10,000
- Time-based permissions: Temporary access grants
- Department-scoped permissions: Only invoices for specific departments
