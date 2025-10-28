# NoFences - Technical Overview

NoFences is a desktop utility for Windows designed to help organize your desktop by creating "fences"—movable and resizable containers for files, folders, and other dynamic content. It includes a background service for automated file organization, backup, and synchronization tasks.

This document provides a technical deep-dive for developers.

## Project Goal

The primary goal of NoFences is to provide a user-friendly way to reduce desktop clutter and automate file management. It is inspired by applications like Stardock Fences, but extends the concept with a configurable background service that can monitor folders, organize files based on rules, manage backups, and synchronize data.

## Modules (Projects)

The solution is modular, with each project handling a distinct responsibility.

### `NoFences` (Main Application)
This is the primary user-facing application and entry point.

- **Technology**: It is a hybrid application using Windows Forms for the system tray icon (`NotifyIcon`) and main application lifecycle, and **WPF** for all modern UI, including the fence windows themselves and configuration dialogs.
- **UI Components**:
    - `FenceWindow`: The WPF window that represents a single fence on the desktop. Its content and behavior change based on the `FenceInfo.Type`.
    - `EditDialog`: A dialog for editing the properties of an existing fence.
    - `View/Modern/`: Contains modern WPF views and viewmodels (e.g., `FolderConfigurationViewModel`) for configuring the background service.
- **Core Logic**:
    - `Program.cs`: The application entry point. It manages the tray icon, handles application startup/shutdown, starts the `NoFencesService`, and listens for messages from other processes via a `NamedPipeServerStream`.
    - `Model/FenceManager.cs`: A singleton class that manages the lifecycle of fences—loading them from storage, creating new ones, and saving their state.
- **Win32 Interop**: The `Win32/` directory contains utility classes for interacting with the Windows API, enabling features like desktop integration, window transparency (`BlurUtil`), and drop shadows (`DropShadow`).

### `NoFencesCore` (Shared Library)
A .NET Standard library containing shared models and utilities used across the solution.

- **Core Models**:
    - `Model/FenceInfo.cs`: The central data model representing a fence. It contains properties for position, size, appearance, and content.
    - `Model/EntryType.cs`: An enum that defines the different types of fences available.
- **Utilities**:
    - `Util/AppEnvUtil.cs`: Provides a consistent way to access application-specific folders (e.g., for settings, logs, and fence data).
    - `Util/FileUtils.cs`: Contains helper methods for file operations.

### `NoFencesDataLayer` (Data Persistence)
This project handles all database interactions for the application's background services.

- **Technology**: Uses **Entity Framework 6** and a **SQLite** database (`ref.db`).
- **`LocalDBContext.cs`**: The main `DbContext` class that defines the database schema through a set of `DbSet` properties.
- **Database Objects (Entities)**:
    - `DeviceInfo`: Represents a physical or logical storage device. It stores a unique ID, name, and flags (`IsRemovable`, `IsBackupDevice`) to track devices reliably.
    - `MonitoredPath`: Represents a folder to be monitored by `NoFencesService`. It is linked to a `DeviceInfo` and can have multiple `FolderConfiguration` rules.
    - `FolderConfiguration`: A specific rule for file organization. It defines a file filter (`.pdf`, `.docx`), a destination folder, and optional processors for renaming, allowing for powerful, automated workflows (e.g., "move all PDFs from Downloads to Z:/Documents/PDFs").
    - `BackupConfig`: Defines a backup job, specifying a source, destination, schedule, and options like compression and incremental backups.
    - `PendingRemoteSync`: An outbox table used to track file changes (creations, modifications, deletions) that need to be synchronized to a remote location.

### `NoFencesService` (Windows Service)
A background service that performs automated file management tasks without requiring the main UI to be open.

- **Functionality**:
    - **File System Monitoring**: Uses `FileSystemWatcher` to monitor folders defined in the `MonitoredPath` table for changes.
    - **Device Monitoring**: Uses `WqlEventQuery` and `ManagementEventWatcher` to detect when new storage devices are connected or removed from the system.
    - **Task Orchestration**: Reads its configuration from the database and executes file organization, backup, or sync tasks accordingly.
