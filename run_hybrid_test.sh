#!/bin/bash

# Script to run only the HybridUpload_GetCDNUrl_EnsureDashboardVisibility test
# This test combines GraphQL and REST to get CDN URL and ensure dashboard visibility

echo "=== Running Hybrid Upload Test ==="
echo "This test will:"
echo "1. Upload image via GraphQL (standalone file)"
echo "2. Create temporary product"
echo "3. Upload same image via REST to get CDN URL"
echo "4. Show detailed results and cleanup"
echo ""

# Change to the project directory
cd "$(dirname "$0")"

# Run the specific test
dotnet test tests/ShopifyLib.Tests/ShopifyLib.Tests.csproj \
    --filter "HybridImageUploadTest.HybridUpload_GetCDNUrl_EnsureDashboardVisibility" \
    --verbosity normal \
    --logger "console;verbosity=detailed"

echo ""
echo "=== Test completed ===" 