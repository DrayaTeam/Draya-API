# Draya API Test Script
# This script tests all the implemented endpoints

$baseUrl = "http://localhost:5286"
$script:accessToken = ""
$script:refreshToken = ""
$script:studentToken = ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Draya API Endpoint Testing" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to make HTTP requests and display results
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [int]$ExpectedStatus = 200
    )
    
    Write-Host "Testing: $Name" -ForegroundColor Yellow
    Write-Host "  Method: $Method $Url" -ForegroundColor Gray
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
        }
        
        if ($Body -ne $null) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
            Write-Host "  Request Body:" -ForegroundColor Gray
            Write-Host "    $($params.Body)" -ForegroundColor DarkGray
        }
        
        $response = Invoke-WebRequest @params -UseBasicParsing
        $statusCode = $response.StatusCode
        $content = $response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
        
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  ✓ PASSED - Status: $statusCode" -ForegroundColor Green
        } else {
            Write-Host "  ✗ FAILED - Expected: $ExpectedStatus, Got: $statusCode" -ForegroundColor Red
        }
        
        if ($content) {
            Write-Host "  Response:" -ForegroundColor Gray
            Write-Host "    $($content | ConvertTo-Json -Depth 2)" -ForegroundColor DarkGray
        }
        
        return @{
            Success = ($statusCode -eq $ExpectedStatus)
            StatusCode = $statusCode
            Content = $content
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $errorContent = ""
        
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $errorContent = $reader.ReadToEnd()
            $reader.Close()
        }
        catch { }
        
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  ✓ PASSED - Status: $statusCode (Error as expected)" -ForegroundColor Green
        } else {
            Write-Host "  ✗ FAILED - Expected: $ExpectedStatus, Got: $statusCode" -ForegroundColor Red
        }
        
        if ($errorContent) {
            Write-Host "  Error Response:" -ForegroundColor Gray
            Write-Host "    $errorContent" -ForegroundColor DarkGray
        } else {
            Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        return @{
            Success = ($statusCode -eq $ExpectedStatus)
            StatusCode = $statusCode
            Content = $null
        }
    }
    finally {
        Write-Host ""
    }
}

# Test counters
$script:totalTests = 0
$script:passedTests = 0
$script:failedTests = 0

