using log4net;
using NoFences.Core.Model;
using NoFences.Model;
using NoFences.Util;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NoFences.View.Canvas.Handlers
{
    /// <summary>
    /// WPF-based handler for displaying an analog or digital clock.
    /// Can show current time, date, and potentially multiple time zones.
    /// This is part of the NEW canvas-based architecture.
    /// </summary>
    public class ClockFenceHandlerWpf : IFenceHandlerWpf
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ClockFenceHandlerWpf));

        private FenceInfo fenceInfo;
        private DispatcherTimer timer;
        private DispatcherTimer weatherTimer;
        private TextBlock timeTextBlock;
        private TextBlock dateTextBlock;
        private System.Windows.Controls.Image weatherIconImage;
        private TextBlock temperatureTextBlock;
        private TextBlock feelsLikeTextBlock;
        private TextBlock humidityTextBlock;
        private TextBlock cloudsTextBlock;
        private TextBlock sunriseTextBlock;
        private TextBlock sunsetTextBlock;
        private TextBlock windDirectionTextBlock;
        private TextBlock windSpeedTextBlock;

        // Event raised when content changes (for auto-height)
        // Clock doesn't need to raise this since it has static size
        public event EventHandler ContentChanged;

        public void Initialize(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo ?? throw new ArgumentNullException(nameof(fenceInfo));
            log.Debug($"Initialized");
        }

        public UIElement CreateContentElement(int titleHeight, FenceThemeDefinition theme)
        {
            log.Debug($"Creating content element");

            var contentBg = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                theme.ContentBackgroundColor.A,
                theme.ContentBackgroundColor.R,
                theme.ContentBackgroundColor.G,
                theme.ContentBackgroundColor.B));

            var textColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                theme.ContentTextColor.A,
                theme.ContentTextColor.R,
                theme.ContentTextColor.G,
                theme.ContentTextColor.B));

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Time display (respect user preferences including font size)
            string timeFormat = GetTimeFormat();
            timeTextBlock = new TextBlock
            {
                Text = DateTime.Now.ToString(timeFormat),
                Foreground = textColor,
                FontSize = fenceInfo.TimeFontSize,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 10)
            };
            stackPanel.Children.Add(timeTextBlock);

            // Date display (only add if ShowDate is enabled)
            if (fenceInfo.ShowDate)
            {
                dateTextBlock = new TextBlock
                {
                    Text = DateTime.Now.ToString("dddd, dd/MM/yyyy"),
                    Foreground = textColor,
                    FontSize = fenceInfo.DateFontSize,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                stackPanel.Children.Add(dateTextBlock);
            }

            // Weather display (if location is configured)
            if (!string.IsNullOrEmpty(fenceInfo.WeatherLocation))
            {
                // Show location name if enabled
                if (fenceInfo.ShowLocation)
                {
                    var locationTextBlock = new TextBlock
                    {
                        Text = fenceInfo.WeatherLocation,
                        Foreground = textColor,
                        FontSize = fenceInfo.WeatherFontSize,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 8),
                        Opacity = 0.8
                    };
                    stackPanel.Children.Add(locationTextBlock);
                }

                // Create a grid for better weather layout
                var weatherGrid = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 20),
                    MaxWidth = 400
                };

                // Define 3 rows for weather info
                weatherGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Row 0: Weather + Temp + Feels like
                weatherGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Row 1: Humidity + Clouds
                weatherGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Row 2: Sunrise/Sunset + Wind

                // Row 0: Weather condition + temperature + feels like
                var row0Panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                // Weather icon (image from OpenWeatherMap)
                weatherIconImage = new System.Windows.Controls.Image
                {
                    Width = 50,
                    Height = 50,
                    Margin = new Thickness(0, 0, 8, 0),
                    Stretch = System.Windows.Media.Stretch.Uniform
                };

                temperatureTextBlock = new TextBlock
                {
                    Text = "--°C",
                    Foreground = textColor,
                    FontSize = fenceInfo.WeatherFontSize + 4, // Slightly larger than other weather text
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                row0Panel.Children.Add(weatherIconImage);
                row0Panel.Children.Add(temperatureTextBlock);

                // Only add "feels like" if enabled
                if (fenceInfo.ShowFeelsLike)
                {
                    feelsLikeTextBlock = new TextBlock
                    {
                        Text = "(feels like --°C)",
                        Foreground = textColor,
                        FontSize = fenceInfo.WeatherFontSize - 2, // Slightly smaller
                        Opacity = 0.8,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    row0Panel.Children.Add(feelsLikeTextBlock);
                }
                Grid.SetRow(row0Panel, 0);
                weatherGrid.Children.Add(row0Panel);

                // Row 1: Humidity + Clouds (respect visibility settings)
                if (fenceInfo.ShowHumidity || fenceInfo.ShowClouds)
                {
                    var row1Panel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    if (fenceInfo.ShowHumidity)
                    {
                        humidityTextBlock = new TextBlock
                        {
                            Text = "💧 --%",
                            Foreground = textColor,
                            FontSize = fenceInfo.WeatherFontSize,
                            Margin = new Thickness(0, 0, 16, 0)
                        };
                        row1Panel.Children.Add(humidityTextBlock);
                    }

                    if (fenceInfo.ShowClouds)
                    {
                        cloudsTextBlock = new TextBlock
                        {
                            Text = "☁️ --%",
                            Foreground = textColor,
                            FontSize = fenceInfo.WeatherFontSize
                        };
                        row1Panel.Children.Add(cloudsTextBlock);
                    }

                    Grid.SetRow(row1Panel, 1);
                    weatherGrid.Children.Add(row1Panel);
                }

                // Row 2: Sunrise/Sunset + Wind (respect visibility settings)
                if (fenceInfo.ShowSunrise || fenceInfo.ShowSunset || fenceInfo.ShowWind)
                {
                    var row2Panel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    if (fenceInfo.ShowSunrise)
                    {
                        sunriseTextBlock = new TextBlock
                        {
                            Text = "🌅 --:--",
                            Foreground = textColor,
                            FontSize = fenceInfo.WeatherFontSize,
                            Margin = new Thickness(0, 0, 12, 0)
                        };
                        row2Panel.Children.Add(sunriseTextBlock);
                    }

                    if (fenceInfo.ShowSunset)
                    {
                        sunsetTextBlock = new TextBlock
                        {
                            Text = "🌇 --:--",
                            Foreground = textColor,
                            FontSize = fenceInfo.WeatherFontSize,
                            Margin = new Thickness(0, 0, 16, 0)
                        };
                        row2Panel.Children.Add(sunsetTextBlock);
                    }

                    if (fenceInfo.ShowWind)
                    {
                        windDirectionTextBlock = new TextBlock
                        {
                            Text = "🧭",
                            Foreground = textColor,
                            FontSize = fenceInfo.WeatherFontSize + 4, // Slightly larger for icon
                            Margin = new Thickness(0, 0, 4, 0)
                        };
                        row2Panel.Children.Add(windDirectionTextBlock);

                        windSpeedTextBlock = new TextBlock
                        {
                            Text = "-- km/h",
                            Foreground = textColor,
                            FontSize = fenceInfo.WeatherFontSize
                        };
                        row2Panel.Children.Add(windSpeedTextBlock);
                    }

                    Grid.SetRow(row2Panel, 2);
                    weatherGrid.Children.Add(row2Panel);
                }

                stackPanel.Children.Add(weatherGrid);


                // Start weather timer (update every 15 minutes)
                weatherTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMinutes(15)
                };
                weatherTimer.Tick += WeatherTimer_Tick;
                weatherTimer.Start();

                // Initial weather fetch
                UpdateWeather();

                log.Debug($"Weather display enabled for location '{fenceInfo.WeatherLocation}'");
            }

            var border = new Border
            {
                Background = contentBg,
                Child = stackPanel
            };

            // Setup timer to update every second
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();

            log.Debug($"Clock started");

            return border;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (timeTextBlock != null)
            {
                string timeFormat = GetTimeFormat();
                timeTextBlock.Text = DateTime.Now.ToString(timeFormat);
            }
            if (dateTextBlock != null)
            {
                dateTextBlock.Text = DateTime.Now.ToString("dddd, dd/MM/yyyy");
            }
        }

        /// <summary>
        /// Gets the time format string based on user preferences.
        /// </summary>
        private string GetTimeFormat()
        {
            bool is12Hour = fenceInfo.TimeFormat == "12h";
            bool showSeconds = fenceInfo.ShowSeconds;

            if (is12Hour)
            {
                return showSeconds ? "hh:mm:ss tt" : "hh:mm tt";
            }
            else
            {
                return showSeconds ? "HH:mm:ss" : "HH:mm";
            }
        }

        private void WeatherTimer_Tick(object sender, EventArgs e)
        {
            UpdateWeather();
        }

        private async void UpdateWeather()
        {
            if (string.IsNullOrEmpty(fenceInfo.WeatherLocation))
                return;

            try
            {
                var weatherData = await WeatherService.GetWeatherAsync(
                    fenceInfo.WeatherLocation,
                    fenceInfo.WeatherApiKey);

                if (weatherData != null && weatherIconImage != null && temperatureTextBlock != null)
                {
                    // Download weather icon
                    var weatherIcon = await WeatherService.GetWeatherIconAsync(weatherData.IconCode);

                    // Update UI on dispatcher thread
                    weatherIconImage.Dispatcher.Invoke(() =>
                    {
                        // Row 0: Weather icon + temperature + feels like
                        if (weatherIcon != null)
                        {
                            weatherIconImage.Source = weatherIcon;
                        }
                        else
                        {
                            // Fallback: Clear the icon if download failed
                            weatherIconImage.Source = null;
                            log.Warn($"Weather icon not available for code: {weatherData.IconCode}");
                        }

                        temperatureTextBlock.Text = $"{weatherData.Temperature}°C";
                        if (feelsLikeTextBlock != null)
                            feelsLikeTextBlock.Text = $"(feels like {weatherData.FeelsLike}°C)";

                        // Row 1: Humidity + Clouds
                        if (humidityTextBlock != null)
                            humidityTextBlock.Text = $"{WeatherService.GetHumidityIcon()} {weatherData.Humidity}%";
                        if (cloudsTextBlock != null)
                            cloudsTextBlock.Text = $"{WeatherService.GetCloudIcon()} {weatherData.Clouds}%";

                        // Row 2: Sunrise/Sunset + Wind
                        if (sunriseTextBlock != null)
                            sunriseTextBlock.Text = $"{WeatherService.GetSunIcon(true)} {WeatherService.FormatTime(weatherData.Sunrise)}";
                        if (sunsetTextBlock != null)
                            sunsetTextBlock.Text = $"{WeatherService.GetSunIcon(false)} {WeatherService.FormatTime(weatherData.Sunset)}";
                        if (windDirectionTextBlock != null)
                            windDirectionTextBlock.Text = WeatherService.GetWindDirectionArrow(weatherData.WindDirection);
                        if (windSpeedTextBlock != null)
                            windSpeedTextBlock.Text = WeatherService.GetWindSpeedInKmH(weatherData.WinsdSpeed);
                    });

                    log.Debug($"Weather updated - {weatherData.Temperature}°C (feels like {weatherData.FeelsLike}°C), {weatherData.Description} (icon: {weatherData.IconCode}), Humidity: {weatherData.Humidity}%, Clouds: {weatherData.Clouds}%");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error updating weather: {ex.Message}", ex);

                // Show error state on UI
                if (weatherIconImage != null && temperatureTextBlock != null)
                {
                    weatherIconImage.Dispatcher.Invoke(() =>
                    {
                        weatherIconImage.Source = null;
                        temperatureTextBlock.Text = "--°C";
                        if (feelsLikeTextBlock != null)
                            feelsLikeTextBlock.Text = "(feels like --°C)";
                        if (humidityTextBlock != null)
                            humidityTextBlock.Text = "💧 --%";
                        if (cloudsTextBlock != null)
                            cloudsTextBlock.Text = "☁️ --%";
                        if (sunriseTextBlock != null)
                            sunriseTextBlock.Text = "🌅 --:--";
                        if (sunsetTextBlock != null)
                            sunsetTextBlock.Text = "🌇 --:--";
                        if (windDirectionTextBlock != null)
                            windDirectionTextBlock.Text = "🧭";
                        if (windSpeedTextBlock != null)
                            windSpeedTextBlock.Text = "-- km/h";
                    });
                }
            }
        }

        public void Refresh()
        {
            // Nothing to refresh for clock
        }

        public void Cleanup()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= Timer_Tick;
                timer = null;
            }

            if (weatherTimer != null)
            {
                weatherTimer.Stop();
                weatherTimer.Tick -= WeatherTimer_Tick;
                weatherTimer = null;
            }

            log.Debug($"Cleaned up");
        }

        public bool HasContent()
        {
            // Clock always has content (the clock itself)
            return true;
        }

        // ✅ IMPLEMENTED (Session 11):
        // - 12h vs 24h format
        // - Show/hide seconds
        // - Show/hide date
        // - Show/hide individual weather elements (6 controls)
        // - Font size customization (time, date, weather)
        // - Show location name
        //
        // TODO: Future enhancements:
        // - Analog vs Digital display (placeholder exists)
        // - Vertical layout (all elements stacked)
        // - PixelPhone layout (big weather icon, hour/minute/second on separate lines)
        // - Time zone selection
        // - Custom color themes
    }
}
