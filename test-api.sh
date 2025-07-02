#!/bin/bash

# HTTP Request Validation Script
# This script runs basic validation tests for the GraphQL API

set -e

HOST="${1:-http://localhost:5225}"
PASSTHROUGH_ENDPOINT="$HOST/passthrough"

echo "🧪 Testing GraphQL API Passthrough at $HOST"
echo "=================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to test HTTP request
test_request() {
    local test_name="$1"
    local payload="$2"
    local expected_status="${3:-200}"
    
    echo -n "Testing: $test_name... "
    
    response=$(curl -s -w "HTTPSTATUS:%{http_code}" \
        -X POST "$PASSTHROUGH_ENDPOINT" \
        -H "Content-Type: application/json" \
        -d "$payload")
    
    http_status=$(echo "$response" | tr -d '\n' | sed -E 's/.*HTTPSTATUS:([0-9]{3})$/\1/')
    response_body=$(echo "$response" | sed -E 's/HTTPSTATUS:[0-9]{3}$//')
    
    if [ "$http_status" -eq "$expected_status" ]; then
        echo -e "${GREEN}✓ PASS${NC} (HTTP $http_status)"
    else
        echo -e "${RED}✗ FAIL${NC} (Expected HTTP $expected_status, got HTTP $http_status)"
        echo "Response: $response_body"
        return 1
    fi
}

# Test cases
echo ""
echo "🔍 Running Core Functionality Tests..."

# Test 1: Consent with ID (should work)
test_request "Consent with ID" \
    '{"Resource": "consent", "variables": {"id": "test-123"}}'

# Test 2: Invalid resource (should return 404)
test_request "Invalid Resource (404 expected)" \
    '{"Resource": "DoesNotExist", "variables": {"id": "123"}}' \
    404

# Test 3: Direct query
test_request "Direct Query" \
    '{"query": "query { ConsentList { id } }", "variables": {}}'

# Test 4: Case insensitive resource
test_request "Case Insensitive Resource" \
    '{"Resource": "CONSENT", "variables": {"id": "test-456"}}'

# Test 5: Case insensitive variable
test_request "Case Insensitive Variable" \
    '{"Resource": "consent", "variables": {"ID": "test-789"}}'

# Test 6: All consents (no variables)
test_request "All Consents (Default)" \
    '{"variables": {}}'

# Test 7: DocumentReference query
test_request "DocumentReference Query" \
    '{"Resource": "documentreference", "variables": {"subject": "pat1", "related": "cons1"}}'

# Test 8: Provenance query
test_request "Provenance Query" \
    '{"Resource": "provenance", "variables": {"id": "prov1"}}'

# Test 9: Provenance case insensitive
test_request "Provenance Case Insensitive" \
    '{"Resource": "PROVENANCE", "variables": {"ID": "prov2"}}'

# Test 10: Provenance without ID (should fallback)
test_request "Provenance Without ID (Fallback)" \
    '{"Resource": "provenance", "variables": {"someField": "value"}}'

echo ""
echo "🧪 Running Edge Case Tests..."

# Test 9: Empty ID
test_request "Empty ID Variable" \
    '{"Resource": "consent", "variables": {"id": ""}}'

# Test 10: Null ID
test_request "Null ID Variable" \
    '{"Resource": "consent", "variables": {"id": null}}'

# Test 11: Empty Provenance ID
test_request "Empty Provenance ID" \
    '{"Resource": "provenance", "variables": {"id": ""}}'

# Test 12: Missing variables for DocumentReference (should fallback)
test_request "Incomplete DocumentReference" \
    '{"Resource": "documentreference", "variables": {"subject": "pat1"}}'

echo ""
echo "🎯 Test Summary"
echo "=============="
echo -e "${GREEN}✓ All tests completed${NC}"
echo ""
echo "💡 Tips:"
echo "  - If tests fail, check that the API is running: dotnet run"
echo "  - For detailed responses, add -v flag to curl commands in the script"
echo "  - Check logs in the API console for debugging information"
echo ""
echo "📁 For more detailed testing, use the HTTP files:"
echo "  - GraphqlApiAidBox.http (main test suite)"
echo "  - GraphqlApiAidBox.dev.http (development tests)"
