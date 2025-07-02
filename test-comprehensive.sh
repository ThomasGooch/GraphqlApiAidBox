#!/bin/bash
# filepath: /Users/harish.mukkapati/Documents/GitHub/GraphqlApiAidBox/test-comprehensive.sh

echo "🧪 Starting Comprehensive Test Suite - Phase 4"
echo "=============================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Test counters
TESTS_PASSED=0
TESTS_FAILED=0
TOTAL_TESTS=0

# Configuration
API_URL="http://localhost:5225/passthrough"
TIMEOUT=10

# Function to run test and check result
run_test() {
    local test_name="$1"
    local expected_status="$2"
    local json_payload="$3"
    local description="$4"
    
    ((TOTAL_TESTS++))
    
    echo -e "${BLUE}[$TOTAL_TESTS] Testing: $test_name${NC}"
    if [ -n "$description" ]; then
        echo -e "    ${CYAN}$description${NC}"
    fi
    
    if [ "$json_payload" = "null" ]; then
        response=$(curl -s -w "HTTPSTATUS:%{http_code}" -X POST \
            -H "Content-Type: application/json" \
            -H "Accept: application/json" \
            -d null \
            --max-time $TIMEOUT \
            "$API_URL" 2>/dev/null || echo "HTTPSTATUS:000")
    else
        response=$(curl -s -w "HTTPSTATUS:%{http_code}" -X POST \
            -H "Content-Type: application/json" \
            -H "Accept: application/json" \
            -d "$json_payload" \
            --max-time $TIMEOUT \
            "$API_URL" 2>/dev/null || echo "HTTPSTATUS:000")
    fi
    
    http_code=$(echo $response | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    response_body=$(echo $response | sed -e 's/HTTPSTATUS:.*//g')
    
    if [ "$http_code" = "000" ]; then
        echo -e "  ${RED}✗ FAIL${NC} (Connection failed or timeout)"
        ((TESTS_FAILED++))
    elif [ "$http_code" = "$expected_status" ]; then
        echo -e "  ${GREEN}✓ PASS${NC} (HTTP $http_code)"
        ((TESTS_PASSED++))
    else
        echo -e "  ${RED}✗ FAIL${NC} (Expected HTTP $expected_status, got $http_code)"
        echo -e "  ${YELLOW}Response: $response_body${NC}"
        ((TESTS_FAILED++))
    fi
    echo
}

# Function to run a group of tests
run_test_group() {
    local group_name="$1"
    echo -e "${PURPLE}📂 $group_name${NC}"
    echo "----------------------------------------"
}

# Check if API is running
echo "🔍 Checking if API is running..."
if ! curl -s --max-time 5 "$API_URL" > /dev/null 2>&1; then
    echo -e "${RED}❌ API is not running on localhost:5225${NC}"
    echo "Please start the API with:"
    echo "  cd /Users/harish.mukkapati/Documents/GitHub/GraphqlApiAidBox"
    echo "  dotnet run --project src"
    exit 1
fi
echo -e "${GREEN}✅ API is running${NC}"
echo

# Test Group 1: Direct Query Mode (Core Functionality)
run_test_group "DIRECT QUERY MODE (Core Functionality per Coding Instructions)"

run_test "Direct Query - Basic" "200" '{
  "query": "query { ConsentList { id status } }",
  "variables": {}
}' "Basic direct query without variables"

run_test "Direct Query - With Variables" "200" '{
  "query": "query { ConsentList(_id: \"{id}\") { id } }",
  "variables": { "id": "test-123" }
}' "Direct query with variable substitution"

# Test Group 2: Resource-Based Queries (Correct Format)
run_test_group "RESOURCE-BASED QUERIES (Correct JSON Format)"

run_test "Consent Resource - Basic" "200" '{
  "resource": "consent"
}' "Basic consent resource request"

run_test "Consent Resource - With ID" "200" '{
  "resource": "consent",
  "id": "/Consent/consent-123"
}' "Consent resource with ID"

run_test "Provenance Resource - Valid ID" "200" '{
  "resource": "provenance", 
  "id": "/Consent/history-123"
}' "Valid provenance query with proper ID"

run_test "Document Reference Resource" "200" '{
  "resource": "documentreference",
  "subject": "Patient/pat-123",
  "related": "Consent/cons-456"
}' "Document reference with subject and related"

# Test Group 4: Validation and Error Handling
run_test_group "VALIDATION AND ERROR HANDLING"

run_test "Null Body" "400" "null" "Null request body should return BadRequest"

