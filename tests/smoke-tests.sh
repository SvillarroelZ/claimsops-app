#!/bin/bash
# =============================================================================
# ClaimsOps Smoke Tests
# =============================================================================
# Automated smoke tests for ClaimsOps MVP services.
# Tests health checks, CRUD operations, and service integration.
#
# Prerequisites:
#   - Docker containers must be running
#   - curl and jq installed
#
# Usage:
#   ./tests/smoke-tests.sh
#
# Exit codes:
#   0 - All tests passed
#   1 - One or more tests failed
# =============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test counters
TESTS_RUN=0
TESTS_PASSED=0
TESTS_FAILED=0

# Service URLs
CLAIMS_SERVICE_URL="http://localhost:5115"
AUDIT_SERVICE_URL="http://localhost:8000"

# =============================================================================
# Helper Functions
# =============================================================================

print_header() {
    echo ""
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

print_test() {
    echo -e "${YELLOW}▶ Test $((TESTS_RUN + 1)): $1${NC}"
}

print_success() {
    echo -e "${GREEN}✓ PASS: $1${NC}"
    TESTS_PASSED=$((TESTS_PASSED + 1))
}

print_failure() {
    echo -e "${RED}✗ FAIL: $1${NC}"
    echo -e "${RED}  Error: $2${NC}"
    TESTS_FAILED=$((TESTS_FAILED + 1))
}

print_info() {
    echo -e "${BLUE}ℹ $1${NC}"
}

print_summary() {
    echo ""
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}Test Summary${NC}"
    echo -e "${BLUE}========================================${NC}"
    echo "Total tests run: $TESTS_RUN"
    echo -e "${GREEN}Passed: $TESTS_PASSED${NC}"
    if [ $TESTS_FAILED -gt 0 ]; then
        echo -e "${RED}Failed: $TESTS_FAILED${NC}"
    else
        echo -e "${GREEN}Failed: $TESTS_FAILED${NC}"
    fi
    echo ""
    
    if [ $TESTS_FAILED -eq 0 ]; then
        echo -e "${GREEN}✓ All tests passed!${NC}"
        return 0
    else
        echo -e "${RED}✗ Some tests failed.${NC}"
        return 1
    fi
}

# Test helper: Check HTTP response code
check_http_status() {
    local url=$1
    local expected=$2
    local description=$3
    
    TESTS_RUN=$((TESTS_RUN + 1))
    print_test "$description"
    
    local status=$(curl -s -o /dev/null -w "%{http_code}" "$url")
    
    if [ "$status" = "$expected" ]; then
        print_success "HTTP $status (expected $expected)"
        return 0
    else
        print_failure "$description" "Expected HTTP $expected, got $status"
        return 1
    fi
}

# Test helper: Check JSON response field
check_json_field() {
    local url=$1
    local field=$2
    local expected=$3
    local description=$4
    
    TESTS_RUN=$((TESTS_RUN + 1))
    print_test "$description"
    
    local response=$(curl -s "$url")
    local value=$(echo "$response" | jq -r ".$field")
    
    if [ "$value" = "$expected" ]; then
        print_success "Field '$field' = '$expected'"
        return 0
    else
        print_failure "$description" "Expected '$expected', got '$value'"
        echo "  Response: $response"
        return 1
    fi
}

# =============================================================================
# Pre-flight Checks
# =============================================================================

print_header "Pre-flight Checks"

# Check if curl is installed
if ! command -v curl &> /dev/null; then
    echo -e "${RED}Error: curl is not installed${NC}"
    exit 1
fi
print_info "curl is installed"

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo -e "${YELLOW}Warning: jq is not installed (optional but recommended)${NC}"
    echo -e "${YELLOW}Install with: sudo apt install jq${NC}"
fi

# Check if services are reachable
print_info "Checking if services are reachable..."

if ! curl -s --max-time 5 "$CLAIMS_SERVICE_URL/health" > /dev/null 2>&1; then
    echo -e "${RED}Error: Claims service is not reachable at $CLAIMS_SERVICE_URL${NC}"
    echo -e "${RED}Make sure Docker containers are running:${NC}"
    echo -e "${RED}  cd docker && docker compose up -d${NC}"
    exit 1
fi
print_info "Claims service is reachable"

if ! curl -s --max-time 5 "$AUDIT_SERVICE_URL/health" > /dev/null 2>&1; then
    echo -e "${RED}Error: Audit service is not reachable at $AUDIT_SERVICE_URL${NC}"
    echo -e "${RED}Make sure Docker containers are running:${NC}"
    echo -e "${RED}  cd docker && docker compose up -d${NC}"
    exit 1
fi
print_info "Audit service is reachable"

echo -e "${GREEN}✓ All pre-flight checks passed${NC}"

# =============================================================================
# Test Suite 1: Health Checks
# =============================================================================

print_header "Test Suite 1: Health Checks"

# Test 1: Claims service health endpoint
check_http_status "$CLAIMS_SERVICE_URL/health" "200" "Claims service health check"

# Test 2: Claims service health status field
check_json_field "$CLAIMS_SERVICE_URL/health" "status" "healthy" "Claims service status is 'healthy'"

# Test 3: Audit service health endpoint
check_http_status "$AUDIT_SERVICE_URL/health" "200" "Audit service health check"

# Test 4: Audit service health status field
check_json_field "$AUDIT_SERVICE_URL/health" "status" "healthy" "Audit service status is 'healthy'"

# =============================================================================
# Test Suite 2: Claims CRUD Operations
# =============================================================================

print_header "Test Suite 2: Claims CRUD Operations"