function Update-TestResults {
    param([bool]$Success)
    $script:totalTests++
    if ($Success) { $script:passedTests++ } else { $script:failedTests++ }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "1. AUTHENTICATION TESTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Register Teacher
$result = Test-Endpoint -Name "Register Teacher 1" -Method POST -Url "$baseUrl/api/v1/auth/register/teacher" -Body @{
    email = "teacher1@example.com"
    password = "Teacher@123456"
    fullName = "John Doe Teacher"
    phone = "+201234567890"
} -ExpectedStatus 201
Update-TestResults -Success $result.Success

# Test 2: Register Another Teacher
$result = Test-Endpoint -Name "Register Teacher 2" -Method POST -Url "$baseUrl/api/v1/auth/register/teacher" -Body @{
    email = "teacher2@example.com"
    password = "Teacher@123456"
    fullName = "Jane Smith Teacher"
    phone = "+201234567891"
} -ExpectedStatus 201
Update-TestResults -Success $result.Success

# Test 3: Register Student
$result = Test-Endpoint -Name "Register Student 1" -Method POST -Url "$baseUrl/api/v1/auth/register/student" -Body @{
    email = "student1@example.com"
    password = "Student@123456"
    fullName = "Alice Johnson Student"
    parentGuardianEmail = "parent1@example.com"
    dateOfBirth = "2010-05-15"
} -ExpectedStatus 201
Update-TestResults -Success $result.Success

# Test 4: Register Another Student
$result = Test-Endpoint -Name "Register Student 2" -Method POST -Url "$baseUrl/api/v1/auth/register/student" -Body @{
    email = "student2@example.com"
    password = "Student@123456"
    fullName = "Bob Williams Student"
    parentGuardianEmail = "parent2@example.com"
    dateOfBirth = "2011-08-22"
} -ExpectedStatus 201
Update-TestResults -Success $result.Success

# Test 5: Login as Teacher
$result = Test-Endpoint -Name "Login as Teacher" -Method POST -Url "$baseUrl/api/v1/auth/login" -Body @{
    email = "teacher1@example.com"
    password = "Teacher@123456"
} -ExpectedStatus 200
Update-TestResults -Success $result.Success

if ($result.Content) {
    $script:accessToken = $result.Content.accessToken
    $script:refreshToken = $result.Content.refreshToken
    Write-Host "  → Saved access token for subsequent tests" -ForegroundColor Green
    Write-Host ""
}

# Test 6: Login as Student
$result = Test-Endpoint -Name "Login as Student" -Method POST -Url "$baseUrl/api/v1/auth/login" -Body @{
    email = "student1@example.com"
    password = "Student@123456"
} -ExpectedStatus 200
Update-TestResults -Success $result.Success

if ($result.Content) {
    $script:studentToken = $result.Content.accessToken
}

# Test 7: Invalid Login (Wrong Password)
$result = Test-Endpoint -Name "Invalid Login - Wrong Password" -Method POST -Url "$baseUrl/api/v1/auth/login" -Body @{
    email = "teacher1@example.com"
    password = "WrongPassword123"
} -ExpectedStatus 401
Update-TestResults -Success $result.Success

# Test 8: Invalid Login (Non-existent Email)
$result = Test-Endpoint -Name "Invalid Login - Non-existent Email" -Method POST -Url "$baseUrl/api/v1/auth/login" -Body @{
    email = "nonexistent@example.com"
    password = "SomePassword123"
} -ExpectedStatus 401
Update-TestResults -Success $result.Success

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "2. AUTHENTICATED ENDPOINT TESTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 9: Get My Profile (Teacher)
if ($script:accessToken) {
    $result = Test-Endpoint -Name "Get My Profile (Teacher)" -Method GET -Url "$baseUrl/api/v1/auth/me" `
        -Headers @{ Authorization = "Bearer $($script:accessToken)" } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
} else {
    Write-Host "  ⚠ SKIPPED - No access token available" -ForegroundColor Yellow
    Write-Host ""
}

# Test 10: Refresh Token
if ($script:refreshToken) {
    $result = Test-Endpoint -Name "Refresh Token" -Method POST -Url "$baseUrl/api/v1/auth/refresh-token" -Body @{
        refreshToken = $script:refreshToken
    } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
    
    if ($result.Content) {
        $newAccessToken = $result.Content.accessToken
        $newRefreshToken = $result.Content.refreshToken
        Write-Host "  → Token refreshed successfully" -ForegroundColor Green
        Write-Host ""
        # Update tokens for subsequent tests
        $script:accessToken = $newAccessToken
        $script:refreshToken = $newRefreshToken
    }
} else {
    Write-Host "  ⚠ SKIPPED - No refresh token available" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "3. PASSWORD RESET TESTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 11: Request Password Reset
$result = Test-Endpoint -Name "Request Password Reset" -Method POST -Url "$baseUrl/api/v1/auth/password-reset/request" -Body @{
    email = "teacher1@example.com"
} -ExpectedStatus 200
Update-TestResults -Success $result.Success

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "4. SUBSCRIPTION ENDPOINT TESTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 12: Get Current Subscription (No subscription expected)
if ($script:accessToken) {
    $result = Test-Endpoint -Name "Get Current Subscription" -Method GET -Url "$baseUrl/api/v1/subscription/current" `
        -Headers @{ Authorization = "Bearer $($script:accessToken)" } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
} else {
    Write-Host "  ⚠ SKIPPED - No access token available" -ForegroundColor Yellow
    Write-Host ""
}

# Test 13: Get Subscription Usage
if ($script:accessToken) {
    $result = Test-Endpoint -Name "Get Subscription Usage" -Method GET -Url "$baseUrl/api/v1/subscription/usage" `
        -Headers @{ Authorization = "Bearer $($script:accessToken)" } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
} else {
    Write-Host "  ⚠ SKIPPED - No access token available" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "5. NEGATIVE TESTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 14: Access subscription without authentication
$result = Test-Endpoint -Name "Subscription without Auth" -Method GET -Url "$baseUrl/api/v1/subscription/current" -ExpectedStatus 401
Update-TestResults -Success $result.Success

# Test 15: Access subscription with student token (should fail - teacher only)
if ($script:studentToken) {
    $result = Test-Endpoint -Name "Subscription with Student Token" -Method GET -Url "$baseUrl/api/v1/subscription/current" `
        -Headers @{ Authorization = "Bearer $($script:studentToken)" } -ExpectedStatus 403
    Update-TestResults -Success $result.Success
} else {
    Write-Host "  ⚠ SKIPPED - No student token available" -ForegroundColor Yellow
    Write-Host ""
}

# Test 16: Duplicate teacher registration
$result = Test-Endpoint -Name "Duplicate Teacher Registration" -Method POST -Url "$baseUrl/api/v1/auth/register/teacher" -Body @{
    email = "teacher1@example.com"
    password = "Teacher@123456"
    fullName = "Duplicate Teacher"
    phone = "+201234567899"
} -ExpectedStatus 409
Update-TestResults -Success $result.Success

# Test 17: Duplicate student registration
$result = Test-Endpoint -Name "Duplicate Student Registration" -Method POST -Url "$baseUrl/api/v1/auth/register/student" -Body @{
    email = "student1@example.com"
    password = "Student@123456"
    fullName = "Duplicate Student"
    parentGuardianEmail = "parent3@example.com"
    dateOfBirth = "2012-03-10"
} -ExpectedStatus 409
Update-TestResults -Success $result.Success

# Test 18: Invalid email format
$result = Test-Endpoint -Name "Invalid Email Format" -Method POST -Url "$baseUrl/api/v1/auth/register/teacher" -Body @{
    email = "invalid-email-format"
    password = "Teacher@123456"
    fullName = "Invalid Email Teacher"
    phone = "+201234567892"
} -ExpectedStatus 400
Update-TestResults -Success $result.Success

# Test 19: Logout
if ($script:accessToken) {
    $result = Test-Endpoint -Name "Logout" -Method POST -Url "$baseUrl/api/v1/auth/logout" `
        -Headers @{ Authorization = "Bearer $($script:accessToken)" } -ExpectedStatus 204
    Update-TestResults -Success $result.Success
} else {
    Write-Host "  ⚠ SKIPPED - No access token available" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total Tests:  $($script:totalTests)" -ForegroundColor White
Write-Host "Passed:       $($script:passedTests)" -ForegroundColor Green
Write-Host "Failed:       $($script:failedTests)" -ForegroundColor Red
Write-Host "Success Rate: $([math]::Round(($script:passedTests / $script:totalTests) * 100, 2))%" -ForegroundColor $(if ($script:failedTests -eq 0) { "Green" } else { "Yellow" })
Write-Host "========================================" -ForegroundColor Cyan
