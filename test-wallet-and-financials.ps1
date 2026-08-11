# Draya API - Wallet, Commission & Financial Admin Test Suite

$baseUrl = "http://localhost:5286"
$teacherToken = ""
$teacherId = ""
$payoutAccountId = ""
$topUpTxId = ""
$withdrawalId = ""

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Draya API - Wallet and Financial Model Test Suite" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

function Run-Test {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [int]$ExpectedStatus = 200
    )

    Write-Host "[$Method] $Name" -ForegroundColor Yellow
    Write-Host "  URL: $Url" -ForegroundColor Gray

    $params = @{
        Uri = $Url
        Method = $Method
        Headers = $Headers
        ContentType = "application/json"
    }

    if ($null -ne $Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }

    try {
        $response = Invoke-WebRequest @params -UseBasicParsing
        $statusCode = [int]$response.StatusCode
        $content = $null
        if ($response.Content) {
            $content = $response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
        }

        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  [PASS] Status: $statusCode" -ForegroundColor Green
            Write-Host ""
            return @{ Success = $true; StatusCode = $statusCode; Content = $content }
        } else {
            Write-Host "  [FAIL] Expected: $ExpectedStatus, Got: $statusCode" -ForegroundColor Red
            Write-Host ""
            return @{ Success = $false; StatusCode = $statusCode; Content = $content }
        }
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  [PASS] Expected status: $ExpectedStatus" -ForegroundColor Green
            Write-Host ""
            return @{ Success = $true; StatusCode = $statusCode; Content = $null }
        } else {
            Write-Host "  [FAIL] Expected: $ExpectedStatus, Got: $statusCode" -ForegroundColor Red
            Write-Host ""
            return @{ Success = $false; StatusCode = $statusCode; Content = $null }
        }
    }
}

# 1. Register Teacher
$teacherEmail = "teacher_wallet_" + [Guid]::NewGuid().ToString().Substring(0,8) + "@draya.com"
$regTeacher = Run-Test -Name "Register Teacher" -Method "POST" -Url "$baseUrl/api/v1/auth/register/teacher" -Body @{
    email = $teacherEmail
    password = "Password123!"
    fullName = "Test Wallet Teacher"
    phone = "+201012345678"
} -ExpectedStatus 201

if ($regTeacher.Success) {
    $teacherToken = $regTeacher.Content.accessToken
    Write-Host "Teacher Token obtained successfully!" -ForegroundColor Cyan
    Write-Host ""
}

$teacherHeaders = @{ "Authorization" = "Bearer $teacherToken" }

# 2. US-117: Get Wallet Balance
Run-Test -Name "US-117: Get Initial Wallet Balance" -Method "GET" -Url "$baseUrl/api/v1/wallet/balance" -Headers $teacherHeaders -ExpectedStatus 200

# 3. US-122: Create Payout Account (accountType: 0 = BankAccount)
$createAccount = Run-Test -Name "US-122: Add Payout Account (Bank)" -Method "POST" -Url "$baseUrl/api/v1/wallet/payout-accounts" -Headers $teacherHeaders -Body @{
    accountType = 0
    accountName = "CIB Savings Account"
    accountIdentifier = "EG1234567890123456789012345"
    isDefault = $true
} -ExpectedStatus 201

if ($createAccount.Success) {
    $payoutAccountId = $createAccount.Content.id
}

# 4. US-122: Get Payout Accounts
Run-Test -Name "US-122: Get Payout Accounts" -Method "GET" -Url "$baseUrl/api/v1/wallet/payout-accounts" -Headers $teacherHeaders -ExpectedStatus 200

# 5. US-119: Initiate Top-Up Checkout
$topUp = Run-Test -Name "US-119: Initiate Top-Up (100 EGP)" -Method "POST" -Url "$baseUrl/api/v1/wallet/topup" -Headers $teacherHeaders -Body @{
    amount = 100.00
} -ExpectedStatus 200

if ($topUp.Success) {
    $topUpTxId = $topUp.Content.transactionId
    Write-Host "Paymob Checkout URL generated: $($topUp.Content.checkoutUrl)" -ForegroundColor Green
    Write-Host ""
}

# 6. US-029: Process Webhook for Top-Up
if ($topUpTxId) {
    Run-Test -Name "US-029: Process Paymob Webhook for Top-Up" -Method "POST" -Url "$baseUrl/api/v1/payments/webhook" -Body @{
        paymentTransactionId = $topUpTxId
        isSuccess = $true
        rawPayload = "TopUp Test Payload"
    } -ExpectedStatus 200
}

# 7. US-117: Check Balance after Top-Up
Run-Test -Name "US-117: Check Wallet Balance after Top-Up" -Method "GET" -Url "$baseUrl/api/v1/wallet/balance" -Headers $teacherHeaders -ExpectedStatus 200

# 8. US-118: Get Wallet Transaction History
$txUrl = "$baseUrl/api/v1/wallet/transactions?pageNumber=1&pageSize=10"
Run-Test -Name "US-118: View Wallet Transaction History" -Method "GET" -Url $txUrl -Headers $teacherHeaders -ExpectedStatus 200

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Teacher Wallet & Paymob Sandbox Endpoint Tests Completed!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
