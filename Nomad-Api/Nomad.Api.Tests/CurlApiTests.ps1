# Comprehensive API Testing Script using PowerShell (Curl-like functionality)
# Tests all Participants API endpoints with happy flow and faulty payloads

$baseUrl = "http://localhost:5232"
$testResults = @()

# Function to make HTTP requests
function Invoke-ApiTest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [string]$TestName,
        [string]$Description
    )
    
    try {
        $uri = "$baseUrl$Endpoint"
        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $Headers
            ErrorAction = "Stop"
        }
        
        if ($Body) {
            $params.Body = $Body
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-RestMethod @params
        $statusCode = 200 # Default for successful Invoke-RestMethod
        
        $result = @{
            TestName = $TestName
            Description = $Description
            Method = $Method
            Endpoint = $Endpoint
            StatusCode = $statusCode
            Success = $true
            Response = $response
            Error = $null
            Timestamp = Get-Date
            RequestBody = $Body
        }
    }
    catch {
        $statusCode = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { "Unknown" }
        $errorMessage = $_.Exception.Message
        
        $result = @{
            TestName = $TestName
            Description = $Description
            Method = $Method
            Endpoint = $Endpoint
            StatusCode = $statusCode
            Success = $false
            Response = $null
            Error = $errorMessage
            Timestamp = Get-Date
            RequestBody = $Body
        }
    }
    
    return $result
}

Write-Host "🚀 Starting Comprehensive API Testing..." -ForegroundColor Green
Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow

# Step 1: Authentication Test
Write-Host "`n🔐 Testing Authentication..." -ForegroundColor Cyan

$loginPayload = @{
    email = "admin@acme-corp.com"
    password = "Password@123"
} | ConvertTo-Json

$authResult = Invoke-ApiTest -Method "POST" -Endpoint "/api/auth/login" -Body $loginPayload -TestName "AUTH-001" -Description "Login with valid credentials"
$testResults += $authResult

# Extract JWT token if login successful
$jwtToken = $null
if ($authResult.Success -and $authResult.Response.token) {
    $jwtToken = $authResult.Response.token
    Write-Host "✅ JWT Token obtained successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to obtain JWT token. Some tests will be skipped." -ForegroundColor Red
}

# Headers for authenticated requests
$authHeaders = @{}
if ($jwtToken) {
    $authHeaders = @{
        "Authorization" = "Bearer $jwtToken"
    }
}

# Step 2: Subjects API Tests
Write-Host "`n👥 Testing Subjects API..." -ForegroundColor Cyan

# Test 1: Get subjects without authentication
$testResults += Invoke-ApiTest -Method "GET" -Endpoint "/api/subjects" -TestName "SUBJ-001" -Description "Get subjects without authentication (should return 401)"

# Test 2: Get subjects with authentication
if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "GET" -Endpoint "/api/subjects" -Headers $authHeaders -TestName "SUBJ-002" -Description "Get subjects with valid authentication"
}

# Test 3: Create subject with valid data
$validSubject = @{
    firstName = "John"
    lastName = "Doe"
    email = "john.doe@example.com"
    phoneNumber = "+1234567890"
    department = "Engineering"
    position = "Software Engineer"
} | ConvertTo-Json

if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/subjects" -Headers $authHeaders -Body $validSubject -TestName "SUBJ-003" -Description "Create subject with valid data"
}

# Test 4: Create subject with invalid email
$invalidEmailSubject = @{
    firstName = "Jane"
    lastName = "Smith"
    email = "invalid-email-format"
    phoneNumber = "+0987654321"
    department = "Marketing"
    position = "Marketing Manager"
} | ConvertTo-Json

if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/subjects" -Headers $authHeaders -Body $invalidEmailSubject -TestName "SUBJ-004" -Description "Create subject with invalid email (should return 400)"
}

# Test 5: Create subject with missing required fields
$incompleteSubject = @{
    firstName = "Bob"
    email = "bob@example.com"
} | ConvertTo-Json

