# Azure Function Deployment Script
# This script creates and deploys the Image Proxy Azure Function

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName = "ImageProxyRG",
    
    [Parameter(Mandatory=$true)]
    [string]$Location = "eastus",
    
    [Parameter(Mandatory=$true)]
    [string]$FunctionAppName = "image-proxy-function",
    
    [Parameter(Mandatory=$true)]
    [string]$StorageAccountName = "imageproxystorage"
)

Write-Host "=== Azure Function Deployment Script ===" -ForegroundColor Green
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow
Write-Host "Function App: $FunctionAppName" -ForegroundColor Yellow
Write-Host "Storage Account: $StorageAccountName" -ForegroundColor Yellow
Write-Host ""

# Check if Azure CLI is installed
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "✅ Azure CLI found: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Host "❌ Azure CLI not found. Please install it first:" -ForegroundColor Red
    Write-Host "   https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" -ForegroundColor Yellow
    exit 1
}

# Check if logged in
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Host "✅ Logged in as: $($account.user.name)" -ForegroundColor Green
} catch {
    Write-Host "❌ Not logged in to Azure. Please run 'az login' first." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🔄 Creating Resource Group..." -ForegroundColor Cyan
az group create --name $ResourceGroupName --location $Location

Write-Host "🔄 Creating Storage Account..." -ForegroundColor Cyan
az storage account create --name $StorageAccountName --location $Location --resource-group $ResourceGroupName --sku Standard_LRS

Write-Host "🔄 Creating Function App..." -ForegroundColor Cyan
az functionapp create --resource-group $ResourceGroupName --consumption-plan-location $Location --runtime dotnet --functions-version 4 --name $FunctionAppName --storage-account $StorageAccountName --os-type Windows

Write-Host ""
Write-Host "✅ Azure resources created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Next steps:" -ForegroundColor Yellow
Write-Host "1. Navigate to the AzureFunction folder: cd src/AzureFunction" -ForegroundColor White
Write-Host "2. Deploy the function using: func azure functionapp publish $FunctionAppName" -ForegroundColor White
Write-Host "3. Get the function URL and outbound IP addresses" -ForegroundColor White
Write-Host ""
Write-Host "🔗 Function App URL: https://$FunctionAppName.azurewebsites.net" -ForegroundColor Cyan
Write-Host ""

# Get outbound IP addresses
Write-Host "🔄 Getting outbound IP addresses..." -ForegroundColor Cyan
$outboundIps = az functionapp show --resource-group $ResourceGroupName --name $FunctionAppName --query "outboundIpAddresses" --output tsv
Write-Host "📡 Outbound IP addresses:" -ForegroundColor Yellow
Write-Host $outboundIps -ForegroundColor White
Write-Host ""
Write-Host "💡 Use these IP addresses to whitelist with  CDN" -ForegroundColor Cyan 