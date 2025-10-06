# 🚀 Part 2: Azure Functions Implementation Guide

## ST10449316 - CLDV6212 Cloud Development

---

## 📋 Assignment Requirements Summary

### **A: Integrating Functions for Robust Application Architecture**

✅ **Required Functions (One per storage type):**
1. **Table Storage Function** - `StoreCustomerProfile` ✅
2. **Blob Storage Function** - `UploadBlob` ✅
3. **File Storage Function** - `UploadFile` ✅
4. **Queue Triggered Function** - `ProcessOrderFromQueue` ✅ **NEW!**

✅ **Key Requirements:**
- At least 1 Queue-triggered function (writes to Orders table) ✅
- Functions use Services from Part 1 ✅
- Services call Functions via HTTP requests or queue messages ✅
- Orders MUST go through Queue → Queue Trigger → Table Storage ✅

---

## 🏗️ Architecture Overview

```
ABC Retail Web App
       ↓
   [Services]
       ↓
┌──────┴──────────────────────────┐
↓                                 ↓
HTTP Triggered Functions    Queue Storage
↓                                 ↓
- StoreCustomerProfile      order-processing
- UploadBlob                      ↓
- UploadFile              Queue Triggered Function
- SubmitOrder (adds to queue)     ↓
                          ProcessOrderFromQueue
                                  ↓
                          Orders Table Storage
```

---

## 📁 Project Structure

```
ABCRetailFunctions/
├── StoreTableDataFunction.cs      ✅ Table Storage (Customers, Products)
├── BlobStorageFunction.cs         ✅ Blob Storage (Images)
├── FileStorageFunction.cs         ✅ File Storage (Contracts)
├── QueueStorageFunction.cs        ✅ Queue + Queue Trigger (Orders)
│   ├── SendQueueMessage           - HTTP: Send message to queue
│   ├── SubmitOrder                - HTTP: Submit order to queue
│   └── ProcessOrderFromQueue      - QUEUE TRIGGER: Process order
├── Services/
│   ├── IAzureStorageService.cs
│   └── AzureStorageService.cs
└── Program.cs
```

---

## 🔧 What Was Added/Changed

### **1. Queue-Triggered Function** (NEW!)

**File**: `QueueStorageFunction.cs`

**Function**: `ProcessOrderFromQueue`
- **Trigger**: Queue trigger (automatically runs when message arrives)
- **Queue**: `order-processing`
- **Purpose**: Reads order from queue and stores in Orders table
- **Connection**: Uses `AzureStorage__ConnectionString`

```csharp
[Function("ProcessOrderFromQueue")]
public async Task ProcessOrderFromQueue(
    [QueueTrigger("order-processing", Connection = "AzureStorage__ConnectionString")] 
    string queueMessage)
{
    // Deserialize order from queue
    var order = JsonSerializer.Deserialize<Order>(queueMessage);
    
    // Store in Table Storage
    await _storageService.AddOrderAsync(order);
}
```

### **2. Submit Order Function** (UPDATED!)

**Function**: `SubmitOrder`
- **Trigger**: HTTP POST
- **Route**: `/api/orders/submit`
- **Purpose**: Accepts order data and adds to queue (NOT directly to table!)
- **Flow**: HTTP Request → Queue → Queue Trigger → Table Storage

```csharp
[Function("SubmitOrder")]
public async Task<HttpResponseData> SubmitOrder(...)
{
    // Create order entity
    var order = new Order { ... };
    
    // Send to QUEUE (not table!)
    await _storageService.SendMessageAsync(JsonSerializer.Serialize(order));
}
```

### **3. NuGet Package Added**

**Package**: `Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues` v5.2.0
- Required for Queue Trigger functionality
- Added to `ABCRetailFunctions.csproj`

---

## 🚀 Deployment Steps

### **Step 1: Build the Project**

```bash
cd ABCRetailFunctions
dotnet restore
dotnet build
```

### **Step 2: Publish the Project**

```bash
dotnet publish -c Release -o ./publish
```

### **Step 3: Create Deployment Package**

```bash
cd publish
Compress-Archive -Path * -DestinationPath ../ABCRetailFunctions-deploy.zip -Force
cd ..
```

### **Step 4: Deploy to Azure**

**Option A: Via Azure Portal**
1. Go to your Function App in Azure Portal
2. Click **"Deployment Center"**
3. Choose **"Local Git"** or **"ZIP Deploy"**
4. Upload `ABCRetailFunctions-deploy.zip`

**Option B: Via Azure CLI**
```bash
az functionapp deployment source config-zip `
  --resource-group AZ-JHB-RSG-VCKNFM-ST10449316-TER `
  --name st10449316-abcretailfunctions `
  --src ABCRetailFunctions-deploy.zip
