using CommunityToolkit.Mvvm.DependencyInjection;
using MahApps.Metro.Controls;
using NoFences.View.Modern;
using NoFencesService.Repository;
using System.IO;
using System.Windows;

namespace NoFences.View
{
    public partial class MonitoredPathFlyout : Flyout
    {
        public MonitoredPathFlyout()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetService<MonitoredPathViewModel>();
        }

        public MonitoredPathViewModel ViewModel => (MonitoredPathViewModel) DataContext;

        private void SaveMonitoredPath(object sender, RoutedEventArgs e)
        {
            ViewModel.Save();
            IsOpen = false;
        }

        private void FolderConfigurationClick(object sender, RoutedEventArgs e)
        {
            var folderConfigView = new FolderConfigurationView();
            folderConfigView.ExecuteWhenLoaded(() => ViewModel.CreateNewFolderConfiguration());
            var result = folderConfigView.ShowDialog();
            if (result.Value)
            {
                ViewModel.AddFolderConfiguration(folderConfigView.ViewModel.SelectedFolderConfiguration);
            }
        }

        private void EditFolderConfigurationClick(object sender, RoutedEventArgs e)
        {
            var folderConfigView = new FolderConfigurationView();
            folderConfigView.ExecuteWhenLoaded(() => ViewModel.EditFolderConfiguration());
            var result = folderConfigView.ShowDialog();
            if (result.Value)
            {
                ViewModel.AddFolderConfiguration(folderConfigView.ViewModel.SelectedFolderConfiguration);
            }
        }

        private void SelectPath_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var folderName = folderDialog.SelectedPath;

                ViewModel.SetPathInfo(folderName);
            }
        }

        private void Flyout_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.IsActive = true;
        }
    }
}
