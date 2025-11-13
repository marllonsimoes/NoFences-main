# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

NoFences is a Windows desktop utility that creates "fences"—movable and resizable containers for organizing desktop files, folders, and dynamic content. The solution includes a background Windows service for automated file organization, backup, and synchronization tasks.

The codebase is currently undergoing a significant refactoring (see branch `gemini/fencewindow-refactor`) to migrate from WPF to a handler-based architecture for fence types.

## Build and Development Commands

### Building the Solution
```bash
# Restore and build entire solution
msbuild NoFences.sln -t:Restore
msbuild NoFences.sln /p:Configuration=Release /p:Platform="Any CPU"

# Debug build
msbuild NoFences.sln /p:Configuration=Debug /p:Platform="Any CPU"
```

### Running the Application
The main entry point is `NoFences/Program.cs`. The application uses a mutex to ensure only one instance runs at a time.

Build output locations:
- Debug: `NoFences/bin/Debug/NoFences.exe`
- Release: `NoFences/bin/Release/NoFences.exe`

#### Command-Line Mode: Master Catalog Importer
NoFences.exe supports a command-line mode for importing software catalog data:

```bash
# Import catalog data from CSV files into master database
NoFences.exe --import-catalog <input_dir> <db_path> [max_steam_games]

# Example
NoFences.exe --import-catalog _software_list master_catalog.db 10000

# With defaults (input: _software_list, db: master_catalog.db, max: 10000)
NoFences.exe --import-catalog
```

This mode:
- Allocates a console window for output (using AllocConsole P/Invoke)
- Imports Software.csv (~9,000 entries) and steam.csv (up to max specified games)
- Creates platform-agnostic master database (~10-15 MB)
- Shows progress with color-coded output
- Exits after import completes

Without `--import-catalog` flag, runs as normal GUI application.

### Database Migrations
The `NoFencesDataLayer` project uses Entity Framework 6 with SQLite. The database file (`ref.db`) is created automatically in the application data folder.

To add a new migration after modifying entities:
```bash
# From the NoFencesDataLayer directory
dotnet ef migrations add <MigrationName>
```

## Solution Architecture

### Project Dependencies
```
NoFences (main UI)
├── NoFencesCore (shared models/utilities)
├── NoFencesDataLayer (EF6 + SQLite)
├── NoFencesExtensions (shell extension)
└── NoFencesService (background service)

NoFencesService
├── NoFencesCore
└── NoFencesDataLayer
```

### Key Architectural Patterns

#### Handler-Based Fence System (Current Refactoring)
The codebase is migrating to a factory pattern for fence rendering:

- **`IFenceHandler`**: Interface defining fence behavior (click, drag/drop, paint, etc.)
- **`FenceHandlerFactory`**: Creates the appropriate handler based on `FenceInfo.Type`
- **`FenceWindow`**: WinForms window that delegates behavior to its assigned handler

Current handlers:
- `FilesFenceHandler`: Handles file/folder fences (EntryType.Files)
- `PictureFenceHandler`: Handles picture display (EntryType.Pictures)

To add a new fence type:
1. Create a new handler implementing `IFenceHandler` in `NoFences/View/Fences/Handlers/`
2. Add the new `EntryType` enum value in `NoFencesCore/Model/EntryType.cs`
3. Register the handler in `DependencyInjectionSetup.cs`

#### Dependency Injection
The application uses `Microsoft.Extensions.DependencyInjection` with `CommunityToolkit.Mvvm.DependencyInjection`:

- Setup: `NoFences/ApplicationLogic/DependencyInjectionSetup.cs`
- Singleton services: `FenceManager`, `FenceHandlerFactory`
- Transient services: Individual fence handlers

#### Application Services
`Program.cs` initializes multiple `IApplicationService` implementations:
- **`TrayIconManager`**: System tray icon and menu
- **`PipeService`**: Named pipe IPC for shell extension communication
- **`WindowsServiceManager`**: (Currently disabled) Manages the background service

#### Data Persistence Strategy
The solution uses two storage mechanisms:

1. **XML Files** (Fence UI state):
   - Location: Managed by `AppEnvUtil.GetAppEnvironmentPath()`
   - Format: Individual `__fence_metadata.xml` files per fence
   - Contains: Position, size, appearance, content references

2. **SQLite Database** (Service configuration):
   - File: `ref.db` in app data folder
   - Context: `LocalDBContext` (Entity Framework 6)
   - Entities:
     - `DeviceInfo`: Physical/logical storage devices
     - `MonitoredPath`: Folders monitored by the service
     - `FolderConfiguration`: File organization rules (filters, destinations, processors)
     - `BackupConfig`: Backup job definitions
     - `PendingRemoteSync`: Outbox for file sync operations

### Inter-Process Communication

**Named Pipe**: `NoFencesPipeServer`
- Used by shell extension to communicate with main app
- Allows context menu to send fence creation requests
- Implementation: `NoFences/ApplicationLogic/PipeService.cs`

### Windows Integration

