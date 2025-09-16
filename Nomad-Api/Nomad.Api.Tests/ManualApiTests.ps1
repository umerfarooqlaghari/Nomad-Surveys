# Manual API Testing Script for Participants API
# This script tests all endpoints with various scenarios and generates a detailed report

$baseUrl = "http://localhost:5232"
$testResults = @()

# Function to make HTTP requests and capture results
function Invoke-ApiTest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [string]$TestName,
        [int]$ExpectedStatusCode
    )
    
    try {
        $uri = "$baseUrl$Endpoint"
        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $Headers
        }
        
        if ($Body) {
            $params.Body = $Body
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-RestMethod @params -ErrorAction Stop
        $statusCode = 200 # Default for successful Invoke-RestMethod
        
        $result = @{
            TestName = $TestName
            Method = $Method
            Endpoint = $Endpoint
            ExpectedStatusCode = $ExpectedStatusCode
            ActualStatusCode = $statusCode
            Success = ($statusCode -eq $ExpectedStatusCode)
            Response = $response
            Error = $null
            Timestamp = Get-Date
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $result = @{
            TestName = $TestName
            Method = $Method
            Endpoint = $Endpoint
            ExpectedStatusCode = $ExpectedStatusCode
            ActualStatusCode = $statusCode
            Success = ($statusCode -eq $ExpectedStatusCode)
            Response = $null
            Error = $_.Exception.Message
            Timestamp = Get-Date
        }
    }
    
    return $result
}

# Test Data
$validSubject = @{
    firstName = "John"
    lastName = "Doe"
    email = "john.doe@example.com"
    phoneNumber = "+1234567890"
    department = "Engineering"
    position = "Software Engineer"
} | ConvertTo-Json

$invalidEmailSubject = @{
    firstName = "Jane"
    lastName = "Smith"
    email = "invalid-email"
    phoneNumber = "+0987654321"
    department = "Marketing"
    position = "Marketing Manager"
} | ConvertTo-Json

$bulkSubjects = @{
    subjects = @(
        @{
            firstName = "John"
            lastName = "Doe"
            email = "john.doe@example.com"
            phoneNumber = "+1234567890"
            department = "Engineering"
            position = "Software Engineer"
        },
        @{
            firstName = "Jane"
            lastName = "Smith"
            email = "invalid-email"
            phoneNumber = "+0987654321"
            department = "Marketing"
            position = "Marketing Manager"
        },
        @{
            firstName = "Bob"
            lastName = "Johnson"
            email = "bob.johnson@example.com"
            phoneNumber = "+1122334455"
            department = "Sales"
            position = "Sales Representative"
        }
    )
} | ConvertTo-Json -Depth 3

$validEvaluator = @{
    evaluatorFirstName = "Alice"
    evaluatorLastName = "Wilson"
    evaluatorEmail = "alice.wilson@example.com"
    evaluatorPhoneNumber = "+1555666777"
    evaluatorDepartment = "HR"
    evaluatorPosition = "HR Manager"
} | ConvertTo-Json

$bulkEvaluators = @{
    evaluators = @(
        @{
            evaluatorFirstName = "Alice"
            evaluatorLastName = "Wilson"
            evaluatorEmail = "alice.wilson@example.com"
            evaluatorPhoneNumber = "+1555666777"
            evaluatorDepartment = "HR"
            evaluatorPosition = "HR Manager"
        },
        @{
            evaluatorFirstName = "Bob"
            evaluatorLastName = "Smith"
            evaluatorEmail = "alice.wilson@example.com"
            evaluatorPhoneNumber = "+1555666778"
            evaluatorDepartment = "HR"
            evaluatorPosition = "HR Assistant"
        }
    )
} | ConvertTo-Json -Depth 3

Write-Host "Starting Participants API Manual Testing..." -ForegroundColor Green
Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow

# Test 1: Subjects API - Unauthorized Access
Write-Host "`nTesting Subjects API - Unauthorized Access..." -ForegroundColor Cyan
$testResults += Invoke-ApiTest -Method "GET" -Endpoint "/api/subjects" -TestName "Get Subjects - No Auth" -ExpectedStatusCode 401
$testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/subjects" -Body $validSubject -TestName "Create Subject - No Auth" -ExpectedStatusCode 401
$testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/subjects/bulk" -Body $bulkSubjects -TestName "Bulk Create Subjects - No Auth" -ExpectedStatusCode 401

# Test 2: Evaluators API - Unauthorized Access
Write-Host "`nTesting Evaluators API - Unauthorized Access..." -ForegroundColor Cyan
$testResults += Invoke-ApiTest -Method "GET" -Endpoint "/api/evaluators" -TestName "Get Evaluators - No Auth" -ExpectedStatusCode 401
$testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/evaluators" -Body $validEvaluator -TestName "Create Evaluator - No Auth" -ExpectedStatusCode 401
$testResults += Invoke-ApiTest -Method "POST" -Endpoint "/api/evaluators/bulk" -Body $bulkEvaluators -TestName "Bulk Create Evaluators - No Auth" -ExpectedStatusCode 401

