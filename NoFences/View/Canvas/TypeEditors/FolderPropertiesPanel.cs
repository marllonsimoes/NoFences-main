using NoFences.Core.Model;
using System.Windows;
using System.Windows.Controls;

namespace NoFences.View.Canvas.TypeEditors
{
    /// <summary>
    /// Properties panel for Folder fence type
    /// </summary>
    public class FolderPropertiesPanel : TypePropertiesPanel
    {
        private TextBox txtPath;
        private Button btnBrowse;
        private CheckBox chkIncludeSubfolders;

        public FolderPropertiesPanel()
        {
            var stack = new StackPanel();

            // Path selection
            stack.Children.Add(new TextBlock { Text = "Folder to Monitor:", Margin = new Thickness(0, 0, 0, 4) });

            var pathPanel = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            btnBrowse = new Button { Content = "Browse...", Width = 80, Margin = new Thickness(8, 0, 0, 0) };
            btnBrowse.Click += BtnBrowse_Click;
            DockPanel.SetDock(btnBrowse, Dock.Right);

            txtPath = new TextBox();
            pathPanel.Children.Add(btnBrowse);
            pathPanel.Children.Add(txtPath);
            stack.Children.Add(pathPanel);

            // Options
            chkIncludeSubfolders = new CheckBox { Content = "Include subfolders", Margin = new Thickness(0, 0, 0, 4) };
            stack.Children.Add(chkIncludeSubfolders);

            stack.Children.Add(new TextBlock
            {
                Text = "This fence will automatically display all files from the selected folder.",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });

            Content = stack;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath.Text = dialog.SelectedPath;
            }
        }

        public override void LoadFromFenceInfo(FenceInfo fenceInfo)
        {
            txtPath.Text = fenceInfo.Path ?? string.Empty;
            // TODO: Add IncludeSubfolders property to FenceInfo if needed
        }

        public override void SaveToFenceInfo(FenceInfo fenceInfo)
        {
            fenceInfo.Path = txtPath.Text;
            // TODO: Save IncludeSubfolders if property is added
        }
    }
}
