using System;

namespace NoFences.Core.Model
{
    /// <summary>
    /// Represents an installed software application
    /// </summary>
    public class InstalledSoftware
    {
        /// <summary>
        /// Display name of the software
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Publisher/company name
        /// </summary>
        public string Publisher { get; set; }

        /// <summary>
        /// Version string
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Installation directory
        /// </summary>
        public string InstallLocation { get; set; }

        /// <summary>
        /// Path to the executable (if available)
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Path to the application icon (if available)
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// Installation date (if available)
        /// </summary>
        public DateTime? InstallDate { get; set; }

        /// <summary>
        /// Uninstall command/path
        /// </summary>
        public string UninstallString { get; set; }

        /// <summary>
        /// Automatically categorized software type
        /// </summary>
        public SoftwareCategory Category { get; set; }

        /// <summary>
        /// Registry key where this software was found
        /// </summary>
        public string RegistryKey { get; set; }

        /// <summary>
        /// Whether this is a 32-bit app on 64-bit Windows (WOW6432Node)
        /// </summary>
        public bool IsWow64 { get; set; }

        public InstalledSoftware()
        {
            Category = SoftwareCategory.Other;
        }

        public override string ToString()
        {
            return $"{Name} ({Publisher}) - {Category}";
        }
    }
}
