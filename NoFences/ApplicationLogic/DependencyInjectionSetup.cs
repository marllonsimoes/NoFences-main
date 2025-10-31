using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NoFences.View;
using NoFences.View.Modern;
using NoFences.View.Service;

namespace NoFences.ApplicationLogic
{
    internal static class DependencyInjectionSetup
    {
        internal static void InitializeIoCContainer()
        {
            Ioc.Default.ConfigureServices(
               new ServiceCollection()
               .AddSingleton<ISettingsService, SettingsService>()
               .AddSingleton<IMonitoredPathService, MonitoredPathService>()
               .AddSingleton<IFoldersConfigurationService, FolderConfigurationService>()
               .AddSingleton<IDeviceInfoService, DeviceInfoService>()
               .AddTransient<MonitoredPathsViewModel>()
               .AddTransient<MonitoredPathViewModel>()
               .AddTransient<FolderConfigurationViewModel>()
               .BuildServiceProvider());
        }
    }
}
