## 📚 Content & Materials
**Route Prefix:** `/api/v1/classrooms/{classroomId}/materials` (upload + list) and `/api/v1/materials` (everything scoped to one material)

### 8.1 Upload Lesson Material
* **Endpoint:** `POST /api/v1/classrooms/{classroomId}/materials`
* **Auth:** `Teacher` (must own the classroom)
* **Request Body:** `multipart/form-data`

| Field | Type | Required | Notes |
|---|---|---|---|
| `title` | string | Yes | |
| `materialType` | string | Yes | `PDF \| DOCX \| PPTX \| Video \| Image` |
| `file` | binary | Yes | |

* **Response:** `202 Accepted`
```json
{
  "materialId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Chapter 3 - Newton's Laws",
  "materialType": "PDF",
  "currentVersion": {
    "versionId": "9c858901-8a57-4791-81fe-4c455b099bc9",
    "versionNumber": 1,
    "fileUrl": "https://drayastorage.blob.core.windows.net/materials/...",
    "parseStatus": "Pending",
    "uploadedAt": "2026-08-13T10:00:00Z"
  },
  "createdAt": "2026-08-13T10:00:00Z"
}
```
* **Errors:** `415` unsupported file type. Note: no plan-based storage quota (`MaxStorageMB`) check — subscriptions are removed. If a flat platform-wide upload size cap is wanted, that's a separate `PlatformSetting` decision, not built here by default.

### 8.2 Get Classroom Materials
* **Endpoint:** `GET /api/v1/classrooms/{classroomId}/materials?page=1&pageSize=20`
* **Auth:** `Teacher` (owner) or `Student` (enrolled)
* **Response:** `200 OK` — paginated list of `MaterialDto` (same shape as `currentVersion` above, one entry per material)

### 8.3 Get Material Detail
* **Endpoint:** `GET /api/v1/materials/{materialId}`
* **Auth:** `Teacher` (owner) or `Student` (enrolled in the parent classroom)
* **Response:** `200 OK` — material info plus its current version, same shape as `8.1`'s response body

### 8.4 Upload New Material Version
* **Endpoint:** `POST /api/v1/materials/{materialId}/versions`
* **Auth:** `Teacher` (owner)
* **Request Body:** `multipart/form-data`

| Field | Type | Required |
|---|---|---|
| `file` | binary | Yes |

* **Response:** `202 Accepted` — new `MaterialVersionDto`, same shape as the `currentVersion` object above
* **Note:** existing exams keep referencing the prior version's chunks automatically — this endpoint must never touch anything already generated from an older version.

### 8.5 Get Material Version History
* **Endpoint:** `GET /api/v1/materials/{materialId}/versions`
* **Auth:** `Teacher` (owner)
* **Response:** `200 OK`
```json
[
  { "versionId": "...", "versionNumber": 2, "parseStatus": "Parsed", "uploadedAt": "2026-08-13T12:00:00Z" },
  { "versionId": "...", "versionNumber": 1, "parseStatus": "Parsed", "uploadedAt": "2026-08-01T09:00:00Z" }
]
```

### 8.6 Get Version Parse Status
* **Endpoint:** `GET /api/v1/materials/{materialId}/versions/{versionId}/status`
* **Auth:** `Teacher` (owner)
* **Response:** `200 OK`
```json
{ "versionId": "...", "parseStatus": "Failed", "errorMessage": "Could not extract text — file may be a scanned image without OCR text layer." }
```
* `parseStatus` is one of `Pending | Parsed | Failed`. Poll this **or** listen for `MaterialParsed` over SignalR — build both, don't make polling the only path (mobile background app state makes SignalR alone unreliable, and a closed tab makes polling-only feel broken).

### 8.7 Soft-Delete Material
* **Endpoint:** `DELETE /api/v1/materials/{materialId}`
* **Auth:** `Teacher` (owner)
* **Response:** `204 No Content`
* **Note:** soft delete only. Deleted material disappears from students' view and from future exam-generation source selection, but stays in the database — don't cascade-delete `MaterialChunk` rows out of Qdrant destructively without a plan for what happens to exams already generated from them (they should keep working, per the versioning principle above).

### 8.8 Get Video Streaming URL
* **Endpoint:** `GET /api/v1/materials/{materialId}/stream`
* **Auth:** `Teacher` (owner) or `Student` (enrolled)
* **Response:** `200 OK`
```json
{ "streamUrl": "https://drayastorage.blob.core.windows.net/videos/...?sv=2024-...&sig=...", "expiresAt": "2026-08-13T11:00:00Z" }
```
* **Errors:** `422` if `materialType` isn't `Video`.
* **Note:** the returned URL is short-lived (SAS token expiry) — don't cache it client-side beyond `expiresAt`, request a fresh one each time playback starts.

---

## What's deliberately *not* an endpoint here

Chunking, embedding generation, and metadata tagging (RAG pipeline) run **automatically** in the background once `parseStatus` moves past `Pending` — there's no endpoint to trigger them manually, and there shouldn't be one. If you find yourself wanting a "re-chunk this" button beyond re-uploading a new version, that's a sign something in the pipeline needs fixing, not a missing feature.
