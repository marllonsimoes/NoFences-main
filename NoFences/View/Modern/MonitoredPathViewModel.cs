using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using NoFences.View.Service;
using NoFencesService.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoFences.View.Modern;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace NoFences.View
{
    public sealed class MonitoredPathViewModel : ObservableRecipient
    {

        public MonitoredPathViewModel(IMonitoredPathService monitoredPathService, IDeviceInfoService deviceInfoService)
        {
            _monitoredPathService = monitoredPathService;
            _deviceInfoService = deviceInfoService;
        }

        private readonly IMonitoredPathService _monitoredPathService;
        private readonly IDeviceInfoService _deviceInfoService;

        private MonitoredPath _selectedMonitoredPath;

        public MonitoredPath SelectedMonitoredPath
        {
            get => _selectedMonitoredPath;
            set
            {
                SetProperty(ref _selectedMonitoredPath, value);
            }
        }

        private FolderConfiguration _folderConfiguration;

        public FolderConfiguration SelectedFolderConfiguration
        {
            get => _folderConfiguration;
            set
            {
                SetProperty(ref _folderConfiguration, value);
            }
        }

        #region IoC property listener
        protected override void OnActivated()
        {
            Messenger.Register<MonitoredPathViewModel, ValueChangedMessage<MonitoredPath>>(this, (r, m) => r.Receive(m.Value));
            Messenger.Register<MonitoredPathViewModel, PropertyChangedMessage<FolderConfiguration>>(this, (r, m) => r.ReceiveFolderConfiguration(m));
        }

        private void Receive(MonitoredPath viewModel)
        {
            SelectedMonitoredPath = viewModel;
        }

        private void ReceiveFolderConfiguration(PropertyChangedMessage<FolderConfiguration> message)
        {
            if (message.Sender.GetType() == typeof(FolderConfigurationViewModel) &&
                message.Sender.Equals(nameof(SelectedFolderConfiguration)))
            {
                SelectedFolderConfiguration = message.NewValue;
            }
        }
        #endregion

        public void Save()
        {
            if (SelectedMonitoredPath.Id != 0)
            {
                _monitoredPathService.Update(SelectedMonitoredPath);
                return;
            }
            _monitoredPathService.Add(SelectedMonitoredPath);
        }

        public DeviceInfo GetDeviceInfoOrCreate(string mountPoint)
        {
            var harddriveInfo = GetDriveInfo(mountPoint);

            var deviceInfo = _deviceInfoService.GetByMountPoint(mountPoint);
            if (harddriveInfo != null)
            {
                if (deviceInfo == null)
                {
                    deviceInfo = new DeviceInfo()
                    {
                        BackupDevice = false,
                        DeviceMountUnit = harddriveInfo.Name,
                        DeviceName = harddriveInfo.Name,
                        MainDevice = false,
                        RemovableDevice = harddriveInfo.DriveType == DriveType.Removable,
                        VirtualDevice = false,
                        DeviceGuid = null
                    };
                    deviceInfo = SaveDeviceInfo(deviceInfo);
                }
            }
            return deviceInfo;
        }

        public DeviceInfo SaveDeviceInfo(DeviceInfo deviceInfo)
        {
            return _deviceInfoService.Add(deviceInfo);
        }

        private DriveInfo GetDriveInfo(string driveMountPoint)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.Name.Equals(driveMountPoint, StringComparison.OrdinalIgnoreCase))
                {
                    return drive;
                }
            }
            return null;
        }

        internal void SetPathInfo(string folderName)
        {
            var rootFolder = Directory.GetDirectoryRoot(folderName);
            var deviceInfo = GetDeviceInfoOrCreate(rootFolder);
            SelectedMonitoredPath.Device = deviceInfo;
            SelectedMonitoredPath.Path = folderName;
            OnPropertyChanged(nameof(SelectedMonitoredPath));
        }

        public void CreateNewFolderConfiguration()
        {
            SelectedFolderConfiguration = new FolderConfiguration()
            {
                Name = "Test 123",
                Description = "Desc",
                FileFilter = "Filter 123",
                FolderInFileName = true
            };
        }

        internal void AddFolderConfiguration()
        {
            SelectedMonitoredPath.FolderConfiguration.Add(SelectedFolderConfiguration);
            OnPropertyChanged(nameof(SelectedMonitoredPath));
        }
    }
}
