namespace NoFencesDataLayer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BackupConfig",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        CompressBeforeBackup = c.Boolean(nullable: false),
                        Incremental = c.Boolean(nullable: false),
                        Scheduled = c.DateTime(nullable: false),
                        Excluded = c.String(maxLength: 2147483647),
                        PathFrom_Id = c.Long(),
                        PathTo_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.MonitoredPath", t => t.PathFrom_Id)
                .ForeignKey("dbo.MonitoredPath", t => t.PathTo_Id)
                .Index(t => t.PathFrom_Id, name: "IX_BackupConfig_PathFrom_Id")
                .Index(t => t.PathTo_Id, name: "IX_BackupConfig_PathTo_Id");
            
            CreateTable(
                "dbo.MonitoredPath",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Path = c.String(maxLength: 2147483647),
                        Exclude = c.String(maxLength: 2147483647),
                        Recursive = c.Boolean(nullable: false),
                        Device_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.DeviceInfo", t => t.Device_Id)
                .Index(t => t.Device_Id, name: "IX_MonitoredPath_Device_Id");
            
            CreateTable(
                "dbo.DeviceInfo",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        DeviceGuid = c.String(maxLength: 2147483647),
                        DeviceName = c.String(maxLength: 2147483647),
                        DeviceMountUnit = c.String(maxLength: 2147483647),
                        RemovableDevice = c.Boolean(nullable: false),
                        VirtualDevice = c.Boolean(nullable: false),
                        BackupDevice = c.Boolean(nullable: false),
                        MainDevice = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.FolderConfiguration",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Name = c.String(maxLength: 2147483647),
                        Description = c.String(maxLength: 2147483647),
                        FileFilter = c.String(maxLength: 2147483647),
                        FolderInFileName = c.Boolean(nullable: false),
                        FolderNameProcessor = c.String(maxLength: 2147483647),
                        FileNameProcessor = c.String(maxLength: 2147483647),
                        MoveTo_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.MonitoredPath", t => t.MoveTo_Id)
                .Index(t => t.MoveTo_Id, name: "IX_FolderConfiguration_MoveTo_Id");
            
            CreateTable(
                "dbo.PendingRemoteSync",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Filename = c.String(maxLength: 2147483647),
                        FileHash = c.String(maxLength: 2147483647),
                        Extension = c.String(maxLength: 2147483647),
                        DateAdded = c.DateTime(nullable: false),
                        DateModified = c.DateTime(nullable: false),
                        DateDeleted = c.DateTime(nullable: false),
                        DateSyncd = c.DateTime(nullable: false),
                        PathId_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.MonitoredPath", t => t.PathId_Id)
                .Index(t => t.PathId_Id, name: "IX_PendingRemoteSync_PathId_Id");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PendingRemoteSync", "PathId_Id", "dbo.MonitoredPath");
            DropForeignKey("dbo.BackupConfig", "PathTo_Id", "dbo.MonitoredPath");
            DropForeignKey("dbo.BackupConfig", "PathFrom_Id", "dbo.MonitoredPath");
            DropForeignKey("dbo.FolderConfiguration", "MoveTo_Id", "dbo.MonitoredPath");
            DropForeignKey("dbo.MonitoredPath", "Device_Id", "dbo.DeviceInfo");
            DropIndex("dbo.PendingRemoteSync", "IX_PendingRemoteSync_PathId_Id");
            DropIndex("dbo.FolderConfiguration", "IX_FolderConfiguration_MoveTo_Id");
            DropIndex("dbo.MonitoredPath", "IX_MonitoredPath_Device_Id");
            DropIndex("dbo.BackupConfig", "IX_BackupConfig_PathTo_Id");
            DropIndex("dbo.BackupConfig", "IX_BackupConfig_PathFrom_Id");
            DropTable("dbo.PendingRemoteSync");
            DropTable("dbo.FolderConfiguration");
            DropTable("dbo.DeviceInfo");
            DropTable("dbo.MonitoredPath");
            DropTable("dbo.BackupConfig");
        }
    }
}
