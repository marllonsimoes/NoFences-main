using NoFences.Core.Model;

namespace NoFences.ViewModels
{
    /// <summary>
    /// ViewModel for Widget fence type properties (weather, system info, etc.).
    /// </summary>
    public class WidgetPropertiesViewModel : ViewModelBase
    {
        private readonly FenceInfo fenceInfo;

        private string _weatherLocation;
        public string WeatherLocation
        {
            get => _weatherLocation;
            set => SetProperty(ref _weatherLocation, value);
        }

        private string _weatherApiKey;
        public string WeatherApiKey
        {
            get => _weatherApiKey;
            set => SetProperty(ref _weatherApiKey, value);
        }

        public WidgetPropertiesViewModel(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo;
            LoadFromFenceInfo();
        }

        private void LoadFromFenceInfo()
        {
            WeatherLocation = fenceInfo.WeatherLocation ?? string.Empty;
            WeatherApiKey = fenceInfo.WeatherApiKey ?? string.Empty;
        }

        public void SaveToFenceInfo(FenceInfo fenceInfo)
        {
            fenceInfo.WeatherLocation = WeatherLocation;
            fenceInfo.WeatherApiKey = WeatherApiKey;
        }
    }
}