The `NoFences/Win32/` directory contains P/Invoke utilities:
- **`DesktopUtil`**: Glues windows to desktop layer, prevents minimize
- **`BlurUtil`**: Applies Windows 10+ acrylic/blur effects
- **`DropShadow`**: Adds drop shadows to borderless windows
- **`WindowUtil`**: Hides from Alt+Tab, manages window messages
- **`IconUtil`**: Extracts icons from files/applications
- **`ShellContextMenu`**: Displays Windows shell context menus

## Important Code Locations

### Fence Management
- **`FenceManager`** (`NoFences/Model/FenceManager.cs`): Singleton managing fence lifecycle
- **`FenceInfo`** (`NoFencesCore/Model/FenceInfo.cs`): Fence data model (XML-serialized)
  - **WARNING**: Do not rename properties—they're used for XML serialization
- **`FenceWindow`** (`NoFences/View/FenceWindow.cs`): WinForms window with handler delegation

### Service Layer
- **`NoFencesService.cs`**: Windows service entry point
- Uses `FileSystemWatcher` for folder monitoring
- Uses `ManagementEventWatcher` (WMI) for device detection
- Logging via **log4net** (config: `log4net-service.xml`)

### Shell Extension
- **`NoFencesExtensions/NewFenceWithImagesExtension.cs`**: SharpShell-based context menu
- Adds "New Fence from here..." to Explorer right-click menu
- Intelligently determines fence type based on selected files

## Development Notes

### Current Refactoring Status
The branch `gemini/fencewindow-refactor` is actively migrating:
- ✅ From: Monolithic `FenceWindow` with type-based conditionals
- ✅ To: Handler pattern with `IFenceHandler` implementations
- ⚠️ In progress: Not all fence types have handlers yet

Missing handlers for:
- `EntryType.Folder`
- `EntryType.Clock`
- `EntryType.Widget`

### Target Framework
- Main projects: .NET Framework 4.8.1
- Core library: .NET Framework 4.8.1 (was .NET Standard, migrated back for compatibility)

### Key NuGet Packages
- **MahApps.Metro**: Modern WPF UI framework
- **CommunityToolkit.Mvvm**: MVVM helpers and DI
- **EntityFramework 6.5.1**: ORM for SQLite
- **System.Data.SQLite**: SQLite database provider
- **log4net**: Logging for background service
- **SharpShell**: Shell extension framework

### Git Workflow
Recent commits indicate:
- WPF components are being removed/refactored
- Package references migrated from packages.config to PackageReference
- GitHub Actions CI/CD pipeline builds and packages releases

### Testing
⚠️ **No test projects currently exist** in the solution. Consider this when adding new functionality.

## Common Tasks

### Adding a New Fence Type
1. Add enum value to `NoFencesCore/Model/EntryType.cs`
2. Create handler class implementing `IFenceHandler` in `NoFences/View/Fences/Handlers/`
3. Register handler in `DependencyInjectionSetup.InitializeIoCContainer()`:
   ```csharp
   .AddTransient<IFenceHandler, YourNewHandler>()
   ```
4. Add to factory dictionary:
   ```csharp
   handlers[EntryType.YourType.ToString()] = typeof(YourNewHandler);
   ```

### Modifying Database Schema
1. Update entity classes in `NoFencesDataLayer/LocalDBContext.cs`
2. Create migration (if needed): `dotnet ef migrations add MigrationName`
3. Migration runs automatically on service startup via `MigrateDatabaseToLatestVersion`

### Debugging the Background Service
The service is currently referenced but not started in `Program.cs` (commented out). To debug:
1. Uncomment `WindowsServiceManager` in services list
2. Ensure elevated privileges if installing as Windows service
3. Check logs in application data folder (configured in `log4net-service.xml`)

## Desktop Integration Architecture (NEW - 2025)

### WorkerW Desktop Integration
The codebase now uses the **WorkerW method** for robust Windows desktop integration, similar to Lively and other modern wallpaper applications.

**Key Components:**

#### `WorkerWIntegration.cs` (`NoFences/Win32/WorkerWIntegration.cs`)
Main desktop integration utility:
```csharp
// Position behind desktop icons (wallpaper layer)
WorkerWIntegration.ParentToBehindDesktopIcons(windowHandle);

// Position above desktop icons (traditional behavior)
WorkerWIntegration.ParentToAboveDesktopIcons(windowHandle);

// Refresh when display changes
WorkerWIntegration.RefreshDesktopIntegration(windowHandle, behindIcons: true);
```

**How it works:**
1. Finds "Progman" window (desktop root)
2. Sends `0x052C` message to spawn WorkerW window
3. Enumerates windows to find WorkerW containing SHELLDLL_DefView
4. Parents target window to this WorkerW

#### `DesktopOverlayManager.cs` (`NoFences/View/DesktopOverlayManager.cs`)
Manages transparent fullscreen overlays that can host WPF components:
```csharp
var overlayManager = new DesktopOverlayManager(DesktopMode.AboveIcons);
overlayManager.Initialize();

// Get overlay for primary screen
var overlay = overlayManager.GetPrimaryOverlay();

// Add WPF content via ElementHost
overlay.Controls.Add(wpfHost);

// Change mode dynamically
overlayManager.SetDesktopMode(DesktopMode.BehindIcons);
```

