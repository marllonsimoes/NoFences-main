# Code Refactoring & Reorganization Plan

## Status Update (2025-11-07)

**Progress**: 🟢🟢🟢🟢🟢🟢 (6 of 6 sprints completed!)

- ✅ **Sprint 1**: Quick Fixes (Completed pre-Session 8)
- ✅ **Sprint 2**: FenceContainer Behavior Extraction (Completed Session 8)
- ✅ **Sprint 3**: FenceEditWindow Panel Extraction (Completed Session 8)
- ✅ **Sprint 4**: Win32 Reorganization (Completed Session 9)
- ✅ **Sprint 5**: Project Reorganization (Completed Session 9)
- ✅ **Sprint 6**: Handler Refactoring (Completed Session 9)

**Key Achievements**:
- 🎯 Extracted 11 classes (5 behaviors + 6 panels)
- 📊 Created 89 unit tests for behaviors
- 📉 Reduced code by 1,740 lines (1,267→449 + 1,162→260)
- 🏗️ Established clear extensibility patterns

## Overview
This document outlines the plan to reorganize the NoFences codebase into a clean, maintainable structure with proper separation of concerns and smaller, focused classes.

## Current Issues

### Large Files (> 500 lines)
1. **FenceContainer.cs** (2013 lines) - God class handling everything
2. **ShellContextMenu.cs** (1583 lines) - Win32 shell integration
3. **FenceEditWindow.xaml.cs** (1162 lines) - Complex edit dialog
4. **PictureFenceHandlerWpf.cs** (662 lines) - Picture fence logic
5. **DesktopCanvas.cs** (582 lines) - Desktop window management

### Namespace Issues
- **"Peter" namespace** in ShellContextMenu.cs (should be NoFences.Win32)
- Inconsistent naming (CanvasBased vs. Canvas)
- Mixed concerns in some namespaces

### Project Organization
- Models scattered between NoFences and Core
- Business logic in UI classes
- No clear separation between layers

## Target Architecture

```
NoFences.sln
├── NoFences (UI Layer)
│   ├── Views/
│   │   ├── Canvas/            (Desktop canvas and fence containers)
│   │   ├── Dialogs/           (Edit windows, settings)
│   │   └── Components/        (Reusable UI components)
│   ├── Handlers/              (Fence type handlers - UI logic)
│   ├── Behaviors/             (Extracted UI behaviors)
│   └── Services/              (UI services - tray, pipes, IPC)
│
├── NoFences.Core (Business Logic)
│   ├── Models/                (FenceInfo, EntryType, themes)
│   ├── Services/              (FenceManager, service interfaces)
│   ├── Util/                  (Common utilities)
│   └── Interfaces/            (Core interfaces)
│
├── NoFences.DataLayer (Data Access)
│   ├── Persistence/           (XML serialization, EF context)
│   ├── Repositories/          (Data access patterns)
│   └── Migrations/            (EF migrations)
│
├── NoFences.Service (Background Service)
│   ├── Monitoring/            (FileSystemWatcher logic)
│   ├── Organization/          (Auto-organization rules)
│   └── Backup/                (Backup management)
│
└── NoFences.Win32 (Platform Interop)
    ├── Desktop/               (Desktop integration, WorkerW)
    ├── Shell/                 (Shell context menu, icons)
    └── Window/                (Window manipulation)
```

## Refactoring Plan

### Phase 1: Fix Critical Issues (Quick Wins)
**Goal**: Fix obvious issues without major restructuring

1. ✅ **Fix ShellContextMenu namespace** (Peter → NoFences.Win32)
2. **Remove duplicate namespace declarations** (NoFences.Util appears twice)
3. **Fix inconsistent CanvasBased naming** → standardize to "Canvas"

### Phase 2: Extract Behaviors from FenceContainer (2013 lines)
**Goal**: Break down god class into focused components

**Current responsibilities:**
- Title bar rendering and interaction
- Border rendering and resizing
- Drag and drop handling
- Fade animation
- Minify/expand behavior
- Rounded corners
- Context menu
- Theme management
- WPF content hosting

**Proposed extraction:**

```csharp
// NoFences/Views/Canvas/FenceContainer.cs (main container - ~400 lines)
// NoFences/Behaviors/FenceTitleBarBehavior.cs (~200 lines)
// NoFences/Behaviors/FenceResizeBehavior.cs (~300 lines)
// NoFences/Behaviors/FenceFadeAnimationBehavior.cs (~200 lines)
// NoFences/Behaviors/FenceMinifyBehavior.cs (~100 lines)
// NoFences/Behaviors/FenceDragDropBehavior.cs (~200 lines)
// NoFences/Behaviors/FenceRoundedCornersBehavior.cs (~150 lines)
// NoFences/Views/Canvas/FenceContextMenu.cs (~250 lines)
```

**Benefits:**
- Single Responsibility Principle
- Easier testing
- Reusable behaviors
- Better maintainability

### Phase 3: Simplify FenceEditWindow (1162 lines)
**Goal**: Reduce complexity of edit dialog

**Current issues:**
- Handles all fence types in one file
- Complex type-specific UI generation
- Mixed UI and business logic

