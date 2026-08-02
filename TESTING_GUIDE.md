# Draya API Testing Guide

This guide will help you run and test the Draya API endpoints.

---

## 🚀 Quick Start

### 1. Start the API Server

```bash
cd "g:\ITI 9M\ITI Graduation Project\Draya\src\Draya.Api"
dotnet run
```

The server will start at:
- HTTP: http://localhost:5286
- HTTPS: https://localhost:7052

Wait for the message: `Now listening on: http://localhost:5286`

---

## 🧪 Testing Methods

You have **4 options** to test the API:

### Option 1: Swagger UI (Recommended - Easiest)

1. **Start the server** (see above)
2. **Open your browser:** http://localhost:5286/swagger
3. **Expand any endpoint** by clicking on it
4. **Click "Try it out"** button
5. **Fill in the request body** with sample data
6. **Click "Execute"**
7. **View the response** below

#### For Protected Endpoints:
1. First, login using `/api/v1/auth/login` endpoint
2. Copy the `accessToken` from the response
3. Click the **"Authorize"** button (🔒 lock icon) at the top right
4. Enter: `Bearer YOUR_ACCESS_TOKEN_HERE`
5. Click **"Authorize"**
6. Now all protected endpoints will include your token automatically

---

### Option 2: HTTP File in VS Code

1. **Install REST Client extension** in VS Code (if not already installed)
2. **Open:** `src/Draya.Api/Draya.Api.Tests.http`
3. **Click "Send Request"** above any request
4. **View response** in the right panel

**Note:** Update the `@accessToken` variable after login to test authenticated endpoints.

---

### Option 3: PowerShell Test Script (Automated)

```powershell
cd "g:\ITI 9M\ITI Graduation Project\Draya"
powershell -ExecutionPolicy Bypass -File test-api.ps1
```

This will automatically test all endpoints and show you:
- ✅ Passed tests in green
- ❌ Failed tests in red
- Test summary at the end

---

### Option 4: cURL Commands

#### Register Teacher
```bash
curl -X POST "http://localhost:5286/api/v1/auth/register/teacher" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"newteacher@example.com\",\"password\":\"Teacher@123456\",\"fullName\":\"New Teacher\",\"phone\":\"+201234567890\"}"
```

#### Login
```bash
curl -X POST "http://localhost:5286/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"teacher1@example.com\",\"password\":\"Teacher@123456\"}"
```

#### Get Profile (replace TOKEN with your access token)
```bash
curl -X GET "http://localhost:5286/api/v1/auth/me" \
  -H "Authorization: Bearer TOKEN"
```

---

## 📝 Test Accounts (Pre-created)

You can use these accounts for testing:

### Teachers
```
Email: teacher1@example.com
Password: Teacher@123456
```

```
Email: teacher2@example.com
Password: Teacher@123456
```

### Students
```
Email: student1@example.com
Password: Student@123456
```

```
Email: student2@example.com
Password: Student@123456
```

---

## 🔍 Available Endpoints

### Authentication Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/v1/auth/register/teacher` | Register new teacher | No |
| POST | `/api/v1/auth/register/student` | Register new student | No |
| POST | `/api/v1/auth/login` | Login user | No |
| POST | `/api/v1/auth/refresh-token` | Refresh access token | No |
| POST | `/api/v1/auth/logout` | Logout user | Yes |
| POST | `/api/v1/auth/password-reset/request` | Request password reset | No |
| POST | `/api/v1/auth/password-reset/confirm` | Confirm password reset | No |
| GET | `/api/v1/auth/me` | Get current user profile | Yes |

### Subscription Endpoints (Teacher Only)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/v1/subscription/current` | Get current subscription | Yes (Teacher) |
| GET | `/api/v1/subscription/usage` | Get usage statistics | Yes (Teacher) |

---

## 📊 Test Scenarios

### Scenario 1: Teacher Registration Flow

1. **Register a new teacher**
   - Endpoint: `POST /api/v1/auth/register/teacher`
   - Expected: 201 Created + tokens

2. **Login with new credentials**
   - Endpoint: `POST /api/v1/auth/login`
   - Expected: 200 OK + tokens

3. **Get profile**
   - Endpoint: `GET /api/v1/auth/me`
   - Expected: 200 OK + teacher profile

4. **Check subscription**
   - Endpoint: `GET /api/v1/subscription/current`
   - Expected: 200 OK (null if no subscription)

---

### Scenario 2: Student Registration Flow

1. **Register a new student**
   - Endpoint: `POST /api/v1/auth/register/student`
   - Include: parent email, date of birth
   - Expected: 201 Created + tokens

2. **Login as student**
   - Endpoint: `POST /api/v1/auth/login`
   - Expected: 200 OK + tokens

3. **Try to access teacher endpoint** (should fail)
   - Endpoint: `GET /api/v1/subscription/current`
   - Expected: 403 Forbidden

---

### Scenario 3: Token Refresh Flow

