using NoFences.Core.Model;
using System.Windows;
using System.Windows.Controls;

namespace NoFences.View.Canvas.TypeEditors
{
    /// <summary>
    /// Properties panel for Widget fence type
    /// </summary>
    public class WidgetPropertiesPanel : TypePropertiesPanel
    {
        private ComboBox cmbWidgetType;

        public WidgetPropertiesPanel()
        {
            var stack = new StackPanel();

            stack.Children.Add(new TextBlock { Text = "Widget Type:", Margin = new Thickness(0, 0, 0, 4) });
            cmbWidgetType = new ComboBox { Margin = new Thickness(0, 0, 0, 8) };
            cmbWidgetType.Items.Add("Weather");
            cmbWidgetType.Items.Add("System Info (CPU/RAM)");
            cmbWidgetType.Items.Add("Calendar");
            cmbWidgetType.Items.Add("Notes");
            cmbWidgetType.Items.Add("RSS Feed");
            cmbWidgetType.SelectedIndex = 0;
            stack.Children.Add(cmbWidgetType);

            stack.Children.Add(new TextBlock
            {
                Text = "Widget system is coming soon!\n\nWidgets will allow you to display dynamic information like weather, system statistics, calendars, and more on your desktop.",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });

            Content = stack;
        }

        public override void LoadFromFenceInfo(FenceInfo fenceInfo)
        {
            // TODO: Implement when widget system is ready
        }

        public override void SaveToFenceInfo(FenceInfo fenceInfo)
        {
            // TODO: Implement when widget system is ready
        }
    }
}
