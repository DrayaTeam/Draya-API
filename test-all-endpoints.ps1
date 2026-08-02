# Comprehensive Draya API Test Suite
# Tests all 21 implemented endpoints

$baseUrl = "http://localhost:5286"
$script:teacherToken = ""
$script:teacherRefreshToken = ""
$script:studentToken = ""
$script:classroomId = ""
$script:subjectId = ""
$script:enrollmentCode = ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Draya API - Complete Endpoint Testing" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [int]$ExpectedStatus = 200,
        [switch]$Silent
    )
    
    if (-not $Silent) {
        Write-Host "Testing: $Name" -ForegroundColor Yellow
        Write-Host "  $Method $Url" -ForegroundColor Gray
    }
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
        }
        
        if ($Body -ne $null) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-WebRequest @params -UseBasicParsing
        $statusCode = $response.StatusCode
        $content = $response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
        
        if ($statusCode -eq $ExpectedStatus) {
            if (-not $Silent) {
                Write-Host "  ✓ PASSED" -ForegroundColor Green
            }
        } else {
            Write-Host "  ✗ FAILED - Expected: $ExpectedStatus, Got: $statusCode" -ForegroundColor Red
        }
        
        return @{
            Success = ($statusCode -eq $ExpectedStatus)
            StatusCode = $statusCode
            Content = $content
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        
        if ($statusCode -eq $ExpectedStatus) {
            if (-not $Silent) {
                Write-Host "  ✓ PASSED (Expected error)" -ForegroundColor Green
            }
        } else {
            Write-Host "  ✗ FAILED - Expected: $ExpectedStatus, Got: $statusCode" -ForegroundColor Red
        }
        
        return @{
            Success = ($statusCode -eq $ExpectedStatus)
            StatusCode = $statusCode
            Content = $null
        }
    }
    finally {
        if (-not $Silent) {
            Write-Host ""
        }
    }
}

$script:totalTests = 0
$script:passedTests = 0

