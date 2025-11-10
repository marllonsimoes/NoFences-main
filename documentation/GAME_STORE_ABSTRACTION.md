# Game Store Abstraction System

## Overview

The NoFences application now supports detecting and displaying games from multiple game store platforms through an extensible abstraction system. This allows users to create fences that automatically populate with games from Steam, Epic Games Store, GOG Galaxy, and other platforms.

## Architecture

### Core Interface: `IGameStoreDetector`

All game store detectors implement this interface:

```csharp
public interface IGameStoreDetector
{
    string PlatformName { get; }
    List<GameInfo> GetInstalledGames();
    bool IsInstalled();
    string GetInstallPath();
    string CreateGameShortcut(string gameId, string gameName, string outputDirectory, string iconPath = null);
}
```

### Universal Game Info Model

The `GameInfo` class represents a game from any platform:

```csharp
public class GameInfo
{
    public string GameId { get; set; }              // Platform-specific ID
    public string Name { get; set; }                // Display name
    public string InstallDir { get; set; }          // Installation directory
    public string ExecutablePath { get; set; }      // Main executable or shortcut
    public string IconPath { get; set; }            // Icon for extraction
    public long SizeOnDisk { get; set; }            // Size in bytes
    public DateTime? LastUpdated { get; set; }      // Last update time
    public string ShortcutPath { get; set; }        // Created shortcut
    public string Platform { get; set; }            // Platform name
    public Dictionary<string, string> Metadata { get; set; }  // Platform-specific data
}
```

## Supported Platforms

**Currently Implemented: 6 Platforms**
- Steam
- Epic Games Store
- GOG Galaxy
- Ubisoft Connect
- EA App
- Amazon Games

### 1. Steam (SteamStoreDetector)

**Detection Method**: VDF (Valve Data Format) parsing

**Key Features**:
- Scans all Steam library folders (C:, D:, E:, etc.)
- Parses `appmanifest_*.acf` files for game metadata
- Extracts game executables from install directories
- Uses Steam's icon cache as fallback
- Creates `steam://rungameid/{appid}` shortcuts

**Icon Sources**:
1. Game's main executable
2. Steam icon cache: `{SteamPath}/appcache/librarycache/{appid}_icon.jpg`
3. Steam.exe (fallback)

**Registry Locations**:
- `HKLM\SOFTWARE\WOW6432Node\Valve\Steam`
- `HKLM\SOFTWARE\Valve\Steam`
- `HKCU\SOFTWARE\Valve\Steam`

### 2. Epic Games Store (EpicGamesStoreDetector)

**Detection Method**: JSON manifest parsing

**Key Features**:
- Reads `.item` files from `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests`
- Extracts DisplayName, AppName, InstallLocation, LaunchExecutable
- Searches common game subdirectories (Binaries/Win64, Game/Binaries/Win64)
- Creates `com.epicgames.launcher://apps/{appname}?action=launch` shortcuts

**Icon Sources**:
1. Game's LaunchExecutable
2. Discovered .exe in Binaries folder
3. EpicGamesLauncher.exe (fallback)

**Registry Locations**:
- `HKLM\SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher`
- `HKCU\SOFTWARE\Epic Games\EOS`

### 3. GOG Galaxy (GOGGalaxyDetector)

**Detection Method**: Registry scanning

**Key Features**:
- Reads game entries from `HKLM\SOFTWARE\WOW6432Node\GOG.com\Games`
- Extracts gameName, path, exe, workingDir from registry
- Handles both absolute and relative executable paths
- Creates `goggalaxy://openGameView/{gameid}` shortcuts

**Icon Sources**:
1. Registered executable from registry
2. Discovered .exe in install directory
3. GalaxyClient.exe (fallback)

**Registry Locations**:
- `HKLM\SOFTWARE\WOW6432Node\GOG.com\Games` (game entries)
- `HKLM\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths` (client path)

### 4. Ubisoft Connect (UbisoftConnectDetector)

**Detection Method**: Registry scanning

**Key Features**:
- Scans `HKLM\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs` for game entries
- Extracts InstallDir from registry per game
- Searches common subdirectories (bin, Binaries, Binaries/Win64)
- Creates `uplay://launch/{gameid}` shortcuts

**Icon Sources**:
1. Discovered .exe in install directory or subdirectories
2. UbisoftConnect.exe or Uplay.exe (fallback)

