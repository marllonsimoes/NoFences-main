using NoFencesDataLayer.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;

namespace NoFencesDataLayer.Util
{
    /// <summary>
    /// Console tool for normalizing CSV catalogs to JSON format.
    /// Run this utility to convert the raw CSV files into structured JSON.
    /// </summary>
    public class CatalogNormalizationTool
    {
        /// <summary>
        /// Main entry point for the normalization tool
        /// </summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("=== NoFences Catalog Normalization Tool ===");
            Console.WriteLine();

            // Default paths
            string inputDir = args.Length > 0 ? args[0] : @"..\_software_list";
            string outputDir = args.Length > 1 ? args[1] : @".\normalized_catalogs";
            int maxSteamGames = args.Length > 2 && int.TryParse(args[2], out int max) ? max : 10000;

            Console.WriteLine($"Input directory: {inputDir}");
            Console.WriteLine($"Output directory: {outputDir}");
            Console.WriteLine($"Max Steam games: {maxSteamGames}");
            Console.WriteLine();

            if (!Directory.Exists(inputDir))
            {
                Console.WriteLine($"ERROR: Input directory not found: {inputDir}");
                Console.WriteLine("Usage: CatalogNormalizationTool <input_dir> <output_dir> [max_steam_games]");
                return;
            }

            // Create output directory
            Directory.CreateDirectory(outputDir);

            try
            {
                // Normalize the catalogs
                var softwarePath = Path.Combine(inputDir, "Software.csv");
                var steamPath = Path.Combine(inputDir, "steam.csv");

                Console.WriteLine("Starting normalization...");
                Console.WriteLine();

                var catalog = CatalogNormalizer.CreateNormalizedCatalog(
                    softwarePath,
                    steamPath,
                    maxSteamGames);

                // Save as JSON
                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };

                // Save full catalog
                var fullCatalogPath = Path.Combine(outputDir, "software_catalog.json");
                var json = JsonConvert.SerializeObject(catalog, jsonSettings);
                File.WriteAllText(fullCatalogPath, json);
                Console.WriteLine($"✓ Saved full catalog: {fullCatalogPath}");
                Console.WriteLine($"  Size: {new FileInfo(fullCatalogPath).Length / 1024 / 1024:F2} MB");
                Console.WriteLine();

                // Save separate files for easier management
                var softwareOnlyPath = Path.Combine(outputDir, "software_only.json");
                var softwareJson = JsonConvert.SerializeObject(new
                {
                    metadata = catalog.Metadata,
                    software = catalog.Software
                }, jsonSettings);
                File.WriteAllText(softwareOnlyPath, softwareJson);
                Console.WriteLine($"✓ Saved software only: {softwareOnlyPath}");
                Console.WriteLine($"  Size: {new FileInfo(softwareOnlyPath).Length / 1024:F2} KB");
                Console.WriteLine();

                var steamOnlyPath = Path.Combine(outputDir, "steam_games.json");
                var steamJson = JsonConvert.SerializeObject(new
                {
                    metadata = catalog.Metadata,
                    steamGames = catalog.SteamGames
                }, jsonSettings);
                File.WriteAllText(steamOnlyPath, steamJson);
                Console.WriteLine($"✓ Saved Steam games: {steamOnlyPath}");
                Console.WriteLine($"  Size: {new FileInfo(steamOnlyPath).Length / 1024 / 1024:F2} MB");
                Console.WriteLine();

                // Print summary
                Console.WriteLine("=== Normalization Complete ===");
                Console.WriteLine($"Total Software: {catalog.Metadata.TotalSoftware:N0}");
                Console.WriteLine($"Total Steam Games: {catalog.Metadata.TotalSteamGames:N0}");
                Console.WriteLine($"Catalog Version: {catalog.Metadata.Version}");
                Console.WriteLine($"Generated: {catalog.Metadata.GeneratedDate:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine();
                Console.WriteLine("Output files:");
                Console.WriteLine($"  - {fullCatalogPath} (full catalog)");
                Console.WriteLine($"  - {softwareOnlyPath} (software entries only)");
                Console.WriteLine($"  - {steamOnlyPath} (Steam games only)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