1. **Login**
   - Save both `accessToken` and `refreshToken`

2. **Use the access token** to make requests

3. **When token expires, refresh it**
   - Endpoint: `POST /api/v1/auth/refresh-token`
   - Send: `{ "refreshToken": "YOUR_REFRESH_TOKEN" }`
   - Expected: New access token + new refresh token

---

### Scenario 4: Password Reset Flow

1. **Request password reset**
   - Endpoint: `POST /api/v1/auth/password-reset/request`
   - Send: `{ "email": "teacher1@example.com" }`
   - Expected: 200 OK

2. **Check logs for reset token** (in development, token is logged)

3. **Confirm password reset**
   - Endpoint: `POST /api/v1/auth/password-reset/confirm`
   - Send: `{ "token": "RESET_TOKEN", "newPassword": "NewPassword@123" }`
   - Expected: 204 No Content

---

## ❌ Expected Error Cases

### Duplicate Email (409 Conflict)
Try registering with an existing email:
```json
POST /api/v1/auth/register/teacher
{
  "email": "teacher1@example.com",
  "password": "Teacher@123456",
  "fullName": "Another Teacher",
  "phone": "+201234567890"
}
```
**Expected:** 409 Conflict

### Invalid Credentials (401 Unauthorized)
Try logging in with wrong password:
```json
POST /api/v1/auth/login
{
  "email": "teacher1@example.com",
  "password": "WrongPassword"
}
```
**Expected:** 401 Unauthorized

### Invalid Email Format (400 Bad Request)
Try registering with invalid email:
```json
POST /api/v1/auth/register/teacher
{
  "email": "not-an-email",
  "password": "Teacher@123456",
  "fullName": "Test Teacher",
  "phone": "+201234567890"
}
```
**Expected:** 400 Bad Request

### Unauthorized Access (401 Unauthorized)
Try accessing protected endpoint without token:
```
GET /api/v1/auth/me
```
**Expected:** 401 Unauthorized

### Forbidden Access (403 Forbidden)
Try accessing teacher-only endpoint with student token:
```
GET /api/v1/subscription/current
Authorization: Bearer STUDENT_TOKEN
```
**Expected:** 403 Forbidden

---

## 🗄️ Database Management

### View Current Migrations
```bash
dotnet ef migrations list --project "src/Draya.Infrastructure" --startup-project "src/Draya.Api"
```

### Apply Migrations (if needed)
```bash
dotnet ef database update --project "src/Draya.Infrastructure" --startup-project "src/Draya.Api"
```

### Reset Database (if needed)
```bash
# Drop database
dotnet ef database drop --project "src/Draya.Infrastructure" --startup-project "src/Draya.Api"

# Recreate and apply migrations
dotnet ef database update --project "src/Draya.Infrastructure" --startup-project "src/Draya.Api"
```

---

## 📖 Additional Resources

- **API Contract:** `docs/API_CONTRACT.md`
- **Architecture:** `docs/ARCHITECTURE.md`
- **Database Schema:** `docs/DATABASE.md`
- **Test Results:** `TEST_RESULTS.md`
- **Quick Test Guide:** `QUICK_TEST.md`
- **Complete Test Summary:** `API_TEST_SUMMARY.md`

---

## 🐛 Troubleshooting

### Server won't start
- Check if port 5286 is already in use
- Ensure database connection string is correct in `appsettings.Development.json`
- Run `dotnet build` to check for compilation errors

### Database errors
- Make sure SQL Server LocalDB is installed
- Run migrations: `dotnet ef database update`
- Check connection string in `appsettings.Development.json`

### 401 Unauthorized errors
- Make sure you're sending the `Authorization: Bearer TOKEN` header
- Check if token has expired (60-minute expiry)
- Use the refresh token endpoint to get a new access token

### 403 Forbidden errors
- Check if you're using the correct role token
- Some endpoints are Teacher-only (subscription endpoints)
- Login with the appropriate user type

---

## ✅ Verification Checklist

Before deploying or continuing development, verify:

- [ ] Server starts without errors
- [ ] Swagger UI is accessible
- [ ] Can register new teacher
- [ ] Can register new student
- [ ] Can login with valid credentials
- [ ] Login rejected with invalid credentials
- [ ] Can access profile with token
- [ ] Token refresh works
- [ ] Password reset request works
- [ ] Duplicate email is rejected
- [ ] Invalid email format is rejected
- [ ] Protected endpoints require authentication
- [ ] Teacher-only endpoints enforce role
- [ ] Logout works

---

## 🎯 Success Criteria

**All tests pass when:**
- ✅ All endpoints return expected status codes
- ✅ Authentication works end-to-end
- ✅ Authorization is properly enforced
- ✅ Validation catches invalid input
- ✅ Error messages are clear and helpful
- ✅ Database operations complete successfully

---

**Happy Testing! 🚀**

For issues or questions, refer to the documentation in the `/docs` folder.
