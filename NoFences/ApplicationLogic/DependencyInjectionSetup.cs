using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NoFences.View.Fences.Handlers;
using NoFences.Model;
using System;
using System.Collections.Generic;

namespace NoFences.ApplicationLogic
{
    internal static class DependencyInjectionSetup
    {
        internal static void InitializeIoCContainer()
        {
            Ioc.Default.ConfigureServices(
               new ServiceCollection()
               .AddTransient<IFenceHandler, PictureFenceHandler>()
               .AddTransient<IFenceHandler, FilesFenceHandler>()
               .AddSingleton((serviceProvider) =>
               {
                   var handlers = new Dictionary<string, Type>();
                   handlers[EntryType.Pictures.ToString()] = typeof(PictureFenceHandler);
                   handlers[EntryType.Files.ToString()] = typeof(FilesFenceHandler);

                   return new FenceHandlerFactory(handlers);
               })
               .AddSingleton<FenceManager>()
               .BuildServiceProvider());
        }
    }
}
