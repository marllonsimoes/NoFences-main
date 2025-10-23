using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MahApps.Metro.Controls;
using NoFences.View.Service;
using NoFencesService.Repository;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace NoFences.View
{
    public sealed class MonitoredPathsViewModel : ObservableRecipient
    {
        public MonitoredPathsViewModel(IMonitoredPathService monitoredPathService)
        {
            _monitoredPathService = monitoredPathService;

            LoadMonitoredPathCommand = new RelayCommand(LoadAll);

            #region Initialize view commands
            // Flywout controls
            this.OpenFlyoutCommand = new SimpleCommand<Flyout>(f => f != null, f => f.SetCurrentValue(Flyout.IsOpenProperty, true));
            this.CloseFlyoutCommand = new SimpleCommand<Flyout>(f => f != null, f => f.SetCurrentValue(Flyout.IsOpenProperty, false));
            #endregion
        }

        private IMonitoredPathService _monitoredPathService;

        #region Flywout controls
        public ICommand OpenFlyoutCommand { get; }

        public ICommand CloseFlyoutCommand { get; }
        #endregion

        public IRelayCommand LoadMonitoredPathCommand { get; }

        public ObservableCollection<MonitoredPath> MonitoredPaths { get; } = new ObservableCollection<MonitoredPath>();

        private MonitoredPath _selectedMonitoredPath;

        public MonitoredPath SelectedMonitoredPath
        {
            get { return _selectedMonitoredPath; }
            set
            {
                SetProperty(ref _selectedMonitoredPath, value);
            }
        }

        public void LoadAll()
        {
            MonitoredPaths.Clear();
            MonitoredPaths.Add(new MonitoredPath());
            foreach (var mn in _monitoredPathService.List())
            {
                MonitoredPaths.Add(mn);
            }
            OnPropertyChanged(nameof(MonitoredPaths));
        }

        internal void EditMonitoredPath(MonitoredPath path)
        {
            SelectedMonitoredPath = path;
            Messenger.Send(new ValueChangedMessage<MonitoredPath>(SelectedMonitoredPath));
        }
    }
}
