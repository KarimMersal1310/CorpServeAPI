# CorpServe API - Feature Documentation & Manual Testing Guide

This document describes all major features implemented in the current backend, including request/proposal/SLA workflows, realtime notifications, and how to test everything manually.

## Tech Context

- `.NET 8`
- `C# 12`
- ASP.NET Core Web API
- EF Core + SQL Server
- ASP.NET Identity + JWT
- SignalR for realtime notifications

---

## 1) Implemented Business Workflows

## A. Request lifecycle (Client)

### Create request
- Client can create a request with:
  - title, description, category, budget range, expected deadline
  - optional AI estimate fields
  - optional attachments
- On success:
  - request status starts as `Pending`
  - in-app/realtime notification is sent to client
  - in-app/realtime notifications are sent to active vendors in the same category

### Update request (new)
- Client can edit owned request data and attachments.
- Supports:
  - update core fields
  - add new attachments
  - remove selected attachments
- **Guard rule:** update is blocked if request has any proposal.

### Delete request (new)
- Client can delete owned request.
- Attachment files are also removed from storage.
- **Guard rule:** delete is blocked if request has any proposal.

---

## B. Proposal lifecycle (Vendor + Client)

### Vendor actions
- Vendor can submit one proposal per request:
  - `Accept`
  - `Negotiate`
  - `Reject`

### Client actions
- Client can view proposals only for own request.
- Client can reject proposal.
- Client can accept one proposal.

### On client accept
- Accepted proposal -> `Accepted`
- All other proposals on same request -> auto `Rejected`
- Request status -> `Active`
- SLA contract is created with status `Inprogress`
- Notifications are sent to:
  - selected vendor
  - other vendors (auto reject info)
  - client (SLA created)

---

## C. SLA contract + monitor

A background service (`SLAStatusMonitorBackgroundService`) runs every 10 minutes and handles:

- deadline warnings
- delayed status when deadline passed
- completed status when request completed
- suspension handling (if client/vendor suspended)
- email + in-app/realtime notifications for SLA events

### Monitor parameters
- Check interval: `10 minutes`
- Warning window: `72 hours`
- Warning cooldown per contract: `12 hours`

---

## D. Vendor verification realtime support

- Vendor submit verification -> vendor + admins notified
- Admin approve/reject verification -> vendor notified
- Notifications are persisted and pushed in realtime

---

## E. Notification system (whole project)

### Persistence
- New entity: `SystemNotification`
- New enum: `NotificationType` (`Info`, `Success`, `Warning`, `Error`)
- Relationship: `ApplicationUser` (1-M) `Notifications`

### Service
- `INotificationService` / `NotificationService`
- Features:
  - send single notification
  - send to multiple users
  - get paginated notifications
  - unread count
  - mark one as read
  - mark all as read

### Realtime
- SignalR hub: `/hubs/notifications`
- Event name: `notificationReceived`
- JWT through query string (`access_token`) is supported for hub connections

---

## 2) API Endpoints

## Authentication
- `POST /api/authentication/register`
- `POST /api/authentication/login`
- `POST /api/authentication/refresh-token`
- `POST /api/authentication/revoke-refresh-token`
- `POST /api/authentication/forgot-password`
- `POST /api/authentication/reset-password`

## Requests
- `POST /api/requests/create` (Client, `multipart/form-data`)
- `PUT /api/requests/{requestId}` (Client, `multipart/form-data`) ✅
- `DELETE /api/requests/{requestId}` (Client) ✅
- `POST /api/requests/generate-estimate` (Client)
- `GET /api/requests/my-requests` (Client)
- `GET /api/requests/vendor-requests` (Vendor)

## Proposals
- `POST /api/proposals/accept` (Vendor)
- `POST /api/proposals/negotiate` (Vendor)
- `POST /api/proposals/reject` (Vendor)
- `GET /api/proposals/submitted` (Vendor)
- `GET /api/proposals/request/{requestId}/count` (Client)
- `GET /api/proposals/request/{requestId}` (Client)
- `POST /api/proposals/{proposalId}/client-accept` (Client)
- `POST /api/proposals/{proposalId}/client-reject` (Client)
- `GET /api/proposals/client-active-requests` (Client)
- `GET /api/proposals/vendor-active-requests` (Vendor)
- `GET /api/proposals/request/{requestId}/sla/client` (Client)
- `GET /api/proposals/request/{requestId}/sla/vendor` (Vendor)

## Notifications
- `GET /api/notifications/my`
- `GET /api/notifications/unread-count`
- `POST /api/notifications/{notificationId}/read`
- `POST /api/notifications/read-all`

## Vendor verify (existing + enhanced by notifications)
- Submit verification (vendor)
- Approve/reject verification (admin)

---

## 3) Request Update/Delete Contract

## `PUT /api/requests/{requestId}`
`multipart/form-data` fields:

- `Title` (required, max 200)
- `Description` (required, max 500)
- `CategoryId` (required)
- `ExpectedDeadline` (required, future date)
- `BudgetMin` (required > 0)
- `BudgetMax` (required > 0 and >= `BudgetMin`)
- `NewAttachments` (optional, repeated file inputs)
- `AttachmentIdsToRemove` (optional, repeated string inputs)

### Validation behavior
- Missing/invalid fields -> `Validation`
- Category not found -> `NotFound`
- Request not found or not owned by client -> `NotFound`
- Request has proposals -> `Conflict` with code `Request.HasProposals`

## `DELETE /api/requests/{requestId}`
- Deletes request only if no proposals exist.
- If proposals exist -> `Conflict` (`Request.HasProposals`).

---

## 4) Realtime Frontend Integration Notes

## Connect to hub
- URL: `/hubs/notifications`
- Provide JWT as query: `access_token=<token>`

## Listen event
- `notificationReceived`
- Payload shape:
  - `id`
  - `title`
  - `message`
  - `type` (string enum name)
  - `isRead`
  - `createdAt`
  - `relatedEntityId`
  - `relatedEntityType`

---

## 5) Manual Test Checklist

1. **Run migrations and start API**
2. **Login as three users**: client, vendor, admin
3. **Realtime connection**: connect to SignalR hub for each user
4. **Create request** (client)
   - Expect notifications for client and matching vendors
5. **Vendor submits proposal**
   - Expect notification for client
6. **Client accepts one proposal**
   - Expect SLA created + notifications for all involved parties
7. **Test request update/delete guard**
   - On request with proposals -> must fail with `Request.HasProposals`
8. **Test request update/delete success**
   - On request without proposals -> should pass
9. **Vendor verification flow**
   - Submit -> admin/vendor notifications
   - Approve/reject -> vendor notifications
10. **SLA monitor**
   - create near deadline SLA and verify warning/delayed notifications

---

## 6) Important Notes

- DTO status/type fields are returned as **string enum names**.
- Notification failures are logged and do not block core business operations.
- Suspended users are blocked from protected flows.
- File attachments are physically removed from storage during request delete and selected attachment removal.

---

## 7) Troubleshooting

## No realtime notifications received
- Verify JWT token validity
- Verify hub path `/hubs/notifications`
- Ensure client sends `access_token` query parameter
- Check backend logs for SignalR or notification warning logs

## Request update/delete unexpectedly blocked
- Check whether any proposal exists for the request (`Request.HasProposals` guard)

## Attachment deletion issues
- Ensure stored `FileUrl` points under `wwwroot`
- Check write/delete permissions on host file system

---

## 8) Quick Build Verification

From solution root:

- Build should succeed after all changes.
- If migration is pending (for notifications), add/apply migration before runtime testing.