Features:
- One transparent overlay per screen
- Automatic multi-monitor handling
- Display change monitoring
- Click-through capable
- Can host WPF and WinForms controls

#### `FenceInfo.BehindDesktopIcons` Property
Each fence can now specify its desktop layer:
```csharp
fenceInfo.BehindDesktopIcons = true;  // Behind icons (like wallpaper)
fenceInfo.BehindDesktopIcons = false; // Above icons (traditional)
```

#### Testing Tool (`DesktopIntegrationTest.cs`)
Run `DesktopIntegrationTest.ShowTestDialog()` to:
- Test behind/above desktop icons modes
- View integration information
- Verify WorkerW discovery
- Debug display change handling

### Desktop Layering

**Traditional Mode (Above Icons):**
```
[Fence Windows] ← Interactive, can be clicked
      ↓
[Desktop Icons]
      ↓
  [WorkerW]
      ↓
  [Progman]
```

**New Behind Icons Mode:**
```
[Desktop Icons]
      ↓
[Fence Windows] ← Behind icons, like wallpaper
      ↓
  [WorkerW]
      ↓
  [Progman]
```

### Display Change Handling
Both `FenceWindow` and `DesktopOverlayManager` automatically handle:
- Resolution changes
- Monitor add/remove
- DPI changes
- Desktop refresh

Events monitored: `SystemEvents.DisplaySettingsChanged`

### Migration Notes
**Old code:**
```csharp
DesktopUtil.GlueToDesktop(handle);
```

**New code (backward compatible):**
```csharp
// Same behavior (above icons)
DesktopUtil.GlueToDesktop(handle);

// Or explicitly specify mode
DesktopUtil.GlueToDesktop(handle, behindIcons: false);
```

See `DESKTOP_INTEGRATION_REFACTORING.md` for complete documentation.

## Security Considerations

### Elevation
- Code includes `ElevationManager` for admin privilege escalation
- Currently disabled in `Program.cs` (lines 28-33)
- Required for Windows service installation/management

### File Operations
- The service monitors arbitrary folder paths configured by users
- File organization rules execute automatically via `FileSystemWatcher`
- Backup operations may access sensitive locations

### Shell Extension
- Runs in Explorer process space
- Uses named pipes for IPC (potential security boundary)
- COM registration required (via SharpShell)

### Desktop Integration Security
- WorkerW manipulation is standard on Windows (used by wallpaper apps)
- No elevated privileges required for desktop parenting
- Display change events are system-level, properly handled
- Add new files to the project so they are compiled. Don't forget to add new dependencies as well, if required.
- you can't execute windows commands in this environment, as we are in WSL and the project is on windows. I can execute the commands and get back with the results.
- remember to update the developer journal in SESSION_CHANGES.html with the latest changes we are performing

## Development Documentation

The project maintains comprehensive session documentation in the `documentation/` directory:

- **Master Index:** `documentation/SESSION_INDEX.html` - Overview of all sessions with navigation
- **Session Files:** `documentation/sessions/session-XX-*.html` - Individual session documentation
- **Current Work:** `SESSION_CHANGES.html` - Active development log (root directory)

### Documentation Structure

The documentation was reorganized in Session 8 into modular files for better maintainability:

```
documentation/
├── SESSION_INDEX.html          # Master index with session summaries
├── README.md                   # Documentation guide
└── sessions/                   # Individual session files (8 sessions)
    ├── session-00-canvas-architecture.html
    ├── session-01-bug-fixes.html
    ├── session-03-image-preprocessing.html
    ├── session-04-smart-filtering.html
    ├── session-05-game-detection.html
    ├── session-06-multi-platform-games.html
    ├── session-07-ui-modernization.html
    └── session-08-sprint-refactoring.html
```

### Updating Documentation

When documenting work:
1. Add details to `SESSION_CHANGES.html` during active development
2. When session is complete, extract to new `session-XX-*.html` file in `documentation/sessions/`
3. Update `documentation/SESSION_INDEX.html` with new session card
4. Update `documentation/README.md` with session summary
5. Clear `SESSION_CHANGES.html` for next session

### Documentation Features

Each session file includes:
- **Objectives** - Session goals
- **Implementation** - Technical details with code samples
- **Files Modified** - Complete change list
- **Testing** - Validation steps
- **Summary** - Achievements and outcomes
- remember to incluse log4net where needed instead of writting logs to the console or Debut.Write...
- code must be compatible with .net 4.8.1. Don't use features from .net 8.
- Session changes.html is for the current session; session index contains the a list to acces the other sessions, with a summary for each. and there are the session's files. keep the same pattern
- check the nuget packages we have so we don't have many different ways to do the same thing. Newtonsoft.JSON is one of the libraries that should be use, as well as log4net when loggin/debugging.
- let's estabilish some boundareis: any data collection should be added to the datalayer. If it provides data of some kind, it must become a repository. If there are business logic, should go to the services. Common classes, utils, helpers should go to the Core module. UI will be in the main module.
- don't add session comments to the changes we are performing in the code.