if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/subjects" -Headers $authHeaders -Body $incompleteSubject -TestName "SUBJ-005" -Description "Create subject with missing required fields (should return 400)"
}

# Test 6: Bulk create subjects with mixed data
$bulkSubjects = @{
    subjects = @(
        @{
            firstName = "Alice"
            lastName = "Johnson"
            email = "alice.johnson@example.com"
            phoneNumber = "+1111111111"
            department = "HR"
            position = "HR Manager"
        },
        @{
            firstName = "Bob"
            lastName = "Wilson"
            email = "invalid-email"
            phoneNumber = "+2222222222"
            department = "Finance"
            position = "Accountant"
        },
        @{
            firstName = "Charlie"
            lastName = "Brown"
            email = "charlie.brown@example.com"
            phoneNumber = "+3333333333"
            department = "Sales"
            position = "Sales Rep"
        }
    )
} | ConvertTo-Json -Depth 3

if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/subjects/bulk" -Headers $authHeaders -Body $bulkSubjects -TestName "SUBJ-006" -Description "Bulk create subjects with mixed valid/invalid data (should return 207 Multi-Status)"
}

# Test 7: Bulk create with empty array
$emptyBulkSubjects = @{
    subjects = @()
} | ConvertTo-Json

if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/subjects/bulk" -Headers $authHeaders -Body $emptyBulkSubjects -TestName "SUBJ-007" -Description "Bulk create subjects with empty array (should return 400)"
}

# Step 3: Evaluators API Tests
Write-Host "`n🔍 Testing Evaluators API..." -ForegroundColor Cyan

# Test 1: Get evaluators without authentication
$testResults += Invoke-ApiTest -Method "GET" -Endpoint "/api/evaluators" -TestName "EVAL-001" -Description "Get evaluators without authentication (should return 401)"

# Test 2: Get evaluators with authentication
if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "GET" -Endpoint "/api/evaluators" -Headers $authHeaders -TestName "EVAL-002" -Description "Get evaluators with valid authentication"
}

# Test 3: Create evaluator with valid data
$validEvaluator = @{
    evaluatorFirstName = "Sarah"
    evaluatorLastName = "Connor"
    evaluatorEmail = "sarah.connor@example.com"
    evaluatorPhoneNumber = "+1555666777"
    evaluatorDepartment = "Management"
    evaluatorPosition = "Team Lead"
} | ConvertTo-Json

if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/evaluators" -Headers $authHeaders -Body $validEvaluator -TestName "EVAL-003" -Description "Create evaluator with valid data"
}

# Test 4: Create evaluator with invalid email
$invalidEmailEvaluator = @{
    evaluatorFirstName = "John"
    evaluatorLastName = "Invalid"
    evaluatorEmail = "not-an-email"
    evaluatorPhoneNumber = "+1555666778"
    evaluatorDepartment = "IT"
    evaluatorPosition = "Developer"
} | ConvertTo-Json

if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/evaluators" -Headers $authHeaders -Body $invalidEmailEvaluator -TestName "EVAL-004" -Description "Create evaluator with invalid email (should return 400)"
}

# Test 5: Bulk create evaluators with duplicates
$bulkEvaluators = @{
    evaluators = @(
        @{
            evaluatorFirstName = "David"
            evaluatorLastName = "Smith"
            evaluatorEmail = "david.smith@example.com"
            evaluatorPhoneNumber = "+1777888999"
            evaluatorDepartment = "Engineering"
            evaluatorPosition = "Senior Developer"
        },
        @{
            evaluatorFirstName = "Emma"
            evaluatorLastName = "Davis"
            evaluatorEmail = "david.smith@example.com"
            evaluatorPhoneNumber = "+1777888998"
            evaluatorDepartment = "Engineering"
            evaluatorPosition = "Junior Developer"
        }
    )
} | ConvertTo-Json -Depth 3

