using log4net;
using log4net.Config;
using NoFences.Core.Model;
using NoFences.Core.Util;
using NoFencesService.Repository;
using NoFencesService.Sync;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

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

        // Cloud and device sync components
        private CloudSyncEngine cloudSyncEngine;
        private HybridSyncManager hybridSyncManager;
        private ServiceStatusPipeServer statusPipeServer;

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

            // Update status
            if (statusPipeServer != null)
            {
                statusPipeServer.UpdateFeatureState("DeviceSync", ServiceFeatureState.Stopped);
            }

            log.Info("Monitoring paused");
        }

        protected override void OnContinue()
        {
            log.Info("Resuming service");
            deviceInsertedWatcher.Start();
            deviceRemovedWatcher.Start();

            // Update status
            if (statusPipeServer != null)
            {
                statusPipeServer.UpdateFeatureState("DeviceSync", ServiceFeatureState.Running);
            }

            log.Info("Monitoring resumed");
        }

        protected override void OnStop()
        {
            log.Info("Stopping service");
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            log.Info("Stopping components");

            // Stop device monitoring
            deviceInsertedWatcher.Stop();
            deviceRemovedWatcher.Stop();

            // Shutdown sync engines
            if (cloudSyncEngine != null)
            {
                cloudSyncEngine.Shutdown();
                log.Info("CloudSyncEngine stopped");
            }

            // Stop pipe server
            if (statusPipeServer != null)
            {
                statusPipeServer.Stop();
                log.Info("StatusPipeServer stopped");
            }

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
            log.Info("Initializing service components");

            // Initialize named pipe server for status communication
            InitializeStatusPipeServer();

            // Initialize cloud sync engine
            InitializeCloudSyncEngine();

            // Initialize hybrid sync manager
            InitializeHybridSyncManager();

            // Start device monitoring
            log.Info("Starting device monitoring");
            QueryDevicesInBackground();

            log.Info("All components configured successfully");
        }

        /// <summary>
        /// Initialize the status pipe server for communication with main app
        /// </summary>
        private void InitializeStatusPipeServer()
        {
            try
            {
                log.Info("Initializing ServiceStatusPipeServer");
                statusPipeServer = new ServiceStatusPipeServer();

                // Register service features
                statusPipeServer.RegisterFeature(
                    "DeviceSync",
                    "Device Sync",
                    "Monitors connected devices and triggers synchronization",
                    isControllable: true);

                statusPipeServer.RegisterFeature(
                    "CloudSync",
                    "Cloud Sync",
                    "Synchronizes files with cloud storage providers",
                    isControllable: true);

                statusPipeServer.RegisterFeature(
                    "FolderMonitoring",
                    "Folder Monitoring",
                    "Watches folders for file changes and organization",
                    isControllable: true);

                // Start the pipe server
                statusPipeServer.Start();
                log.Info("ServiceStatusPipeServer started successfully");
            }
            catch (Exception ex)
            {
                log.Error("Failed to initialize ServiceStatusPipeServer", ex);
            }
        }

        /// <summary>
        /// Initialize the cloud sync engine
        /// </summary>
        private void InitializeCloudSyncEngine()
        {
            try
            {
                log.Info("Initializing CloudSyncEngine");
                cloudSyncEngine = new CloudSyncEngine();

                // TODO: Register cloud providers (OneDrive, Google Drive, Dropbox, etc.)
                // Example:
                // var oneDriveProvider = new OneDriveProvider();
                // cloudSyncEngine.RegisterProvider(oneDriveProvider);

                log.Info("CloudSyncEngine initialized successfully");

                if (statusPipeServer != null)
                {
                    statusPipeServer.UpdateFeatureState("CloudSync", ServiceFeatureState.Running);
                }
            }
            catch (Exception ex)
            {
                log.Error("Failed to initialize CloudSyncEngine", ex);
                if (statusPipeServer != null)
                {
                    statusPipeServer.UpdateFeatureState("CloudSync", ServiceFeatureState.Error, ex.Message);
                }
            }
        }

        /// <summary>
        /// Initialize the hybrid sync manager
        /// </summary>
        private void InitializeHybridSyncManager()
        {
            try
            {
                log.Info("Initializing HybridSyncManager");
                hybridSyncManager = new HybridSyncManager(cloudSyncEngine);

                // Subscribe to device sync events
                hybridSyncManager.DeviceSyncTriggered += OnDeviceSyncTriggered;

                // TODO: Load sync configurations from database
                // Example:
                // var configs = localDatabaseContext.SyncConfigurations.ToList();
                // foreach (var config in configs)
                // {
                //     hybridSyncManager.AddSyncConfiguration(config);
                // }

                log.Info("HybridSyncManager initialized successfully");
            }
            catch (Exception ex)
            {
                log.Error("Failed to initialize HybridSyncManager", ex);
                if (statusPipeServer != null)
                {
                    statusPipeServer.UpdateFeatureState("DeviceSync", ServiceFeatureState.Error, ex.Message);
                }
            }
        }

        /// <summary>
        /// Handle device sync triggered event
        /// </summary>
        private void OnDeviceSyncTriggered(object sender, DeviceSyncEventArgs e)
        {
            if (e.Success)
            {
                log.Info($"Device sync successful: {e.ConfigurationName} → {e.DriveLetter} ({e.VolumeLabel})");
            }
            else
            {
                log.Error($"Device sync failed: {e.ConfigurationName} → {e.DriveLetter} - {e.ErrorMessage}");
            }
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
            try
            {
                ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];

                log.Debug("Device inserted event received");

                // Wait a moment for the device to be fully ready
                Thread.Sleep(2000);

                // Check for newly available drives
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Removable).ToList();

                foreach (var drive in drives)
                {
                    log.Info($"Detected new drive: {drive.Name} - {drive.VolumeLabel}");

                    // Get device serial number if possible
                    string serialNumber = GetDriveSerialNumber(drive.Name);

                    // Notify hybrid sync manager
                    if (hybridSyncManager != null)
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                await hybridSyncManager.OnDeviceConnected(
                                    drive.Name.TrimEnd('\\'),
                                    drive.VolumeLabel,
                                    serialNumber);
                            }
                            catch (Exception ex)
                            {
                                log.Error($"Error in device connected handler: {ex.Message}", ex);
                            }
                        });
                    }
                }

                if (statusPipeServer != null)
                {
                    statusPipeServer.UpdateFeatureState("DeviceSync", ServiceFeatureState.Running);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in DeviceInsertedEvent: {ex.Message}", ex);
            }
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];

                log.Debug("Device removed event received");

                // Get list of currently available drives
                var currentDrives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d => d.Name.TrimEnd('\\'))
                    .ToList();

                log.Info($"Current drives: {string.Join(", ", currentDrives)}");

                // TODO: Track previously known drives to detect which one was removed
                // For now, just log the event
            }
            catch (Exception ex)
            {
                log.Error($"Error in DeviceRemovedEvent: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get drive serial number using WMI
        /// </summary>
        private string GetDriveSerialNumber(string driveLetter)
        {
            try
            {
                var cleanDrive = driveLetter.TrimEnd('\\').Replace(":", "");
                var query = $"SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID = '{cleanDrive}:'";

                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var serialNumber = obj["VolumeSerialNumber"]?.ToString();
                        if (!string.IsNullOrEmpty(serialNumber))
                        {
                            log.Debug($"Drive {driveLetter} serial number: {serialNumber}");
                            return serialNumber;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warn($"Failed to get serial number for drive {driveLetter}: {ex.Message}");
            }

            return null;
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