- **Logging**: Uses **log4net** for robust logging of its background activities.

### `NoFencesExtensions` (Shell Extension)
This project extends the Windows Explorer shell to provide seamless integration with NoFences.

- **Technology**: Uses the **SharpShell** library to create a COM-based shell context menu extension.
- **`NewFenceWithImagesExtension.cs`**: Adds a "New Fence from here..." option to the right-click menu in Windows Explorer.
- **Workflow**: When a user selects one or more files and clicks the menu item, the extension creates a `FenceInfo` object, intelligently determines the best `EntryType` (e.g., `Picture`, `SlideShow`, or `File`), and sends it to the main `NoFences` application via a named pipe to instantly create the new fence.

### Installer Projects
- **`NoFences.Installer` (WiX)** and **`NoFencesInstaller` (Visual Studio Installer)**: Projects for building a distributable installer for the application.

## Architecture and Key Concepts

### Fence Types
A core concept in NoFences is that a fence can have different behaviors based on its `EntryType`. The main types include:
- **`File`**: The default type. A container for a list of shortcuts to files and folders.
- **`Folder`**: Displays the live contents of a single folder.
- **`Picture`**: Displays a single static image.
- **`SlideShow`**: Displays images from a specified folder in a slideshow format.
- **`TodoItems`**: A simple checklist or to-do list.
- **`Clock`**: Displays the current time.

The behavior is controlled within `FenceWindow.cs`, which alters its UI and event handlers based on the `Type` property of its `FenceInfo` data model.

### Data Persistence Strategy
The application uses a dual-strategy for data storage, which separates UI state from service configuration:
1.  **XML Files**: The `NoFences` application stores the state of the visual fences (position, size, content) in individual XML files (`__fence_metadata.xml`). This makes the UI state portable and easy to debug.
2.  **SQLite Database**: The `NoFencesService` uses a central SQLite database for all its configuration, including folder rules, device information, and backup jobs. This provides a transactional and relational store for complex background tasks.

### Inter-Process Communication (IPC)
A **Named Pipe** (`NoFencesPipeServer`) is used for communication between the `NoFencesExtensions` shell extension and the main `NoFences` application. This allows the context menu to send a message to the running application to create a new fence.

## Future Development Guide

### Suggested Improvements

1.  **Add a Unit Testing Project**: The solution lacks automated tests. Adding projects for unit and integration tests (e.g., using MSTest, NUnit, or xUnit) would significantly improve code quality and reduce regressions.
2.  **Consolidate Data Persistence**: The dual-storage strategy works but could be unified. Migrating the fence state from XML files into the SQLite database would create a single, consistent data source and simplify data management.
3.  **Refactor `Program.cs`**: The main `Program.cs` file has a large number of responsibilities (UI management, service control, IPC, process elevation). This logic should be refactored into smaller, single-responsibility classes to improve maintainability.
4.  **Complete WPF Migration**: The project is a mix of WinForms and WPF. A full migration to WPF would modernize the codebase, simplify the UI layer, and remove legacy dependencies.
5.  **Dependency Injection in `NoFencesService`**: The service currently instantiates its dependencies directly. Implementing dependency injection would make it more modular and easier to test.

### Potential New Features

- **Cloud Sync**: Implement functionality to synchronize fence layouts and service configurations across multiple devices using a cloud service (e.g., OneDrive, Google Drive, or a custom backend).
- **Advanced Rule Engine**: Improve the file organization rules engine, allowing for more complex conditions (e.g., based on file metadata like EXIF data) and actions (e.g., running a script).
- **Plugin Marketplace**: Develop a more robust plugin system and a simple marketplace or repository where users can discover and install new types of fences or service extensions.
- **UI/UX Overhaul**: Redesign the configuration UI to be more intuitive and user-friendly, perhaps with a wizard-style setup for new rules.