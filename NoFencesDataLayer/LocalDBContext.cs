using NoFencesDataLayer.Migrations;
using NoFencesService.Util;
using SQLite.CodeFirst;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace NoFencesService.Repository
{
    [DbConfigurationType(typeof(LocalDBConfiguration))]
    public class LocalDBContext : DbContext
    {
        public DbSet<FolderConfiguration> FolderConfigurations { get; set; }

        public DbSet<BackupConfig> BackupConfigs { get; set; }

        public DbSet<PendingRemoteSync> PendingRemoteSyncs { get; set; }

        public DbSet<MonitoredPath> MonitoredPaths { get; set; }

        public DbSet<DeviceInfo> DevicesInfo { get; set; }

        private static readonly string serviceDatabase = "ref.db";

        private static string basePath = Path.Combine(
                new string[] {
                    AppEnvUtil.GetAppEnvironmentPath(),
                    serviceDatabase
                });

        static LocalDBContext() => Database.SetInitializer(new MigrateDatabaseToLatestVersion<LocalDBContext, Configuration>(true));

        public LocalDBContext() : base($"Data Source={basePath}") { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<LocalDBContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);
        }
    }

    [Table("FolderConfiguration")]
    public class FolderConfiguration
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FileFilter { get; set; }
        public virtual MonitoredPath MoveTo { get; set; }
        public bool FolderInFileName { get; set; }
        public string FolderNameProcessor{ get; set; }
        public string FileNameProcessor { get; set; }
    }

    [Table("MonitoredPath")]
    public class MonitoredPath
    {
        [Key]
        public long Id { get; set; }
        public string Path { get; set; }
        public string Exclude { get; set; }
        public bool Recursive { get; set; }
        public virtual DeviceInfo Device { get; set; }
        public virtual List<FolderConfiguration> FolderConfiguration { get; set; }

        [NotMapped]
        public string FolderName
        {
            get
            {
                return Path != null ? Path.Split('\\').Last() : string.Empty;
            }
        }
    }

    [Table("DeviceInfo")]
    public class DeviceInfo
    {
        public long Id { get; set; }
        public string DeviceGuid { get; set; }
        public string DeviceName { get; set; }
        public string DeviceMountUnit { get; set; }
        public bool RemovableDevice { get; set; }
        public bool VirtualDevice { get; set; }
        public bool BackupDevice { get; set; }
        public bool MainDevice { get; set; }
    }

    [Table("PendingRemoteSync")]
    public class PendingRemoteSync
    {
        public long Id { get; set; }
        public virtual MonitoredPath PathId { get; set; }
        public string Filename { get; set; }
        public string FileHash { get; set; }
        public string Extension { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateModified { get; set; }
        public DateTime DateDeleted { get; set; }
        public DateTime DateSyncd { get; set; }
    }

    [Table("BackupConfig")]
    public class BackupConfig
    {
        public long Id { get; set; }
        public virtual MonitoredPath PathFrom { get; set; }
        public bool CompressBeforeBackup { get; set; }
        public bool Incremental { get; set; }
        public virtual MonitoredPath PathTo { get; set; }
        public DateTime Scheduled { get; set; }
        public string Excluded { get; set; }
    }


    public class StructureTest
    {
        public void Method()
        {
            var deviceInfo_C = new DeviceInfo() { DeviceName = "Windows", MainDevice = false, RemovableDevice = false, VirtualDevice = false, BackupDevice = false, DeviceMountUnit = "C" };
            var deviceInfo_E = new DeviceInfo() { DeviceName = "Games", MainDevice = false, RemovableDevice = false, VirtualDevice = false, BackupDevice = false, DeviceMountUnit = "E" };
            var deviceInfo_Z = new DeviceInfo() { DeviceName = "Main storage", MainDevice = true, RemovableDevice = false, VirtualDevice = false, BackupDevice = false, DeviceMountUnit = "Z" };
            var deviceInfo_Removable = new DeviceInfo() { DeviceName = "External backup drive", MainDevice = false, RemovableDevice = true, VirtualDevice = false, BackupDevice = true, DeviceMountUnit = "D" };

            var monitoredPath_target_stuff = new MonitoredPath() { Device = deviceInfo_Z, Path = "/stuff", Recursive = false };
            var monitoredPath_target_books = new MonitoredPath() { Device = deviceInfo_Z, Path = "/stuff/knowledge", Recursive = false };
            var monitoredPath_target_media = new MonitoredPath() { Device = deviceInfo_Z, Path = "/stuff/images", Recursive = false };
            var monitoredPath_target_software = new MonitoredPath() { Device = deviceInfo_Z, Path = "/stuff/installers", Recursive = false };

            var folderConfiguration_Downloads_Documents = new FolderConfiguration() {
                FileFilter = ".doc|.docx|.xls|.xlsx|.pdf|.odf|.ods|.ppt|.pptx|.odp|.txt|.rtf|.md",
                Description = "Documents in downloads folder",
                FolderInFileName = true,
                Name = "Downloaded Documents",
                MoveTo = monitoredPath_target_stuff
            };
            var folderConfiguration_Downloads_Software = new FolderConfiguration()
            {
                FileFilter = ".exe|.msi|.zip|.7z|.tar|.gz|.bz|.rar",
                Description = "Software in downloads folder",
                FolderInFileName = true,
                Name = "Downloaded Software",
                MoveTo = monitoredPath_target_software
            };
            var folderConfiguration_Downloads_Pictures = new FolderConfiguration()
            {
                FileFilter = ".jpg|.jpeg|.gif|.raw|.png|.cr2|.webp|.mov|.mpeg|.mpg|.avi|.mkv|.mp4|.dng|.tiff",
                Description = "Media in downloads folder",
                FolderInFileName = false,
                Name = "Downloaded Media",
                MoveTo = monitoredPath_target_media,
                FileNameProcessor="FileMediaMetadataProcessor",
                FolderNameProcessor="FolderMediaMetadataProcessor"
            };
            var folderConfiguration_Downloads_Books = new FolderConfiguration()
            {
                FileFilter = ".epub|.mobi|.pdf|.txt|.rtf|.doc|.docx|.azw|.cbz|.cbr|.azw3",
                Description = "Books in downloads folder",
                FolderInFileName = false,
                Name = "Downloaded Books",
                MoveTo = monitoredPath_target_books,
                FileNameProcessor="FileBookMetadataProcessor",
                FolderNameProcessor="FolderBookMetadataProcessor"
            };

            var monitoredPath_source_downloads = new MonitoredPath() { 
                Device=deviceInfo_C, 
                Path="/Users/marl/Downloads", 
                Recursive=false, 
                FolderConfiguration=new List<FolderConfiguration>() { 
                    folderConfiguration_Downloads_Documents, 
                    folderConfiguration_Downloads_Books, 
                    folderConfiguration_Downloads_Pictures, 
                    folderConfiguration_Downloads_Software
                }
            };

            var backupConfig = new BackupConfig();
            var pendingRemoveSync = new PendingRemoteSync();
        }
    }
}
