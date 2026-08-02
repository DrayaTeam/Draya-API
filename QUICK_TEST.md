# Quick Test Guide - Draya API

## Server Running
✅ **API is running at:** http://localhost:5286  
✅ **Swagger UI:** http://localhost:5286/swagger

---

## Quick Manual Tests (Use HTTP Client or Swagger)

### 1. Test Registration (Teacher)

```http
POST http://localhost:5286/api/v1/auth/register/teacher
Content-Type: application/json

{
  "email": "testteacher@example.com",
  "password": "Teacher@123456",
  "fullName": "Test Teacher",
  "phone": "+201234567890"
}
```

**Expected:** 201 Created with tokens

---

### 2. Test Login

```http
POST http://localhost:5286/api/v1/auth/login
Content-Type: application/json

{
  "email": "teacher1@example.com",
  "password": "Teacher@123456"
}
```

**Expected:** 200 OK with access token  
**Copy the `accessToken` from response for next tests**

---

### 3. Test Get Profile (Authenticated)

```http
GET http://localhost:5286/api/v1/auth/me
Authorization: Bearer YOUR_ACCESS_TOKEN_HERE
```

**Expected:** 200 OK with profile data

---

### 4. Test Subscription Endpoint (Teacher Only)

```http
GET http://localhost:5286/api/v1/subscription/current
Authorization: Bearer YOUR_ACCESS_TOKEN_HERE
```

**Expected:** 200 OK (returns null if no subscription)

---

## Test Accounts Created

### Teachers
- **Email:** teacher1@example.com
- **Password:** Teacher@123456
- **ID:** 47c6eb62-8ef4-4cf0-976e-3e77a51e0732

- **Email:** teacher2@example.com
- **Password:** Teacher@123456
- **ID:** 045be2cd-085b-4f0d-ac9d-0a24cc60a3d6

### Students
- **Email:** student1@example.com
- **Password:** Student@123456
- **ID:** cd248e7b-f076-4055-a70b-a06e275d0785

- **Email:** student2@example.com
- **Password:** Student@123456
- **ID:** 28f6888b-8a1a-4224-ae6f-5875d3da442c

---

## Using Swagger UI (Easiest Method)

1. Open browser: http://localhost:5286/swagger
2. Click on any endpoint to expand it
3. Click "Try it out"
4. Fill in the request body
5. Click "Execute"
6. See the response below

### For authenticated endpoints:
1. First, login using `/api/v1/auth/login`
2. Copy the `accessToken` from response
3. Click the "Authorize" button (lock icon) at top right
4. Enter: `Bearer YOUR_TOKEN_HERE`
5. Click "Authorize"
6. Now you can test protected endpoints

---

## Verification Checklist

✅ Server is running  
✅ Database migrations applied  
✅ Can register new users (Teacher & Student)  
✅ Can login with credentials  
✅ Receives JWT access token  
✅ Can access protected endpoints with token  
✅ Authorization by role works (Teacher-only endpoints)  
✅ Duplicate email prevention works (409 Conflict)  
✅ Invalid credentials rejected (401 Unauthorized)  
✅ Invalid email format rejected (400 Bad Request)  
✅ Password reset request works  
✅ Token refresh works  
✅ Logout works  
✅ Profile retrieval works  
✅ Subscription endpoints respond correctly  

---

## All Tests Pass! 🎉

The Draya API is fully functional and ready for frontend integration!
