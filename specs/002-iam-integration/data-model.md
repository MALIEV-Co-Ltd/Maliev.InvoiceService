# Data Model: Authorization Entities

## Overview
This document defines the structure of the authorization entities (Permissions and Roles) that will be registered with the IAM service.

## Entities

### Permission
Represents a fine-grained action that can be performed on a specific resource.

| Field | Type | Description |
|-------|------|-------------|
| PermissionId | string | Unique identifier in format `invoice.{resource}.{action}` |
| Description | string | Human-readable explanation of the permission |

### Role
A collection of permissions assigned to a principal (user or service account).

| Field | Type | Description |
|-------|------|-------------|
| RoleId | string | Unique identifier in format `roles.invoice.{role-name}` |
| Description | string | Detailed description of the role's purpose |
| PermissionIds | string[] | List of PermissionIds included in this role |

## Predefined Roles & Permissions

### 1. Invoice Operations
- `invoice.invoices.create`
- `invoice.invoices.read`
- `invoice.invoices.update`
- `invoice.invoices.delete`
- `invoice.invoices.finalize`
- `invoice.invoices.approve`
- `invoice.invoices.void`
- `invoice.invoices.export`
- `invoice.invoices.send`

### 2. Segment Operations
- `invoice.segments.create`
- `invoice.segments.read`
- `invoice.segments.update`
- `invoice.segments.delete`

### 3. Split Operations
- `invoice.splits.create`
- `invoice.splits.manage`

### 4. File Operations
- `invoice.files.upload`
- `invoice.files.read`
- `invoice.files.delete`
- `invoice.files.register`

### 5. Reporting Operations
- `invoice.reports.currency`
- `invoice.reports.analytics`
- `invoice.reports.export`

## Role Definitions

- **roles.invoice.admin**: All `invoice.*` permissions.
- **roles.invoice.manager**: Invoices (create, read, update, finalize, approve, send), Segments (create, read, update), Files (upload, read), Reports (currency, analytics).
- **roles.invoice.creator**: Invoices (create, read, update), Segments (create, read, update), Files (upload, read).
- **roles.invoice.viewer**: Invoices (read), Segments (read), Files (read), Reports (currency).
- **roles.invoice.accountant**: Invoices (read, approve, void, finalize), Segments (read), Reports (currency, analytics, export).
