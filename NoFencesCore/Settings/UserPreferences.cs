using NoFences.Core.Util;
using System;
using System.IO;
using System.Xml.Serialization;

namespace NoFences.Core.Settings
{
    /// <summary>
    /// User preferences and settings stored locally
    /// </summary>
    [Serializable]
    public class UserPreferences
    {
        public bool HasSeenWelcomeTips { get; set; } = false;
        public DateTime FirstRunDate { get; set; } = DateTime.Now;
        public int FencesCreatedCount { get; set; } = 0;

        // Auto-Update Settings
        public bool AutoCheckForUpdates { get; set; } = true;
        public int CheckFrequencyHours { get; set; } = 24;
        public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;
        public bool AutoDownloadUpdates { get; set; } = false;
        public string LastSkippedVersion { get; set; } = null;

        // API Keys for Metadata Enrichment
        public string RawgApiKey { get; set; } = null;

        private static readonly string PreferencesPath = Path.Combine(AppEnvUtil.BasePath,"UserPreferences.xml");

        /// <summary>
        /// Loads user preferences from disk or creates default if not found
        /// </summary>
        public static UserPreferences Load()
        {
            try
            {
                if (File.Exists(PreferencesPath))
                {
                    var serializer = new XmlSerializer(typeof(UserPreferences));
                    using (var reader = new StreamReader(PreferencesPath))
                    {
                        return (UserPreferences)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception)
            {
                // If load fails, return default preferences
            }

            return new UserPreferences();
        }

        /// <summary>
        /// Saves user preferences to disk
        /// </summary>
        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(PreferencesPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var serializer = new XmlSerializer(typeof(UserPreferences));
                using (var writer = new StreamWriter(PreferencesPath))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception)
            {
                // Silently fail if save doesn't work
            }
        }
    }
}
