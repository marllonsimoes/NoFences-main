using log4net;
using Newtonsoft.Json.Linq;
using NoFences.Core.Util;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace NoFencesDataLayer.Repositories
{
    /// <summary>
    /// Repository for Amazon Games data access.
    /// Reads from Amazon Games SQLite database (CommonData.sqlite).
    /// Session 11: Separates data access from business logic (detector pattern).
    /// </summary>
    public class AmazonGamesRepository : IAmazonGamesRepository
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AmazonGamesRepository));

        /// <summary>
        /// Gets all installed Amazon Games from the SQLite databases.
        /// Database locations:
        ///   - GameInstallInfo.sqlite: Installation data
        ///   - ProductDetails.sqlite: Rich metadata
        /// </summary>
        public List<GameInfo> GetInstalledGames()
        {
            var games = new List<GameInfo>();

            try
            {
                string sqlFolder = GetSqlFolderPath();
                if (string.IsNullOrEmpty(sqlFolder))
                {
                    log.Debug("Amazon Games Sql folder not found");
                    return games;
                }

                string gameInstallDbPath = Path.Combine(sqlFolder, "GameInstallInfo.sqlite");
                string productDetailsDbPath = Path.Combine(sqlFolder, "ProductDetails.sqlite");

                if (!File.Exists(gameInstallDbPath))
                {
                    log.Debug($"GameInstallInfo.sqlite not found at {gameInstallDbPath}");
                    return games;
                }

                log.Debug($"Reading Amazon Games databases from {sqlFolder}");

                // First, get all installed games from GameInstallInfo.sqlite
                var installData = new Dictionary<string, Tuple<string, string>>(); // Id -> (InstallDir, ProductTitle)

                using (var connection = new SQLiteConnection($"Data Source={gameInstallDbPath};Version=3;Read Only=True;"))
                {
                    connection.Open();

                    // First, discover what columns actually exist in the table
                    log.Debug("Discovering GameInstallInfo table schema...");
                    var columns = new List<string>();
                    using (var schemaCommand = new SQLiteCommand("PRAGMA table_info(DbSet)", connection))
                    using (var schemaReader = schemaCommand.ExecuteReader())
                    {
                        while (schemaReader.Read())
                        {
                            string columnName = schemaReader["name"]?.ToString();
                            columns.Add(columnName);
                            log.Debug($"  Column: {columnName}");
                        }
                    }

                    // Build query to include ProductTitle from GameInstallInfo
                    string query = @"
                        SELECT Id, InstallDirectory, ProductTitle
                        FROM DbSet
                        WHERE Installed = 1";

                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string id = reader["Id"]?.ToString();
                            string installDir = reader["InstallDirectory"]?.ToString();
                            string productTitle = reader["ProductTitle"]?.ToString();

                            if (!string.IsNullOrEmpty(id))
                            {
                                installData[id] = Tuple.Create(installDir, productTitle);
                                log.Debug($"Found game: Id={id}, Title={productTitle}, InstallDir={installDir}");
                            }
                        }
                    }
                }

                log.Debug($"Found {installData.Count} installed games from GameInstallInfo.sqlite");

                // Second, get product details if available
                var productDetails = new Dictionary<string, string>(); // Id -> Details JSON

                if (File.Exists(productDetailsDbPath))
                {
                    try
                    {
                        using (var connection = new SQLiteConnection($"Data Source={productDetailsDbPath};Version=3;Read Only=True;"))
                        {
                            connection.Open();

                            string query = @"
                                SELECT Id, Details
                                FROM DbSet";

                            using (var command = new SQLiteCommand(query, connection))
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string id = reader["Id"]?.ToString();
                                    string details = reader["Details"]?.ToString();

                                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(details))
                                    {
                                        productDetails[id] = details;
                                    }
                                }
                            }
                        }
                        log.Debug($"Found {productDetails.Count} product details from ProductDetails.sqlite");
                    }
                    catch (Exception ex)
                    {
                        log.Warn($"Failed to read ProductDetails.sqlite: {ex.Message}");
                    }
                }

                // Combine data and create GameInfo objects
                foreach (var kvp in installData)
                {
                    try
                    {
                        string productId = kvp.Key;
                        string installDir = kvp.Value.Item1;
                        string productTitle = kvp.Value.Item2;
                        string detailsJson = productDetails.ContainsKey(productId) ? productDetails[productId] : null;

                        var gameInfo = ParseDatabaseRecord(productId, installDir, productTitle, detailsJson);
                        if (gameInfo != null)
                        {
                            games.Add(gameInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error parsing game record for {kvp.Key}: {ex.Message}", ex);
                    }
                }

                log.Debug($"Successfully created {games.Count} GameInfo objects");
            }
            catch (Exception ex)
            {
                log.Error($"Error reading Amazon Games databases: {ex.Message}", ex);
            }

            return games;
        }

        /// <summary>
        /// Checks if the Amazon Games databases are accessible.
        /// </summary>
        public bool IsAvailable()
        {
            string sqlFolder = GetSqlFolderPath();
            if (string.IsNullOrEmpty(sqlFolder))
                return false;

            string gameInstallDbPath = Path.Combine(sqlFolder, "GameInstallInfo.sqlite");
            return File.Exists(gameInstallDbPath);
        }

        /// <summary>
        /// Gets the path to the Amazon Games Sql folder containing databases.
        /// </summary>
        private string GetSqlFolderPath()
        {
            try
            {
                string amazonGamesPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Amazon Games");

                string sqlFolderPath = Path.Combine(amazonGamesPath, "Data", "Games", "Sql");

                if (Directory.Exists(sqlFolderPath))
                {
                    return sqlFolderPath;
                }

                log.Debug($"Amazon Games Sql folder not found at {sqlFolderPath}");
            }
            catch (Exception ex)
            {
                log.Error($"Error getting Amazon Games Sql folder path: {ex.Message}", ex);
            }

            return null;
        }

        /// <summary>
        /// Gets the path to the Amazon Games SQLite database folder.
        /// Kept for interface compatibility.
        /// </summary>
        public string GetDatabasePath()
        {
            return GetSqlFolderPath();
        }

        /// <summary>
        /// Parses a database record from GameInstallInfo and ProductDetails.
        /// Extracts rich metadata from JSON fields using Newtonsoft.Json.
        /// </summary>
        private GameInfo ParseDatabaseRecord(string productId, string installDir, string productTitle, string detailsJson)
        {
            try
            {
                if (string.IsNullOrEmpty(productId))
                {
                    log.Debug("Skipping record with empty ProductId");
                    return null;
                }

                // Use ProductTitle from GameInstallInfo.sqlite as primary source
                // Fallback to productId if ProductTitle is empty
                if (string.IsNullOrEmpty(productTitle))
                {
                    productTitle = productId;
                    log.Debug($"ProductTitle empty for {productId}, using ID as fallback");
                }

                // Verify install directory exists
                if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
                {
                    log.Debug($"Install directory not found for {productTitle}: {installDir}");
                    return null;
                }

                // Calculate install size
                long installSize = 0;
                try
                {
                    var dirInfo = new DirectoryInfo(installDir);
                    foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        installSize += file.Length;
                    }
                }
                catch
                {
                    // Size calculation can fail for large directories
                }

                // Get last updated time from directory
                DateTime? lastUpdated = null;
                try
                {
                    lastUpdated = Directory.GetLastWriteTime(installDir);
                }
                catch { }

                // Parse rich metadata from ProductDetails JSON
                var metadata = new Dictionary<string, string>
                {
                    ["ProductId"] = productId,
                    ["Source"] = "AmazonGamesDatabase",
                    ["InstallDirectory"] = installDir
                };

                if (!string.IsNullOrEmpty(detailsJson))
                {
                    try
                    {
                        var details = JObject.Parse(detailsJson);

                        // Extract product title from ProductDetails as secondary fallback
                        // (only if GameInstallInfo.ProductTitle was empty and we used ID)
                        if (productTitle == productId)
                        {
                            string title = details["ProductTitle"]?.ToString();
                            if (!string.IsNullOrEmpty(title))
                            {
                                productTitle = title;
                                log.Debug($"Using ProductTitle from ProductDetails for {productId}: {title}");
                            }
                        }

                        // Extract developers
                        var developers = details["Developers"]?.ToObject<List<string>>();
                        if (developers != null && developers.Count > 0)
                        {
                            metadata["Developers"] = string.Join(", ", developers);
                        }

                        // Extract publisher
                        string publisher = details["ProductPublisher"]?.ToString();
                        if (!string.IsNullOrEmpty(publisher))
                        {
                            metadata["Publisher"] = publisher;
                        }

                        // Extract genres
                        var genres = details["Genres"]?.ToObject<List<string>>();
                        if (genres != null && genres.Count > 0)
                        {
                            metadata["Genres"] = string.Join(", ", genres);
                        }

                        // Extract game modes
                        var gameModes = details["GameModes"]?.ToObject<List<string>>();
                        if (gameModes != null && gameModes.Count > 0)
                        {
                            metadata["GameModes"] = string.Join(", ", gameModes);
                        }

                        // Extract ratings
                        string esrbRating = details["EsrbRating"]?.ToString();
                        if (!string.IsNullOrEmpty(esrbRating))
                        {
                            metadata["EsrbRating"] = esrbRating;
                        }

                        // Extract release date
                        string releaseDate = details["ReleaseDate"]?.ToString();
                        if (!string.IsNullOrEmpty(releaseDate))
                        {
                            metadata["ReleaseDate"] = releaseDate;
                        }

                        // Extract description
                        string description = details["ProductDescription"]?.ToString();
                        if (!string.IsNullOrEmpty(description))
                        {
                            metadata["Description"] = description;
                        }

                        // Extract icon URL
                        string iconUrl = details["ProductIconUrl"]?.ToString();
                        if (!string.IsNullOrEmpty(iconUrl))
                        {
                            metadata["IconUrl"] = iconUrl;
                        }

                        log.Debug($"Extracted rich metadata for {productTitle}: {metadata.Count} fields");
                    }
                    catch (Exception ex)
                    {
                        log.Warn($"Failed to parse ProductDetails JSON for {productTitle}: {ex.Message}");
                    }
                }

                return new GameInfo
                {
                    GameId = productId,
                    Name = productTitle,
                    InstallDir = installDir,
                    ExecutablePath = null, // Will be set by detector
                    IconPath = null, // Will be set by detector
                    SizeOnDisk = installSize,
                    LastUpdated = lastUpdated,
                    ShortcutPath = null, // Will be set by detector
                    Platform = "Amazon Games",
                    Metadata = metadata
                };
            }
            catch (Exception ex)
            {
                log.Error($"Error parsing database record for {productId}: {ex.Message}", ex);
                return null;
            }
        }
    }
}
