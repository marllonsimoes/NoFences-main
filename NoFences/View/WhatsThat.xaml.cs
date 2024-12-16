using CommunityToolkit.Mvvm.DependencyInjection;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using NoFences.View.Modern;
using NoFencesService.Repository;
using System.Data.Entity;
using System.IO;
using System;
using System.Windows;
using System.Windows.Controls;

namespace NoFences.View
{
    /// <summary>
    /// Interaction logic for WhatsThat.xaml
    /// </summary>
    public partial class WhatsThat : MetroWindow
    {
        public WhatsThat()
        {
            InitializeComponent();
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();

            DataContext = Ioc.Default.GetService<MonitoredPathsViewModel>();
            MonitoredPathFlyout.ClosingFinished += MonitoredPathFlyout_ClosingFinished;
        }

        private void MonitoredPathFlyout_ClosingFinished(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadAll();
        }

        public MonitoredPathsViewModel ViewModel => (MonitoredPathsViewModel) DataContext;


        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadAll();
        }

        private void MetroWindow_Activated(object sender, EventArgs e)
        {
            ViewModel.IsActive = true;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Visibility = Visibility.Hidden;
            e.Cancel = true;
        }

        private void EditMonitoredPath(object sender, RoutedEventArgs e)
        {
            var tileDataContext = (sender as Tile).DataContext;
            ViewModel.EditMonitoredPath((NoFencesService.Repository.MonitoredPath)tileDataContext);
        }
    }
}