if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/evaluators/bulk" -Headers $authHeaders -Body $bulkEvaluators -TestName "EVAL-005" -Description "Bulk create evaluators with duplicate emails (should return 207 Multi-Status)"
}

# Step 4: SubjectEvaluators API Tests
Write-Host "`n🔗 Testing SubjectEvaluators API..." -ForegroundColor Cyan

# Test 1: Get subject-evaluator relationships without authentication
$testResults += Invoke-ApiTest -Method "GET" -Endpoint "/api/subject-evaluators" -TestName "REL-001" -Description "Get subject-evaluator relationships without authentication (should return 401)"

# Test 2: Get subject-evaluator relationships with authentication
if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "GET" -Endpoint "/api/subject-evaluators" -Headers $authHeaders -TestName "REL-002" -Description "Get subject-evaluator relationships with valid authentication"
}

# Step 5: Edge Cases and Security Tests
Write-Host "`n🛡️ Testing Edge Cases and Security..." -ForegroundColor Cyan

# Test 1: SQL Injection attempt
$sqlInjectionSubject = @{
    firstName = "'; DROP TABLE Subjects; --"
    lastName = "Hacker"
    email = "hacker@example.com"
    phoneNumber = "+1234567890"
    department = "Security"
    position = "Penetration Tester"
} | ConvertTo-Json

if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/subjects" -Headers $authHeaders -Body $sqlInjectionSubject -TestName "SEC-001" -Description "SQL Injection attempt in firstName field"
}

# Test 2: XSS attempt
$xssSubject = @{
    firstName = "&lt;script&gt;alert('XSS')&lt;/script&gt;"
    lastName = "Test"
    email = "xss@example.com"
    phoneNumber = "+1234567890"
    department = "Security"
    position = "Security Analyst"
} | ConvertTo-Json

if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/subjects" -Headers $authHeaders -Body $xssSubject -TestName "SEC-002" -Description "XSS attempt in firstName field"
}

# Test 3: Extremely long name
$longName = "A" * 500
$longNameSubject = @{
    firstName = $longName
    lastName = "Test"
    email = "longname@example.com"
    phoneNumber = "+1234567890"
    department = "Testing"
    position = "Test Engineer"
} | ConvertTo-Json

if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/subjects" -Headers $authHeaders -Body $longNameSubject -TestName "EDGE-001" -Description "Subject with extremely long firstName 500 characters"
}

# Test 4: Unicode characters
$unicodeSubject = @{
    firstName = "Zhang"
    lastName = "San"
    email = "zhang.san@example.com"
    phoneNumber = "+86-138-0013-8000"
    department = "Development"
    position = "Software Engineer"
} | ConvertTo-Json

if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/subjects" -Headers $authHeaders -Body $unicodeSubject -TestName "EDGE-002" -Description "Subject with Unicode-like characters"
}

# Test 5: Invalid JSON
if ($jwtToken) {
    $testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/subjects" -Headers $authHeaders -Body "{ invalid json }" -TestName "EDGE-003" -Description "Invalid JSON payload should return 400"
}

Write-Host "`n📊 Generating Test Report..." -ForegroundColor Green

# Generate summary
$totalTests = $testResults.Count
$successfulTests = ($testResults | Where-Object { $_.Success }).Count
$failedTests = $totalTests - $successfulTests

Write-Host "`n📋 Test Summary:" -ForegroundColor Yellow
Write-Host "===============" -ForegroundColor Yellow
Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Successful: $successfulTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
Write-Host "Success Rate: $([math]::Round(($successfulTests / $totalTests) * 100, 2))%" -ForegroundColor Yellow

# Save detailed results
$reportPath = "CurlApiTestResults_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
$testResults | ConvertTo-Json -Depth 5 | Out-File $reportPath

Write-Host "`n💾 Detailed results saved to: $reportPath" -ForegroundColor Cyan
Write-Host "API Testing completed!" -ForegroundColor Green

return $testResults