**Registry Locations**:
- `HKLM\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs` (game entries)
- `HKLM\SOFTWARE\WOW6432Node\Ubisoft\Launcher` (InstallDir for client)

**Filters**: Excludes launchers, crash reporters, ubisoft overlay, uplay launcher executables

### 5. EA App (EAAppDetector)

**Detection Method**: Registry scanning

**Key Features**:
- Reads game entries from `HKLM\SOFTWARE\WOW6432Node\EA Games`
- Extracts DisplayName, Install Dir/InstallLocation from registry
- Supports both EA Desktop (new) and Origin (legacy) clients
- Creates `origin2://game/launch?offerIds={offerid}` shortcuts

**Icon Sources**:
1. Discovered .exe in install directory or subdirectories
2. EA Desktop.exe or Origin.exe (fallback)

**Registry Locations**:
- `HKLM\SOFTWARE\WOW6432Node\EA Games` (game entries)
- `HKLM\SOFTWARE\WOW6432Node\Origin` (legacy client path)

**Client Detection**:
- EA Desktop: `%LOCALAPPDATA%\Electronic Arts\EA Desktop\EA Desktop.exe`
- Origin: Registry or default paths in Program Files

**Filters**: Excludes launchers, EA overlay, origin, ealink, activation executables

### 6. Amazon Games (AmazonGamesDetector)

**Detection Method**: Fuel.json file parsing

**Key Features**:
- Reads Fuel.json files from `%LOCALAPPDATA%\Amazon Games\Data\Games\{ProductId}\`
- Extracts Id (product ID), ProductTitle, InstallDirectory from JSON
- Uses regex-based JSON parsing (no external dependencies)
- Creates `amazon-games://play/{productid}` shortcuts

**Icon Sources**:
1. Discovered .exe in install directory or subdirectories
2. Amazon Games.exe (fallback)

**Data Locations**:
- Game data: `%LOCALAPPDATA%\Amazon Games\Data\Games\{ProductId}\Fuel.json`
- Client: `%LOCALAPPDATA%\Amazon Games\App\Amazon Games.exe`

**Filters**: Excludes launchers, crash reporters, amazon launcher executables

**Note**: Amazon Games uses individual JSON manifests per game rather than a central registry or database

## Integration with NoFences

### InstalledAppsUtil Integration

The `InstalledAppsUtil` class automatically uses all available detectors:

```csharp
private static List<InstalledSoftware> GetAllGames()
{
    var detectors = new List<IGameStoreDetector>
    {
        new SteamStoreDetector(),
        new EpicGamesStoreDetector(),
        new GOGGalaxyDetector(),
        new UbisoftConnectDetector(),
        new EAAppDetector(),
        new AmazonGamesDetector()
    };

    foreach (var detector in detectors)
    {
        if (detector.IsInstalled())
        {
            var games = GetGamesFromStore(detector);
            allGames.AddRange(games);
        }
    }
}
```

### Fence Integration

When creating a **Software fence** with category **Games**, the system:

1. Calls `InstalledAppsUtil.GetByCategory(SoftwareCategory.Games)`
2. Receives games from all installed platforms
3. Creates shortcuts in `%APPDATA%/NoFences/{Platform}Shortcuts/`
4. Populates icon cache with game executables
5. Displays games with proper icons in the fence

### Icon Extraction Flow

```
Game Detected
    ↓
Find game executable or use manifest data
    ↓
Store in GameInfo.IconPath
    ↓
Convert to InstalledSoftware.IconPath
    ↓
Cache in FilesFenceHandlerWpf.iconPathCache
    ↓
Pass to FenceEntry.FromPath(path, iconPath)
    ↓
FenceEntry.ExtractIcon() uses IconPath
    ↓
Icon displayed in fence
```

## Adding New Game Stores

To add support for a new game store (e.g., Ubisoft Connect, EA App, Amazon Games):

### 1. Create Detector Class

```csharp
public class UbisoftConnectDetector : IGameStoreDetector
{
    public string PlatformName => "Ubisoft Connect";

    public List<GameInfo> GetInstalledGames()
    {
        // Implement detection logic
    }

    public bool IsInstalled()
    {
        // Check if Ubisoft Connect is installed
    }

    public string GetInstallPath()
    {
        // Find Ubisoft Connect installation
    }

    public string CreateGameShortcut(string gameId, string gameName, string outputDirectory, string iconPath = null)
    {
        // Create uplay:// or ubisoft-connect:// shortcut
    }
}
```

