using System;
using System.Collections.Generic;
using System.Linq;

namespace NoFences.Core.Util
{
    /// <summary>
    /// Steam game store detector implementing IGameStoreDetector
    /// Wraps SteamGameDetector for the new abstraction
    /// </summary>
    public class SteamStoreDetector : IGameStoreDetector
    {
        public string PlatformName => "Steam";

        public List<GameInfo> GetInstalledGames()
        {
            var steamGames = SteamGameDetector.GetInstalledGames();
            return steamGames.Select(ConvertToGameInfo).ToList();
        }

        public bool IsInstalled()
        {
            var installPath = GetInstallPath();
            return !string.IsNullOrEmpty(installPath);
        }

        public string GetInstallPath()
        {
            // Use the existing static method via reflection or expose it
            // For now, we'll check the standard paths
            return SteamGameDetector.GetSteamInstallPath();
        }

        public string CreateGameShortcut(string gameId, string gameName, string outputDirectory, string iconPath = null)
        {
            if (!int.TryParse(gameId, out int appId))
                return null;

            return SteamGameDetector.CreateSteamShortcut(appId, gameName, outputDirectory, iconPath);
        }

        /// <summary>
        /// Converts SteamGameInfo to generic GameInfo
        /// </summary>
        private GameInfo ConvertToGameInfo(SteamGameInfo steamGame)
        {
            return new GameInfo
            {
                GameId = steamGame.AppID.ToString(),
                Name = steamGame.Name,
                InstallDir = steamGame.InstallDir,
                ExecutablePath = steamGame.ExecutablePath,
                IconPath = steamGame.IconPath,
                SizeOnDisk = steamGame.SizeOnDisk,
                LastUpdated = steamGame.LastUpdated,
                ShortcutPath = steamGame.ShortcutPath,
                Platform = PlatformName,
                Metadata = new Dictionary<string, string>
                {
                    ["AppID"] = steamGame.AppID.ToString(),
                    ["LibraryPath"] = steamGame.LibraryPath ?? ""
                }
            };
        }
    }
}
