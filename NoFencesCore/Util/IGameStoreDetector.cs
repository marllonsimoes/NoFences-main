using System;
using System.Collections.Generic;

namespace NoFences.Core.Util
{
    /// <summary>
    /// Represents a game from any game store/platform
    /// </summary>
    public class GameInfo
    {
        /// <summary>
        /// Platform-specific game identifier (e.g., Steam AppID, Epic AppName)
        /// </summary>
        public string GameId { get; set; }

        /// <summary>
        /// Display name of the game
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Full path to the game's installation directory
        /// </summary>
        public string InstallDir { get; set; }

        /// <summary>
        /// Path to the game's main executable (if found)
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Path to the game's icon/logo file
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// Size of the game on disk in bytes
        /// </summary>
        public long SizeOnDisk { get; set; }

        /// <summary>
        /// Last update/install time
        /// </summary>
        public DateTime? LastUpdated { get; set; }

        /// <summary>
        /// Path to the created shortcut for launching the game
        /// </summary>
        public string ShortcutPath { get; set; }

        /// <summary>
        /// The game store/platform this game belongs to
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Any platform-specific metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }

        public GameInfo()
        {
            Metadata = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Interface for detecting installed games from various game stores/platforms
    /// Implementations: Steam, Epic Games Store, GOG Galaxy, Ubisoft Connect, EA App, etc.
    /// </summary>
    public interface IGameStoreDetector
    {
        /// <summary>
        /// Name of the game store/platform (e.g., "Steam", "Epic Games Store")
        /// </summary>
        string PlatformName { get; }

        /// <summary>
        /// Gets all installed games from this platform
        /// </summary>
        List<GameInfo> GetInstalledGames();

        /// <summary>
        /// Checks if this game store/platform is installed on the system
        /// </summary>
        bool IsInstalled();

        /// <summary>
        /// Gets the installation path of the game store client
        /// </summary>
        string GetInstallPath();

        /// <summary>
        /// Creates a shortcut for launching the game
        /// </summary>
        /// <param name="gameId">Platform-specific game identifier</param>
        /// <param name="gameName">Display name of the game</param>
        /// <param name="outputDirectory">Directory to create the shortcut in</param>
        /// <param name="iconPath">Optional path to icon file</param>
        /// <returns>Path to the created shortcut</returns>
        string CreateGameShortcut(string gameId, string gameName, string outputDirectory, string iconPath = null);
    }
}
