# 📚 Draya API — Learning Materials & Content Integration Guide

This guide details all endpoints, data models, versioning architecture, and download/streaming flows for **Learning Materials & Content** (PDFs, Videos, DOCX, PPTX, Images) on the Draya platform.

---

## 🔑 Authentication Header

All materials endpoints require the `Authorization` Bearer token header:

```http
Authorization: Bearer <access_token>
```

---

## 💡 Architecture Concepts: Versioning & Downloads

### 1. Why do Materials have "Versions"?
In educational platforms, teachers constantly update lesson files (fixing a typo in a worksheet, updating presentation slides, or uploading an edited lecture recording).

* **Stable Material ID**: The `MaterialId` never changes. Student bookmarks, lesson playlists, notes, and Q&A references remain intact across file updates.
* **Zero Downtime**: When a teacher uploads a new version of a video or heavy document, the previous version remains accessible while the new file processes in the background (`Pending` ➔ `Parsed`).
* **Audit & History**: Teachers can view all previous versions (`GET /api/v1/materials/{id}/versions`) and check when updates occurred.
* **AI & Search Indexing**: Vector embeddings and semantic search chunks are tied to version hashes, so updating a document re-indexes only the modified content without corrupting search references.

---

### 2. Can Students Download Materials (PDFs, Docs, Images)?
**Yes!**

* **For Documents (PDF, DOCX, PPTX, Images)**:
  * When fetching materials via `GET /api/v1/classrooms/{id}/materials` or `GET /api/v1/students/materials`, each item includes `currentVersion.fileUrl`.
  * **Web**: Trigger direct browser download or view in embedded PDF readers via `<a href="..." download>`.
  * **Mobile (iOS & Android)**: Load directly in native PDF viewers (`PDFKit`, `PdfRenderer`, `flutter_pdfview`) or save to device storage.
* **For Videos**:
  * Call `GET /api/v1/materials/{materialId}/stream` to receive a secure, temporary streaming URL (`streamUrl`) ready for `<video>` tags, AVPlayer (iOS), or ExoPlayer (Android).

---

## 📋 Supported Material Types & Parsing Statuses

### `materialType` values:
* `"Video"` — Video file (e.g. `.mp4`, `.mov`, `.mkv`). Uploads and processes in background with Cloudinary.
* `"PDF"` — PDF document.
* `"DOCX"` — Word document.
* `"PPTX"` — PowerPoint presentation.
* `"Image"` — Image file (`.png`, `.jpg`, `.jpeg`, `.webp`).

### `parseStatus` values:
* `"Pending"` — Video or heavy asset is currently uploading/processing in the background.
* `"Parsed"` — Material processing is complete and ready for streaming/viewing.
* `"Failed"` — Processing encountered an error (see `errorMessage`).

---

## 1. 📤 Upload Lesson Material (Teacher)

Upload a new learning material file to a specific classroom.

### Endpoint:
```http
POST /api/v1/classrooms/{classroomId}/materials
```

* **Authentication**: Required (`Teacher` role)
* **Content-Type**: `multipart/form-data`

### Form Data Parameters:
| Parameter | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `title` | `string` | **Yes** | Title of the material (e.g., `"Physics Chapter 1 - Vectors"`) |
| `materialType` | `string` | **Yes** | One of: `"Video"`, `"PDF"`, `"DOCX"`, `"PPTX"`, `"Image"` |
| `file` | `File` / `Binary` | **Yes** | The actual file to upload |

### Response (`202 Accepted`):
```json
{
  "materialId": "e3a89e90-53cb-4f30-8be0-b99b5a266ce2",
  "title": "Physics Chapter 1 - Vectors",
  "materialType": "Video",
  "createdAt": "2026-08-16T04:00:00Z",
  "currentVersion": {
    "versionId": "f7685aa2-4c28-4444-9fa2-5883a992a001",
    "versionNumber": 1,
    "fileUrl": null,
    "parseStatus": "Pending",
    "uploadedAt": "2026-08-16T04:00:00Z",
    "errorMessage": null
  }
}
```

---

## 2. 📖 Get Materials by Classroom (Teacher & Student)

Fetch paginated materials uploaded inside a specific classroom.

### Endpoint:
```http
GET /api/v1/classrooms/{classroomId}/materials?page=1&pageSize=20
```

* **Authentication**: Required (`Teacher` or `Student`)
* **Query Parameters**:
  * `page` (default: `1`)
  * `pageSize` (default: `20`)

