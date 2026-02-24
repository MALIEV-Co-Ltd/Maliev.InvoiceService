# InvoiceService Implementation Plan - Permission-Based Authorization Migration

## Overview
Migrate InvoiceService from policy-based to permission-based authorization in 5 phases over 2-3 days.

## Phase 1: Create Permission Definitions (Day 1, Morning - 2 hours)
**Goal**: Define all permissions and roles as constants

### Tasks
1. Create Authorization folder
   ```bash
   mkdir -p Maliev.InvoiceService.Api/Authorization
   ```

2. Create InvoicePermissions.cs
   ```csharp
   // Authorization/InvoicePermissions.cs
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
           InvoicesCreate, InvoicesRead, InvoicesUpdate, InvoicesDelete,
           InvoicesFinalize, InvoicesApprove, InvoicesVoid, InvoicesExport, InvoicesSend,
           SegmentsCreate, SegmentsRead, SegmentsUpdate, SegmentsDelete,
           SplitsCreate, SplitsManage,
           FilesUpload, FilesRead, FilesDelete, FilesRegister,
           ReportsCurrency, ReportsAnalytics, ReportsExport
       };
   }
   ```

3. Create InvoicePredefinedRoles.cs
   ```csharp
   using Maliev.Aspire.ServiceDefaults.IAM;

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

       public static readonly RoleRegistration[] All = new[]
       {
           Admin, Manager, Creator, Viewer
       };
   }
   ```

4. Verify constants compile correctly
   - Build project
   - Check for typos

**Estimated Time**: 2 hours

**Files Created**:
- `Maliev.InvoiceService.Api/Authorization/InvoicePermissions.cs`
- `Maliev.InvoiceService.Api/Authorization/InvoicePredefinedRoles.cs`

## Phase 2: Create IAM Registration Service (Day 1, Afternoon - 2 hours)
**Goal**: Register permissions and roles with IAM on startup

