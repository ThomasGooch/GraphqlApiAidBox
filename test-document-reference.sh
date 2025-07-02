#!/bin/bash

# Document Reference API Testing Script
# Tests the GetDocumentReferenceUrlQuery.graphql functionality

API_URL="http://localhost:5225/passthrough"
TOTAL_TESTS=0
PASSED_TESTS=0

echo "🧪 Document Reference API Testing"
echo "=================================="
echo "API Endpoint: $API_URL"
echo ""

# Function to run a test
run_test() {
    local test_name="$1"
    local json_payload="$2"
    local expected_pattern="$3"
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    echo "Test $TOTAL_TESTS: $test_name"
    
    response=$(curl -s -X POST "$API_URL" \
        -H "Content-Type: application/json" \
        -d "$json_payload")
    
    if echo "$response" | grep -q "$expected_pattern"; then
        echo "✅ PASSED"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo "❌ FAILED"
        echo "Response: $response"
        echo "Expected pattern: $expected_pattern"
    fi
    echo ""
}

# Test 1: Valid Document Reference Request
run_test "Valid Document Reference Request" \
'{
  "Resource": "documentreference",
  "variables": {
    "subject": "Patient/test-patient-123",
    "related": "Consent/consent-456"
  }
}' \
"GetDocumentReferenceUrl"

# Test 2: Case Insensitive Resource Name
run_test "Case Insensitive Resource Name" \
'{
  "Resource": "DOCUMENTREFERENCE",
  "variables": {
    "subject": "Patient/case-test-789",
    "related": "Consent/case-consent-abc"
  }
}' \
"GetDocumentReferenceUrl"

# Test 3: Case Insensitive Variable Names
run_test "Case Insensitive Variable Names" \
'{
  "Resource": "documentreference",
  "variables": {
    "SUBJECT": "Patient/uppercase-subject",
    "RELATED": "Consent/uppercase-related"
  }
}' \
"GetDocumentReferenceUrl"

# Test 4: Variable Substitution Verification
run_test "Variable Substitution - Subject" \
'{
  "Resource": "documentreference",
  "variables": {
    "subject": "Patient/substitution-test",
    "related": "Consent/substitution-consent"
  }
}' \
"Patient/substitution-test"

# Test 5: Variable Substitution Verification - Related
run_test "Variable Substitution - Related" \
'{
  "Resource": "documentreference",
  "variables": {
    "subject": "Patient/another-test",
    "related": "Consent/another-consent-123"
  }
}' \
"Consent/another-consent-123"

# Test 6: Special Characters in Variables
run_test "Special Characters in Variables" \
'{
  "Resource": "documentreference",
  "variables": {
    "subject": "Patient/test-with-dashes_and_underscores",
    "related": "Consent/consent-with-special-chars@123"
  }
}' \
"GetDocumentReferenceUrl"

# Test 7: Missing Subject Parameter (Should Fall Back)
run_test "Missing Subject Parameter" \
'{
  "Resource": "documentreference",
  "variables": {
    "related": "Consent/consent-456"
  }
}' \
"GetAllConsents"

# Test 8: Missing Related Parameter (Should Fall Back)
run_test "Missing Related Parameter" \
'{
  "Resource": "documentreference",
  "variables": {
    "subject": "Patient/test-123"
  }
}' \
"GetAllConsents"

# Test 9: Empty Variables (Should Fall Back)
run_test "Empty Variables" \
'{
  "Resource": "documentreference",
  "variables": {}
}' \
"GetAllConsents"

# Test 10: No Variables Property (Should Fall Back)
run_test "No Variables Property" \
'{
  "Resource": "documentreference"
}' \
"GetAllConsents"

# Test 11: Direct Query Mode
run_test "Direct Query Mode" \
'{
  "query": "query { DocumentReferenceList(subject: \"Patient/direct-123\", related: \"Consent/direct-456\") { content { attachment { url } } } }",
  "variables": {}
}' \
"DocumentReferenceList"

# Test 12: Multiple Variable Types
run_test "Multiple Variable Types" \
'{
  "Resource": "documentreference",
  "variables": {
    "subject": "Patient/123",
    "related": "Consent/456",
    "extra": "ignored-parameter"
  }
}' \
"GetDocumentReferenceUrl"

echo "📊 Test Results Summary"
echo "======================"
echo "Total Tests: $TOTAL_TESTS"
echo "Passed: $PASSED_TESTS"
echo "Failed: $((TOTAL_TESTS - PASSED_TESTS))"

if [ $PASSED_TESTS -eq $TOTAL_TESTS ]; then
    echo "🎉 ALL TESTS PASSED!"
    exit 0
else
    echo "❌ Some tests failed!"
    exit 1
fi