function Update-TestResults {
    param([bool]$Success)
    $script:totalTests++
    if ($Success) { $script:passedTests++ }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MODULE 1: AUTHENTICATION (8 endpoints)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Register Teacher
$result = Test-Endpoint -Name "1. Register Teacher" -Method POST -Url "$baseUrl/api/v1/auth/register/teacher" -Body @{
    email = "newteacher@test.com"
    password = "Teacher@123456"
    fullName = "New Test Teacher"
    phone = "+201234567890"
} -ExpectedStatus 201
Update-TestResults -Success $result.Success

# Test 2: Register Student
$result = Test-Endpoint -Name "2. Register Student" -Method POST -Url "$baseUrl/api/v1/auth/register/student" -Body @{
    email = "newstudent@test.com"
    password = "Student@123456"
    fullName = "New Test Student"
    parentGuardianEmail = "parent@test.com"
    dateOfBirth = "2010-05-15"
} -ExpectedStatus 201
Update-TestResults -Success $result.Success

# Test 3: Login as Teacher
$result = Test-Endpoint -Name "3. Login as Teacher" -Method POST -Url "$baseUrl/api/v1/auth/login" -Body @{
    email = "newteacher@test.com"
    password = "Teacher@123456"
} -ExpectedStatus 200
Update-TestResults -Success $result.Success

if ($result.Content) {
    $script:teacherToken = $result.Content.accessToken
    $script:teacherRefreshToken = $result.Content.refreshToken
    Write-Host "  → Teacher token saved" -ForegroundColor Green
    Write-Host ""
}

# Test 4: Login as Student
$result = Test-Endpoint -Name "4. Login as Student" -Method POST -Url "$baseUrl/api/v1/auth/login" -Body @{
    email = "newstudent@test.com"
    password = "Student@123456"
} -ExpectedStatus 200
Update-TestResults -Success $result.Success

if ($result.Content) {
    $script:studentToken = $result.Content.accessToken
    Write-Host "  → Student token saved" -ForegroundColor Green
    Write-Host ""
}

# Test 5: Get My Profile (Teacher)
if ($script:teacherToken) {
    $result = Test-Endpoint -Name "5. Get My Profile (Teacher)" -Method GET -Url "$baseUrl/api/v1/auth/me" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
}

# Test 6: Refresh Token
if ($script:teacherRefreshToken) {
    $result = Test-Endpoint -Name "6. Refresh Token" -Method POST -Url "$baseUrl/api/v1/auth/refresh-token" -Body @{
        refreshToken = $script:teacherRefreshToken
    } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
}

# Test 7: Request Password Reset
$result = Test-Endpoint -Name "7. Request Password Reset" -Method POST -Url "$baseUrl/api/v1/auth/password-reset/request" -Body @{
    email = "newteacher@test.com"
} -ExpectedStatus 200
Update-TestResults -Success $result.Success

# Test 8: Logout
if ($script:teacherToken) {
    $result = Test-Endpoint -Name "8. Logout" -Method POST -Url "$baseUrl/api/v1/auth/logout" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -ExpectedStatus 204
    Update-TestResults -Success $result.Success
    
    # Re-login to get fresh token
    $result = Test-Endpoint -Name "Re-login Teacher" -Method POST -Url "$baseUrl/api/v1/auth/login" -Body @{
        email = "newteacher@test.com"
        password = "Teacher@123456"
    } -ExpectedStatus 200 -Silent
    
    if ($result.Content) {
        $script:teacherToken = $result.Content.accessToken
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MODULE 2: SUBSCRIPTIONS (2 endpoints)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 9: Get Current Subscription
if ($script:teacherToken) {
    $result = Test-Endpoint -Name "9. Get Current Subscription" -Method GET -Url "$baseUrl/api/v1/subscription/current" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
}

# Test 10: Get Subscription Usage
if ($script:teacherToken) {
    $result = Test-Endpoint -Name "10. Get Subscription Usage" -Method GET -Url "$baseUrl/api/v1/subscription/usage" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MODULE 3: SUBJECTS (2 endpoints)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 11: Get All Subjects
if ($script:teacherToken) {
    $result = Test-Endpoint -Name "11. Get All Subjects" -Method GET -Url "$baseUrl/api/v1/subjects" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
}

# Test 12: Create Subject
if ($script:teacherToken) {
    $result = Test-Endpoint -Name "12. Create Subject" -Method POST -Url "$baseUrl/api/v1/subjects" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -Body @{
        name = "Mathematics"
    } -ExpectedStatus 201
    Update-TestResults -Success $result.Success
    
    if ($result.Content) {
        $script:subjectId = $result.Content.id
        Write-Host "  → Subject ID saved: $($script:subjectId)" -ForegroundColor Green
        Write-Host ""
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MODULE 4: CLASSROOMS (6 endpoints)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 13: Create Classroom
if ($script:teacherToken -and $script:subjectId) {
    $result = Test-Endpoint -Name "13. Create Classroom" -Method POST -Url "$baseUrl/api/v1/classrooms" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -Body @{
        subjectId = $script:subjectId
        name = "Grade 10 Math - Section A"
    } -ExpectedStatus 201
    Update-TestResults -Success $result.Success
    
    if ($result.Content) {
        $script:classroomId = $result.Content.classroomId
        $script:enrollmentCode = $result.Content.enrollmentCode
        Write-Host "  → Classroom ID saved: $($script:classroomId)" -ForegroundColor Green
        Write-Host "  → Enrollment code saved: $($script:enrollmentCode)" -ForegroundColor Green
        Write-Host ""
    }
}

# Test 14: Get Classrooms (Teacher's own)
if ($script:teacherToken) {
    $result = Test-Endpoint -Name "14. Get Classrooms (Teacher)" -Method GET -Url "$baseUrl/api/v1/classrooms?page=1&pageSize=20" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
}

# Test 15: Get Classroom Details
if ($script:teacherToken -and $script:classroomId) {
    $result = Test-Endpoint -Name "15. Get Classroom Details" -Method GET -Url "$baseUrl/api/v1/classrooms/$($script:classroomId)" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
}

# Test 16: Update Classroom
if ($script:teacherToken -and $script:classroomId -and $script:subjectId) {
    $result = Test-Endpoint -Name "16. Update Classroom" -Method PUT -Url "$baseUrl/api/v1/classrooms/$($script:classroomId)" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -Body @{
        name = "Grade 10 Math - Section A (Updated)"
        subjectId = $script:subjectId
        isActive = $true
    } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
}

# Test 17: Regenerate Enrollment Code
if ($script:teacherToken -and $script:classroomId) {
    $result = Test-Endpoint -Name "17. Regenerate Enrollment Code" -Method POST `
        -Url "$baseUrl/api/v1/classrooms/$($script:classroomId)/regenerate-code" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
    
    if ($result.Content) {
        $script:enrollmentCode = $result.Content.enrollmentCode
        Write-Host "  → New enrollment code: $($script:enrollmentCode)" -ForegroundColor Green
        Write-Host ""
    }
}

# Test 18: Deactivate Classroom will be tested last

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MODULE 5: ENROLLMENT (3 endpoints)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 19: Student Enrolls in Classroom
if ($script:studentToken -and $script:enrollmentCode) {
    $result = Test-Endpoint -Name "19. Student Enrolls in Classroom" -Method POST -Url "$baseUrl/api/v1/classrooms/enroll" `
        -Headers @{ Authorization = "Bearer $($script:studentToken)" } -Body @{
        enrollmentCode = $script:enrollmentCode
    } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
}

# Test 20: Get Classroom Roster
if ($script:teacherToken -and $script:classroomId) {
    $result = Test-Endpoint -Name "20. Get Classroom Roster" -Method GET `
        -Url "$baseUrl/api/v1/classrooms/$($script:classroomId)/students?page=1&pageSize=20" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -ExpectedStatus 200
    Update-TestResults -Success $result.Success
}

# Test 21: Remove Student from Classroom
if ($script:teacherToken -and $script:classroomId -and $script:studentToken) {
    # Get student ID from student token
    $result = Test-Endpoint -Name "Get Student Profile" -Method GET -Url "$baseUrl/api/v1/auth/me" `
        -Headers @{ Authorization = "Bearer $($script:studentToken)" } -ExpectedStatus 200 -Silent
    
    if ($result.Content) {
        $studentId = $result.Content.userId
        
        $result = Test-Endpoint -Name "21. Remove Student from Classroom" -Method DELETE `
            -Url "$baseUrl/api/v1/classrooms/$($script:classroomId)/students/$studentId" `
            -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -ExpectedStatus 204
        Update-TestResults -Success $result.Success
    }
}

# Now test deactivate classroom (Test 18)
Write-Host "Testing final classroom operation..." -ForegroundColor Yellow
Write-Host ""
if ($script:teacherToken -and $script:classroomId) {
    $result = Test-Endpoint -Name "18. Deactivate Classroom" -Method DELETE -Url "$baseUrl/api/v1/classrooms/$($script:classroomId)" `
        -Headers @{ Authorization = "Bearer $($script:teacherToken)" } -ExpectedStatus 204
    Update-TestResults -Success $result.Success
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total Tests:  $($script:totalTests)" -ForegroundColor White
Write-Host "Passed:       $($script:passedTests)" -ForegroundColor Green
Write-Host "Failed:       $($script:totalTests - $script:passedTests)" -ForegroundColor Red

if ($script:totalTests -gt 0) {
    $successRate = [math]::Round(($script:passedTests / $script:totalTests) * 100, 2)
    Write-Host "Success Rate: $successRate%" -ForegroundColor $(if ($script:passedTests -eq $script:totalTests) { "Green" } else { "Yellow" })
}

Write-Host "========================================" -ForegroundColor Cyan

if ($script:passedTests -eq $script:totalTests) {
    Write-Host ""
    Write-Host "🎉 ALL TESTS PASSED! 🎉" -ForegroundColor Green
    Write-Host ""
}
