# Draya Product Backlog (v2.1 — Wallet & Commission Model)

> Updated following the pivot from Subscription Tiers to the Wallet & Commission Model.

---

## Removed Stories (Obsolete)

| Story ID | Title | Reason for Removal |
|---|---|---|
| **US-010** | View Current Subscription Plan | Subscription plans removed |
| **US-011** | View Subscription Usage | Plan usage counters replaced by AI exam usage tracking |
| **US-012** | Enforce Quota Limits on Actions | Classroom/Student/Storage limits removed |
| **US-013** | Notify Teacher on Quota Limit Reached | Subscription quota limits removed |
| **US-093** | SuperAdmin Manages Subscription Plan Catalog | Subscription catalog removed |
| **US-030** | Generate Monthly Payout Statement | Payout statement table removed (wallet is financial source of truth) |
| **US-031** | Teacher Views Payout Statements | Replaced by Wallet & Withdrawal history (US-117, US-118, US-121) |
| **US-032** | SuperAdmin Views All Payout Statements | Replaced by Admin Financial Overview & Withdrawals (US-124, US-125) |
| **US-033** | SuperAdmin Marks Payout Statement as Paid | Replaced by Withdrawal approval/paid flow (US-126, US-127) |

---

## Modified Stories

### US-025 — Teacher Sets Classroom Pricing
- **User Story**: As a teacher, I want to set a price for my classroom, so that students pay to enroll.
- **Acceptance Criteria**:
  - `Price` must be `>= 0` (0 = free).
  - Currency is EGP.
  - Changing price only affects future enrollments (snapshotted at checkout time).
  - *Dev Note*: The commission split (US-029) happens at payment time, not when price is set.

### US-029 — System Processes Paymob Webhook
- **User Story**: As a system, I want to process Paymob payment webhooks, so that enrollments and wallet balances are updated automatically and idempotently.
- **Acceptance Criteria**:
  - Validates HMAC signature on webhook payload.
  - On successful payment for `Purpose: ClassroomEnrollment`:
    - Snapshots `CommissionPercent` from `PlatformSetting.PlatformCommissionPercent`.
    - Computes `CommissionAmount` (`GrossAmount × CommissionPercent / 100`) and `TeacherAmount` (`GrossAmount − CommissionAmount`).
    - Snapshots both onto `PaymentTransaction`.
    - Creates `WalletTransaction` (`Type: ClassroomEarning`, `Amount: +TeacherAmount`, `BalanceType: Earned`).
    - Increases `TeacherWallet.EarnedBalance` by `TeacherAmount`.
    - Finalizes `Enrollment` row.
    - All steps execute atomically in a single database transaction.
  - Webhook processing MUST be idempotent — duplicate webhooks must never double-credit wallet balance or duplicate enrollments.

### US-034 — SuperAdmin Marks Transaction as Refunded
- **User Story**: As a SuperAdmin, I want to mark a transaction as refunded, so that customer refunds are tracked and balances reversed.
- **Acceptance Criteria**:
  - Refunding a `Paid` classroom transaction creates a compensating `WalletTransaction` (`Type: Refund`, `Amount: -TeacherAmount`, `BalanceType: Earned`).
  - Decreases `TeacherWallet.EarnedBalance` by `TeacherAmount`.
  - If resulting `EarnedBalance` would drop below 0, flag wallet for admin attention rather than silently allowing negative balance or rewriting history.

### US-048 — Teacher Requests AI-Generated Exam
- **User Story**: As a teacher, I want to generate an AI exam, using my monthly free quota or purchasing generations with my wallet balance.
- **Acceptance Criteria**:
  - Checks `UsageCounter.FreeExamsUsed` for current month against `PlatformSetting.FreeMonthlyAIExamQuota`.
  - **Under Quota**: Generates exam, increments `FreeExamsUsed`, no charge.
  - **At/Over Quota**: Checks `TeacherWallet` combined balance (`EarnedBalance` + `PurchasedBalance`) against `PlatformSetting.AIExamPrice`.
    - Spends `EarnedBalance` first, then `PurchasedBalance` for remainder.
    - Insufficient combined balance returns `INSUFFICIENT_BALANCE` error and blocks generation.
  - Charging and counter increments occur ONLY after successful exam generation.
  - Creates `WalletTransaction` (`Type: AIExamCharge`, `Amount: -price`, split `Earned`/`Purchased`) and increments `PaidExamsGenerated` / `TotalExamsGenerated`.

---

## Teacher Wallet & Billing Stories