### Tasks
1. Create InvoiceIAMRegistrationService.cs
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
           // Map each permission constant to registration
           return new[]
           {
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
                   PermissionId = InvoicePermissions.InvoicesFinalize,
                   ResourceType = "invoices",
                   Action = "finalize",
                   Description = "Finalize invoices (lock for editing)",
                   IsCritical = true  // Critical operation
               },
               // ... add all 21+ permissions
           };
       }

       protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
       {
           return InvoicePredefinedRoles.All;
       }
   }
   ```

2. Update Program.cs
   ```csharp
   // Add IAM client
   builder.Services.AddIAMClient(builder.Configuration, "InvoiceService");

   // Register permissions/roles on startup
   builder.Services.AddHostedService<InvoiceIAMRegistrationService>();
   ```

3. Add configuration
   ```json
   // appsettings.json
   {
     "Features": {
       "PermissionBasedAuthEnabled": false
     },
     "ExternalServices": {
       "IAM": {
         "BaseUrl": "http://localhost:5100",
         "ServiceAccountToken": "dev-token",
         "Timeout": 5000,
         "RetryCount": 2,
         "CircuitBreakerThreshold": 5
       }
     }
   }
   ```

4. Test registration
   - Start IAM service locally
   - Start InvoiceService
   - Check logs for "Registered X permissions for InvoiceService"
   - Verify permissions in IAM database

**Estimated Time**: 2 hours

**Files Created**:
- `Maliev.InvoiceService.Api/Services/InvoiceIAMRegistrationService.cs`

**Files Modified**:
- `Maliev.InvoiceService.Api/Program.cs`
- `Maliev.InvoiceService.Api/appsettings.json`

## Phase 3: Update Controller Attributes (Day 1-2 - 3 hours)
**Goal**: Replace all [Authorize(Policy)] with [RequirePermission]

### Tasks
1. Update InvoicesController.cs
   ```csharp
   // Find all methods and update

   // BEFORE:
   [Authorize(Policy = "EmployeeOrHigher")]
   public async Task<IActionResult> CreateInvoice(...)

   // AFTER:
   [RequirePermission(InvoicePermissions.InvoicesCreate)]
   public async Task<IActionResult> CreateInvoice(...)
   ```

   **All methods to update**:
   - CreateInvoice → InvoicesCreate
   - GetInvoice → InvoicesRead
   - UpdateInvoice → InvoicesUpdate
   - DeleteInvoice → InvoicesDelete
   - FinalizeInvoice → InvoicesFinalize
   - ApproveInvoice → InvoicesApprove
   - VoidInvoice → InvoicesVoid
   - ExportInvoices → InvoicesExport
   - SendInvoice → InvoicesSend
   - GetCurrencyReport → ReportsCurrency
   - GetAnalytics → ReportsAnalytics

2. Update InvoiceSegmentsController.cs
   ```csharp
   [HttpPost]
   [RequirePermission(InvoicePermissions.SegmentsCreate)]
   public async Task<IActionResult> CreateSegment(...)

   [HttpGet("{id}")]
   [RequirePermission(InvoicePermissions.SegmentsRead)]
   public async Task<IActionResult> GetSegment(...)

   [HttpPut("{id}")]
   [RequirePermission(InvoicePermissions.SegmentsUpdate)]
   public async Task<IActionResult> UpdateSegment(...)

   [HttpDelete("{id}")]
   [RequirePermission(InvoicePermissions.SegmentsDelete)]
   public async Task<IActionResult> DeleteSegment(...)
   ```

3. Update SplitInvoicesController.cs (if exists)
   ```csharp
   [RequirePermission(InvoicePermissions.SplitsCreate)]
   [RequirePermission(InvoicePermissions.SplitsManage)]
   ```

4. Update file-related endpoints
   ```csharp
   [HttpPost("{id}/files")]
   [RequirePermission(InvoicePermissions.FilesUpload)]
   public async Task<IActionResult> UploadFile(...)

   [HttpPost("pdf/register")]
   [RequirePermission(InvoicePermissions.FilesRegister)]  // Service-to-service
   public async Task<IActionResult> RegisterPDF(...)
   ```

5. Remove old authorization code
   ```csharp
   // DELETE from Program.cs:
   builder.Services.AddAuthorization(options =>
   {
       options.AddPolicy("EmployeeOrHigher", ...);
       options.AddPolicy("Manager", ...);
   });
   ```

**Estimated Time**: 3 hours

**Files Modified**:
- `Maliev.InvoiceService.Api/Controllers/InvoicesController.cs`
- `Maliev.InvoiceService.Api/Controllers/InvoiceSegmentsController.cs`
- `Maliev.InvoiceService.Api/Controllers/InvoiceFilesController.cs` (if separate)
- `Maliev.InvoiceService.Api/Program.cs` (remove policies)

## Phase 4: Update Tests (Day 2 - 4 hours)
**Goal**: Update all tests to use permission-based auth

### Tasks
1. Update integration test setup
   ```csharp
   using Maliev.Aspire.ServiceDefaults.Testing;

   public class InvoiceControllerTests : IClassFixture<WebApplicationFactory>
   {
       [Fact]
       public async Task CreateInvoice_WithCreatePermission_ReturnsCreated()
       {
           // Arrange
           var client = _factory.CreateClient()
               .WithTestAuth(InvoicePermissions.InvoicesCreate);

           // Act
           var response = await client.PostAsJsonAsync("/invoices", new { ... });

           // Assert
           response.StatusCode.Should().Be(HttpStatusCode.Created);
       }

       [Fact]
       public async Task CreateInvoice_WithoutCreatePermission_ReturnsForbidden()
       {
           // Arrange - Only read permission
           var client = _factory.CreateClient()
               .WithTestAuth(InvoicePermissions.InvoicesRead);

           // Act
           var response = await client.PostAsJsonAsync("/invoices", new { ... });

           // Assert
           response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
       }
   }
   ```

2. Update existing tests
   - Find all tests using .WithRole("Manager") or similar
   - Replace with .WithTestAuth(InvoicePermissions.xxx)

3. Add new permission-specific tests
   ```csharp
   [Fact]
   public async Task FinalizeInvoice_WithFinalizePermission_Succeeds()

   [Fact]
   public async Task FinalizeInvoice_WithoutFinalizePermission_Forbidden()

   [Fact]
   public async Task ApproveInvoice_WithApprovePermission_Succeeds()

   [Fact]
   public async Task ApproveInvoice_WithoutApprovePermission_Forbidden()
   ```

4. Test multiple permission combinations
   ```csharp
   [Fact]
   public async Task CreateAndFinalizeWorkflow_WithBothPermissions_Succeeds()
   {
       var client = _factory.CreateClient()
           .WithTestAuth(
               InvoicePermissions.InvoicesCreate,
               InvoicePermissions.InvoicesFinalize);

       // Create
       var createResponse = await client.PostAsJsonAsync("/invoices", ...);
       var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

       // Finalize
       var finalizeResponse = await client.PostAsync($"/invoices/{invoice.Id}/finalize", null);

       // Both should succeed
       createResponse.Should().BeSuccessful();
       finalizeResponse.Should().BeSuccessful();
   }
   ```

5. Run all tests and fix failures

**Estimated Time**: 4 hours

**Files Modified**:
- `Maliev.InvoiceService.Tests/Integration/InvoicesControllerTests.cs`
- `Maliev.InvoiceService.Tests/Integration/InvoiceSegmentsControllerTests.cs`
- All other integration test files

## Phase 5: Deployment & Verification (Day 2-3 - 2 hours)
**Goal**: Deploy with feature flag and verify

### Tasks
1. Deploy to dev environment
   - Feature flag OFF initially
   - Verify deployment successful
   - Verify no regressions

2. Enable feature flag
   ```json
   {
     "Features": {
       "PermissionBasedAuthEnabled": true
     }
   }
   ```

3. Test manually
   - Login as user with invoice-admin role
   - Verify can create invoice
   - Verify can finalize invoice
   - Login as user with invoice-viewer role
   - Verify can only read invoices
   - Verify cannot create/update/delete

4. Create test users in IAM
   ```bash
   # Grant invoice-admin role to test user
   POST /api/v1/principals/{principalId}/roles
   { "roleId": "invoice-admin" }

   # Grant invoice-viewer role to another user
   POST /api/v1/principals/{principalId}/roles
   { "roleId": "invoice-viewer" }
   ```

5. Verify IAM registration
   - Check IAM database for registered permissions
   - Check IAM database for registered roles
   - Verify counts: 21+ permissions, 4+ roles

6. Test service-to-service
   - UploadService calls RegisterPDF endpoint
   - Verify UploadService's service account has invoice.files.register permission

7. Monitor logs
   - Check for authorization errors
   - Check for IAM registration errors
   - Verify permission checks are working

8. Deploy to staging
   - Enable feature flag
   - Run full regression suite
   - Smoke test all endpoints

9. Deploy to production
   - Feature flag OFF initially
   - Monitor for 1 hour
   - Enable flag for 10% traffic
   - Monitor for 2 hours
   - Full rollout (100%)

**Estimated Time**: 2 hours

## Testing Checklist

### Manual Tests
- [ ] Create invoice with invoice-creator role → Success
- [ ] Create invoice with invoice-viewer role → Forbidden
- [ ] Finalize invoice with invoice-manager role → Success
- [ ] Finalize invoice with invoice-creator role → Forbidden
- [ ] Approve invoice with invoice-accountant role → Success
- [ ] Approve invoice with invoice-creator role → Forbidden
- [ ] View invoice with invoice-viewer role → Success
- [ ] Export report with invoice-manager role → Success
- [ ] Export report with invoice-viewer role → Forbidden

### Automated Tests
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Permission check tests pass
- [ ] Service registration test passes
- [ ] IAM client tests pass

### Service-to-Service Tests
- [ ] UploadService can register PDF (has permission)
- [ ] Other services cannot register PDF (no permission)

## Rollback Plan

### Immediate Rollback
1. Set `PermissionBasedAuthEnabled=false` in appsettings
2. Service reverts to old policy-based auth
3. No code changes needed
4. Monitor for 10 minutes
5. Verify functionality restored

### Code Rollback
1. Deploy previous git tag
2. Verify deployment
3. Run smoke tests
4. All functionality restored

## Success Criteria

- [ ] All 21+ permissions registered with IAM
- [ ] All 4+ predefined roles registered
- [ ] All controller methods have [RequirePermission]
- [ ] No [Authorize(Policy)] attributes remain
- [ ] No policy configurations in Program.cs
- [ ] All integration tests pass
- [ ] Manual tests pass for all roles
- [ ] Service-to-service calls work
- [ ] Zero authorization errors in logs
- [ ] Performance is acceptable (< 1ms permission checks)

## Total Estimated Time

- Phase 1: 2 hours
- Phase 2: 2 hours
- Phase 3: 3 hours
- Phase 4: 4 hours
- Phase 5: 2 hours

**Total: ~13 hours (~2 days)**

## Dependencies

- ServiceDefaults with RequirePermission attribute deployed
- IAM service deployed and accessible
- Service account token configured
- Test users created in IAM with appropriate roles

## Next Steps After Completion

1. Document permission requirements in API documentation
2. Update user guides with new role names
3. Migrate next business service (OrderService, PaymentService, etc.)
4. Create permission assignment UI for admins
5. Monitor IAM service performance
