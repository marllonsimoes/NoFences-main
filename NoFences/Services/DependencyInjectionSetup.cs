using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NoFences.Core.Model;
using NoFences.Model;
using NoFences.Model.Canvas;
using NoFences.View.Canvas.Handlers;
using System;
using System.Collections.Generic;

namespace NoFences.Services
{
    /// <summary>
    /// Dependency injection setup for the NEW canvas-based architecture with WPF handlers.
    /// This is separate from the original DependencyInjectionSetup.
    ///
    /// For the original DI setup, see DependencyInjectionSetup.cs
    /// </summary>
    public static class DependencyInjectionSetup
    {
        /// <summary>
        /// Initializes the IoC container for the canvas-based architecture with WPF handlers.
        /// </summary>
        /// <param name="useWorkerW">Whether to use WorkerW integration (experimental)</param>
        /// <returns>Service provider with all dependencies configured</returns>
        public static IServiceProvider InitializeIoCContainer(bool useWorkerW = false)
        {
            var services = new ServiceCollection();

            services.AddTransient<IFenceHandlerWpf, PictureFenceHandlerWpf>();
            services.AddTransient<IFenceHandlerWpf, FilesFenceHandlerWpf>();
            services.AddTransient<IFenceHandlerWpf, VideoFenceHandlerWpf>();
            services.AddTransient<IFenceHandlerWpf, ClockFenceHandlerWpf>();
            services.AddTransient<IFenceHandlerWpf, WidgetFenceHandlerWpf>();

            // Register WPF handler factory
            services.AddSingleton((serviceProvider) =>
            {
                var handlers = new Dictionary<string, Type>();
                handlers[EntryType.Pictures.ToString()] = typeof(PictureFenceHandlerWpf);
                handlers[EntryType.Files.ToString()] = typeof(FilesFenceHandlerWpf);
                handlers[EntryType.Video.ToString()] = typeof(VideoFenceHandlerWpf);
                handlers[EntryType.Clock.ToString()] = typeof(ClockFenceHandlerWpf);
                handlers[EntryType.Widget.ToString()] = typeof(WidgetFenceHandlerWpf);

                return new FenceHandlerFactoryWpf(handlers);
            });

            services.AddSingleton<FenceManager>(sp =>
            {
                var handlerFactory = sp.GetRequiredService<FenceHandlerFactoryWpf>();
                return new FenceManager(handlerFactory, useWorkerW);
            });

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Sets up the IoC container using CommunityToolkit.Mvvm (for compatibility with existing code).
        /// </summary>
        public static void InitializeIoCContainerWithToolkit(bool useWorkerW = false)
        {
            var serviceProvider = InitializeIoCContainer(useWorkerW);

            Ioc.Default.ConfigureServices(serviceProvider);
        }
    }
}
