using log4net;
using NoFences.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoFences.Core.Model
{
    /// <summary>
    /// High-level type classification for software based on metadata source and purpose.
    /// This is distinct from SoftwareCategory which provides detailed categorization.
    /// </summary>
    public enum SoftwareType
    {
        /// <summary>
        /// Video games (typically enriched from RAWG API)
        /// </summary>
        Game,

        /// <summary>
        /// General applications (typically enriched from Winget, CNET, Wikipedia)
        /// </summary>
        Application,

        /// <summary>
        /// Developer tools and SDKs
        /// </summary>
        Tool,

        /// <summary>
        /// System utilities and helpers
        /// </summary>
        Utility,

        /// <summary>
        /// Unknown or not yet determined
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Categories for installed software
    /// </summary>
    public enum SoftwareCategory
    {
        All,                    // All software
        Games,                  // Video games
        GamingPlatforms,        // Steam, Epic, GOG, etc.
        OfficeProductivity,     // Microsoft Office, LibreOffice, etc.
        Design,                 // Adobe, Affinity, GIMP, etc.
        Development,            // IDEs, compilers, version control
        Media,                  // Video/audio players, editors
        Communication,          // Browsers, email, messaging
        Utilities,              // System tools, file managers
        Security,               // Antivirus, firewalls
        Other                   // Uncategorized
    }

    /// <summary>
    /// Provides categorization logic for installed software
    /// </summary>
    public static class SoftwareCategorizer
    {

        private static readonly ILog log = LogManager.GetLogger(typeof(SoftwareCategorizer));

        // Keywords for categorization (lowercase for case-insensitive matching)
        private static readonly Dictionary<SoftwareCategory, List<string>> CategoryKeywords = new Dictionary<SoftwareCategory, List<string>>
        {
            {
                SoftwareCategory.GamingPlatforms, new List<string>
                {
                    "steam", "epic games", "gog galaxy", "origin", "uplay", "ubisoft connect",
                    "battle.net", "blizzard", "xbox", "game pass", "ea app", "eletronic arts", "itch.io",
                    "playnite", "lutris", "legendary", "amazon games", "humble app", "legacy games"
                }
            },
            {
                SoftwareCategory.OfficeProductivity, new List<string>
                {
                    "microsoft office", "word", "excel", "powerpoint", "outlook", "onenote",
                    "libreoffice", "openoffice", "wps office", "google workspace",
                    "notion", "evernote", "adobe acrobat", "pdf", "onlyoffice", "workflowy"
                }
            },
            {
                SoftwareCategory.Design, new List<string>
                {
                    "adobe", "photoshop", "illustrator", "indesign", "premiere", "after effects",
                    "affinity", "gimp", "inkscape", "blender", "3ds max", "maya", "cinema 4d",
                    "sketch", "figma", "canva", "corel", "paint.net", "painter", "autodesk", "da vinci"
                }
            },
            {
                SoftwareCategory.Development, new List<string>
                {
                    "visual studio", "vscode", "jetbrains", "intellij", "pycharm", "webstorm",
                    "eclipse", "netbeans", "atom", "sublime", "notepad++",
                    "git", "github desktop", "sourcetree", "tortoise",
                    "python", "node", "java", "docker", "kubernetes", "postman",
                    "android studio", "xcode", "unity", "unreal engine"
                }
            },
            {
                SoftwareCategory.Media, new List<string>
                {
                    "vlc", "media player", "spotify", "itunes", "audacity", "obs studio",
                    "handbrake", "ffmpeg", "plex", "kodi", "winamp",
                    "kdenlive", "shotcut", "jellyfin"
                }
            },
            {
                SoftwareCategory.Communication, new List<string>
                {
                    "chrome", "firefox", "edge", "brave", "opera", "browser",
                    "outlook", "thunderbird", "mail",
                    "discord", "slack", "teams", "zoom", "skype", "telegram", "whatsapp",
                    "signal"
                }
            },
            {
                SoftwareCategory.Utilities, new List<string>
                {
                    "7-zip", "winrar", "winzip",
                    "ccleaner", "windirstat", "treesize",
                    "notepad", "total commander", "file explorer",
                    "powertoys", "autohotkey", "rainmeter"
                }
            },
            {
                SoftwareCategory.Security, new List<string>
                {
                    "antivirus", "firewall", "malwarebytes", "kaspersky", "norton", "mcafee",
                    "avast", "avg", "bitdefender", "windows defender", "eset"
                }
            }
        };

        // Publisher-based categorization (takes priority over keyword matching)
        private static readonly Dictionary<string, SoftwareCategory> PublisherMap = new Dictionary<string, SoftwareCategory>(StringComparer.OrdinalIgnoreCase)
        {
            // Gaming platforms
            //{ "Valve Corporation", SoftwareCategory.GamingPlatforms },
            //{ "Epic Games", SoftwareCategory.GamingPlatforms },
            //{ "GOG.com", SoftwareCategory.GamingPlatforms },
            //{ "CD Projekt RED", SoftwareCategory.GamingPlatforms },
            //{ "Electronic Arts", SoftwareCategory.GamingPlatforms },
            //{ "Ubisoft", SoftwareCategory.GamingPlatforms },
            //{ "Blizzard Entertainment", SoftwareCategory.GamingPlatforms },
            //{ "Amazon Games", SoftwareCategory.GamingPlatforms },

            // Office
            { "Microsoft Corporation", SoftwareCategory.OfficeProductivity },
            { "The Document Foundation", SoftwareCategory.OfficeProductivity }, // LibreOffice

            // Design
            { "Adobe Systems", SoftwareCategory.Design },
            { "Adobe Inc.", SoftwareCategory.Design },
            { "Serif", SoftwareCategory.Design }, // Affinity
            { "Blender Foundation", SoftwareCategory.Design },
            { "Autodesk", SoftwareCategory.Design },

            // Development
            { "JetBrains", SoftwareCategory.Development },
            { "GitHub", SoftwareCategory.Development },
            { "Oracle", SoftwareCategory.Development },
            { "Python Software Foundation", SoftwareCategory.Development },
            { "Node.js Foundation", SoftwareCategory.Development },
            { "Unity Technologies", SoftwareCategory.Development },

            // Security
            { "Malwarebytes", SoftwareCategory.Security },
            { "Kaspersky", SoftwareCategory.Security },
            { "Norton", SoftwareCategory.Security },
            { "McAfee", SoftwareCategory.Security }
        };

        /// <summary>
        /// Categorizes software based on name, publisher, and install location
        /// </summary>
        public static SoftwareCategory Categorize(string name, string publisher = null, string installLocation = null)
        {
            log.Debug($"Categorizing software: Name='{name}', Publisher='{publisher}', InstallLocation='{installLocation}'");
            if (string.IsNullOrEmpty(name))
                return SoftwareCategory.Other;

            // Keyword-based categorization
            string lowerName = name.ToLower();
            string lowerPublisher = publisher?.ToLower() ?? string.Empty;

            log.Debug($"Trying to get category by keywork: Lowercase Name='{lowerName}', Lowercase Publisher='{lowerPublisher}'");
            foreach (var kvp in CategoryKeywords)
            {
                foreach (var keyword in kvp.Value)
                {
                    if (lowerName.Contains(keyword) || lowerPublisher.Contains(keyword))
                    {
                        log.Debug($"Found category: {kvp}");
                        return kvp.Key;
                    }
                }
            }

            log.Debug($"Trying to find by publisher: {publisher}");
            // Check publisher first (most reliable)
            if (!string.IsNullOrEmpty(publisher))
            {
                if (PublisherMap.TryGetValue(publisher, out var publisherCategory))
                {
                    log.Debug($"Foung category by publisher: {publisherCategory}");
                    return publisherCategory;
                }
            }

            log.Debug($"Trying to find by installLocation: {installLocation}");
            // Check install location for gaming platform hints
            if (!string.IsNullOrEmpty(installLocation))
            {
                string lowerLocation = installLocation.ToLower();
                if (lowerLocation.Contains("steam") || lowerLocation.Contains("steamapps"))
                    return SoftwareCategory.GamingPlatforms;
                if (lowerLocation.Contains("epic") || lowerLocation.Contains("epicgames"))
                    return SoftwareCategory.GamingPlatforms;
                if (lowerLocation.Contains("gog galaxy"))
                    return SoftwareCategory.GamingPlatforms;
            }

            return SoftwareCategory.Other;
        }

        /// <summary>
        /// Gets a friendly display name for a software category
        /// </summary>
        public static string GetCategoryDisplayName(SoftwareCategory category)
        {
            switch (category)
            {
                case SoftwareCategory.All: return "All Software";
                case SoftwareCategory.Games: return "Games";
                case SoftwareCategory.GamingPlatforms: return "Gaming Platforms";
                case SoftwareCategory.OfficeProductivity: return "Office & Productivity";
                case SoftwareCategory.Design: return "Design & Graphics";
                case SoftwareCategory.Development: return "Development Tools";
                case SoftwareCategory.Media: return "Media & Entertainment";
                case SoftwareCategory.Communication: return "Communication";
                case SoftwareCategory.Utilities: return "Utilities";
                case SoftwareCategory.Security: return "Security";
                case SoftwareCategory.Other: return "Other";
                default: return category.ToString();
            }
        }

        /// <summary>
        /// Gets a description of what software is in a category
        /// </summary>
        public static string GetCategoryDescription(SoftwareCategory category)
        {
            switch (category)
            {
                case SoftwareCategory.All:
                    return "All installed software";
                case SoftwareCategory.Games:
                    return "Video games";
                case SoftwareCategory.GamingPlatforms:
                    return "Steam, Epic, GOG, etc.";
                case SoftwareCategory.OfficeProductivity:
                    return "Office suites, productivity tools";
                case SoftwareCategory.Design:
                    return "Graphics, video, 3D modeling";
                case SoftwareCategory.Development:
                    return "IDEs, compilers, version control";
                case SoftwareCategory.Media:
                    return "Players, editors, streaming";
                case SoftwareCategory.Communication:
                    return "Browsers, email, messaging";
                case SoftwareCategory.Utilities:
                    return "System tools, utilities";
                case SoftwareCategory.Security:
                    return "Antivirus, firewalls";
                case SoftwareCategory.Other:
                    return "Uncategorized software";
                default:
                    return string.Empty;
            }
        }
    }
}
