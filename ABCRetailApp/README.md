# ABC Retail App

A simple **ASP.NET Core MVC** application that demonstrates core **Azure Storage** services:

* **Azure Table Storage** – structured, schemaless entities
* **Azure Blob Storage** – file/image uploads & downloads
* **Azure Queue Storage** – lightweight async messaging
* **Azure Files** – SMB-based file shares

The UI uses a clean **Orange / Black / White** theme.

**Live site:** [https://st10449316abcretails.azurewebsites.net/](https://st10449316abcretails.azurewebsites.net/)

---

## ✨ Highlights

* End‑to‑end CRUD flows for products and storage-backed entities
* Upload/download blobs, list containers, and view object metadata
* Send/read queue messages for demo workflows
* Examples for using Table Storage entities and queries
* File Share interactions (create/list/share paths)
* Production‑ready config via **appsettings.json** *or* environment variables
* Trim, tidy codebase (seeders & sample data providers **removed**) so you can bring your own data

> 🔐 **Security-first:** No secrets in repo. Use environment variables or a secret store (Azure Key Vault) for connection strings.

---

## 🧱 Architecture

* **ASP.NET Core MVC** on **.NET 8**
* Storage SDK: **Azure.Data.Tables**, **Azure.Storage.Blobs**, **Azure.Storage.Queues**, **Azure.Storage.Files.Shares**
* Layered folders: `Controllers`, `Services` (optional), `Models`, `Views`, `wwwroot`

```
ABC-Retail-App/
├── Controllers/           # MVC controllers (Products, Blobs, Queues, Files, Tables)
├── Models/                # POCOs & view models
├── Services/              # Storage helpers/wrappers (if present)
├── Views/                 # Razor pages & partials
├── wwwroot/               # Static assets (css/js/img)
├── appsettings.json       # App config (keep secrets OUT of source control)
├── Program.cs             # Host & middleware pipeline
└── README.md
```

---

## 🚀 Getting Started

### 1) Prerequisites

* **.NET 8 SDK** – [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
* **Azure subscription** & **Storage account** (or **Azurite** local emulator)
* **Visual Studio 2022**, **VS Code**, or **Rider**

### 2) Clone

```bash
git clone https://github.com/<your-username>/abc-retail-app.git
cd abc-retail-app
```

### 3) Configure storage

Choose **one** of the options below.

**Option A – appsettings.json**

```json
{
  "ConnectionStrings": {
    "AzureStorage": "<your-azure-storage-connection-string>"
  }
}
```

**Option B – Environment variable** (recommended for prod)

* **Windows (PowerShell):**

  ```powershell
  $Env:AzureStorage = "<your-azure-storage-connection-string>"
  ```
* **macOS/Linux (bash):**

  ```bash
  export AzureStorage="<your-azure-storage-connection-string>"
  ```

> If you also use Azure Functions locally, you might see `AzureWebJobsStorage`. For this MVC app, the default is `AzureStorage`, but you can rename via `Program.cs`/config binding if needed.

**Using Azurite (local emulator)**

```bash
# Install (if needed)
npm i -g azurite
# Start emulator
azurite --location ./_azurite --silent --debug ./_azurite/debug.log
# Default connection string
export AzureStorage="UseDevelopmentStorage=true"
```

### 4) Run

```bash
# Restore, build, run
dotnet restore
dotnet build
dotnet run
```

App runs at **[http://localhost:5000](http://localhost:5000)** (or the port shown in console).

---

## 🔧 Azure Setup (Quick CLI)

```bash
# Login & create a resource group
az login
az group create -n rg-abc-retail -l southafricanorth

# Create a general-purpose v2 storage account
az storage account create \
  -n abcretailstor$RANDOM \
  -g rg-abc-retail \
  -l southafricanorth \
  --sku Standard_LRS \
  --kind StorageV2

# Show connection string
az storage account show-connection-string \
  -n <your-storage-account-name> -g rg-abc-retail -o tsv
```

> For blob images used by the demo UI, you may want a container with limited public read access (or use SAS tokens). Consider private containers for production and serve via your app.

---

## 📚 Feature Guide

### Tables

* Create/Query/Update/Delete entities
* Partition & Row keys exposed for learning

### Blobs

* Upload files (images/docs)
* List containers & blobs
* Download & delete

### Queues

* Send messages from UI
* Peek/dequeue for demo processing

### Files (Azure Files)

* Create shares & directories
* Upload/list files via SDK

> ⚠️ Since **seeders** were removed, create your own sample data via the UI or seed through scripts.

---

## 🖼️ Theming

* **Primary:** Orange
* **Neutrals:** Black / White
* Responsive layout with simple components and accessible contrast. Customize in `wwwroot/css` as needed.

---

## 🔒 Security Notes

* **Never commit** secrets (connection strings, keys) to Git.
* Prefer **Key Vault** or deployment‑slot app settings.
* Use **SAS tokens** or **Managed Identity** where applicable.
* Validate file uploads; set reasonable size limits & content checks if exposing publicly.

---

## 🧪 Local Testing Tips

* Use **Azurite** for offline development
* Add a few products via the **Tables**/Products UI
* Upload sample images to **Blobs** and reference their URLs in your views
* Send a **Queue** message and verify dequeue

---

## 🐞 Troubleshooting

* **403/404 on blobs** – Check container access level or use SAS
* **Queue not receiving** – Verify connection string & queue name; ensure emulator/service is running
* **Table entity conflicts** – Ensure unique PartitionKey/RowKey pairs
* **Azure Files errors on Linux/macOS** – Requires SMB support; prefer SDK interactions if mounting is troublesome

View runtime logs:

```bash
dotnet run --verbosity normal
```

---

## 📦 Deployment

### Azure App Service (quick path)

1. Build & publish

   ```bash
   dotnet publish -c Release -o ./publish
   ```
2. Create App Service & deploy (via VS/CLI/GitHub Actions)
3. Set **Application Settings** in Azure Portal:

   * `AzureStorage` = `<your-connection-string>`
4. Restart app and verify `/health` or home page.

### GitHub Actions (sample)

Add a workflow that:

* Checks out
* Sets up .NET 8
* Publishes app
* Deploys to Azure WebApp using a publish profile/credentials

---

## 🧭 Roadmap (ideas)

* ✅ Basic CRUD & storage demos
* ⏳ Add pagination & search on table lists
* ⏳ Add image thumbnails via Azure Functions
* ⏳ Enable MSI + role assignments to drop connection strings


---

## 🔗 Useful Links

* **Live App:** [https://st10449316abcretails.azurewebsites.net/](https://st10449316abcretails.azurewebsites.net/)
* Azure Storage SDKs: [https://learn.microsoft.com/azure/storage/](https://learn.microsoft.com/azure/storage/)
* Azurite Emulator: [https://learn.microsoft.com/azure/storage/common/storage-use-azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite)
* Queue/Blob/Table quickstarts: [https://learn.microsoft.com/azure/storage/common/storage-introduction](https://learn.microsoft.com/azure/storage/common/storage-introduction)
