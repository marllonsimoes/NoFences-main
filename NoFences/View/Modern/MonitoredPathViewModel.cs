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
using System.Collections.ObjectModel;

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

        public ObservableCollection<FolderConfiguration> FolderConfigurationList { get; } = new ObservableCollection<FolderConfiguration>();


        #region IoC property listener
        protected override void OnActivated()
        {
            Messenger.Register<MonitoredPathViewModel, ValueChangedMessage<MonitoredPath>>(this, (r, m) => r.Receive(m.Value));
            Messenger.Register<MonitoredPathViewModel, PropertyChangedMessage<FolderConfiguration>>(this, (r, m) => r.ReceiveFolderConfiguration(m));
        }

        private void Receive(MonitoredPath viewModel)
        {
            FolderConfigurationList.Clear();
            SelectedMonitoredPath = viewModel;
            if (SelectedMonitoredPath != null)
            {
                if (SelectedMonitoredPath.FolderConfiguration == null)
                {
                    SelectedMonitoredPath.FolderConfiguration = new List<FolderConfiguration>();
                }

                foreach (var fc in SelectedMonitoredPath.FolderConfiguration)
                {
                    FolderConfigurationList.Add(fc);
                }
            }
        }

        private void ReceiveFolderConfiguration(PropertyChangedMessage<FolderConfiguration> message)
        {
            if (message.Sender.GetType() == typeof(FolderConfigurationViewModel) &&
                message.PropertyName.Equals(nameof(SelectedFolderConfiguration)))
            {
                SelectedFolderConfiguration = message.NewValue;
                OnPropertyChanged(nameof(SelectedMonitoredPath));
                OnPropertyChanged(nameof(SelectedFolderConfiguration));
            }
        }
        #endregion

        public void Save()
        {
            SelectedMonitoredPath.FolderConfiguration.Clear();
            foreach (var fc in FolderConfigurationList)
            {
                SelectedMonitoredPath.FolderConfiguration.Add(fc);
            }

            if (SelectedMonitoredPath.Id != 0)
            {
                _monitoredPathService.Update(_selectedMonitoredPath);
                return;
            }
            _monitoredPathService.Add(_selectedMonitoredPath);
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
            SelectedFolderConfiguration = new FolderConfiguration();
            Messenger.Send(new PropertyChangedMessage<FolderConfiguration>(this, nameof(SelectedFolderConfiguration), null, SelectedFolderConfiguration));
        }

        public void EditFolderConfiguration()
        {
            Messenger.Send(new PropertyChangedMessage<FolderConfiguration>(this, nameof(SelectedFolderConfiguration), null, SelectedFolderConfiguration));
        }

        internal void AddFolderConfiguration(FolderConfiguration folderConfig)
        {
            if (folderConfig.Id != 0)
            {
                FolderConfigurationList[FolderConfigurationList.IndexOf(FolderConfigurationList.FirstOrDefault(fc => fc.Id == folderConfig.Id))] = folderConfig;
            }
            else
            {
                FolderConfigurationList.Add(SelectedFolderConfiguration);
            }
            OnPropertyChanged(nameof(SelectedMonitoredPath));
        }
    }
}
