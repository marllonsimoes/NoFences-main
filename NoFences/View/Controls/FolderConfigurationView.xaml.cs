using CommunityToolkit.Mvvm.DependencyInjection;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using NoFences.View.Modern;
using NoFencesService.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace NoFences.View
{
    /// <summary>
    /// Interaction logic for MonitoredPath.xaml
    /// </summary>
    public partial class FolderConfigurationView : MetroWindow
    {
        public FolderConfigurationView()
        {
            InitializeComponent();
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();

            DataContext = Ioc.Default.GetService<FolderConfigurationViewModel>();
        }

        public FolderConfigurationViewModel ViewModel => (FolderConfigurationViewModel)DataContext;

        private void SaveChangesClick(object sender, RoutedEventArgs e)
        {
            ViewModel.Save();
            DialogResult = true;
            Close();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.IsActive = true;
        }
    }
}
