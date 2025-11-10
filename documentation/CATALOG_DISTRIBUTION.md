# Software Catalog Distribution Guide

**Created:** 2025-11-07 (Session 9)
**Purpose:** Guide for distributing the software catalog database to end users

---

## 📦 Overview

NoFences uses a software catalog database to accurately categorize applications and games. This guide explains how to build, distribute, and download the catalog.

---

## 🔨 Building the Master Catalog

### 1. Prepare CSV Files

Place your CSV files in `_software_list/` directory:
- `Software.csv` - General software entries (~9,000 entries)
- `steam.csv` - Steam game database (~76,988 entries)

### 2. Run the Import Tool

```cmd
# Build the application
msbuild NoFences.sln /p:Configuration=Release /p:Platform="Any CPU"

# Run the catalog importer
cd NoFences\bin\Release
NoFences.exe --import-catalog ..\..\..\\_software_list master_catalog.db 10000

# Output: master_catalog.db (~10-15 MB)
```

### 3. Verify the Database

The tool will show statistics:
```
Catalog Version:      1
Total Software:       9,000 entries
Total Games:          10,000 entries
Database size:        12.34 MB
```

---

## 🌐 Hosting the Catalog

### Option 1: Static File Hosting

Upload `master_catalog.db` to any web server:
- GitHub Releases
- Azure Blob Storage
- AWS S3
- Your own web server

Example URL:
```
https://yourserver.com/catalogs/software_catalog.db
```

### Option 2: CDN Distribution

For better performance, use a CDN:
```
https://cdn.yourserver.com/nofences/v1/software_catalog.db
```

### Versioning

Include version in URL for easy updates:
```
https://yourserver.com/catalogs/v1/software_catalog.db
https://yourserver.com/catalogs/v2/software_catalog.db
```

---

## 📥 Client-Side Download

### Automatic Download (First Run)

NoFences automatically downloads the catalog on first run if local catalog doesn't exist.

**Code Example:**
```csharp
using NoFencesDataLayer.Services;

// Download catalog on first run
if (!SoftwareCatalogInitializer.IsCatalogInitialized())
{
    var catalogUrl = "https://yourserver.com/catalogs/software_catalog.db";
    var localPath = Path.Combine(AppEnvUtil.GetAppEnvironmentPath(), "software_catalog.db");

    bool success = CatalogDownloadService.DownloadCatalog(catalogUrl, localPath,
        progress => {
            Console.WriteLine($"Downloading: {progress}%");
        });

    if (success)
    {
        // Import downloaded catalog into local database
        // ... initialization code ...
    }
}
```

### Manual Update

Users can manually update their catalog:
```csharp
// Check if update is available
var catalogUrl = "https://yourserver.com/catalogs/software_catalog.db";
bool available = CatalogDownloadService.CheckCatalogAvailability(catalogUrl);

if (available)
{
    long? size = CatalogDownloadService.GetRemoteCatalogSize(catalogUrl);
    Console.WriteLine($"Update available: {size / 1024 / 1024} MB");

    // Download latest version
    CatalogDownloadService.DownloadCatalog(catalogUrl, localPath);
}
```

---

## 🔄 Update Strategy

### Version Tracking

The database includes version tracking:
- **CatalogVersion** table stores current version number
- **LastUpdated** timestamp shows when catalog was last modified
- **ChangeLog** table records all changes

### Incremental Updates (Future)

For future implementation:
1. Client checks remote version number
2. If remote > local, download only changes since last version
3. Apply changes incrementally
4. Update local version number

**Benefits:**
- Faster updates (KB instead of MB)
- Reduced bandwidth
- Less download time

---

## 📊 Catalog Statistics

### Expected Sizes

| Component | Size | Entries |
|-----------|------|---------|
| Software entries | ~2 MB | 9,000 |
| Game entries | ~8 MB | 10,000 |
| Indexes | ~2 MB | 8 indexes |
| **Total** | **~12 MB** | **19,000** |

### Compression

For faster downloads, compress the database:
```bash
# Create compressed version
gzip master_catalog.db

# Result: master_catalog.db.gz (~3-4 MB)
```

Client should decompress after download.

---

## 🔐 Security Considerations

### HTTPS Required

Always host catalogs over HTTPS:
- Prevents man-in-the-middle attacks
- Ensures data integrity
- Required for modern web standards

### Integrity Verification (Recommended)

Provide checksums for verification:
```
https://yourserver.com/catalogs/software_catalog.db
https://yourserver.com/catalogs/software_catalog.db.sha256
```

**Client-side verification:**
```csharp
// Download checksum
string expectedHash = DownloadChecksum(url + ".sha256");

// Calculate file hash
string actualHash = CalculateSHA256(localPath);

if (expectedHash != actualHash)
{
    throw new Exception("Catalog integrity check failed!");
}
```

---

## 🚀 Deployment Workflow

### 1. Build New Version

```cmd
NoFences.exe --import-catalog _software_list master_catalog_v2.db 10000
```

### 2. Test Locally

```cmd
# Copy to test location
copy master_catalog_v2.db test_catalog.db

# Verify in application
```

### 3. Generate Checksum

```bash
sha256sum master_catalog_v2.db > master_catalog_v2.db.sha256
```

### 4. Upload to Server

```bash
# Upload both files
upload master_catalog_v2.db https://yourserver.com/catalogs/
upload master_catalog_v2.db.sha256 https://yourserver.com/catalogs/
```

### 5. Update Application Config

Point NoFences to new version:
```json
{
  "catalogUrl": "https://yourserver.com/catalogs/master_catalog_v2.db"
}
```

---

## 📝 Configuration

### Default URL

Change the default catalog URL in `CatalogDownloadService.cs`:
```csharp
private const string DEFAULT_CATALOG_URL = "https://yourserver.com/catalogs/software_catalog.db";
```

### User Configuration

Allow users to specify custom catalog URLs:
```csharp
// Settings file or registry
string customUrl = ConfigurationManager.AppSettings["CatalogUrl"];
CatalogDownloadService.DownloadCatalog(customUrl ?? DEFAULT_CATALOG_URL, localPath);
```

---

## 🎯 Best Practices

1. **Version your catalogs** - Use v1, v2, etc. in filenames
2. **Keep old versions** - Allow users to rollback if needed
3. **Compress for distribution** - Use gzip to reduce download size
4. **Provide checksums** - Ensure data integrity
5. **Use CDN** - For faster global distribution
6. **Monitor bandwidth** - Track download metrics
7. **Document changes** - Maintain changelog for each version

---

## 📞 Troubleshooting

### Download Fails

Check:
- URL is correct and accessible
- Firewall isn't blocking connection
- Certificate is valid (HTTPS)
- Sufficient disk space

### Catalog Won't Import

Verify:
- Database file isn't corrupted
- SQLite version compatibility
- File permissions are correct

### Size Too Large

Options:
- Reduce max game entries in import
- Remove unnecessary metadata
- Compress before distribution

---

## 🔮 Future Enhancements

1. **Delta Updates** - Download only changes
2. **Automatic Updates** - Background sync
3. **Multiple Sources** - Fallback URLs
4. **Peer-to-Peer** - Distribute via P2P
5. **Regional Catalogs** - Localized versions
6. **Custom Catalogs** - User-defined sources

---

**Ready to distribute your software catalog!** 🎉
