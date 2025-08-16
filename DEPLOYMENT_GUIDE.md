# Azure Function Deployment Guide

This guide walks you through deploying the Image Proxy Azure Function to bypass CDN restrictions.

## 📁 Project Structure

```
Shopify-Lib/
├── src/
│   └── AzureFunction/          # Azure Function code
│       ├── ImageProxy.cs       # Main function code
│       ├── ImageProxyFunction.csproj
│       └── host.json
├── tests/
│   └── ShopifyLib.Tests/
│       └── EndToEndAzureProxyTest.cs  # End-to-end test
└── deploy-azure-function.ps1   # Deployment script
```

## 🚀 Quick Start

### Option 1: Automated Deployment (Recommended)

1. **Run the deployment script:**
   ```powershell
   .\deploy-azure-function.ps1 -ResourceGroupName "ImageProxyRG" -Location "eastus" -FunctionAppName "image-proxy-function" -StorageAccountName "imageproxystorage"
   ```

2. **Deploy the function:**
   ```bash
   cd src/AzureFunction
   func azure functionapp publish image-proxy-function
   ```

3. **Get the function URL and update your test:**
   ```bash
   # Get function URL
   az functionapp function show --resource-group ImageProxyRG --name image-proxy-function --function-name ImageProxy --query "invokeUrlTemplate"
   
   # Get outbound IP addresses
   az functionapp show --resource-group ImageProxyRG --name image-proxy-function --query "outboundIpAddresses"
   ```

### Option 2: Manual Azure Portal Setup

1. **Create Resource Group**
   - Go to Azure Portal → Resource groups → + Create
   - Name: `ImageProxyRG`
   - Region: Choose closest to you

2. **Create Storage Account**
   - Go to Storage accounts → + Create
   - Resource group: `ImageProxyRG`
   - Name: `imageproxystorage` (must be globally unique)
   - Performance: Standard
   - Redundancy: LRS

3. **Create Function App**
   - Go to Function App → + Create
   - Resource group: `ImageProxyRG`
   - Name: `image-proxy-function` (must be globally unique)
   - Runtime: .NET 6
   - Hosting plan: Consumption
   - Storage account: Select the one you created

4. **Deploy the Function**
   ```bash
   cd src/AzureFunction
   func azure functionapp publish image-proxy-function
   ```

## ⚙️ Configuration

### Update Test Configuration

Add the Azure Function URL to your `appsettings.json`:

```json
{
  "AzureFunctionUrl": "https://image-proxy-function.azurewebsites.net/api/ImageProxy?code=YOUR_FUNCTION_KEY"
}
```

Or set as environment variable:
```bash
export AZURE_FUNCTION_URL="https://image-proxy-function.azurewebsites.net/api/ImageProxy?code=YOUR_FUNCTION_KEY"
```

### Get Function Key

1. Go to Azure Portal → Your Function App → Functions → ImageProxy
2. Click "Get Function URL"
3. Copy the URL (includes the function key)

## 🧪 Testing

### Run the End-to-End Test

```bash
cd tests/ShopifyLib.Tests
dotnet test --filter "CompleteEndToEndFlow_ToShopify" --verbosity normal
```

### Test the Azure Function Directly

```bash
curl -X POST "https://image-proxy-function.azurewebsites.net/api/ImageProxy?code=YOUR_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://dynamic.images.ca/v1/gifts/gifts/673419406239/1.jpg",
    "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
    "referrer": "https://.ca"
  }'
```

## 🔧 Troubleshooting

### Common Issues

1. **Function not found**
   - Make sure you're in the correct directory: `src/AzureFunction`
   - Verify the function app name matches

2. **Deployment fails**
   - Check Azure CLI is logged in: `az login`
   - Verify you have permissions to create resources

3. **Test fails with "Function not accessible"**
   - Check the function URL is correct
   - Verify the function key is included in the URL

4. **Timeout errors**
   - Increase timeout in the Azure Function (currently 30 seconds)
   - Check network connectivity

### Debug Locally

```bash
cd src/AzureFunction
func start
```

Then test with:
```bash
curl -X POST "http://localhost:7071/api/ImageProxy" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://example.com/image.jpg"}'
```

## 📊 Monitoring

- **Azure Portal**: Go to your Function App → Monitor
- **Application Insights**: Automatically enabled for detailed logging
- **Logs**: Check function execution logs for errors

## 💰 Cost Optimization

- **Consumption Plan**: Pay per execution (good for low volume)
- **Premium Plan**: For higher volume with VNET integration
- **Monitoring**: Set up alerts for high usage

## 🔒 Security

- **Function Key**: Keep the function key secure
- **URL Validation**: The function validates URLs before downloading
- **Rate Limiting**: Consider adding rate limiting for production use
- **IP Whitelisting**: Whitelist your application's IPs if needed

## 📈 Next Steps

1. **Deploy and test** the function
2. **Monitor performance** and adjust timeouts
3. **Add caching** for frequently accessed images
4. **Set up alerts** for errors and high usage
5. **Scale** to Premium plan if needed for production

## 🆘 Support

If you encounter issues:

1. Check the Azure Function logs in the portal
2. Verify the function URL and key are correct
3. Test the function directly with curl
4. Check network connectivity and firewall rules 