```

### **Step 5: Verify Configuration**

Ensure these settings exist in Function App Configuration:
- `AzureStorage__ConnectionString`
- `AzureStorage__TableName` = `CustomerProfiles`
- `AzureStorage__ProductTableName` = `Products`
- `AzureStorage__OrderTableName` = `Orders`
- `AzureStorage__BlobContainerName` = `product-images`
- `AzureStorage__QueueName` = `order-processing`
- `AzureStorage__FileShareName` = `contracts`

---

## 🧪 Testing Guide

### **Test 1: Store Customer Profile** ✅

**Function**: `StoreCustomerProfile`
**Method**: POST
**Route**: `/api/customers`

**Test Body**:
```json
{
  "FirstName": "John",
  "LastName": "Doe",
  "Email": "john.doe@example.com",
  "Phone": "0821234567",
  "Address": "123 Main Street, Johannesburg"
}
```

**Expected Result**: `200 OK` with customer stored in `CustomerProfiles` table

---

### **Test 2: Upload Blob** ⚠️

**Function**: `UploadBlob`
**Method**: POST
**Route**: `/api/blobs/upload`
**Query**: `?fileName=test-image.jpg`

**Note**: Requires multipart/form-data (can't test easily in portal)
**Alternative**: Test via PowerShell or Postman

---

### **Test 3: Upload File** ✅

**Function**: `UploadFile`
**Method**: POST
**Route**: `/api/files/upload`
**Query**: `?fileName=contract.pdf`

**Test Body**: `This is a test contract document`

**Expected Result**: `200 OK` with file stored in `contracts` file share

---

### **Test 4: Submit Order** ✅ **IMPORTANT!**

**Function**: `SubmitOrder`
**Method**: POST
**Route**: `/api/orders/submit`

**Test Body**:
```json
{
  "CustomerRowKey": "customer-guid-here",
  "ProductRowKey": "product-guid-here",
  "CustomerName": "John Doe",
  "ProductName": "Laptop",
  "Quantity": 2,
  "UnitPrice": 15999.99,
  "Notes": "Express delivery please"
}
```

**Expected Flow**:
1. HTTP request received ✅
2. Order added to `order-processing` queue ✅
3. Queue trigger fires automatically ✅
4. Order stored in `Orders` table ✅

**Expected Result**: `200 OK` with message "Order submitted successfully and queued for processing"

---

### **Test 5: Verify Queue Trigger** ✅

**How to verify the queue trigger worked:**

1. **Check the Queue**:
   - Go to Storage Account → Storage browser → Queues → `order-processing`
   - After submitting order, queue should be empty (message processed)

2. **Check the Orders Table**:
   - Go to Storage Account → Storage browser → Tables → `Orders`
   - You should see your order with Status = "Processing"

3. **Check Function Logs**:
   - Go to Function App → Functions → `ProcessOrderFromQueue`
   - Click "Monitor" or "Logs"
   - You should see: "Successfully processed and stored order {orderId} from queue"

---

## 📸 Screenshots for Assignment

### **Required Screenshots:**

1. **Functions List**
   - Show all 4 storage type functions
   - Highlight the Queue Trigger function

2. **Queue Trigger Function Code**
   - Show `ProcessOrderFromQueue` function
   - Highlight the `[QueueTrigger]` attribute

3. **Submit Order Test**
   - Show HTTP request with order data
   - Show 200 OK response

4. **Queue Processing**
   - Show empty queue (after processing)
   - OR show message in queue before processing

5. **Orders Table**
   - Show order stored in Orders table
   - Show Status = "Processing"

6. **Function Logs**
   - Show logs from `ProcessOrderFromQueue`
   - Show successful processing message

---

## 🎯 How This Meets Requirements

### ✅ **Requirement 1**: Create 4 functions (one for each storage type)
- **Table**: `StoreCustomerProfile`, `StoreProduct`
- **Blob**: `UploadBlob`
- **Queue**: `SubmitOrder` (HTTP) + `ProcessOrderFromQueue` (Queue Trigger)
- **File**: `UploadFile`

### ✅ **Requirement 2**: At least 1 Queue triggered function
- `ProcessOrderFromQueue` - Triggered by `order-processing` queue
- Writes to Orders table

### ✅ **Requirement 3**: Functions use Services from Part 1
- All functions inject `IAzureStorageService`
- Service methods called: `AddOrderAsync`, `SendMessageAsync`, etc.

### ✅ **Requirement 4**: Services call Functions via HTTP/Queue
- Web app services send HTTP requests to functions
- Orders sent to queue, not directly to storage

### ✅ **Requirement 5**: Orders only update queue, not storage directly
- `SubmitOrder` function adds to queue
- `ProcessOrderFromQueue` (queue trigger) updates storage
- **No direct storage writes for orders!**

---

## 🔍 Common Issues & Solutions

### **Issue 1**: Queue trigger not firing

**Solution**:
- Check `AzureStorage__ConnectionString` is set correctly
- Verify queue name is `order-processing`
- Check Function App logs for errors
- Restart Function App

### **Issue 2**: "Connection string not found"

**Solution**:
- Add all required configuration settings (see Step 5 above)
- Use double underscores `__` not single `_`
- Restart Function App after adding settings

### **Issue 3**: Order not appearing in table

**Solution**:
- Check queue trigger logs for errors
- Verify Orders table exists in storage account
- Check if message is stuck in queue (poison queue)

### **Issue 4**: 500 Internal Server Error

**Solution**:
- Check Logs tab in Function App
- Verify all configuration settings are present
- Check storage account connection string is correct
- Ensure tables/containers/queues exist

---

## 📚 Additional Resources

- [Azure Functions Queue Trigger](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger)
- [Azure Table Storage](https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview)
- [Azure Queue Storage](https://learn.microsoft.com/en-us/azure/storage/queues/storage-queues-introduction)