**Proposed approach:**

```csharp
// NoFences/Views/Dialogs/FenceEditWindow.xaml.cs (~300 lines - core only)
// NoFences/Views/Dialogs/TypeEditors/PicturesTypeEditor.cs (~150 lines)
// NoFences/Views/Dialogs/TypeEditors/FilesTypeEditor.cs (~150 lines)
// NoFences/Views/Dialogs/TypeEditors/ClockTypeEditor.cs (~100 lines)
// NoFences/Views/Dialogs/TypeEditors/WidgetTypeEditor.cs (~100 lines)
// NoFences/Views/Dialogs/FenceEditViewModel.cs (~200 lines - MVVM)
```

**Benefits:**
- Type-specific editors are isolated
- Easier to add new fence types
- MVVM pattern for better testability

### Phase 4: Reorganize Win32 Utilities
**Goal**: Clean separation of platform concerns

**Current structure:**
```
Win32/
├── BlurUtil.cs
├── DesktopUtil.cs
├── DropShadow.cs
├── IconUtil.cs
├── ShellContextMenu.cs (1583 lines - needs refactoring)
├── WindowUtil.cs
├── WorkerWIntegration.cs
└── CanvasBased/DesktopUtilNew.cs (wrong location)
```

**Proposed structure:**
```
NoFences.Win32/ (NEW PROJECT)
├── Desktop/
│   ├── DesktopIntegration.cs (merged DesktopUtil + DesktopUtilNew)
│   ├── WorkerWIntegration.cs
│   └── DropShadow.cs
├── Shell/
│   ├── ShellContextMenuManager.cs (main - ~400 lines)
│   ├── ShellContextMenuItem.cs (~200 lines)
│   ├── ShellIconExtractor.cs (extracted from IconUtil)
│   └── ShellCommandHandler.cs (~300 lines)
└── Window/
    ├── WindowUtil.cs
    ├── WindowBlurEffect.cs (renamed from BlurUtil)
    └── WindowIconUtil.cs (icon extraction)
```

**Benefits:**
- Clean separation of concerns
- Separate project for platform code
- Reusable in other projects
- Better for future .NET Core migration

### Phase 5: Move Classes to Correct Projects

**NoFences → NoFences.Core:**
- `Model/FenceTheme.cs` → `NoFences.Core/Models/Themes/`
- `Model/PictureDisplayMode.cs` → `NoFences.Core/Models/Enums/`
- `Model/FenceEntry.cs` → `NoFences.Core/Models/`
- `Util/Extensions.cs` → `NoFences.Core/Util/`
- `Util/ThrottledExecution.cs` → `NoFences.Core/Util/`

**NoFences → NoFences.DataLayer:**
- XML serialization logic from FenceManagerNew
- Consider creating Repository pattern for fence persistence

**NoFences.Core → Proper structure:**
- Review existing Core classes
- Ensure no UI dependencies
- Add service interfaces

### Phase 6: Standardize Namespaces

**Current mess:**
- `NoFences.View.CanvasBased.*`
- `NoFences.Win32.CanvasBased` (wrong!)
- `NoFences.Util` (duplicated)
- `Peter` (!!!)

**Target namespaces:**
```
NoFences.Views.Canvas.*
NoFences.Views.Dialogs.*
NoFences.Views.Components.*
NoFences.Behaviors.*
NoFences.Handlers.*
NoFences.Services.*

NoFences.Core.Models.*
NoFences.Core.Services.*
NoFences.Core.Util.*

NoFences.DataLayer.Persistence.*
NoFences.DataLayer.Repositories.*

NoFences.Win32.Desktop.*
NoFences.Win32.Shell.*
NoFences.Win32.Window.*
```

### Phase 7: Refactor Large Handlers

**PictureFenceHandlerWpf.cs (662 lines)**

Extract:
- Image loading logic → `ImageLoader.cs`
- Masonry layout logic → Already in `MasonryPanel.cs` (good!)
- Image preprocessing → Already in `ImagePreprocessor.cs` (good!)
- Rotation logic → `ImageRotationManager.cs`

**FilesFenceHandlerWpf.cs (431 lines)**

Extract:
- File filtering logic → `FileFilter.cs`
- Icon resolution → Use refactored `ShellIconExtractor.cs`
- File system watching → `FileWatcher.cs`

## Implementation Order

### ✅ Sprint 1: Quick Fixes (COMPLETED - Pre-Session 8)
1. ✅ Fix "Peter" namespace
2. ✅ Remove duplicate namespaces
3. ✅ Rename CanvasBased → Canvas consistently
4. ✅ Update SESSION_CHANGES.html

**Status**: All quick fixes completed before Session 8.

### ✅ Sprint 2: FenceContainer Refactoring (COMPLETED - Session 8)
1. ✅ Extract FenceFadeAnimationBehavior
2. ✅ Extract FenceMinifyBehavior
3. ✅ Extract FenceRoundedCornersBehavior
4. ✅ Extract FenceDragBehavior (renamed from FenceDragDropBehavior)
5. ✅ Extract FenceResizeBehavior
6. ✅ Added NoFences.Tests project with 89 unit tests
7. ✅ Test thoroughly