### 2. Register in InstalledAppsUtil

```csharp
var detectors = new List<IGameStoreDetector>
{
    new SteamStoreDetector(),
    new EpicGamesStoreDetector(),
    new GOGGalaxyDetector(),
    new UbisoftConnectDetector()  // ADD HERE
};
```

### 3. Add Publisher Mapping

```csharp
private static string GetPublisherForPlatform(string platformName)
{
    switch (platformName)
    {
        case "Ubisoft Connect":
            return "Ubisoft";
        // ...
    }
}
```

## Platform Launch Protocols

Different stores use different URL schemes for launching games:

| Platform | Protocol Format |
|----------|----------------|
| Steam | `steam://rungameid/{appid}` |
| Epic Games Store | `com.epicgames.launcher://apps/{appname}?action=launch&silent=true` |
| GOG Galaxy | `goggalaxy://openGameView/{gameid}` |
| Ubisoft Connect | `uplay://launch/{gameid}` or `ubisoft-connect://launch/{gameid}` |
| EA App | `origin2://game/launch?offerIds={offerid}` |
| Amazon Games | `amazon-games://play/{productid}` |

## Common Detection Patterns

### Registry-Based Detection (GOG, Ubisoft, EA)

```csharp
using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Company\Product"))
{
    if (key != null)
    {
        var gameName = key.GetValue("GameName") as string;
        var installPath = key.GetValue("InstallPath") as string;
        // ...
    }
}
```

### File-Based Detection (Steam, Epic)

```csharp
string manifestFolder = @"C:\ProgramData\Platform\Manifests";
var manifestFiles = Directory.GetFiles(manifestFolder, "*.ext");

foreach (var file in manifestFiles)
{
    string content = File.ReadAllText(file);
    // Parse using regex or JSON
}
```

### SQLite Database (Some platforms)

Some platforms use SQLite databases. You would need to:
1. Add System.Data.SQLite NuGet package
2. Query the database for installed games

```csharp
using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
{
    connection.Open();
    var command = new SQLiteCommand("SELECT * FROM InstalledGames", connection);
    var reader = command.ExecuteReader();
    // ...
}
```

## Troubleshooting

### Games Not Appearing

1. **Check if platform is installed**: Use `detector.IsInstalled()`
2. **Check debug output**: All detectors write to `Debug.WriteLine`
3. **Verify manifest/registry locations**: Paths may vary by version
4. **Check install directory exists**: Games must have valid InstallDir

### Icons Not Showing

1. **Verify IconPath is populated**: Check `InstalledSoftware.IconPath`
2. **Check icon cache**: Ensure `iconPathCache[path]` is set in handler
3. **Verify executable exists**: Icon extraction requires valid file
4. **Check permissions**: Some game directories may have restricted access

### Shortcuts Not Working

1. **Verify protocol handler is registered**: Check Windows default programs
2. **Test protocol manually**: Run `start steam://rungameid/440` in cmd
3. **Check launcher is running**: Some protocols require launcher to be active
4. **Verify game ID format**: Each platform has specific ID format

## Performance Considerations

### Lazy Loading

Detectors only run when:
- Software fence is created with Games category
- User manually refreshes a Games fence
- Application starts (cached in memory)

### Caching Strategy

- **Icon cache**: Per-handler dictionary, cleared on refresh
- **Game list**: Regenerated on each GetAllInstalled() call
- **Shortcuts**: Created once, reused until deleted

### Optimization Tips

1. **Skip unavailable platforms**: Use `IsInstalled()` check first
2. **Parallel scanning**: Could be added for multiple detectors
3. **Background refresh**: Consider async loading for large libraries
4. **Incremental updates**: Could track last scan time and only rescan changed items

## Security Considerations

### Path Validation

All detectors validate paths before use:
- Check `Directory.Exists()` for install directories
- Check `File.Exists()` for executables
- Sanitize file names for shortcuts

### Registry Safety

- Use try-catch for all registry operations
- Check for null keys before accessing values
- Handle missing or malformed registry data gracefully

### Manifest Parsing