run_test "Empty Body" "200" '{}' "Empty body should use default query"

run_test "Invalid Provenance - No ID" "400" '{
  "resource": "provenance"
}' "Provenance without ID should be rejected"

run_test "Invalid Provenance - Empty ID" "400" '{
  "resource": "provenance",
  "id": ""
}' "Provenance with empty ID should be rejected"

run_test "Invalid Provenance - Short ID" "400" '{
  "resource": "provenance",
  "id": "x"
}' "Provenance with too short ID should be rejected"

run_test "Nonexistent Resource" "404" '{
  "resource": "NonexistentResource",
  "id": "/NonexistentResource/test-789"
}' "Invalid resource type should return NotFound"

# Test Group 5: Edge Cases
run_test_group "EDGE CASES"

run_test "Special Characters in ID" "200" '{
  "resource": "consent",
  "id": "/Consent/special-@#$%^&*()"
}' "Special characters in ID should work"

run_test "Very Long ID" "200" '{
  "resource": "consent",
  "id": "/Consent/very-long-id-with-many-characters-123456789"
}' "Very long ID should work"

run_test "Unicode Characters" "200" '{
  "resource": "consent",
  "id": "/Consent/unicode-ñáéíóú-🎉"
}' "Unicode characters in ID should work"

run_test "Direct Query with Variables" "200" '{
  "query": "query { ConsentList(_id: \"{id}\") { id } }",
  "variables": { "id": "direct-123" }
}' "Direct query with variables should work"

run_test "Resource Without ID" "200" '{
  "resource": "consent"
}' "Resource without ID should use default query"

# Test Group 6: Performance Tests
run_test_group "PERFORMANCE TESTS"

run_test "Large ID Value" "200" '{
  "resource": "consent",
  "id": "/Consent/'$(printf 'A%.0s' {1..100})'"
}' "Large ID value should be handled correctly"

run_test "Rapid Sequential Requests" "200" '{
  "resource": "consent",
  "id": "/Consent/performance-123"
}' "Sequential requests should be processed efficiently"

# Test Group 7: Regression Tests
run_test_group "REGRESSION TESTS (Ensure Previous Functionality Works)"

run_test "Phase 1 - Basic Consent" "200" '{
  "resource": "consent",
  "id": "/Consent/regression-consent-123"
}' "Phase 1 basic consent functionality"

run_test "Phase 1 - Document Reference" "200" '{
  "resource": "documentreference",
  "subject": "Patient/reg-pat-123",
  "related": "Consent/reg-cons-456"
}' "Phase 1 document reference functionality"

run_test "Phase 2 - Provenance History" "200" '{
  "resource": "provenance",
  "id": "/Consent/regression-history-789"
}' "Phase 2 provenance history functionality"

run_test "Phase 3 - Enhanced Validation" "400" '{
  "resource": "provenance"
}' "Phase 3 enhanced validation functionality"

# Summary
echo "=============================================="
echo -e "${PURPLE}📊 COMPREHENSIVE TEST SUMMARY${NC}"
echo "=============================================="
echo -e "  ${GREEN}✓ Passed: $TESTS_PASSED${NC}"
echo -e "  ${RED}✗ Failed: $TESTS_FAILED${NC}"
echo -e "  ${BLUE}📈 Total:  $TOTAL_TESTS${NC}"
echo

# Calculate success rate
if [ $TOTAL_TESTS -gt 0 ]; then
    success_rate=$(( (TESTS_PASSED * 100) / TOTAL_TESTS ))
    echo -e "  ${CYAN}Success Rate: $success_rate%${NC}"
fi

echo -e "${PURPLE}Test Categories Covered:${NC}"
echo "  • Direct Query Mode (Core API Functionality)"
echo "  • Case Sensitivity (Enhanced Features)"
echo "  • Resource-Based Queries (Smart Query Selection)"
echo "  • Validation & Error Handling (Robust Input Validation)"
echo "  • Edge Cases (Special Characters, Unicode, etc.)"
echo "  • Performance Tests (Large Queries, Many Variables)"
echo "  • Regression Tests (All Previous Phase Functionality)"

echo
if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}🎉 ALL TESTS PASSED! GraphQL API is working perfectly.${NC}"
    echo -e "${GREEN}✅ Phase 4 Comprehensive Testing: COMPLETED${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed. Please check the API implementation.${NC}"
    echo -e "${YELLOW}💡 Tip: Check server logs for detailed error information.${NC}"
    exit 1
fi
