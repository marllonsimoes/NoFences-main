using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NoFences.Core.Model;
using NoFences.Core.Util;
using NoFences.Model;
using NoFences.Model.Canvas;
using NoFences.View.Canvas.Handlers;
using NoFencesDataLayer.Repositories;
using NoFencesDataLayer.Services;
using NoFencesDataLayer.Services.Metadata;
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

            // Session 12: Register ALL repositories
            services.AddSingleton<IInstalledSoftwareRepository, InstalledSoftwareRepository>();
            services.AddSingleton<ISoftwareReferenceRepository, SoftwareReferenceRepository>(); // Session 12: DB refactor
            services.AddSingleton<IAmazonGamesRepository, AmazonGamesRepository>();
            services.AddSingleton<IFenceRepository, XmlFenceRepository>();

            // Session 12: Register database contexts for DI
            services.AddSingleton<NoFencesDataLayer.MasterCatalog.MasterCatalogContext>();
            services.AddSingleton<NoFencesService.Repository.LocalDBContext>();

            // Session 12: Register ALL services
            services.AddSingleton<InstalledSoftwareService>();
            services.AddSingleton<SoftwareCatalogService>();
            services.AddSingleton<EnhancedInstalledAppsService>();
            services.AddSingleton<CatalogDownloadService>();

            // Session 12: Register metadata providers (stateless, use Singleton for efficiency)
            // Game metadata providers
            services.AddSingleton<IGameMetadataProvider, RawgApiClient>();

            // Software metadata providers (sorted by priority in MetadataEnrichmentService)
            services.AddSingleton<ISoftwareMetadataProvider, WingetApiClient>();    // Priority 1
            services.AddSingleton<ISoftwareMetadataProvider, CnetScraperClient>();   // Priority 10
            services.AddSingleton<ISoftwareMetadataProvider, WikipediaApiClient>(); // Priority 99

            // Metadata enrichment service (receives IEnumerable<providers> from DI)
            services.AddSingleton<MetadataEnrichmentService>();

            // Session 12: Register game store detectors
            services.AddSingleton<IGameStoreDetector, AmazonGamesDetector>();
            services.AddSingleton<IGameStoreDetector, SteamStoreDetector>();
            services.AddSingleton<IGameStoreDetector, GOGGalaxyDetector>();
            services.AddSingleton<IGameStoreDetector, EpicGamesStoreDetector>();
            services.AddSingleton<IGameStoreDetector, EAAppDetector>();
            services.AddSingleton<IGameStoreDetector, UbisoftConnectDetector>();

            // Register fence handlers
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
