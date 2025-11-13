using NoFences.Core.Model;
using System.Windows;
using System.Windows.Controls;

namespace NoFences.View.Canvas.TypeEditors
{
    /// <summary>
    /// Properties panel for Clock fence type
    /// </summary>
    public class ClockPropertiesPanel : TypePropertiesPanel
    {
        private ComboBox cmbClockStyle;
        private ComboBox cmbTimeFormat;
        private CheckBox chkShowSeconds;
        private CheckBox chkShowDate;
        private TextBox txtWeatherLocation;
        private TextBox txtWeatherApiKey;

        // Weather element visibility checkboxes
        private CheckBox chkShowFeelsLike;
        private CheckBox chkShowHumidity;
        private CheckBox chkShowClouds;
        private CheckBox chkShowSunrise;
        private CheckBox chkShowSunset;
        private CheckBox chkShowWind;
        private CheckBox chkShowLocation;

        // Layout and font customization
        private ComboBox cmbLayout;
        private Slider sldTimeFontSize;
        private TextBlock lblTimeFontSizeValue;
        private Slider sldDateFontSize;
        private TextBlock lblDateFontSizeValue;
        private Slider sldWeatherFontSize;
        private TextBlock lblWeatherFontSizeValue;

        public ClockPropertiesPanel()
        {
            var stack = new StackPanel();

            // Clock Style
            stack.Children.Add(new TextBlock { Text = "Clock Style:", Margin = new Thickness(0, 0, 0, 4) });
            cmbClockStyle = new ComboBox { Margin = new Thickness(0, 0, 0, 8) };
            cmbClockStyle.Items.Add("Digital");
            cmbClockStyle.Items.Add("Analog");
            cmbClockStyle.SelectedIndex = 0;
            stack.Children.Add(cmbClockStyle);

            // Time Format
            stack.Children.Add(new TextBlock { Text = "Time Format:", Margin = new Thickness(0, 0, 0, 4) });
            cmbTimeFormat = new ComboBox { Margin = new Thickness(0, 0, 0, 8) };
            cmbTimeFormat.Items.Add("12-hour (AM/PM)");
            cmbTimeFormat.Items.Add("24-hour");
            cmbTimeFormat.SelectedIndex = 1;
            stack.Children.Add(cmbTimeFormat);

            // Options
            chkShowSeconds = new CheckBox { Content = "Show seconds", Margin = new Thickness(0, 0, 0, 4), IsChecked = true };
            stack.Children.Add(chkShowSeconds);

            chkShowDate = new CheckBox { Content = "Show date", Margin = new Thickness(0, 0, 0, 4), IsChecked = true };
            stack.Children.Add(chkShowDate);

            // Layout section separator
            stack.Children.Add(new System.Windows.Controls.Separator { Margin = new Thickness(0, 12, 0, 12) });

            // Layout Style
            stack.Children.Add(new TextBlock { Text = "Layout Style:", Margin = new Thickness(0, 0, 0, 4) });
            cmbLayout = new ComboBox { Margin = new Thickness(0, 0, 0, 8) };
            cmbLayout.Items.Add("Horizontal (default)");
            cmbLayout.Items.Add("Vertical (stacked elements)");
            cmbLayout.Items.Add("Pixel Phone (big weather icon)");
            cmbLayout.SelectedIndex = 0;
            stack.Children.Add(cmbLayout);

            // Font Sizes
            stack.Children.Add(new TextBlock { Text = "Font Sizes:", Margin = new Thickness(0, 0, 0, 4), FontWeight = FontWeights.SemiBold });

            // Time font size
            var timeFontPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            timeFontPanel.Children.Add(new TextBlock { Text = "Time: ", Width = 70, VerticalAlignment = VerticalAlignment.Center });
            sldTimeFontSize = new Slider { Minimum = 24, Maximum = 96, Value = 48, Width = 150, VerticalAlignment = VerticalAlignment.Center };
            sldTimeFontSize.ValueChanged += (s, e) => { if (lblTimeFontSizeValue != null) lblTimeFontSizeValue.Text = $"{(int)e.NewValue}px"; };
            timeFontPanel.Children.Add(sldTimeFontSize);
            lblTimeFontSizeValue = new TextBlock { Text = "48px", Width = 50, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            timeFontPanel.Children.Add(lblTimeFontSizeValue);
            stack.Children.Add(timeFontPanel);

            // Date font size
            var dateFontPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            dateFontPanel.Children.Add(new TextBlock { Text = "Date: ", Width = 70, VerticalAlignment = VerticalAlignment.Center });
            sldDateFontSize = new Slider { Minimum = 10, Maximum = 48, Value = 16, Width = 150, VerticalAlignment = VerticalAlignment.Center };
            sldDateFontSize.ValueChanged += (s, e) => { if (lblDateFontSizeValue != null) lblDateFontSizeValue.Text = $"{(int)e.NewValue}px"; };
            dateFontPanel.Children.Add(sldDateFontSize);
            lblDateFontSizeValue = new TextBlock { Text = "16px", Width = 50, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            dateFontPanel.Children.Add(lblDateFontSizeValue);
            stack.Children.Add(dateFontPanel);

            // Weather font size
            var weatherFontPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            weatherFontPanel.Children.Add(new TextBlock { Text = "Weather: ", Width = 70, VerticalAlignment = VerticalAlignment.Center });
            sldWeatherFontSize = new Slider { Minimum = 10, Maximum = 48, Value = 16, Width = 150, VerticalAlignment = VerticalAlignment.Center };
            sldWeatherFontSize.ValueChanged += (s, e) => { if (lblWeatherFontSizeValue != null) lblWeatherFontSizeValue.Text = $"{(int)e.NewValue}px"; };
            weatherFontPanel.Children.Add(sldWeatherFontSize);
            lblWeatherFontSizeValue = new TextBlock { Text = "16px", Width = 50, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            weatherFontPanel.Children.Add(lblWeatherFontSizeValue);
            stack.Children.Add(weatherFontPanel);

            // Weather section separator
            stack.Children.Add(new System.Windows.Controls.Separator { Margin = new Thickness(0, 12, 0, 12) });

            // Weather Location
            stack.Children.Add(new TextBlock { Text = "Weather Location:", Margin = new Thickness(0, 0, 0, 4) });
            txtWeatherLocation = new TextBox { Margin = new Thickness(0, 0, 0, 4) };
            stack.Children.Add(txtWeatherLocation);
            stack.Children.Add(new TextBlock
            {
                Text = "City name (e.g., \"London\", \"New York\", \"Tokyo\")\nLeave empty to disable weather display",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Weather API Key
            stack.Children.Add(new TextBlock { Text = "OpenWeatherMap API Key (Optional):", Margin = new Thickness(0, 0, 0, 4) });
            txtWeatherApiKey = new TextBox { Margin = new Thickness(0, 0, 0, 4) };
            stack.Children.Add(txtWeatherApiKey);
            stack.Children.Add(new TextBlock
            {
                Text = "Get a free API key at https://openweathermap.org/api\nLeave empty to use the default demo key (limited)",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Weather Elements section
            stack.Children.Add(new TextBlock
            {
                Text = "Weather Elements to Show:",
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            });

            chkShowFeelsLike = new CheckBox { Content = "Show \"feels like\" temperature", Margin = new Thickness(0, 0, 0, 4), IsChecked = true };
            stack.Children.Add(chkShowFeelsLike);

            chkShowHumidity = new CheckBox { Content = "Show humidity", Margin = new Thickness(0, 0, 0, 4), IsChecked = true };
            stack.Children.Add(chkShowHumidity);

            chkShowClouds = new CheckBox { Content = "Show cloud coverage", Margin = new Thickness(0, 0, 0, 4), IsChecked = true };
            stack.Children.Add(chkShowClouds);

            chkShowSunrise = new CheckBox { Content = "Show sunrise time", Margin = new Thickness(0, 0, 0, 4), IsChecked = true };
            stack.Children.Add(chkShowSunrise);

            chkShowSunset = new CheckBox { Content = "Show sunset time", Margin = new Thickness(0, 0, 0, 4), IsChecked = true };
            stack.Children.Add(chkShowSunset);

            chkShowWind = new CheckBox { Content = "Show wind information", Margin = new Thickness(0, 0, 0, 4), IsChecked = true };
            stack.Children.Add(chkShowWind);

            chkShowLocation = new CheckBox { Content = "Show location name", Margin = new Thickness(0, 0, 0, 8), IsChecked = false };
            stack.Children.Add(chkShowLocation);

            stack.Children.Add(new TextBlock
            {
                Text = "Note: Analog clock style and some advanced layout features are coming in future updates.",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });

            Content = stack;
        }

        public override void LoadFromFenceInfo(FenceInfo fenceInfo)
        {
            // Load clock style
            cmbClockStyle.SelectedIndex = fenceInfo.ClockStyle == "Analog" ? 1 : 0;

            // Load time format
            cmbTimeFormat.SelectedIndex = fenceInfo.TimeFormat == "12h" ? 0 : 1;

            // Load clock display options
            chkShowSeconds.IsChecked = fenceInfo.ShowSeconds;
            chkShowDate.IsChecked = fenceInfo.ShowDate;

            // Load layout style
            cmbLayout.SelectedIndex = fenceInfo.ClockLayout == "Vertical" ? 1
                : fenceInfo.ClockLayout == "PixelPhone" ? 2
                : 0;

            // Load font sizes
            sldTimeFontSize.Value = fenceInfo.TimeFontSize;
            sldDateFontSize.Value = fenceInfo.DateFontSize;
            sldWeatherFontSize.Value = fenceInfo.WeatherFontSize;

            // Load weather properties
            txtWeatherLocation.Text = fenceInfo.WeatherLocation ?? string.Empty;
            txtWeatherApiKey.Text = fenceInfo.WeatherApiKey ?? string.Empty;

            // Load weather element visibility
            chkShowFeelsLike.IsChecked = fenceInfo.ShowFeelsLike;
            chkShowHumidity.IsChecked = fenceInfo.ShowHumidity;
            chkShowClouds.IsChecked = fenceInfo.ShowClouds;
            chkShowSunrise.IsChecked = fenceInfo.ShowSunrise;
            chkShowSunset.IsChecked = fenceInfo.ShowSunset;
            chkShowWind.IsChecked = fenceInfo.ShowWind;
            chkShowLocation.IsChecked = fenceInfo.ShowLocation;
        }

        public override void SaveToFenceInfo(FenceInfo fenceInfo)
        {
            // Save clock style
            fenceInfo.ClockStyle = cmbClockStyle.SelectedIndex == 1 ? "Analog" : "Digital";

            // Save time format
            fenceInfo.TimeFormat = cmbTimeFormat.SelectedIndex == 0 ? "12h" : "24h";

            // Save clock display options
            fenceInfo.ShowSeconds = chkShowSeconds.IsChecked ?? true;
            fenceInfo.ShowDate = chkShowDate.IsChecked ?? true;

            // Save layout style
            fenceInfo.ClockLayout = cmbLayout.SelectedIndex == 1 ? "Vertical"
                : cmbLayout.SelectedIndex == 2 ? "PixelPhone"
                : "Horizontal";

            // Save font sizes
            fenceInfo.TimeFontSize = (int)sldTimeFontSize.Value;
            fenceInfo.DateFontSize = (int)sldDateFontSize.Value;
            fenceInfo.WeatherFontSize = (int)sldWeatherFontSize.Value;

            // Save weather properties
            fenceInfo.WeatherLocation = txtWeatherLocation.Text?.Trim() ?? string.Empty;
            fenceInfo.WeatherApiKey = txtWeatherApiKey.Text?.Trim() ?? string.Empty;

            // Save weather element visibility
            fenceInfo.ShowFeelsLike = chkShowFeelsLike.IsChecked ?? true;
            fenceInfo.ShowHumidity = chkShowHumidity.IsChecked ?? true;
            fenceInfo.ShowClouds = chkShowClouds.IsChecked ?? true;
            fenceInfo.ShowSunrise = chkShowSunrise.IsChecked ?? true;
            fenceInfo.ShowSunset = chkShowSunset.IsChecked ?? true;
            fenceInfo.ShowWind = chkShowWind.IsChecked ?? true;
            fenceInfo.ShowLocation = chkShowLocation.IsChecked ?? false;
        }
    }
}
