# 🚀 Draya API — Frontend & Mobile Integration Guide

This guide documents the newly added endpoints for **Frontend (Web)** and **Mobile** developers.

---

## 🔑 Global Authentication Header

All endpoints (unless marked public) require the Bearer token in the `Authorization` header:

```http
Authorization: Bearer <access_token>
```

---

## 1. 🔐 Change Password (In User Profile)

Allows an authenticated user (**Teacher** or **Student**) to change their password directly from their profile/account settings page.

### Endpoint:
```http
POST /api/v1/auth/change-password
```

* **Authentication**: Required (`Teacher` or `Student`)
* **Content-Type**: `application/json`

### Request Body:
```json
{
  "currentPassword": "OldPassword123",
  "newPassword": "NewSecurePassword123",
  "confirmPassword": "NewSecurePassword123"
}
```

### Password Validation Rules:
* Minimum 8 characters.
* At least 1 uppercase letter (`A-Z`).
* At least 1 numeric digit (`0-9`).
* `confirmPassword` must match `newPassword`.

### Responses:
* **`204 No Content`**: Password changed successfully (empty body).
* **`400 Bad Request`**:
  ```json
  {
    "error": {
      "code": "INVALID_CURRENT_PASSWORD",
      "message": "Current password is incorrect.",
      "details": []
    }
  }
  ```
* **`401 Unauthorized`**: Missing or expired access token.

---

## 2. 🖼️ Profile Picture Upload (Cloudinary)

Allows **Students** and **Teachers** to upload and update their profile avatar picture. Files are stored securely on Cloudinary.

### A. Student Avatar Upload
```http
POST /api/v1/students/profile/picture
```
* **Authentication**: Required (`Student`)
* **Content-Type**: `multipart/form-data`

### B. Teacher Avatar Upload
```http
POST /api/v1/teachers/profile/picture
```
* **Authentication**: Required (`Teacher`)
* **Content-Type**: `multipart/form-data`

### Request Form Data:
| Parameter | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `file` | `File` / `Binary` | **Yes** | Image file (e.g. `.png`, `.jpg`, `.jpeg`, `.webp`) |

### Response (`200 OK`):
```json
{
  "profilePictureUrl": "https://res.cloudinary.com/q89yjswe/image/upload/v1723789000/Profiles/Students/c86e0c0a/avatar.jpg"
}
```

> **Note**: The `profilePictureUrl` is also automatically returned in:
> * `GET /api/v1/auth/me` (Profile details for current user)
> * `GET /api/v1/teachers` (List of teachers)
> * `GET /api/v1/teachers/{id}` (Teacher profile details)

---

## 3. 💬 Q&A Classroom Channel Photos & Author Profiles

Questions and replies in the classroom Q&A channel now support image attachments and return complete author profile metadata (`authorName`, `authorRole`, `authorProfilePictureUrl`, and `imageUrl`).

---

### A. Create Question with Photo Attachment

```http
POST /api/v1/classrooms/{classroomId}/questions/with-photo
```

* **Authentication**: Required (Enrolled `Student` or Classroom `Teacher`)
* **Content-Type**: `multipart/form-data`

#### Request Form Data:
| Parameter | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `content` | `string` | **Yes** | Question text |
| `file` | `File` / `Binary` | No | Optional attached screenshot / problem photo |

*(Alternative: You can also use `POST /api/v1/classrooms/{classroomId}/questions` with `application/json` payload `{ "content": "...", "imageUrl": "https://..." }`)*

#### Response (`201 Created`):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "classroomId": "4a117cb8-5b1b-4cf7-9a4f-56bb93bb182b",
  "authorId": "c86e0c0a-d8cb-4720-94d7-ea76d2a71f01",
  "authorName": "Ahmed Ali",
  "authorRole": "Student",
  "authorProfilePictureUrl": "https://res.cloudinary.com/.../avatar.jpg",
  "content": "How do we solve step 3 in this problem?",
  "imageUrl": "https://res.cloudinary.com/.../Questions/problem.jpg",
  "createdAt": "2026-08-16T03:40:00Z",
  "voteCount": 0,
  "replyCount": 0,
  "hasTeacherAnswer": false,
  "hasVoted": false,
  "isAuthor": true
}
```

---

### B. Reply to Question with Photo Attachment

```http
POST /api/v1/classrooms/{classroomId}/questions/{questionId}/replies/with-photo
```

* **Authentication**: Required (Enrolled `Student` or Classroom `Teacher`)
* **Content-Type**: `multipart/form-data`

#### Request Form Data:
| Parameter | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `content` | `string` | **Yes** | Reply explanation text |
| `file` | `File` / `Binary` | No | Optional attached solution diagram / photo |

#### Response (`201 Created`):
```json
{
  "id": "e963c6cf-c99e-4e4b-bb57-19e09d57a66f",
  "questionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "authorId": "5f1a92e2-9bca-4b0d-9e6b-a25e12cb5d01",
  "authorName": "Dr. Mohamed",
  "authorRole": "Teacher",
  "authorProfilePictureUrl": "https://res.cloudinary.com/.../teacher.jpg",
  "content": "Here is the explanation diagram.",
  "imageUrl": "https://res.cloudinary.com/.../Replies/diagram.jpg",
  "createdAt": "2026-08-16T03:42:00Z",
  "isTeacherAnswer": true,
  "isAuthor": true
}
```

---

### C. Fetch Classroom Questions List
```http
GET /api/v1/classrooms/{classroomId}/questions?sortBy=recent&filterBy=all&page=1&pageSize=20
```
* Returns `items` containing `authorName`, `authorRole`, `authorProfilePictureUrl`, and `imageUrl`.

---

## 4. 📚 All Enrolled Materials (For Students)

Allows an enrolled student to fetch all learning materials (videos, PDFs, documents) across all classrooms they are actively enrolled in with pagination.

### Endpoint:
```http
GET /api/v1/students/materials?page=1&pageSize=20
```
*(Alias route: `GET /api/v1/materials/enrolled?page=1&pageSize=20`)*

* **Authentication**: Required (`Student`)
* **Query Parameters**:
  * `page` (default: `1`)
  * `pageSize` (default: `20`)

### Response (`200 OK`):
```json
{
  "items": [
    {
      "materialId": "b47c0ea8-48b2-4d1e-bd04-45aa3e35ccb1",
      "title": "Physics Chapter 1 - Mechanics",
      "materialType": "Video",
      "createdAt": "2026-08-15T12:00:00Z",
      "currentVersion": {
        "versionId": "fa982a0b-1932-4762-b883-9e45136894c2",
        "versionNumber": 1,
        "fileUrl": "Teacher_1/Physics/video_1",
        "parseStatus": "Parsed",
        "uploadedAt": "2026-08-15T12:00:00Z",
        "errorMessage": null
      }
    },
    {
      "materialId": "d891aa12-1111-4f1e-aa04-55aa3e35cc99",
      "title": "Calculus Summary Notes PDF",
      "materialType": "Document",
      "createdAt": "2026-08-14T09:30:00Z",
      "currentVersion": {
        "versionId": "bb112233-1932-4762-b883-9e45136894c2",
        "versionNumber": 1,
        "fileUrl": "Teacher_2/Calculus/notes.pdf",
        "parseStatus": "Parsed",
        "uploadedAt": "2026-08-14T09:30:00Z",
        "errorMessage": null
      }
    }
  ],
  "totalCount": 15,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```