### Response (`200 OK`):
```json
{
  "items": [
    {
      "materialId": "e3a89e90-53cb-4f30-8be0-b99b5a266ce2",
      "title": "Physics Chapter 1 - Vectors",
      "materialType": "Video",
      "createdAt": "2026-08-16T04:00:00Z",
      "currentVersion": {
        "versionId": "f7685aa2-4c28-4444-9fa2-5883a992a001",
        "versionNumber": 1,
        "fileUrl": "Teacher_Ahmed/Physics_101/video_1",
        "parseStatus": "Parsed",
        "uploadedAt": "2026-08-16T04:00:00Z",
        "errorMessage": null
      }
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

---

## 3. 🎓 Get All Enrolled Materials (Student)

Allows a student to retrieve all materials across **all classrooms they are actively enrolled in**.

### Endpoint:
```http
GET /api/v1/students/materials?page=1&pageSize=20
```
*(Alias: `GET /api/v1/materials/enrolled?page=1&pageSize=20`)*

* **Authentication**: Required (`Student` role)
* **Query Parameters**:
  * `page` (default: `1`)
  * `pageSize` (default: `20`)

### Response (`200 OK`):
```json
{
  "items": [
    {
      "materialId": "e3a89e90-53cb-4f30-8be0-b99b5a266ce2",
      "title": "Physics Chapter 1 - Vectors",
      "materialType": "Video",
      "createdAt": "2026-08-16T04:00:00Z",
      "currentVersion": {
        "versionId": "f7685aa2-4c28-4444-9fa2-5883a992a001",
        "versionNumber": 1,
        "fileUrl": "Teacher_Ahmed/Physics_101/video_1",
        "parseStatus": "Parsed",
        "uploadedAt": "2026-08-16T04:00:00Z",
        "errorMessage": null
      }
    }
  ],
  "totalCount": 12,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

---

## 4. 🔍 Get Single Material Detail

Fetch details and the latest version of a specific material by its ID.

### Endpoint:
```http
GET /api/v1/materials/{materialId}
```

* **Authentication**: Required (`Teacher` or `Student`)

### Response (`200 OK`):
```json
{
  "materialId": "e3a89e90-53cb-4f30-8be0-b99b5a266ce2",
  "title": "Physics Chapter 1 - Vectors",
  "materialType": "Video",
  "createdAt": "2026-08-16T04:00:00Z",
  "currentVersion": {
    "versionId": "f7685aa2-4c28-4444-9fa2-5883a992a001",
    "versionNumber": 1,
    "fileUrl": "Teacher_Ahmed/Physics_101/video_1",
    "parseStatus": "Parsed",
    "uploadedAt": "2026-08-16T04:00:00Z",
    "errorMessage": null
  }
}
```

---

## 5. 🎬 Get Secure Video Streaming URL (Teacher & Student)

Generates a secure, temporary streaming URL for video playback on web/mobile video players.

### Endpoint:
```http
GET /api/v1/materials/{materialId}/stream
```

* **Authentication**: Required (`Teacher` or `Student`)

### Response (`200 OK`):
```json
{
  "provider": "Cloudinary",
  "videoId": "Teacher_Ahmed/Physics_101/video_1",
  "streamUrl": "https://res.cloudinary.com/q89yjswe/video/upload/v1723789000/Teacher_Ahmed/Physics_101/video_1.mp4",
  "expiresAt": "2026-08-16T06:00:00Z"
}
```

> **Tip**: Pass `streamUrl` directly into `<video src="...">` or your mobile video player (AVPlayer on iOS / ExoPlayer on Android).

---

## 6. 🔄 Upload New Material Version (Teacher)

Upload an updated file version for an existing material (e.g. updated syllabus or re-recorded lecture).

### Endpoint:
```http
POST /api/v1/materials/{materialId}/versions
```

* **Authentication**: Required (`Teacher` role)
* **Content-Type**: `multipart/form-data`

### Form Data Parameters:
| Parameter | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `file` | `File` / `Binary` | **Yes** | The updated file |

### Response (`202 Accepted`):
```json
{
  "versionId": "9b12e6c5-84fa-4d56-8a03-2415ab545cc1",
  "versionNumber": 2,
  "fileUrl": null,
  "parseStatus": "Pending",
  "uploadedAt": "2026-08-16T04:15:00Z",
  "errorMessage": null
}
```

---

## 7. 📜 Get Material Version History (Teacher)

View all previous versions of a material.

### Endpoint:
```http
GET /api/v1/materials/{materialId}/versions
```

* **Authentication**: Required (`Teacher` role)

### Response (`200 OK`):
```json
[
  {
    "versionId": "9b12e6c5-84fa-4d56-8a03-2415ab545cc1",
    "versionNumber": 2,
    "fileUrl": "Teacher_Ahmed/Physics_101/video_v2",
    "parseStatus": "Parsed",
    "uploadedAt": "2026-08-16T04:15:00Z",
    "errorMessage": null
  },
  {
    "versionId": "f7685aa2-4c28-4444-9fa2-5883a992a001",
    "versionNumber": 1,
    "fileUrl": "Teacher_Ahmed/Physics_101/video_v1",
    "parseStatus": "Parsed",
    "uploadedAt": "2026-08-16T04:00:00Z",
    "errorMessage": null
  }
]
```

---

## 8. ⏳ Check Version Processing Status (Teacher)

Check whether a background video upload or document processing is complete.

### Endpoint:
```http
GET /api/v1/materials/{materialId}/versions/{versionId}/status
```

* **Authentication**: Required (`Teacher` role)

### Response (`200 OK`):
```json
{
  "versionId": "9b12e6c5-84fa-4d56-8a03-2415ab545cc1",
  "parseStatus": "Parsed",
  "errorMessage": null
}
```

---

## 9. 🗑️ Delete Material (Teacher)

Soft-deletes a material so it is no longer visible to students or teachers.

### Endpoint:
```http
DELETE /api/v1/materials/{materialId}
```

* **Authentication**: Required (`Teacher` role)

### Response:
* **`204 No Content`** (Empty body on successful deletion)
