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

            stack.Children.Add(new TextBlock
            {
                Text = "Note: Analog clock style is coming in a future update.",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });

            Content = stack;
        }

        public override void LoadFromFenceInfo(FenceInfo fenceInfo)
        {
            // Load weather properties
            txtWeatherLocation.Text = fenceInfo.WeatherLocation ?? string.Empty;
            txtWeatherApiKey.Text = fenceInfo.WeatherApiKey ?? string.Empty;

            // TODO: Load other clock-specific properties when implemented
            // (Clock style, time format, show seconds/date)
        }

        public override void SaveToFenceInfo(FenceInfo fenceInfo)
        {
            // Save weather properties
            fenceInfo.WeatherLocation = txtWeatherLocation.Text?.Trim() ?? string.Empty;
            fenceInfo.WeatherApiKey = txtWeatherApiKey.Text?.Trim() ?? string.Empty;

            // TODO: Save other clock-specific properties when implemented
            // (Clock style, time format, show seconds/date)
        }
    }
}
