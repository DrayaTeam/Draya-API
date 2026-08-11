# Draya API - Implemented Endpoints

## Authentication Module

### 1. POST /api/v1/auth/register/teacher
- **Description:** Teacher self-registration (automatically initializes `TeacherWallet`)
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

## Classrooms Module

### 9. GET /api/v1/subjects
- **Description:** List all available subjects
- **Auth:** Bearer token required
- **Status:** ✅ Implemented

### 10. POST /api/v1/subjects
- **Description:** Create a new subject (Teacher role)
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 11. POST /api/v1/classrooms
- **Description:** Create classroom (Teacher role)
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 12. GET /api/v1/classrooms
- **Description:** Get teacher's classrooms (Teacher role)
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 13. GET /api/v1/classrooms/{id}
- **Description:** Get classroom details
- **Auth:** Bearer token required
- **Status:** ✅ Implemented

### 14. PUT /api/v1/classrooms/{id}
- **Description:** Update classroom name/price (Teacher role)
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 15. POST /api/v1/classrooms/{id}/deactivate
- **Description:** Soft-deactivate a classroom (Teacher role)
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 16. POST /api/v1/classrooms/{id}/regenerate-code
- **Description:** Regenerate enrollment code (Teacher role)
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 17. POST /api/v1/classrooms/enroll
- **Description:** Enroll student via enrollment code (Student role)
- **Auth:** Bearer token required (Student)
- **Status:** ✅ Implemented

### 18. GET /api/v1/classrooms/{id}/roster
- **Description:** Get classroom student roster (Teacher role)
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 19. DELETE /api/v1/classrooms/{id}/students/{studentId}
- **Description:** Remove student from classroom (Teacher role)
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

---

## Teacher Wallet & Billing Module

### 20. GET /api/v1/wallet/balance (US-117)
- **Description:** View Earned balance, Purchased balance, and Available Earned balance
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 21. GET /api/v1/wallet/transactions (US-118)
- **Description:** View paginated wallet ledger history
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 22. POST /api/v1/wallet/topup (US-119)
- **Description:** Initiate Paymob checkout for top-up of Purchased balance
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 23. POST /api/v1/wallet/withdrawals (US-120)
- **Description:** Request withdrawal of available Earned balance
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 24. GET /api/v1/wallet/withdrawals (US-121)
- **Description:** View teacher's own withdrawal request history
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 25. GET /api/v1/wallet/payout-accounts (US-122)
- **Description:** Get list of payout accounts (Bank / Mobile Wallet)
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 26. POST /api/v1/wallet/payout-accounts (US-122)
- **Description:** Add new payout account
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 27. PUT /api/v1/wallet/payout-accounts/{id} (US-122)
- **Description:** Update existing payout account
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

### 28. DELETE /api/v1/wallet/payout-accounts/{id} (US-122)
- **Description:** Delete payout account
- **Auth:** Bearer token required (Teacher)
- **Status:** ✅ Implemented

---

## Payments & Paymob Webhook Module

### 29. POST /api/v1/payments/webhook (US-029)
- **Description:** Process Paymob webhook (idempotent, computes commission split, credits wallet, finalizes enrollment / top-up)
- **Auth:** Public / HMAC Verified
- **Status:** ✅ Implemented

### 30. POST /api/v1/payments/{id}/refund (US-034)
- **Description:** Refund payment transaction & reverse teacher earning
- **Auth:** Bearer token required (SuperAdmin)
- **Status:** ✅ Implemented

---

## Platform Financial Administration Module

### 31. GET /api/v1/admin/financial/settings (US-123)
- **Description:** View platform settings (AI Exam price, free quota, commission percent)
- **Auth:** Bearer token required (SuperAdmin)
- **Status:** ✅ Implemented

### 32. PUT /api/v1/admin/financial/settings (US-123)
- **Description:** Update platform settings
- **Auth:** Bearer token required (SuperAdmin)
- **Status:** ✅ Implemented

### 33. GET /api/v1/admin/financial/overview (US-124)
- **Description:** Financial overview dashboard aggregations
- **Auth:** Bearer token required (SuperAdmin)
- **Status:** ✅ Implemented

### 34. GET /api/v1/admin/financial/withdrawals (US-125)
- **Description:** Review all withdrawal requests with teacher details & payout accounts
- **Auth:** Bearer token required (SuperAdmin)
- **Status:** ✅ Implemented

### 35. POST /api/v1/admin/financial/withdrawals/{id}/approve (US-126)
- **Description:** Approve pending withdrawal request
- **Auth:** Bearer token required (SuperAdmin)
- **Status:** ✅ Implemented

### 36. POST /api/v1/admin/financial/withdrawals/{id}/reject (US-126)
- **Description:** Reject withdrawal request and restore balance
- **Auth:** Bearer token required (SuperAdmin)
- **Status:** ✅ Implemented

### 37. POST /api/v1/admin/financial/withdrawals/{id}/mark-paid (US-127)
- **Description:** Mark approved withdrawal as paid & create ledger entry
- **Auth:** Bearer token required (SuperAdmin)
- **Status:** ✅ Implemented

### 38. POST /api/v1/admin/financial/adjustments (US-128)
- **Description:** Manual wallet balance adjustment with reason audit trail
- **Auth:** Bearer token required (SuperAdmin)
- **Status:** ✅ Implemented
