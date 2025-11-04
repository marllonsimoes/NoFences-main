using log4net;
using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using NoFences.Core.Model;
using NoFences.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace NoFences.View
{
    /// <summary>
    /// Service status monitoring window
    /// </summary>
    public partial class ServiceStatusWindow : MetroWindow
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ServiceStatusWindow));

        private readonly ServiceStatusClient serviceClient;
        private readonly ObservableCollection<FeatureViewModel> features;

        public ServiceStatusWindow()
        {
            InitializeComponent();

            serviceClient = new ServiceStatusClient();
            features = new ObservableCollection<FeatureViewModel>();

            FeaturesItemsControl.ItemsSource = features;

            // Subscribe to service events
            serviceClient.StatusReceived += ServiceClient_StatusReceived;
            serviceClient.FeatureStateChanged += ServiceClient_FeatureStateChanged;

            // Load status after window loads
            Loaded += async (s, e) => await RefreshStatusAsync();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshStatusAsync();
        }

        private async Task RefreshStatusAsync()
        {
            try
            {
                RefreshButton.IsEnabled = false;
                StatusText.Text = "Checking service status...";

                var isRunning = await serviceClient.IsServiceRunningAsync();

                if (isRunning)
                {
                    var status = await serviceClient.GetServiceStatusAsync();
                    if (status != null)
                    {
                        UpdateServiceStatus(status);
                    }
                }
                else
                {
                    ShowServiceNotRunning();
                }
            }
            catch (Exception ex)
            {
                log.Error("Error refreshing service status", ex);
                StatusText.Text = $"Error: {ex.Message}";
                ShowServiceNotRunning();
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }

        private void UpdateServiceStatus(StatusResponseMessage status)
        {
            // Update service info
            ServiceStatusText.Text = "Service is running";
            ServiceVersionText.Text = $"Version {status.ServiceVersion}";
            ServiceIcon.Kind = PackIconMaterialKind.CheckCircle;
            ServiceIcon.Foreground = (Brush)FindResource("MahApps.Brushes.Accent");

            // Update features
            features.Clear();
            foreach (var feature in status.Features)
            {
                features.Add(new FeatureViewModel(feature, serviceClient));
            }

            StatusText.Text = $"Last updated: {DateTime.Now:HH:mm:ss} - {features.Count} features available";
        }

        private void ShowServiceNotRunning()
        {
            ServiceStatusText.Text = "Service is not running";
            ServiceVersionText.Text = "Start the NoFences Service to enable background sync";
            ServiceIcon.Kind = PackIconMaterialKind.ServerOff;
            ServiceIcon.Foreground = (Brush)FindResource("MahApps.Brushes.Gray5");

            features.Clear();
            StatusText.Text = "Service not accessible - background features unavailable";
        }

        private void ServiceClient_StatusReceived(object sender, StatusResponseMessage e)
        {
            Dispatcher.Invoke(() =>
            {
                log.Info($"Status received: {e.Features.Count} features");
            });
        }

        private void ServiceClient_FeatureStateChanged(object sender, FeatureStateChangedMessage e)
        {
            Dispatcher.Invoke(() =>
            {
                log.Info($"Feature '{e.FeatureId}' state changed: {e.State}");

                var feature = features.FirstOrDefault(f => f.FeatureId == e.FeatureId);
                if (feature != null)
                {
                    feature.State = e.State;
                    feature.IsEnabled = (e.State == ServiceFeatureState.Running);
                }

                StatusText.Text = $"Feature '{e.FeatureId}' is now {e.State}";
            });
        }

        private async void FeatureToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch?.DataContext is FeatureViewModel feature)
            {
                try
                {
                    StatusText.Text = $"{(feature.IsEnabled ? "Enabling" : "Disabling")} {feature.DisplayName}...";

                    bool success;
                    if (feature.IsEnabled)
                    {
                        success = await serviceClient.EnableFeatureAsync(feature.FeatureId);
                    }
                    else
                    {
                        success = await serviceClient.DisableFeatureAsync(feature.FeatureId);
                    }

                    if (!success)
                    {
                        StatusText.Text = $"Failed to {(feature.IsEnabled ? "enable" : "disable")} {feature.DisplayName}";
                        // Revert toggle
                        toggleSwitch.IsOn = !toggleSwitch.IsOn;
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Error toggling feature {feature.FeatureId}", ex);
                    StatusText.Text = $"Error: {ex.Message}";
                    // Revert toggle
                    toggleSwitch.IsOn = !toggleSwitch.IsOn;
                }
            }
        }
    }

    /// <summary>
    /// ViewModel for a service feature
    /// </summary>
    public class FeatureViewModel : INotifyPropertyChanged
    {
        private readonly ServiceStatusClient serviceClient;

        public string FeatureId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool IsControllable { get; set; }
        public DateTime LastStateChange { get; set; }
        public string ErrorMessage { get; set; }

        private ServiceFeatureState _state;
        public ServiceFeatureState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged(nameof(State));
                }
            }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public FeatureViewModel(ServiceFeatureStatus feature, ServiceStatusClient client)
        {
            serviceClient = client;
            FeatureId = feature.FeatureId;
            DisplayName = feature.DisplayName;
            Description = feature.Description;
            State = feature.State;
            IsControllable = feature.IsControllable;
            LastStateChange = feature.LastStateChange;
            ErrorMessage = feature.ErrorMessage;
            IsEnabled = (feature.State == ServiceFeatureState.Running);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