| Story ID | User Story | Acceptance Criteria | Development Tasks | Suggested Owner |
|---|---|---|---|---|
| **US-117 — View Wallet Balance** | As a teacher, I want to see my Earned and Purchased balances separately, so that I understand what I can withdraw versus what's spending credit. | - Returns `EarnedBalance` and `PurchasedBalance` from `TeacherWallet` as two distinct numbers.<br>- UI clearly labels withdrawable vs non-withdrawable balance. | - Build wallet-balance endpoint<br>- Build balance display UI<br>- Write unit tests | Backend + Frontend + Mobile |
| **US-118 — View Wallet Transaction History** | As a teacher, I want to see a full history of my wallet movements, so that I understand where my balance came from and went. | - Returns paginated `WalletTransaction` rows for the teacher, newest first.<br>- Shows type, amount, balance type, reference ID, description in plain language. | - Build ledger endpoint<br>- Build transaction history UI<br>- Write unit tests | Backend + Frontend |
| **US-119 — Teacher Tops Up Balance** | As a teacher, I want to add money to my Purchased balance, so that I can pay for AI exam generations beyond my free quota. | - Teacher chooses top-up amount, redirected to Paymob checkout.<br>- Webhook confirms payment: `PurchasedBalance` increases by paid amount, `WalletTransaction` (`Type: TeacherTopUp`) created.<br>- Top-up money never touches `EarnedBalance`.<br>- Idempotent webhook handling. | - Support `Purpose: TeacherTopUp` on `PaymentTransaction`<br>- Build top-up checkout endpoint<br>- Extend webhook handler<br>- Build top-up UI<br>- Integration tests | Backend + Frontend + Mobile |
| **US-120 — Teacher Requests Withdrawal** | As a teacher, I want to request a withdrawal of my eligible earnings, so that I get paid. | - Request up to current `EarnedBalance`, never more.<br>- Creates `WithdrawalRequest` (`Status: Pending`).<br>- Requested amount reserved/deducted from available earned balance immediately to prevent double-withdrawal. | - Build withdrawal endpoint<br>- Implement balance reservation<br>- Build request UI<br>- Concurrency unit tests | Backend + Frontend |
| **US-121 — Teacher Views Withdrawal History** | As a teacher, I want to see past and pending withdrawal requests, so that I can track payout status. | - Returns paginated `WithdrawalRequest` rows with current status. | - Build withdrawal history endpoint<br>- Build history UI<br>- Unit tests | Backend + Frontend |
| **US-122 — Teacher Manages Payout Account** | As a teacher, I want to provide my bank or mobile wallet details, so that admin knows where to send payouts. | - Teacher can add/edit `TeacherPayoutAccount` (Bank Account or Mobile Wallet).<br>- Only destination identifiers collected (IBAN, wallet phone number) — no sensitive card numbers or credentials. | - Build payout account CRUD endpoints<br>- Build payout account UI<br>- Unit tests | Backend + Frontend |

---

## Platform Administration (Financial) Stories

| Story ID | User Story | Acceptance Criteria | Development Tasks | Suggested Owner |
|---|---|---|---|---|
| **US-123 — SuperAdmin Manages Platform Settings** | As a SuperAdmin, I want to configure AI exam pricing, free quota, and commission percentage, so that values are configurable. | - Admin updates `AIExamPrice`, `FreeMonthlyAIExamQuota`, `PlatformCommissionPercent` in `PlatformSetting`.<br>- Applies to future transactions only — never recalculates historical rows.<br>- Audit fields updated (`UpdatedAt`, `UpdatedByAdminId`). | - Build single-row settings endpoints<br>- Build admin settings UI<br>- Audit log integration<br>- Unit tests | Backend + Frontend |
| **US-124 — SuperAdmin Views Financial Overview** | As a SuperAdmin, I want a platform financial dashboard, so that I can monitor revenue, commission, and payout obligations. | - Displays total classroom revenue, total commission collected, total teacher earnings, outstanding earned balance, total top-ups, total AI exam charges. | - Build financial aggregation endpoint<br>- Build admin dashboard UI<br>- Unit tests | Backend + Frontend |
| **US-125 — SuperAdmin Reviews Withdrawal Requests** | As a SuperAdmin, I want to review pending withdrawal requests, so that I can process teacher payouts. | - Lists `WithdrawalRequest` rows filterable by status.<br>- Shows associated `TeacherPayoutAccount` details alongside request. | - Build admin withdrawal listing endpoint<br>- Build admin review UI<br>- Unit tests | Backend + Frontend |
| **US-126 — SuperAdmin Approves/Rejects Withdrawal** | As a SuperAdmin, I want to approve or reject a withdrawal request, so that only valid requests proceed. | - Approve moves `Status: Pending → Approved`.<br>- Reject moves to `Rejected`, requires `RejectionReason`, reverses balance reservation so teacher can request again. | - Build approve/reject endpoints<br>- Build review UI with reason field<br>- Unit tests | Backend + Frontend |
| **US-127 — SuperAdmin Marks Withdrawal as Paid** | As a SuperAdmin, I want to mark a withdrawal as paid after manually transferring money, so that records reflect reality. | - Only callable on `Approved` request.<br>- Sets `Status: Paid`, `ProcessedAt`, optional `AdminNote`.<br>- Creates `WalletTransaction` (`Type: Withdrawal`, `Amount: -amount`, `BalanceType: Earned`). | - Build mark-paid endpoint<br>- Build admin action UI<br>- Log to AuditLog<br>- Unit tests | Backend + Frontend |
| **US-128 — SuperAdmin Makes Manual Balance Adjustment** | As a SuperAdmin, I want to manually adjust a teacher's wallet balance in exceptional cases, so that support issues are resolved cleanly. | - Every adjustment creates `WalletTransaction` (`Type: Adjustment`) with required reason.<br>- Logged to AuditLog and wallet ledger. | - Build adjustment endpoint with reason<br>- Build admin UI with confirmation<br>- Unit tests | Backend + Frontend |