# Test 3: SubjectEvaluators API - Unauthorized Access
Write-Host "`nTesting SubjectEvaluators API - Unauthorized Access..." -ForegroundColor Cyan
$testResults += Invoke-ApiTest -Method "GET" -Endpoint "/api/subject-evaluators" -TestName "Get Subject-Evaluators - No Auth" -ExpectedStatusCode 401

# Test 4: Invalid Endpoints
Write-Host "`nTesting Invalid Endpoints..." -ForegroundColor Cyan
$testResults += Invoke-ApiTest -Method "GET" -Endpoint "/api/nonexistent" -TestName "Non-existent Endpoint" -ExpectedStatusCode 404

# Test 5: Health Check (if available)
Write-Host "`nTesting Health Check..." -ForegroundColor Cyan
$testResults += Invoke-ApiTest -Method "GET" -Endpoint "/health" -TestName "Health Check" -ExpectedStatusCode 200

# Generate Test Report
Write-Host "`nGenerating Test Report..." -ForegroundColor Green

$reportPath = "TestResults_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
$testResults | ConvertTo-Json -Depth 5 | Out-File $reportPath

# Display Summary
Write-Host "`nTest Summary:" -ForegroundColor Yellow
Write-Host "=============" -ForegroundColor Yellow

$totalTests = $testResults.Count
$passedTests = ($testResults | Where-Object { $_.Success }).Count
$failedTests = $totalTests - $passedTests

Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
Write-Host "Success Rate: $([math]::Round(($passedTests / $totalTests) * 100, 2))%" -ForegroundColor Yellow

# Display Failed Tests
if ($failedTests -gt 0) {
    Write-Host "`nFailed Tests:" -ForegroundColor Red
    $testResults | Where-Object { -not $_.Success } | ForEach-Object {
        Write-Host "- $($_.TestName): Expected $($_.ExpectedStatusCode), Got $($_.ActualStatusCode)" -ForegroundColor Red
        if ($_.Error) {
            Write-Host "  Error: $($_.Error)" -ForegroundColor DarkRed
        }
    }
}

# Display Passed Tests
Write-Host "`nPassed Tests:" -ForegroundColor Green
$testResults | Where-Object { $_.Success } | ForEach-Object {
    Write-Host "✓ $($_.TestName)" -ForegroundColor Green
}

Write-Host "`nDetailed results saved to: $reportPath" -ForegroundColor Yellow

# Additional Manual Testing Instructions
Write-Host "`n" + "="*80 -ForegroundColor Magenta
Write-Host "MANUAL TESTING INSTRUCTIONS FOR AUTHENTICATED ENDPOINTS" -ForegroundColor Magenta
Write-Host "="*80 -ForegroundColor Magenta

Write-Host @"

To test authenticated endpoints, you need a valid JWT token. Follow these steps:

1. First, authenticate and get a JWT token:
   POST /api/auth/login
   Body: { "email": "admin@acme-corp.com", "password": "Password@123" }

2. Use the token in Authorization header:
   Authorization: Bearer {your_jwt_token}

3. Test the following scenarios with authentication:

SUBJECTS API:
- GET /api/subjects (should return 200 with subjects list)
- POST /api/subjects with valid data (should return 201)
- POST /api/subjects with invalid email (should return 400)
- POST /api/subjects/bulk with mixed data (should return 207 Multi-Status)
- PUT /api/subjects/{id} with valid data (should return 200)
- DELETE /api/subjects/{id} (should return 204)

EVALUATORS API:
- GET /api/evaluators (should return 200 with evaluators list)
- POST /api/evaluators with valid data (should return 201)
- POST /api/evaluators with duplicate email (should return 409)
- POST /api/evaluators/bulk with duplicates (should return 207 Multi-Status)

SUBJECT-EVALUATORS API:
- GET /api/subject-evaluators (should return 200)
- POST /api/subject-evaluators with valid IDs (should return 201)
- POST /api/subject-evaluators with invalid IDs (should return 400)

BULK OPERATIONS TESTING:
- Verify 207 Multi-Status responses for partial success scenarios
- Test with empty arrays (should return 400)
- Test with large datasets (performance testing)

EDGE CASES:
- Test with special characters in names
- Test with extremely long strings
- Test with SQL injection attempts
- Test with XSS attempts
- Test cross-tenant data isolation

"@ -ForegroundColor White

Write-Host "Testing completed. Review the results above and follow manual testing instructions for authenticated endpoints." -ForegroundColor Green