- Validate JSON/VDF structure
- Handle malformed or corrupted files
- Don't trust file contents blindly

## Future Enhancements

### Implemented Platforms (6)

✅ **Steam** - VDF parsing, multi-library support, icon extraction
✅ **Epic Games Store** - JSON manifest parsing, executable discovery
✅ **GOG Galaxy** - Registry scanning, DRM-free games
✅ **Ubisoft Connect** (formerly Uplay) - Registry scanning, multi-path executable search
✅ **EA App** (formerly Origin) - Registry scanning, dual client support
✅ **Amazon Games** (Prime Gaming) - Fuel.json parsing, no external dependencies

### Additional Platforms (Future)

- **Battle.net** (Blizzard games) - Product database + .agent files
- **Rockstar Games Launcher** - Registry-based detection
- **Xbox Game Pass** (Microsoft Store) - AppX manifests (complex)
- **Itch.io** - SQLite database
- **Humble Bundle** - Registry/manifest parsing

### Planned Features

- **Cloud save detection**: Identify games with cloud saves
- **Play time tracking**: Read play time from platform APIs
- **Achievement integration**: Show achievement progress
- **Game state detection**: Show if game is installed/downloading/updating
- **Library sync**: Sync game lists across NoFences instances
- **Custom categories**: User-defined game categories beyond platform
- **Favorites system**: Mark favorite games for quick access

## API Reference

### IGameStoreDetector Methods

#### GetInstalledGames()
Returns list of all installed games for this platform.

**Returns**: `List<GameInfo>`

**Example**:
```csharp
var detector = new SteamStoreDetector();
var games = detector.GetInstalledGames();
foreach (var game in games)
{
    Console.WriteLine($"{game.Name} - {game.Platform}");
}
```

#### IsInstalled()
Checks if the game store client is installed on this system.

**Returns**: `bool`

**Example**:
```csharp
if (detector.IsInstalled())
{
    var games = detector.GetInstalledGames();
}
```

#### GetInstallPath()
Gets the installation directory of the game store client.

**Returns**: `string` (null if not found)

**Example**:
```csharp
var steamPath = new SteamStoreDetector().GetInstallPath();
// Returns: "C:\Program Files (x86)\Steam"
```

#### CreateGameShortcut()
Creates a URL shortcut for launching the game.

**Parameters**:
- `gameId`: Platform-specific game identifier
- `gameName`: Display name for shortcut file
- `outputDirectory`: Where to create the shortcut
- `iconPath`: Optional path to icon file

**Returns**: `string` (path to created shortcut)

**Example**:
```csharp
var shortcut = detector.CreateGameShortcut(
    "440",
    "Team Fortress 2",
    @"C:\Users\User\AppData\Roaming\NoFences\Shortcuts",
    @"C:\SteamLibrary\steamapps\common\Team Fortress 2\tf2.exe"
);
```

## Testing

### Manual Testing

1. **Install test platforms**: Install Steam, Epic, GOG
2. **Install test games**: Install 2-3 games on each platform
3. **Create Games fence**: Use Software filter with Games category
4. **Verify display**: Check all games appear with correct icons
5. **Test launching**: Double-click shortcuts to verify they work
6. **Test refresh**: Modify installed games and refresh fence

### Debug Output

All detectors write detailed logs:
```
SteamStoreDetector: Found Steam at C:\Program Files (x86)\Steam
SteamStoreDetector: Found 3 library folders
SteamStoreDetector: Found 25 games in D:\SteamLibrary\steamapps
SteamStoreDetector: Total 125 Steam games found
```

Enable debug output in Visual Studio: Debug → Windows → Output

### Common Test Cases

1. **Empty library**: Platform installed but no games
2. **Multiple libraries**: Games on C:, D:, E: drives
3. **Uninstalled games**: Manifests exist but games removed
4. **Corrupted manifests**: Malformed JSON/VDF files
5. **Missing executables**: Game installed but .exe deleted
6. **Permission issues**: Game in restricted directory

## References

- [Steam VDF Format](https://developer.valvesoftware.com/wiki/KeyValues)
- [Epic Games Launcher Manifest Format](https://dev.epicgames.com/docs/services)
- [GOG Galaxy Registry Structure](https://www.gog.com/galaxy)
- [Windows Registry Best Practices](https://docs.microsoft.com/en-us/windows/win32/sysinfo/registry)
