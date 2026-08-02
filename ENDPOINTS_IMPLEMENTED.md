# Draya API - Implemented Endpoints

## Authentication Module (Module 2)

### 1. POST /api/v1/auth/register/teacher
- **Description:** Teacher self-registration
- **Auth:** None
- **Status:** ✅ Implemented

### 2. POST /api/v1/auth/register/student
- **Description:** Student self-registration
- **Auth:** None
- **Status:** ✅ Implemented

### 3. POST /api/v1/auth/login
- **Description:** Login, returns access + refresh tokens
- **Auth:** None
- **Status:** ✅ Implemented

### 4. POST /api/v1/auth/refresh-token
- **Description:** Exchange refresh token for new access token
- **Auth:** Refresh token in body
- **Status:** ✅ Implemented

### 5. POST /api/v1/auth/logout
- **Description:** Revokes the current refresh token
- **Auth:** Bearer token required
- **Status:** ✅ Implemented

### 6. POST /api/v1/auth/password-reset/request
- **Description:** Request password reset
- **Auth:** None
- **Status:** ✅ Implemented

### 7. POST /api/v1/auth/password-reset/confirm
- **Description:** Confirm password reset with token
- **Auth:** None
- **Status:** ✅ Implemented

### 8. GET /api/v1/auth/me
- **Description:** Returns the caller's own profile
- **Auth:** Bearer token required
- **Status:** ✅ Implemented

---

## Subscription Module (Module 8 - Partial)

### 9. GET /api/v1/subscription/current
- **Description:** Get current subscription (Teacher only)
- **Auth:** Bearer token required (Teacher role)
- **Status:** ✅ Implemented

### 10. GET /api/v1/subscription/usage
- **Description:** Get subscription usage statistics (Teacher only)
- **Auth:** Bearer token required (Teacher role)
- **Status:** ✅ Implemented

---

## Subjects Module (Module 3 - Partial)

### 11. GET /api/v1/subjects
- **Description:** Lookup list for classroom creation (all subjects)
- **Auth:** Bearer token required (any role)
- **Status:** ✅ Implemented

### 12. POST /api/v1/subjects
- **Description:** Teacher creates new subject
- **Auth:** Bearer token required (Teacher role)
- **Status:** ✅ Implemented

---

## Classrooms Module (Module 3 - Core Features)

### 13. POST /api/v1/classrooms
- **Description:** Teacher creates a classroom
- **Auth:** Bearer token required (Teacher role)
- **Status:** ✅ Implemented
- **Features:** Validates subject, checks subscription quota, generates enrollment code

### 14. GET /api/v1/classrooms
- **Description:** List classrooms (Teacher: owned, Student: enrolled)
- **Auth:** Bearer token required
- **Status:** ✅ Implemented (Teacher only currently)
- **Pagination:** Supports page & pageSize query parameters

### 15. GET /api/v1/classrooms/{classroomId}
- **Description:** Get classroom details
- **Auth:** Bearer token required
- **Status:** ✅ Implemented
- **Authorization:** Teachers can access owned classrooms, Students can access enrolled classrooms

### 16. PUT /api/v1/classrooms/{classroomId}
- **Description:** Teacher updates classroom (name, subject, active status)
- **Auth:** Bearer token required (Teacher role, owner only)
- **Status:** ✅ Implemented

### 17. DELETE /api/v1/classrooms/{classroomId}
- **Description:** Teacher deactivates classroom (soft delete)
- **Auth:** Bearer token required (Teacher role, owner only)
- **Status:** ✅ Implemented

### 18. POST /api/v1/classrooms/{classroomId}/regenerate-code
- **Description:** Teacher regenerates enrollment code
- **Auth:** Bearer token required (Teacher role, owner only)
- **Status:** ✅ Implemented

---

## Enrollment Module (Module 3 - Enrollment Features)

### 19. POST /api/v1/classrooms/enroll
- **Description:** Student joins classroom via enrollment code
- **Auth:** Bearer token required (Student role)
- **Status:** ✅ Implemented
- **Features:** Validates code, checks classroom active status, validates quota

### 20. GET /api/v1/classrooms/{classroomId}/students
- **Description:** Teacher views classroom roster (paginated)
- **Auth:** Bearer token required (Teacher role, owner only)
- **Status:** ✅ Implemented
- **Pagination:** Supports page & pageSize query parameters

### 21. DELETE /api/v1/classrooms/{classroomId}/students/{studentId}
- **Description:** Teacher removes student from classroom
- **Auth:** Bearer token required (Teacher role, owner only)
- **Status:** ✅ Implemented
- **Behavior:** Soft delete, preserves all historical data

---

## Summary

**Total Endpoints Implemented:** 21

### By Module:
- **Authentication:** 8 endpoints
- **Subscriptions:** 2 endpoints
- **Subjects:** 2 endpoints
- **Classrooms:** 6 endpoints
- **Enrollment:** 3 endpoints

### By HTTP Method:
- **GET:** 7 endpoints
- **POST:** 11 endpoints
- **PUT:** 1 endpoint
- **DELETE:** 2 endpoints

### Authorization Levels:
- **Public (No Auth):** 4 endpoints
- **Authenticated (Any Role):** 2 endpoints
- **Teacher Only:** 11 endpoints
- **Student Only:** 1 endpoint
- **Role-Specific:** 3 endpoints (Teacher or Student based on context)

---

## Next Modules to Implement (Not Yet Done)

- Materials management
- Exams & question bank
- Grading system
- Reports & analytics
- Payments (Paymob integration)
- Chat & notifications
- Admin panel
