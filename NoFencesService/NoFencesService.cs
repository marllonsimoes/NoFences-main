using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using NoFencesService.Repository;
using NoFencesService.Util;
using System;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Web;
using Microsoft.Win32;

namespace NoFencesService
{
    public partial class NoFencesService : ServiceBase
    {
        private static WqlEventQuery deviceInsertedQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
        private ManagementEventWatcher deviceInsertedWatcher = new ManagementEventWatcher(deviceInsertedQuery);

        private static WqlEventQuery deviceRemovedQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
        private ManagementEventWatcher deviceRemovedWatcher = new ManagementEventWatcher(deviceRemovedQuery);

        private LocalDBContext localDatabaseContext = new LocalDBContext();

        private ILog log;

        public NoFencesService()
        {
            XmlConfigurator.Configure();
            log = LogManager.GetLogger(typeof(NoFencesService));

            InitializeComponent();

            log.Info("Checking and creating default folders");
            AppEnvUtil.EnsureAppEnvironmentPathExists();

            log.Info("Checkigng and creating database file");
            AppEnvUtil.EnsureAllDataRequirementsAreAvaiable();
        }

        #region constructors and service methods

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        protected override void OnPause()
        {
            log.Info("Pausing service");
            deviceInsertedWatcher.Stop();
            deviceRemovedWatcher.Stop();
            log.Info("Monitoring paused");
        }

        protected override void OnContinue()
        {
            log.Info("Resuming service");
            deviceInsertedWatcher.Start();
            deviceRemovedWatcher.Start();
            log.Info("Monitoring resumed");
        }

        protected override void OnStop()
        {
            log.Info("Stopping service");
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            log.Info("Monitoring stopped");
            deviceInsertedWatcher.Stop();
            deviceRemovedWatcher.Stop();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            log.Info("Service stopped");
        }

        protected override void OnStart(string[] args)
        {
            log.Info("Starting service");
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            Thread.Sleep(10000);
            log.Info("Configuring components");
            ConfigureAndValidateComponents();
            CheckDevicesAndFolders();
            CreateFolderWatchers();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        #endregion

        private void ConfigureAndValidateComponents()
        {
            log.Info("Configuring watcher for Downloads folder");
            
            LocalDBContext localDBContext = new LocalDBContext();
            
            var monitoredPaths = localDBContext.MonitoredPaths.ToList();

            foreach ( var monitoredPath in monitoredPaths )
            {
                
            }


            log.Info("Configuring watcher for Downloads folder");

            //Debug.WriteLine("Querying devices in background");
            //QueryDevicesInBackground();
            //Debug.WriteLine("Query configuration and create the monitors");
        }


        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            log.Info($"An error happened when operatring a file: {e.GetException().Message}");
        }

        private void HandleWatcherEvent(object sender, FileSystemEventArgs e)
        {
            log.Info($"A change happened in the folder {e.FullPath}");
            log.Info($"The file {e.Name} was {e.ChangeType}");

            if (sender.GetType().IsAssignableFrom(typeof(FileSystemWatcher)))
            {
                var watcher = (FileSystemWatcher)sender;
                var changeResult = watcher.WaitForChanged(e.ChangeType);
                log.Info($"What is this?");
                log.Info($"WatcherPath: {watcher.Path}");
                log.Info($"Name: {changeResult.Name}");
                log.Info($"OldName: {changeResult.OldName}");
            }
        }

        private void CheckDevicesAndFolders()
        {

        }

        private void ValidateFolderStructureIntegrity()
        {

        }

        private void RegisterChanges()
        {

        }

        private void RegisterInconsistencies()
        {

        }

        private void CreateFolderWatchers()
        {

        }

        private void QueryDevicesInBackground()
        {
            deviceInsertedWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
            deviceInsertedWatcher.Start();

            deviceRemovedWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
            deviceRemovedWatcher.Start();
        }

        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            foreach (var property in instance.Properties)
            {
                Console.WriteLine(property.Name + " = " + property.Value);
                Debug.WriteLine(property.Name + " = " + property.Value);
            }
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            foreach (var property in instance.Properties)
            {
                Console.WriteLine(property.Name + " = " + property.Value);
                Debug.WriteLine(property.Name + " = " + property.Value);
            }
        }

        private void ListAllDevices()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                Console.WriteLine($"{drive.Name} - {drive.VolumeLabel} - {drive.DriveType} - {drive.IsReady}");
                Debug.WriteLine($"{drive.Name} - {drive.VolumeLabel} - {drive.DriveType} - {drive.IsReady}");
            }
        }
    }

    #region ServiceState enumeration and struct
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };
    #endregion
}
