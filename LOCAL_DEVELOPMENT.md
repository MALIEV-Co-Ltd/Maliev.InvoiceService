# Local Development - Quick Start (Without Full Docker Compose)

This guide helps you run the Invoice Service locally in Visual Studio **without requiring all external services**.

---

## 🚀 **Minimal Setup (Just View Scalar UI)**

### **1. Start PostgreSQL Only**

The Invoice Service requires **only PostgreSQL** to start. You can use Docker for just PostgreSQL:

```bash
# Quick start PostgreSQL for local dev (port 5433 to avoid conflicts with native PostgreSQL)
docker run -d `
  --name invoice-postgres-local `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=postgres_dev_password `
  -e POSTGRES_DB=invoice_db `
  -p 5433:5432 `
  postgres:18

# Verify it's running
docker ps | Select-String "invoice-postgres-local"
```

### **2. Apply Database Migrations**

```bash
cd Maliev.InvoiceService.Data
dotnet ef database update --startup-project ../Maliev.InvoiceService.Api
```

### **3. Start in Visual Studio**

1. Open `Maliev.InvoiceService.sln` in **Visual Studio 2022**
2. Select profile: **https** (from dropdown)
3. Press **F5**
4. Browser automatically opens to: `https://localhost:5001/invoices/scalar/v1`

---

## ✅ **What Works Without External Services**

With **only PostgreSQL running**, you can:
- ✅ View **Scalar UI** API documentation
- ✅ Create, read, update invoices
- ✅ Finalize and cancel invoices
- ✅ Split invoices
- ✅ View audit logs
- ✅ View analytics

**What doesn't work** (requires external services):
- ❌ Currency conversion (needs Currency Service)
- ❌ Payment allocation via events (needs RabbitMQ + Payment Service)
- ❌ PDF generation (needs PDF Service)

---

## 🔧 **Configuration Defaults for Local Dev**

The service automatically uses these defaults in `Development` mode:

| Service | Status | Fallback |
|---------|--------|----------|
| **PostgreSQL** | ✅ **Required** | N/A |
| **Redis** | ⚠️ Optional | Uses in-memory cache |
| **RabbitMQ** | ⚠️ Disabled | Uses in-memory transport (events ignored) |
| **External APIs** | ⚠️ Optional | Returns errors if called |

**Configured in:** `appsettings.Development.json`

```json
{
  "Redis": {
    "Configuration": ""  // Empty = use in-memory
  },
  "RabbitMQ": {
    "Enabled": false  // Disabled for local dev
  }
}
```

---

## 🐳 **Optional: Start All Services with Docker Compose**

For **full functionality** (currency conversion, payment events, PDF generation):

```bash
# Start PostgreSQL + Redis + RabbitMQ
docker-compose -f docker-compose.dev.yml up -d

# Enable RabbitMQ in appsettings.Development.json
# Change: "Enabled": false  →  "Enabled": true

# Enable Redis in appsettings.Development.json
# Change: "Configuration": ""  →  "Configuration": "localhost:6379"
```

---

## 🧹 **Cleanup**

```bash
# Stop and remove PostgreSQL
docker stop invoice-postgres-local
docker rm invoice-postgres-local

# Or stop all services
docker-compose -f docker-compose.dev.yml down
```

---

## 🛠 **Troubleshooting**

### **Issue: Application hangs on startup**

**Cause:** RabbitMQ is enabled but not running

**Solution:**
```json
// appsettings.Development.json
{
  "RabbitMQ": {
    "Enabled": false  // ← Set to false
  }
}
```

### **Issue: Database connection error**

**Cause:** PostgreSQL is not running

**Solution:**
```bash
# Start PostgreSQL container
docker run -d --name invoice-postgres-local `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=postgres_dev_password `
  -e POSTGRES_DB=invoice_db `
  -p 5432:5432 postgres:18
```

### **Issue: Browser doesn't open to Scalar UI**

**Cause:** Wrong launch profile selected in Visual Studio

**Solution:**
1. Click dropdown next to **Play button** in Visual Studio
2. Select **"https"** profile
3. Press F5 again

### **Issue: Password authentication failed for user "postgres"**

**Cause:** Native PostgreSQL installation is running on port 5432

**Solution:**
The Invoice Service is configured to use port 5433 to avoid conflicts with native PostgreSQL installations. Ensure you're running the Docker container on port 5433:
```bash
docker run -d --name invoice-postgres-local `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=postgres_dev_password `
  -e POSTGRES_DB=invoice_db `
  -p 5433:5432 postgres:18
```

---

## 📝 **Next Steps**

- **Explore API:** Navigate to `https://localhost:5001/invoices/scalar/v1`
- **Test Endpoints:** Use the `.http` file: `Maliev.InvoiceService.Api.http`
- **Run Tests:** `dotnet test`
- **Full Setup:** See `quickstart.md` for production-like setup

---

**Status:** ✅ Updated for local development without Docker Compose
**Last Updated:** 2025-01-14