# Test 5: List claims (empty or with data)
TESTS_RUN=$((TESTS_RUN + 1))
print_test "List all claims"
CLAIMS_LIST=$(curl -s "$CLAIMS_SERVICE_URL/api/claims")
if echo "$CLAIMS_LIST" | jq empty 2>/dev/null; then
    print_success "GET /api/claims returns valid JSON"
else
    print_failure "List all claims" "Response is not valid JSON"
fi

# Test 6: Create a new claim
TESTS_RUN=$((TESTS_RUN + 1))
print_test "Create a new claim"

CREATE_RESPONSE=$(curl -s -X POST "$CLAIMS_SERVICE_URL/api/claims" \
  -H "Content-Type: application/json" \
  -d '{
    "memberId": "SMOKE-TEST-001",
    "amount": 123.45,
    "currency": "USD"
  }')

CLAIM_ID=$(echo "$CREATE_RESPONSE" | jq -r '.id')

if [ "$CLAIM_ID" != "null" ] && [ ! -z "$CLAIM_ID" ]; then
    print_success "Created claim with ID: $CLAIM_ID"
else
    print_failure "Create a new claim" "Failed to create claim or parse ID"
    echo "  Response: $CREATE_RESPONSE"
fi

# Test 7: Verify created claim has correct status
TESTS_RUN=$((TESTS_RUN + 1))
print_test "Verify created claim has status 'Draft'"
CLAIM_STATUS=$(echo "$CREATE_RESPONSE" | jq -r '.status')
if [ "$CLAIM_STATUS" = "Draft" ]; then
    print_success "Claim status is 'Draft'"
else
    print_failure "Verify claim status" "Expected 'Draft', got '$CLAIM_STATUS'"
fi

# Test 8: Get claim by ID
if [ ! -z "$CLAIM_ID" ] && [ "$CLAIM_ID" != "null" ]; then
    TESTS_RUN=$((TESTS_RUN + 1))
    print_test "Get claim by ID"
    
    GET_RESPONSE=$(curl -s "$CLAIMS_SERVICE_URL/api/claims/$CLAIM_ID")
    GET_CLAIM_ID=$(echo "$GET_RESPONSE" | jq -r '.id')
    
    if [ "$GET_CLAIM_ID" = "$CLAIM_ID" ]; then
        print_success "Retrieved claim with matching ID"
    else
        print_failure "Get claim by ID" "Expected ID '$CLAIM_ID', got '$GET_CLAIM_ID'"
    fi
fi

# Test 9: Get non-existent claim (should return 404)
TESTS_RUN=$((TESTS_RUN + 1))
print_test "Get non-existent claim returns 404"
FAKE_ID="00000000-0000-0000-0000-000000000000"
NOT_FOUND_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$CLAIMS_SERVICE_URL/api/claims/$FAKE_ID")
if [ "$NOT_FOUND_STATUS" = "404" ]; then
    print_success "Non-existent claim returns HTTP 404"
else
    print_failure "Get non-existent claim" "Expected HTTP 404, got $NOT_FOUND_STATUS"
fi

# =============================================================================
# Test Suite 3: Audit Service Integration
# =============================================================================

print_header "Test Suite 3: Audit Service Integration"

# Test 10: List audit events
TESTS_RUN=$((TESTS_RUN + 1))
print_test "List all audit events"
AUDIT_LIST=$(curl -s "$AUDIT_SERVICE_URL/audit")
if echo "$AUDIT_LIST" | jq empty 2>/dev/null; then
    print_success "GET /audit returns valid JSON"
else
    print_failure "List audit events" "Response is not valid JSON"
fi

# Test 11: Verify audit event was created for the claim
if [ ! -z "$CLAIM_ID" ] && [ "$CLAIM_ID" != "null" ]; then
    TESTS_RUN=$((TESTS_RUN + 1))
    print_test "Verify audit event exists for created claim"
    
    # Wait a moment for async audit call to complete
    sleep 1
    
    AUDIT_FOR_CLAIM=$(curl -s "$AUDIT_SERVICE_URL/audit?claim_id=$CLAIM_ID")
    AUDIT_COUNT=$(echo "$AUDIT_FOR_CLAIM" | jq '. | length')
    
    if [ "$AUDIT_COUNT" -gt 0 ]; then
        print_success "Found $AUDIT_COUNT audit event(s) for claim $CLAIM_ID"
    else
        print_failure "Verify audit event" "No audit events found for claim $CLAIM_ID"
        echo "  Audit response: $AUDIT_FOR_CLAIM"
    fi
fi

# =============================================================================
# Test Suite 4: Validation Tests
# =============================================================================

print_header "Test Suite 4: Input Validation"

# Test 12: Create claim with invalid data (missing memberId)
TESTS_RUN=$((TESTS_RUN + 1))
print_test "Create claim with missing memberId returns 400"
INVALID_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$CLAIMS_SERVICE_URL/api/claims" \
  -H "Content-Type: application/json" \
  -d '{"amount": 100}')

if [ "$INVALID_STATUS" = "400" ]; then
    print_success "Missing memberId returns HTTP 400"
else
    print_failure "Validation test" "Expected HTTP 400, got $INVALID_STATUS"
fi

# Test 13: Create claim with negative amount
TESTS_RUN=$((TESTS_RUN + 1))
print_test "Create claim with negative amount returns 400"
NEGATIVE_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$CLAIMS_SERVICE_URL/api/claims" \
  -H "Content-Type: application/json" \
  -d '{"memberId": "TEST", "amount": -100}')

if [ "$NEGATIVE_STATUS" = "400" ]; then
    print_success "Negative amount returns HTTP 400"
else
    print_failure "Negative amount validation" "Expected HTTP 400, got $NEGATIVE_STATUS"
fi

# =============================================================================
# Summary
# =============================================================================

print_summary
exit $?
