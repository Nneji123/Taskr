## Authentication

This API uses **JWT Bearer** authentication. Every authenticated request must include an `Authorization` header:

```
Authorization: Bearer <your_access_token_here>
```

Get an access token by calling `POST /v1/auth/login` with your email and password. Use the returned `refreshToken` to obtain new tokens via `POST /v1/auth/refresh` (rotated on use, with reuse detection).

### Auth Endpoints

| Endpoint | Description |
|---|---|
| `POST /v1/auth/register` | Create a new account |
| `POST /v1/auth/login` | Exchange credentials for access + refresh tokens |
| `POST /v1/auth/refresh` | Rotate refresh token for a new access + refresh pair |
| `GET /v1/auth/me` | Get the currently authenticated user |
| `POST /v1/auth/password-reset` | Request a 6-digit OTP via email |
| `POST /v1/auth/password-reset/confirm` | Submit OTP + new password |
| `POST /v1/auth/change-password` | Authenticated password change (requires old password) |

## File Fields and Uploads

All file fields in the API (such as `avatarUrl`, `coverImageUrl`, `attachments`) expect **URLs** as values, not file objects. To upload a file:

1. **Upload your file** using the file upload endpoint:
   - **Endpoint:** `POST /v1/files`
   - **Authentication:** Requires Bearer token
   - **Content-Type:** `multipart/form-data`
   - **Body:** Form data with `files` field (single or multiple files)
   - **Max size:** 10 MB per file
   - **Allowed types:** `image/jpeg`, `image/png`, `image/gif`, `image/webp`, `application/pdf`

2. **Use the returned URL** in the field of your choice. URLs are validated server-side to ensure they were uploaded through this API.

### Example File Upload Request

```bash
curl -X POST https://api.taskr.com/v1/files \
  -H "Authorization: Bearer <token>" \
  -F "files=@/path/to/image.jpg"
```

### Example File Upload Response

```json
{
  "success": true,
  "message": "Successfully uploaded 1 file(s).",
  "data": {
    "id": "abc123",
    "url": "https://cdn.example.com/uploads/abc123_image.jpg",
    "originalFilename": "image.jpg",
    "fileSize": 12345,
    "fileSizeDisplay": "12.1 KB",
    "contentType": "image/jpeg",
    "createdAt": "2026-07-01T13:00:00Z"
  }
}
```

### URL Validation

When a URL is submitted in a file field (e.g. `coverImageUrl`), the API:
- Verifies the URL format
- Confirms the host is a trusted storage provider (S3, Cloudinary, or local)
- Performs an HTTP `HEAD` request to confirm accessibility
- Validates the `Content-Type` is an allowed image/PDF type
- Rejects files larger than 10 MB

This prevents users from setting arbitrary external URLs as their avatar/cover image.

## Projects and Tasks

| Resource | Endpoints |
|---|---|
| **Projects** | `GET /v1/projects`, `POST /v1/projects`, `GET /v1/projects/{id}`, `PATCH /v1/projects/{id}`, `DELETE /v1/projects/{id}` |
| **Tasks** | `GET /v1/projects/{projectId}/tasks`, `POST /v1/projects/{projectId}/tasks`, `GET /v1/tasks/{id}`, `PATCH /v1/tasks/{id}`, `DELETE /v1/tasks/{id}` |

All resources are **owner-scoped**: you can only read, update, or delete resources you own.

## Pagination

List endpoints support pagination via query parameters:

| Parameter | Default | Description |
|---|---|---|
| `page` | `1` | Page number (1-indexed) |
| `pageSize` | `20` | Items per page (max 100) |
| `search` | — | Full-text search on the name/title field |
| `startDate` | — | Filter items created on/after this date (ISO 8601) |
| `endDate` | — | Filter items created on/before this date (ISO 8601) |
| `sort` | — | Sort field (e.g. `-createdAt` for descending) |

### Paginated Response Format

```json
{
  "success": true,
  "message": "Operation successful",
  "data": {
    "items": [ ... ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 42,
    "totalPages": 3,
    "hasNext": true,
    "hasPrevious": false
  },
  "errors": null
}
```

## Response Format

All responses use a standard envelope:

```json
{
  "success": true,
  "message": "Operation successful",
  "data": { ... },
  "errors": null
}
```

| Field | Description |
|---|---|
| `success` | `true` for success, `false` for errors |
| `message` | Human-readable status message |
| `data` | Response payload (any type, or `null` on errors) |
| `errors` | Field-level errors (validation) or `null` |

## Status Codes

| Code | Meaning |
|---|---|
| `200` | Success |
| `201` | Created |
| `204` | No content |
| `400` | Bad request (malformed JSON, missing required fields) |
| `401` | Unauthorized (missing or invalid token) |
| `403` | Forbidden (insufficient permissions) |
| `404` | Not found |
| `409` | Conflict (duplicate email, etc.) |
| `422` | Unprocessable entity (validation failed) |
| `429` | Too many requests (rate limited) |
| `500` | Internal server error (unhandled exception) |

## Rate Limiting

| Policy | Limit | Applied to |
|---|---|---|
| `auth-strict` | 50 / 5 min per IP | Register, login, refresh, password reset |
| `api-default` | 100 / min per user | All read endpoints (`GET`) |
| `write-strict` | 30 / min per user | All write endpoints (`POST`, `PATCH`, `DELETE`) |

## CLI Commands

Administrative commands are run via the same Docker image:

```bash
# List available commands
docker exec taskr-api-1 dotnet API.dll cli

# Seed default admin user
docker exec taskr-api-1 dotnet API.dll cli seed:admin
```

## Support

- **Email:** support@taskr.com
- **Version:** v1.0.0