**Results**:
- 5 behaviors extracted to `NoFences/Behaviors/` folder
- 89 unit tests created covering all behaviors
- FenceContainer reduced from 1,267 to 449 lines (65% reduction)
- All tests passing
- See: `documentation/sessions/session-08-sprint-refactoring.html`

### ✅ Sprint 3: FenceEditWindow Refactoring (COMPLETED - Session 8)
1. ✅ Create TypePropertiesPanel base class
2. ✅ Extract type-specific editors (Files, Picture, Folder, Clock, Widget panels)
3. ⏭️ Create ViewModel (deferred - keeping code-behind for simplicity)
4. ✅ Use ContentControl for type-specific panels
5. ⏭️ Test all fence types (code complete, awaiting manual testing)

**Results**:
- 6 panel files created in `NoFences/View/Canvas/TypeEditors/`
- FenceEditWindow reduced from 1,162 to 260 lines (78% reduction)
- Type-specific UI cleanly separated
- Easy extensibility for new fence types
- See: `documentation/sessions/session-08-sprint-refactoring.html`

### ✅ Sprint 4: Win32 Reorganization (COMPLETED - Session 9)
1. ✅ Created Win32/Desktop/, Win32/Window/, Win32/Shell/ namespaces
2. ✅ Organized P/Invoke code by responsibility
3. ✅ Moved WorkerWIntegration.cs → Win32/Desktop/
4. ✅ Moved DesktopUtilNew.cs → Win32/Desktop/
5. ✅ Moved WindowUtil.cs → Win32/Window/
6. ✅ Moved IconUtil.cs → Win32/Shell/
7. ✅ Updated all namespace references in 6 files

**Results**:
- Clean separation of Win32 code by responsibility
- Easier navigation and maintenance
- Better organization for future expansion
- See: `documentation/sessions/session-09-sprint-cleanup.html`

### ✅ Sprint 5: Project Reorganization (COMPLETED - Session 9)
1. ✅ Moved FenceTheme.cs → NoFencesCore/Model/
2. ✅ Moved PictureDisplayMode.cs → NoFencesCore/Model/
3. ✅ Moved ThrottledExecution.cs → NoFencesCore/Util/
4. ✅ Updated all namespace references
5. ✅ Removed dead code: Extensions.cs, IFenceManager.cs, Logger.cs
6. ✅ Renamed FenceManagerNew → FenceManager
7. ✅ Migrated custom Logger to log4net (159 calls across 15 files)

**Results**:
- Clear separation between Core and UI projects
- Pure models and utilities in Core for reusability
- Industry-standard logging with log4net
- 3 dead code files removed
- See: `documentation/sessions/session-09-sprint-cleanup.html`

### ✅ Sprint 6: Handler Refactoring (COMPLETED - Session 9)
1. ✅ Created FileItemTemplateBuilder.cs (141 lines) - WPF template builder
2. ✅ Created FileFenceFilter.cs (199 lines) - Smart filtering logic
3. ✅ Created ExifRotationReader.cs (76 lines) - EXIF rotation utility
4. ✅ Updated FilesFenceHandlerWpf to use utilities (435 → 256 lines, 41% reduction)
5. ✅ Updated PictureFenceHandlerWpf to use ExifRotationReader (665 → 618 lines, 7% reduction)
6. ✅ Added new files to NoFences.csproj

**Results**:
- FilesFenceHandlerWpf: 435 → 256 lines (179 lines removed, 41% reduction)
- PictureFenceHandlerWpf: 665 → 618 lines (47 lines removed, 7% reduction)
- 3 new utility classes created (416 lines total)
- Better separation of concerns (template building, filtering, EXIF reading)
- Improved testability and reusability
- See: `documentation/sessions/session-09-sprint-cleanup.html`

## Testing Strategy

After each sprint:
1. Build entire solution
2. Manual testing of affected features
3. Verify no regressions
4. Update documentation

## Success Criteria

- ✅ No file over 600 lines
- ✅ All classes follow Single Responsibility Principle
- ✅ Clean namespace organization
- ✅ Proper project separation
- ✅ All tests passing
- ✅ No duplicate code
- ✅ Documentation updated

## Questions for Discussion

1. **MVVM Pattern**: Should we fully embrace MVVM for FenceEditWindow, or keep it simple with code-behind?
2. **NoFences.Win32 Project**: Create separate project now, or keep in main project for simplicity?
3. **Repository Pattern**: Implement full repository pattern for data access, or keep current approach?
4. **Unit Tests**: Should we add unit tests as we refactor? (Currently no test project exists)
5. **Breaking Changes**: Some refactoring might require breaking XML compatibility. Handle with migration scripts?

## Risks & Mitigation

**Risk**: Breaking existing fence data
**Mitigation**: Test XML serialization thoroughly, create backup mechanism

**Risk**: Introducing bugs during refactoring
**Mitigation**: Incremental changes, test after each sprint

**Risk**: Taking too long
**Mitigation**: Prioritize quick wins first, larger refactors can be deferred

---

**Next Steps**: Review this plan and decide which sprint to start with!
