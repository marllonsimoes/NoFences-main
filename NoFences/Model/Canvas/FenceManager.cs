using log4net;
using NoFences.Core.Model;
using NoFencesDataLayer.Repositories;
using NoFences.View.Canvas;
using NoFences.View.Canvas.Handlers;
using System;
using System.Collections.Generic;

namespace NoFences.Model.Canvas
{
    /// <summary>
    /// FenceManager that works with the DesktopCanvas architecture with WPF handlers.
    /// Instead of creating individual Form windows, this adds fences as
    /// UserControls to a single DesktopCanvas window, using WPF for content.
    ///
    /// This is part of the NEW canvas-based architecture with WPF integration.
    /// For the original manager, see FenceManager.cs
    /// </summary>
    public class FenceManager
    {
        #region Private Fields

        private static readonly ILog log = LogManager.GetLogger(typeof(FenceManager));

        private readonly IFenceRepository fenceRepository;
        private readonly FenceHandlerFactoryWpf fenceHandlerFactory;
        private readonly DesktopCanvas desktopCanvas;
        private readonly Dictionary<Guid, FenceInfo> loadedFences;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a FenceManager with default XML repository.
        /// </summary>
        public FenceManager(FenceHandlerFactoryWpf fenceHandlerFactory, bool useWorkerW = false)
            : this(fenceHandlerFactory, new XmlFenceRepository(), useWorkerW)
        {
        }

        /// <summary>
        /// Creates a FenceManager with custom repository (for testing or alternative storage).
        /// </summary>
        public FenceManager(FenceHandlerFactoryWpf fenceHandlerFactory, IFenceRepository repository, bool useWorkerW = false)
        {
            this.fenceHandlerFactory = fenceHandlerFactory ?? throw new ArgumentNullException(nameof(fenceHandlerFactory));
            this.fenceRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.loadedFences = new Dictionary<Guid, FenceInfo>();

            // Create the desktop canvas with WPF support
            desktopCanvas = new DesktopCanvas(fenceHandlerFactory, useWorkerW);

            // Wire up events
            desktopCanvas.FenceChanged += DesktopCanvas_FenceChanged;
            desktopCanvas.FenceDeleted += DesktopCanvas_FenceDeleted;

            log.Info($"FenceManager initialized with {repository.GetType().Name}");
        }

        #endregion

        #region Public Properties

        public DesktopCanvas Canvas => desktopCanvas;

        public int FenceCount => loadedFences.Count;

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the desktop canvas window.
        /// </summary>
        public void ShowCanvas()
        {
            if (!desktopCanvas.Visible)
            {
                desktopCanvas.Show();
                log.Debug("DesktopCanvas shown");
            }
        }

        /// <summary>
        /// Hides the desktop canvas window (and all fences).
        /// </summary>
        public void HideCanvas()
        {
            if (desktopCanvas.Visible)
            {
                desktopCanvas.Hide();
                log.Debug("DesktopCanvas hidden");
            }
        }

        /// <summary>
        /// Loads all saved fences from storage and adds them to the canvas.
        /// </summary>
        public void LoadFences()
        {
            try
            {
                var fences = fenceRepository.GetAll();
                int loadedCount = 0;

                foreach (var fenceInfo in fences)
                {
                    try
                    {
                        desktopCanvas.AddFence(fenceInfo);
                        loadedFences[fenceInfo.Id] = fenceInfo;
                        loadedCount++;
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error adding fence {fenceInfo.Name} to canvas: {ex.Message}");
                    }
                }

                log.Info($"Loaded {loadedCount} fences from repository");
            }
            catch (Exception ex)
            {
                log.Error($"Error loading fences: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a new fence with default settings.
        /// </summary>
        public void CreateFence(string name)
        {
            var fenceInfo = new FenceInfo(Guid.NewGuid())
            {
                Name = name,
                PosX = 100,
                PosY = 250,
                Height = 300,
                Width = 300
            };

            CreateFence(name, fenceInfo);
        }

        /// <summary>
        /// Creates a new fence from existing FenceInfo.
        /// </summary>
        public void CreateFence(string name, FenceInfo fenceInfo)
        {
            if (fenceInfo == null)
                throw new ArgumentNullException(nameof(fenceInfo));

            try
            {
                // Save to disk
                UpdateFence(fenceInfo);

                // Add to canvas
                desktopCanvas.AddFence(fenceInfo);

                // Track it
                loadedFences[fenceInfo.Id] = fenceInfo;

                log.Info($"Created fence: {fenceInfo.Name}");
            }
            catch (Exception ex)
            {
                log.Error($"Error creating fence: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Removes a fence from the canvas and deletes its data.
        /// </summary>
        public void RemoveFence(FenceInfo fenceInfo)
        {
            if (fenceInfo == null)
                return;

            try
            {
                // Remove from canvas
                desktopCanvas.RemoveFence(fenceInfo.Id);

                // Delete from repository
                fenceRepository.Delete(fenceInfo.Id);

                // Remove from tracking
                loadedFences.Remove(fenceInfo.Id);

                log.Info($"Removed fence: {fenceInfo.Name}");
            }
            catch (Exception ex)
            {
                log.Error($"Error removing fence: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves fence data to repository.
        /// </summary>
        public void UpdateFence(FenceInfo fenceInfo)
        {
            if (fenceInfo == null)
                return;

            try
            {
                if (fenceRepository.Save(fenceInfo))
                {
                    // Update tracking
                    loadedFences[fenceInfo.Id] = fenceInfo;
                }
                else
                {
                    log.Error($"Failed to save fence {fenceInfo.Name}");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error updating fence {fenceInfo.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a fence by ID.
        /// </summary>
        public FenceInfo GetFence(Guid id)
        {
            loadedFences.TryGetValue(id, out var fenceInfo);
            return fenceInfo;
        }

        /// <summary>
        /// Gets all loaded fences.
        /// </summary>
        public IEnumerable<FenceInfo> GetAllFences()
        {
            return loadedFences.Values;
        }

        #endregion

        #region Private Methods

        private void DesktopCanvas_FenceChanged(object sender, FenceInfo fenceInfo)
        {
            // Auto-save when fence changes
            UpdateFence(fenceInfo);
        }

        private void DesktopCanvas_FenceDeleted(object sender, FenceInfo fenceInfo)
        {
            // Cleanup when fence is deleted from canvas
            RemoveFence(fenceInfo);
        }

        #endregion
    }
}
