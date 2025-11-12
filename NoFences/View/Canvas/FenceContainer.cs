using ControlzEx.Theming;
using log4net;
using MahApps.Metro.IconPacks;
using NoFences.Behaviors;
using NoFences.Core.Model;
using NoFences.Core.Settings;
using NoFences.Core.Util;
using NoFences.View.Canvas.Handlers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using WpfControls = System.Windows.Controls;
using WpfMedia = System.Windows.Media;

namespace NoFences.View.Canvas
{
    /// <summary>
    /// A WinForms UserControl that represents a single fence using WPF content.
    /// This version hosts WPF UIElements via ElementHost instead of using WinForms painting.
    ///
    /// This is part of the NEW canvas-based architecture with WPF integration.
    /// For the original Form-per-fence approach, see FenceWindow.cs
    /// </summary>
    public class FenceContainer : System.Windows.Forms.UserControl
    {
        #region Private Fields

        private static readonly ILog log = LogManager.GetLogger(typeof(FenceContainer));

        private readonly FenceInfo fenceInfo;
        private IFenceHandlerWpf fenceHandler;
        private readonly ThrottledExecution throttledMove = new ThrottledExecution(TimeSpan.FromSeconds(4));
        private readonly ThrottledExecution throttledResize = new ThrottledExecution(TimeSpan.FromSeconds(4));

        // Behavior instances
        private FenceFadeAnimationBehavior fadeAnimation;
        private FenceMinifyBehavior minifyBehavior;
        private FenceRoundedCornersBehavior roundedCornersBehavior;
        private FenceDragBehavior dragBehavior;
        private FenceResizeBehavior resizeBehavior;

        private Font titleFont;
        private int logicalTitleHeight;
        private int titleHeight;
        private const int titleOffset = 3;

        private FenceThemeDefinition currentTheme;

        private ElementHost elementHost;
        private Panel titlePanel;
        private Button helpButton;

        // Drop zone overlay for visual drag & drop feedback
        private DropZoneOverlay dropZoneOverlay;
        private DroppedContentAnalysis currentDragAnalysis;

        // Resize border panels
        private Panel borderLeft;
        private Panel borderRight;
        private Panel borderTop;
        private Panel borderBottom;
        private Panel borderBottomRight; // Corner grip
        private const int BorderSize = 5; // Visible border thickness

        // Hover state for resize borders
        private bool isBorderHovered = false;

        // For fade animation compatibility
        private bool isFadedOut = false;
        private double currentOpacity = 1.0;
        private const double FadedContentOpacity = 0.8;

        private WpfControls.ContextMenu contextMenu;
        private WpfControls.MenuItem lockedMenuItem;
        private WpfControls.MenuItem minifyMenuItem;
        private WpfControls.MenuItem themeMenuItem;
        private WpfControls.MenuItem addFilesMenuItem;
        private WpfControls.MenuItem editMenuItem;
        private WpfControls.MenuItem deleteMenuItem;

        #endregion

        #region Events

        public event EventHandler<FenceInfo> FenceDeleted;
        public event EventHandler<FenceInfo> FenceEdited;
        public event EventHandler<FenceInfo> FenceChanged;

        #endregion

        #region Constructor

        public FenceContainer(FenceInfo fenceInfo, FenceHandlerFactoryWpf handlerFactory)
        {
            this.fenceInfo = fenceInfo ?? throw new ArgumentNullException(nameof(fenceInfo));

            // Basic setup
            this.DoubleBuffered = true;
            this.AllowDrop = true;

            // Wire up drag-and-drop events
            this.DragEnter += FenceContainer_DragEnter;
            this.DragLeave += FenceContainer_DragLeave;
            this.DragDrop += FenceContainer_DragDrop;

            // Apply theme
            ApplyTheme();

            // Position and size with boundary enforcement
            var constrainedBounds = ConstrainToScreenBounds(
                fenceInfo.PosX,
                fenceInfo.PosY,
                fenceInfo.Width,
                fenceInfo.Height);

            this.Location = new Point(constrainedBounds.X, constrainedBounds.Y);
            this.Size = new Size(constrainedBounds.Width, constrainedBounds.Height);

            // Update FenceInfo if position was adjusted
            if (fenceInfo.PosX != constrainedBounds.X || fenceInfo.PosY != constrainedBounds.Y)
            {
                fenceInfo.PosX = constrainedBounds.X;
                fenceInfo.PosY = constrainedBounds.Y;
                log.Debug($"FenceContainer: Position adjusted to stay within screen bounds");
            }

            log.Debug($"FenceContainer: Position=({constrainedBounds.X}, {constrainedBounds.Y}), Size=({constrainedBounds.Width}, {constrainedBounds.Height})");

            // Title setup
            logicalTitleHeight = (fenceInfo.TitleHeight < 16 || fenceInfo.TitleHeight > 100) ? 35 : fenceInfo.TitleHeight;
            ReloadFonts();

            // Create title panel
            CreateTitlePanel();

            // Create resize borders
            CreateResizeBorders();

            // Create WPF content area
            CreateWpfContentArea(handlerFactory);

            // Initialize behaviors
            InitializeBehaviors();

            // Minify setup
            if (fenceInfo.CanMinify)
            {
                minifyBehavior.TryMinify();
                // Initialize fade state for minified fences to 30% opacity
                fadeAnimation.SetMinifiedOpacity();
            }

            // Create context menu
            CreateContextMenu();

            // Wire up events
            this.Resize += FenceContainer_Resize;
            this.MouseEnter += FenceContainer_MouseEnter;
            this.MouseLeave += FenceContainer_MouseLeave;
            // Note: Resize is now handled by border panels, not container mouse events
            // this.HandleCreated += FenceContainer_HandleCreated; // Blur disabled for now

            // Start fade animation tracking
            fadeAnimation.Start();

            // Apply rounded corners if configured
            roundedCornersBehavior.Apply();

            log.Info($"FenceContainer (WPF) created: {fenceInfo.Name} at ({fenceInfo.PosX}, {fenceInfo.PosY})");
        }

        private void ShowWelcomeTipsIfNeeded()
        {
            try
            {
                var prefs = UserPreferences.Load();

                // Show tips on first fence creation or if user hasn't seen them
                if (!prefs.HasSeenWelcomeTips)
                {
                    prefs.HasSeenWelcomeTips = true;
                    prefs.FencesCreatedCount++;
                    prefs.Save();

                    // Delay showing help to let the fence fully render first
                    var welcomeTimer = new System.Windows.Forms.Timer();
                    welcomeTimer.Interval = 500; // 500ms delay
                    welcomeTimer.Tick += (s, e) =>
                    {
                        welcomeTimer.Stop();
                        welcomeTimer.Dispose();

                        try
                        {
                            var helpWindow = new FenceHelpWindow(fenceInfo);
                            helpWindow.ShowDialog();
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Error showing welcome tips: {ex.Message}", ex);
                        }
                    };
                    welcomeTimer.Start();

                    log.Debug("Showing welcome tips for first-time user");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error checking/showing welcome tips: {ex.Message}", ex);
            }
        }

        private void InitializeBehaviors()
        {
            // 1. Minify Behavior (created first so fade animation can reference it)
            minifyBehavior = new FenceMinifyBehavior(
                this,
                () => titleHeight,
                () => fenceInfo.CanMinify);

            // 2. Fade Animation Behavior
            fadeAnimation = new FenceFadeAnimationBehavior(
                this,
                () => fenceInfo,
                () => fenceHandler?.HasContent() ?? false);

            // Wire up fade animation events
            fadeAnimation.OpacityChanged += (s, opacity) =>
            {
                currentOpacity = opacity;
                isFadedOut = fadeAnimation.IsFadedOut;
                ApplyFadeOpacity();
            };

            fadeAnimation.MouseEntered += (s, e) =>
            {
                // Mouse entered - expand if minified
                if (fenceInfo.CanMinify && minifyBehavior.IsMinified)
                {
                    minifyBehavior.TryExpand();
                    log.Debug($"FenceContainer: Mouse entered, expanding fence '{fenceInfo.Name}'");
                }
            };

            fadeAnimation.MouseLeft += (s, e) =>
            {
                // Mouse left - try to minify if CanMinify is enabled
                if (fenceInfo.CanMinify && !minifyBehavior.IsMinified)
                {
                    if (minifyBehavior.TryMinify())
                    {
                        log.Debug($"FenceContainer: Mouse left, minified fence '{fenceInfo.Name}'");
                    }
                }
            };

            // Wire up minify behavior events
            minifyBehavior.StateChanged += (s, e) =>
            {
                // Update fade animation based on minify state
                if (e.IsMinified)
                {
                    fadeAnimation.SetMinifiedOpacity();
                }
                else
                {
                    fadeAnimation.ResetOpacity();
                }

                // Save height changes
                fenceInfo.Height = minifyBehavior.GetSaveHeight();
                NotifyChanged();
            };

            // 3. Rounded Corners Behavior
            roundedCornersBehavior = new FenceRoundedCornersBehavior(
                this,
                () => fenceInfo.CornerRadius,
                BorderSize);

            roundedCornersBehavior.RegisterTitlePanel(titlePanel);
            roundedCornersBehavior.RegisterContentControl(elementHost);

            // 4. Drag Behavior
            dragBehavior = new FenceDragBehavior(
                this,
                titlePanel,
                () => this.Parent?.ClientRectangle ?? Rectangle.Empty,
                () => !fenceInfo.Locked);

            dragBehavior.PositionChanged += (s, pos) =>
            {
                throttledMove.Run(() =>
                {
                    fenceInfo.PosX = this.Location.X;
                    fenceInfo.PosY = this.Location.Y;
                    NotifyChanged();
                });
            };

            dragBehavior.DragEnded += (s, pos) =>
            {
                fenceInfo.PosX = pos.X;
                fenceInfo.PosY = pos.Y;
                NotifyChanged();
            };

            dragBehavior.Attach();

            // 5. Resize Behavior
            resizeBehavior = new FenceResizeBehavior(
                this,
                () => this.Parent?.ClientRectangle ?? Rectangle.Empty,
                () => !fenceInfo.Locked,
                150);

            resizeBehavior.RegisterBorders(
                borderLeft,
                borderRight,
                borderTop,
                borderBottom,
                borderBottomRight);

            resizeBehavior.ResizeStarted += (s, e) =>
            {
                this.SuspendLayout();
                log.Debug($"Border resize started: {e.Direction} at {e.StartLocation}");
            };

            resizeBehavior.ResizeChanged += (s, e) =>
            {
                // Force title panel to redraw
                titlePanel?.Invalidate();
            };

            resizeBehavior.ResizeEnded += (s, e) =>
            {
                this.ResumeLayout(true);

                // Force complete repaint
                titlePanel?.Invalidate();
                titlePanel?.Update();
                this.Invalidate(true);
                this.Update();

                // Save new position and size
                fenceInfo.Width = e.FinalSize.Width;
                fenceInfo.Height = e.FinalSize.Height;
                fenceInfo.PosX = e.FinalLocation.X;
                fenceInfo.PosY = e.FinalLocation.Y;
                NotifyChanged();

                // Update minify behavior's previous height if not minified
                minifyBehavior.UpdatePreviousHeight(e.FinalSize.Height);

                log.Debug($"Border resize completed: Location={e.FinalLocation}, Size={e.FinalSize}");

                // If auto-height is enabled, recalculate
                if (fenceInfo.AutoHeight)
                {
                    var adjustTimer = new System.Windows.Forms.Timer();
                    adjustTimer.Interval = 50;
                    adjustTimer.Tick += (s2, e2) =>
                    {
                        adjustTimer.Stop();
                        adjustTimer.Dispose();
                        AdjustHeightToContent();
                        log.Debug($"Auto-height recalculated after resize: Height={this.Height}");
                    };
                    adjustTimer.Start();
                }
            };

            resizeBehavior.Attach();
        }

        #endregion

        #region Public Properties

        public FenceInfo FenceInfo => fenceInfo;

        public bool IsLocked
        {
            get => fenceInfo.Locked;
            set
            {
                fenceInfo.Locked = value;
                if (lockedMenuItem != null)
                    lockedMenuItem.IsChecked = value;
                NotifyChanged();
            }
        }

        public bool CanMinify
        {
            get => fenceInfo.CanMinify;
            set
            {
                fenceInfo.CanMinify = value;
                if (minifyMenuItem != null)
                    minifyMenuItem.IsChecked = value;

                // If CanMinify is disabled and fence is currently minified, expand it
                if (!value && minifyBehavior.IsMinified)
                {
                    minifyBehavior.ForceExpand();
                    fadeAnimation.ResetOpacity();
                    log.Debug($"Fence '{fenceInfo.Name}' expanded because CanMinify was disabled");
                }

                NotifyChanged();
            }
        }

        #endregion

        #region Initialization

        private void CreateTitlePanel()
        {
            titlePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = titleHeight,
                BackColor = Color.Transparent
            };

            titlePanel.Paint += TitlePanel_Paint;
            titlePanel.MouseClick += TitlePanel_MouseClick;

            // Add help button to title panel (? icon)
            helpButton = new Button
            {
                Text = "?",
                Width = 20,
                Height = 20,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = currentTheme.TitleTextColor,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = false
            };

            helpButton.FlatAppearance.BorderSize = 0;
            helpButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, currentTheme.TitleTextColor);
            helpButton.Click += HelpButton_Click;

            titlePanel.Controls.Add(helpButton);
            PositionHelpButton();

            // Reposition help button when title panel resizes
            titlePanel.Resize += (s, e) => PositionHelpButton();

            this.Controls.Add(titlePanel);
        }

        private void PositionHelpButton()
        {
            if (helpButton != null && titlePanel != null)
            {
                // Position help button in top-right corner of title panel
                helpButton.Location = new Point(titlePanel.Width - helpButton.Width - 5, (titlePanel.Height - helpButton.Height) / 2);
            }
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Show help window for this fence
                var helpWindow = new FenceHelpWindow(fenceInfo);
                helpWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                log.Error($"Error showing help window: {ex.Message}", ex);
            }
        }

        private void ApplyTheme()
        {
            // Parse theme from FenceInfo
            FenceTheme theme = FenceTheme.Dark; // Default
            if (!string.IsNullOrEmpty(fenceInfo.Theme) && Enum.TryParse<FenceTheme>(fenceInfo.Theme, out var parsedTheme))
            {
                theme = parsedTheme;
            }

            currentTheme = FenceThemeColors.GetTheme(theme);

            // Apply theme colors - use content background (usually black) instead of BackgroundColor
            // This prevents magenta tint during fade animations
            this.BackColor = currentTheme.ContentBackgroundColor;

            log.Debug($"FenceContainer: Applied theme '{fenceInfo.Theme}' - BackColor={currentTheme.ContentBackgroundColor}");
        }

        private void CreateResizeBorders()
        {
            // Left border (starts subtle, becomes visible on hover)
            borderLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = BorderSize,
                BackColor = GetBorderColor(false), // Start subtle
                Cursor = Cursors.SizeWE
            };

            // Right border (starts subtle, becomes visible on hover)
            borderRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = BorderSize,
                BackColor = GetBorderColor(false), // Start subtle
                Cursor = Cursors.SizeWE
            };

            // Top border (starts subtle, becomes visible on hover)
            borderTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = BorderSize,
                BackColor = GetBorderColor(false), // Start subtle
                Cursor = Cursors.SizeNS
            };

            // Bottom border (starts subtle, becomes visible on hover)
            borderBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = BorderSize,
                BackColor = GetBorderColor(false), // Start subtle
                Cursor = Cursors.SizeNS
            };

            // Bottom-right corner grip (starts subtle, becomes prominent on hover)
            borderBottomRight = new Panel
            {
                Width = 15,
                Height = 15,
                BackColor = GetCornerGripColor(false), // Start subtle
                Cursor = Cursors.SizeNWSE
            };

            // Add borders to container
            this.Controls.Add(borderLeft);
            this.Controls.Add(borderRight);
            this.Controls.Add(borderTop);
            this.Controls.Add(borderBottom);
            this.Controls.Add(borderBottomRight);

            // Position corner grip manually since it can't dock
            this.Resize += (s, e) => PositionCornerGrip();
            PositionCornerGrip();

            // Update border visibility based on AutoHeight setting
            UpdateResizeBordersForAutoHeight();
        }

        private void PositionCornerGrip()
        {
            if (borderBottomRight != null)
            {
                borderBottomRight.Location = new Point(
                    this.Width - borderBottomRight.Width - BorderSize,
                    this.Height - borderBottomRight.Height - BorderSize);
                borderBottomRight.BringToFront();
            }
        }

        /// <summary>
        /// Updates resize border visibility based on AutoHeight setting.
        /// When AutoHeight is enabled, vertical resizing is disabled to prevent conflicts.
        /// </summary>
        private void UpdateResizeBordersForAutoHeight()
        {
            if (fenceInfo.AutoHeight)
            {
                // Disable vertical resizing when AutoHeight is enabled
                if (borderTop != null)
                {
                    borderTop.Visible = false;
                    borderTop.Enabled = false;
                }
                if (borderBottom != null)
                {
                    borderBottom.Visible = false;
                    borderBottom.Enabled = false;
                }
                if (borderBottomRight != null)
                {
                    borderBottomRight.Visible = false;
                    borderBottomRight.Enabled = false;
                }
                // Keep left/right borders enabled for horizontal resizing
            }
            else
            {
                // Enable all borders when AutoHeight is disabled
                if (borderTop != null)
                {
                    borderTop.Visible = true;
                    borderTop.Enabled = true;
                }
                if (borderBottom != null)
                {
                    borderBottom.Visible = true;
                    borderBottom.Enabled = true;
                }
                if (borderBottomRight != null)
                {
                    borderBottomRight.Visible = true;
                    borderBottomRight.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Gets the border color based on hover state
        /// </summary>
        /// <param name="isHovered">Whether the mouse is hovering over the fence</param>
        /// <returns>Border color with appropriate opacity</returns>
        private Color GetBorderColor(bool isHovered)
        {
            if (isHovered)
            {
                // Full opacity when hovered - borders clearly visible
                return currentTheme.BorderColor;
            }
            else
            {
                // Very subtle when not hovered - only 15% opacity
                return Color.FromArgb(38, currentTheme.BorderColor.R, currentTheme.BorderColor.G, currentTheme.BorderColor.B);
            }
        }

        /// <summary>
        /// Gets the corner grip color based on hover state
        /// </summary>
        /// <param name="isHovered">Whether the mouse is hovering over the fence</param>
        /// <returns>Corner grip color with appropriate opacity</returns>
        private Color GetCornerGripColor(bool isHovered)
        {
            if (isHovered)
            {
                // More prominent when hovered - 80% opacity
                return Color.FromArgb(200, currentTheme.BorderColor.R, currentTheme.BorderColor.G, currentTheme.BorderColor.B);
            }
            else
            {
                // Nearly invisible when not hovered - 10% opacity
                return Color.FromArgb(25, currentTheme.BorderColor.R, currentTheme.BorderColor.G, currentTheme.BorderColor.B);
            }
        }

        /// <summary>
        /// Updates border appearance based on hover state
        /// </summary>
        private void UpdateBorderHoverState(bool isHovered)
        {
            isBorderHovered = isHovered;

            // Update all border colors
            if (borderLeft != null) borderLeft.BackColor = GetBorderColor(isHovered);
            if (borderRight != null) borderRight.BackColor = GetBorderColor(isHovered);
            if (borderTop != null) borderTop.BackColor = GetBorderColor(isHovered);
            if (borderBottom != null) borderBottom.BackColor = GetBorderColor(isHovered);
            if (borderBottomRight != null) borderBottomRight.BackColor = GetCornerGripColor(isHovered);

            log.Debug($"FenceContainer: Border hover state changed to {isHovered} for fence '{fenceInfo.Name}'");
        }


        private void CreateWpfContentArea(FenceHandlerFactoryWpf handlerFactory)
        {
            log.Debug($"Creating handler for fence type: {fenceInfo.Type}");

            // Create handler
            fenceHandler = handlerFactory.CreateFenceHandler(fenceInfo);
            log.Debug($"Handler created: {fenceHandler?.GetType().Name ?? "null"}");

            // Subscribe to content changed event for auto-height adjustment
            if (fenceHandler != null)
            {
                fenceHandler.ContentChanged += FenceHandler_ContentChanged;
            }

            // Create ElementHost to host WPF content
            // Use Padding to push content below title instead of Dock.Fill to avoid overlap
            elementHost = new ElementHost
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent, // Transparent background for WPF content
                Padding = new Padding(0, 0, 0, 0) // No padding - borders handle spacing
            };
            log.Debug($"ElementHost created with transparent background");

            // Get WPF content from handler with theme
            var wpfContent = fenceHandler.CreateContentElement(titleHeight, currentTheme);
            log.Debug($"WPF content element created: {wpfContent?.GetType().Name ?? "null"}");

            // Create container grid to hold content + drop zone overlay
            var containerGrid = new WpfControls.Grid();

            // Add content to grid
            if (wpfContent != null)
            {
                containerGrid.Children.Add(wpfContent);
            }

            // Create drop zone overlay (initially hidden)
            dropZoneOverlay = new DropZoneOverlay();
            dropZoneOverlay.DropCompleted += DropZoneOverlay_DropCompleted;
            containerGrid.Children.Add(dropZoneOverlay);
            log.Debug("Drop zone overlay created and added to container");

            // Add WPF event handlers (right-click and drag & drop)
            if (wpfContent != null)
            {
                AttachWpfEventHandlers(wpfContent);
            }

            elementHost.Child = containerGrid;

            // Add ElementHost - it will dock Fill and respect the title panel's Top dock
            this.Controls.Add(elementHost);

            // Bring title panel to front to ensure it's above content
            // BringToFront affects Z-order (visual stacking), not layout order
            titlePanel.BringToFront();

            log.Debug($"ElementHost added to controls, total controls: {this.Controls.Count}, titleHeight: {titleHeight}");

            // Auto-adjust height if enabled (with delay to allow content to render)
            if (fenceInfo.AutoHeight)
            {
                // Immediate adjustment
                AdjustHeightToContent();

                // Delayed adjustment to catch fully rendered content
                var adjustTimer = new System.Windows.Forms.Timer();
                adjustTimer.Interval = 100; // 100ms delay
                adjustTimer.Tick += (s, e) =>
                {
                    adjustTimer.Stop();
                    adjustTimer.Dispose();
                    AdjustHeightToContent();
                };
                adjustTimer.Start();
            }
        }

        /// <summary>
        /// Adjusts the fence height to fit all content (for auto-height mode)
        /// </summary>
        private void AdjustHeightToContent()
        {
            if (elementHost?.Child == null || this.Parent == null)
                return;

            try
            {
                // Calculate available height from current position to bottom
                var parentBounds = this.Parent.ClientRectangle;
                int currentY = this.Location.Y;
                int availableHeight = parentBounds.Height - currentY;

                // Force layout update first
                elementHost.Child.UpdateLayout();

                // Measure the WPF content with current width
                elementHost.Child.Measure(new System.Windows.Size(
                    elementHost.Width,
                    double.PositiveInfinity));

                var desiredContentHeight = elementHost.Child.DesiredSize.Height;

                // Add title height and some padding
                int contentDesiredHeight = (int)(desiredContentHeight + titleHeight + 20); // 20px padding

                log.Debug($"Auto-height: Content desired height: {contentDesiredHeight}px, available height: {availableHeight}px, current: {this.Height}px");

                // Use the smaller of content height or available height (content-aware)
                int newHeight = Math.Min(contentDesiredHeight, availableHeight);

                // Set minimum height (at least title bar + some content)
                const int MinHeight = 100;
                newHeight = Math.Max(MinHeight, newHeight);

                // Always update height, even if it's shrinking
                if (newHeight != this.Height)
                {
                    log.Debug($"Auto-adjusting fence height from {this.Height} to {newHeight}px");

                    this.Height = newHeight;
                    fenceInfo.Height = newHeight;
                    minifyBehavior.UpdatePreviousHeight(newHeight); // Update behavior for minify

                    // Force layout refresh
                    this.PerformLayout();
                    elementHost.Refresh();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error adjusting height to content: {ex.Message}", ex);
            }
        }

        #endregion

        #region Context Menu

        private void CreateContextMenu()
        {
            // Apply MahApps.Metro theme sync with Windows dark/light mode
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();

            contextMenu = new WpfControls.ContextMenu();

            // Apply MahApps.Metro styling (if WPF Application exists)
            if (System.Windows.Application.Current != null)
            {
                contextMenu.Style = System.Windows.Application.Current.TryFindResource("MahApps.Styles.ContextMenu") as System.Windows.Style;
            }

            // Locked (checkbox with lock icon)
            lockedMenuItem = new WpfControls.MenuItem
            {
                Header = "Locked",
                IsCheckable = true,
                IsChecked = fenceInfo.Locked,
                Icon = new PackIconMaterial
                {
                    Kind = PackIconMaterialKind.Lock,
                    Width = 16,
                    Height = 16
                }
            };
            lockedMenuItem.Click += (s, e) =>
            {
                fenceInfo.Locked = lockedMenuItem.IsChecked;
                NotifyChanged();
            };

            // Can Minify (checkbox with minimize icon)
            minifyMenuItem = new WpfControls.MenuItem
            {
                Header = "Can Minify",
                IsCheckable = true,
                IsChecked = fenceInfo.CanMinify,
                Icon = new PackIconMaterial
                {
                    Kind = PackIconMaterialKind.WindowMinimize,
                    Width = 16,
                    Height = 16
                }
            };
            minifyMenuItem.Click += (s, e) =>
            {
                // Use the property setter to trigger expansion logic
                this.CanMinify = minifyMenuItem.IsChecked;
            };

            // Theme submenu (with palette icon)
            themeMenuItem = new WpfControls.MenuItem
            {
                Header = "Theme",
                Icon = new PackIconMaterial
                {
                    Kind = PackIconMaterialKind.Palette,
                    Width = 16,
                    Height = 16,
                    Foreground = new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(156, 39, 176)) // Purple
                }
            };
            CreateThemeSubmenu();

            // Add Files... (only for Files fences)
            if (fenceInfo.Type == EntryType.Files.ToString())
            {
                addFilesMenuItem = new WpfControls.MenuItem
                {
                    Header = "Add Files...",
                    Icon = new PackIconMaterial
                    {
                        Kind = PackIconMaterialKind.FileDocumentPlus,
                        Width = 16,
                        Height = 16,
                        Foreground = new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(76, 175, 80)) // Green
                    }
                };
                addFilesMenuItem.Click += (s, e) => AddFilesMenuItem_Click(s, EventArgs.Empty);
            }

            // Edit Fence (with edit icon)
            editMenuItem = new WpfControls.MenuItem
            {
                Header = "Edit Fence...",
                Icon = new PackIconMaterial
                {
                    Kind = PackIconMaterialKind.Pencil,
                    Width = 16,
                    Height = 16,
                    Foreground = new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(33, 150, 243)) // Blue
                }
            };
            editMenuItem.Click += (s, e) => EditMenuItem_Click(s, EventArgs.Empty);

            // Delete Fence (with delete icon)
            deleteMenuItem = new WpfControls.MenuItem
            {
                Header = "Delete Fence",
                Icon = new PackIconMaterial
                {
                    Kind = PackIconMaterialKind.Delete,
                    Width = 16,
                    Height = 16,
                    Foreground = new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(220, 80, 80)) // Red
                }
            };
            deleteMenuItem.Click += (s, e) =>
            {
                if (MessageBox.Show(
                    "Really remove this fence? Your files will not be deleted.",
                    "Remove Fence",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    FenceDeleted?.Invoke(this, fenceInfo);
                }
            };

            contextMenu.Items.Add(lockedMenuItem);
            contextMenu.Items.Add(minifyMenuItem);
            contextMenu.Items.Add(themeMenuItem);
            contextMenu.Items.Add(new WpfControls.Separator());

            // Add the "Add Files..." item if this is a Files fence
            if (addFilesMenuItem != null)
            {
                contextMenu.Items.Add(addFilesMenuItem);
            }

            contextMenu.Items.Add(editMenuItem);
            contextMenu.Items.Add(deleteMenuItem);
        }

        private void CreateThemeSubmenu()
        {
            themeMenuItem.Items.Clear();

            // Add menu item for each theme
            foreach (FenceTheme theme in Enum.GetValues(typeof(FenceTheme)))
            {
                var themeItem = new WpfControls.MenuItem
                {
                    Header = FenceThemeColors.GetThemeDisplayName(theme),
                    IsCheckable = true,
                    IsChecked = fenceInfo.Theme == theme.ToString()
                };

                themeItem.Click += (s, e) =>
                {
                    // Update theme
                    fenceInfo.Theme = theme.ToString();

                    // Reapply theme
                    ApplyTheme();

                    // Update border colors
                    UpdateBorderColors();

                    // Recreate WPF content with new theme
                    if (elementHost != null && fenceHandler != null)
                    {
                        var newContent = fenceHandler.CreateContentElement(titleHeight, currentTheme);
                        elementHost.Child = newContent;

                        // Re-add right-click handler
                        if (newContent != null)
                        {
                            newContent.MouseRightButtonDown += (s2, e2) =>
                            {
                                if (contextMenu != null)
                                {
                                    contextMenu.PlacementTarget = newContent;
                                    contextMenu.Placement = WpfControls.Primitives.PlacementMode.MousePoint;
                                    contextMenu.IsOpen = true;
                                }
                                e2.Handled = true;
                            };
                        }
                    }

                    // Repaint everything
                    titlePanel?.Invalidate();
                    this.Invalidate(true);
                    this.Refresh();

                    // Update menu checkmarks
                    foreach (WpfControls.MenuItem item in themeMenuItem.Items)
                    {
                        item.IsChecked = false;
                    }
                    ((WpfControls.MenuItem)s).IsChecked = true;

                    // Save changes
                    NotifyChanged();

                    log.Debug($"Theme changed to: {theme}");
                };

                themeMenuItem.Items.Add(themeItem);
            }
        }

        private void EditMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Store the original CanMinify state before opening dialog
                bool wasCanMinifyEnabled = fenceInfo.CanMinify;

                // Create and show the WPF edit window
                var editWindow = new FenceEditWindow(fenceInfo);

                // Show as modal dialog
                bool? result = editWindow.ShowDialog();

                if (result == true)
                {
                    // Properties were saved to fenceInfo by the dialog
                    log.Debug($"Fence properties edited: {fenceInfo.Name}");

                    // Update title height if changed
                    if (logicalTitleHeight != fenceInfo.TitleHeight)
                    {
                        logicalTitleHeight = fenceInfo.TitleHeight;
                        ReloadFonts();

                        // Update title panel height to match new titleHeight
                        if (titlePanel != null)
                        {
                            titlePanel.Height = titleHeight;
                            titlePanel.Invalidate();
                        }

                        log.Debug($"Title height changed from {logicalTitleHeight} to {fenceInfo.TitleHeight}, panel height now {titleHeight}px");
                    }

                    // Check if CanMinify was disabled while fence is minified - if so, expand it
                    if (wasCanMinifyEnabled && !fenceInfo.CanMinify && minifyBehavior.IsMinified)
                    {
                        minifyBehavior.ForceExpand();
                        fadeAnimation.ResetOpacity();
                        log.Debug($"Fence '{fenceInfo.Name}' expanded because CanMinify was disabled in edit dialog");
                    }

                    // If fade effect was disabled, ensure fence is at full opacity
                    if (!fenceInfo.EnableFadeEffect)
                    {
                        fadeAnimation.ResetOpacity();
                        log.Debug($"Fence '{fenceInfo.Name}' reset to full opacity because fade effect was disabled");
                    }

                    // Update context menu checkbox to reflect new state
                    if (minifyMenuItem != null)
                    {
                        minifyMenuItem.IsChecked = fenceInfo.CanMinify;
                    }

                    // Reapply theme if changed
                    ApplyTheme();
                    UpdateBorderColors();

                    // Recreate WPF content if type or other properties changed
                    RecreateWpfContent();

                    // Update resize borders based on AutoHeight setting
                    UpdateResizeBordersForAutoHeight();

                    // Apply rounded corners if corner radius changed
                    roundedCornersBehavior.Apply();

                    // Trigger events
                    FenceEdited?.Invoke(this, fenceInfo);
                    NotifyChanged();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error opening edit window: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to open edit window: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void AddFilesMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Show dialog with options to add files or folders
                var result = MessageBox.Show(
                    "What would you like to add?\n\nYes = Files\nNo = Folder",
                    "Add to Fence",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                    return;

                List<string> selectedPaths = new List<string>();

                if (result == DialogResult.Yes)
                {
                    // Add files
                    using (var openFileDialog = new System.Windows.Forms.OpenFileDialog())
                    {
                        openFileDialog.Title = "Select Files to Add";
                        openFileDialog.Multiselect = true;
                        openFileDialog.Filter = "All Files (*.*)|*.*";
                        openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            selectedPaths.AddRange(openFileDialog.FileNames);
                        }
                    }
                }
                else if (result == DialogResult.No)
                {
                    // Add folder
                    using (var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        folderBrowserDialog.Description = "Select Folder to Add";
                        folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;
                        folderBrowserDialog.ShowNewFolderButton = false;

                        if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                        {
                            selectedPaths.Add(folderBrowserDialog.SelectedPath);
                        }
                    }
                }

                // Add selected items to fence
                if (selectedPaths.Count > 0)
                {
                    if (fenceInfo.Items == null)
                        fenceInfo.Items = new List<string>();

                    int addedCount = 0;
                    foreach (var path in selectedPaths)
                    {
                        if (!fenceInfo.Items.Contains(path))
                        {
                            fenceInfo.Items.Add(path);
                            addedCount++;
                        }
                    }

                    // Clear path if it was set (convert from folder monitoring to item list)
                    if (!string.IsNullOrEmpty(fenceInfo.Path))
                    {
                        log.Debug($"Files fence converted from folder monitoring to item list");
                        fenceInfo.Path = null;
                    }

                    // Clear filter if it was set (convert from smart filter to item list)
                    if (fenceInfo.Filter != null)
                    {
                        log.Debug($"Files fence converted from smart filter to item list");
                        fenceInfo.Filter = null;
                    }

                    // Refresh content
                    RefreshContent();
                    NotifyChanged();

                    log.Info($"Added {addedCount} item(s) to fence '{fenceInfo.Name}' (total: {fenceInfo.Items.Count})");

                    MessageBox.Show(
                        $"Added {addedCount} item(s) to fence.\nTotal items: {fenceInfo.Items.Count}",
                        "Files Added",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error adding files to fence: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to add files: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void RecreateWpfContent()
        {
            if (elementHost != null && fenceHandler != null)
            {
                try
                {
                    // Unsubscribe from old handler events
                    if (fenceHandler != null)
                    {
                        fenceHandler.ContentChanged -= FenceHandler_ContentChanged;
                    }

                    // Cleanup old handler
                    fenceHandler.Cleanup();

                    // Create new handler for potentially changed type
                    var factory = new FenceHandlerFactoryWpf(GetHandlerDictionary());
                    fenceHandler = factory.CreateFenceHandler(fenceInfo);

                    // Subscribe to new handler events
                    if (fenceHandler != null)
                    {
                        fenceHandler.ContentChanged += FenceHandler_ContentChanged;
                    }

                    // Create new content
                    var newContent = fenceHandler.CreateContentElement(titleHeight, currentTheme);

                    // Recreate container grid with new content + overlay
                    var containerGrid = new WpfControls.Grid();
                    if (newContent != null)
                    {
                        containerGrid.Children.Add(newContent);
                    }

                    // Recreate drop zone overlay (initially hidden)
                    dropZoneOverlay = new DropZoneOverlay();
                    dropZoneOverlay.DropCompleted += DropZoneOverlay_DropCompleted;
                    containerGrid.Children.Add(dropZoneOverlay);

                    elementHost.Child = containerGrid;
                    log.Debug("RecreateWpfContent: Container grid and overlay recreated");

                    // Re-attach WPF event handlers (right-click)
                    if (newContent != null)
                    {
                        AttachWpfEventHandlers(newContent);
                    }

                    // Repaint
                    titlePanel?.Invalidate();
                    this.Invalidate(true);
                    this.Refresh();

                    // Apply rounded corners
                    roundedCornersBehavior.Apply();

                    // Auto-adjust height if enabled (with delay)
                    if (fenceInfo.AutoHeight)
                    {
                        // Immediate adjustment
                        AdjustHeightToContent();

                        // Delayed adjustment to catch fully rendered content
                        var adjustTimer = new System.Windows.Forms.Timer();
                        adjustTimer.Interval = 100;
                        adjustTimer.Tick += (s2, e2) =>
                        {
                            adjustTimer.Stop();
                            adjustTimer.Dispose();
                            AdjustHeightToContent();
                        };
                        adjustTimer.Start();
                    }

                    log.Debug($"WPF content recreated for fence: {fenceInfo.Name}");
                }
                catch (Exception ex)
                {
                    log.Error($"Error recreating WPF content: {ex.Message}", ex);
                }
            }
        }

        private Dictionary<string, Type> GetHandlerDictionary()
        {
            // Return the handler type mappings
            var handlers = new Dictionary<string, Type>();
            handlers[EntryType.Pictures.ToString()] = typeof(PictureFenceHandlerWpf);
            handlers[EntryType.Files.ToString()] = typeof(FilesFenceHandlerWpf);
            handlers[EntryType.Video.ToString()] = typeof(VideoFenceHandlerWpf);
            // Folder type removed - merged with Files
            handlers[EntryType.Clock.ToString()] = typeof(ClockFenceHandlerWpf);
            handlers[EntryType.Widget.ToString()] = typeof(WidgetFenceHandlerWpf);
            return handlers;
        }

        private void UpdateBorderColors()
        {
            // Update border colors based on current hover state
            if (borderLeft != null) borderLeft.BackColor = GetBorderColor(isBorderHovered);
            if (borderRight != null) borderRight.BackColor = GetBorderColor(isBorderHovered);
            if (borderTop != null) borderTop.BackColor = GetBorderColor(isBorderHovered);
            if (borderBottom != null) borderBottom.BackColor = GetBorderColor(isBorderHovered);
            if (borderBottomRight != null) borderBottomRight.BackColor = GetCornerGripColor(isBorderHovered);
        }

        #endregion

        #region Event Handlers - Title Panel

        private void TitlePanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            byte alpha = (byte)(currentOpacity * 255);

            // Background with theme color and current opacity
            using (var bgBrush = new SolidBrush(Color.FromArgb(alpha, currentTheme.TitleBackgroundColor)))
            {
                e.Graphics.FillRectangle(bgBrush, titlePanel.ClientRectangle);
            }

            // Title text with theme color and current opacity
            using (var textBrush = new SolidBrush(Color.FromArgb(alpha, currentTheme.TitleTextColor)))
            {
                e.Graphics.DrawString(
                    fenceInfo.Name,
                    titleFont,
                    textBrush,
                    new PointF(titlePanel.Width / 2, titleOffset),
                    new StringFormat { Alignment = StringAlignment.Center });
            }
        }

        private void TitlePanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && contextMenu != null)
            {
                // Convert WinForms coordinates to screen coordinates
                var screenPoint = titlePanel.PointToScreen(e.Location);

                // Show WPF context menu at the converted position
                contextMenu.PlacementRectangle = new System.Windows.Rect(
                    screenPoint.X,
                    screenPoint.Y,
                    0,
                    0);
                contextMenu.Placement = WpfControls.Primitives.PlacementMode.AbsolutePoint;
                contextMenu.IsOpen = true;
            }
        }

        #endregion

        #region Event Handlers - Container

        /// <summary>
        /// Recursively fades all background brushes in a WPF visual tree
        /// </summary>
        private void FadeWpfElementBackgrounds(System.Windows.UIElement element, byte alpha)
        {
            if (element == null) return;

            // Use alpha value as-is without any minimum restriction
            // Fences can fade all the way to alpha=0 (fully transparent)
            // User will handle image black pixel issues separately by editing/filtering images

            // Fade backgrounds for different control types
            // We set background to content color with alpha, even if it was null/transparent
            // This prevents desktop color from bleeding through

            if (element is WpfControls.Panel panel)
            {
                // Always set background, even if null/transparent
                panel.Background = new WpfMedia.SolidColorBrush(
                    WpfMedia.Color.FromArgb(alpha,
                        currentTheme.ContentBackgroundColor.R,
                        currentTheme.ContentBackgroundColor.G,
                        currentTheme.ContentBackgroundColor.B));
            }
            else if (element is WpfControls.Border border)
            {
                // Always set background, even if null/transparent
                border.Background = new WpfMedia.SolidColorBrush(
                    WpfMedia.Color.FromArgb(alpha,
                        currentTheme.ContentBackgroundColor.R,
                        currentTheme.ContentBackgroundColor.G,
                        currentTheme.ContentBackgroundColor.B));
            }
            else if (element is WpfControls.Control control)
            {
                // Handle ScrollViewer and other controls with Background property
                // Always set background, even if null/transparent
                control.Background = new WpfMedia.SolidColorBrush(
                    WpfMedia.Color.FromArgb(alpha,
                        currentTheme.ContentBackgroundColor.R,
                        currentTheme.ContentBackgroundColor.G,
                        currentTheme.ContentBackgroundColor.B));
            }

            // Recursively process children
            if (element is WpfControls.Panel panelWithChildren)
            {
                foreach (System.Windows.UIElement child in panelWithChildren.Children)
                {
                    FadeWpfElementBackgrounds(child, alpha);
                }
            }
            else if (element is WpfControls.ContentControl contentControl && contentControl.Content is System.Windows.UIElement childElement)
            {
                FadeWpfElementBackgrounds(childElement, alpha);
            }
            else if (element is WpfControls.Decorator decorator && decorator.Child != null)
            {
                FadeWpfElementBackgrounds(decorator.Child, alpha);
            }
        }

        private void ApplyFadeOpacity()
        {
            try
            {
                int alpha = (int)(currentOpacity * 255);

                if (isFadedOut)
                {
                    // Fade out: Hide title, borders, background
                    // Title panel background and text (needs Invalidate to trigger repaint)
                    if (titlePanel != null)
                    {
                        titlePanel.BackColor = Color.FromArgb(alpha, currentTheme.TitleBackgroundColor);
                        titlePanel.Invalidate(); // Force repaint for custom Paint event
                    }

                    // Help button - hide when faded out (alpha below threshold)
                    if (helpButton != null)
                    {
                        // Hide button completely when opacity is very low
                        // This ensures the "?" text disappears with the fence
                        if (alpha < 10)
                        {
                            helpButton.Visible = false;
                        }
                        else
                        {
                            helpButton.Visible = true;
                            helpButton.ForeColor = Color.FromArgb(alpha, currentTheme.TitleTextColor);
                            helpButton.BackColor = Color.FromArgb(alpha, Color.Transparent);
                            int mouseOverAlpha = Math.Min(50, alpha);
                            helpButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(mouseOverAlpha, currentTheme.TitleTextColor);
                        }
                    }

                    // Borders - apply fade to base border colors (respecting hover state)
                    Color baseBorderColor = GetBorderColor(isBorderHovered);
                    Color baseCornerColor = GetCornerGripColor(isBorderHovered);

                    if (borderLeft != null) borderLeft.BackColor = Color.FromArgb((int)(alpha * (baseBorderColor.A / 255.0)), baseBorderColor.R, baseBorderColor.G, baseBorderColor.B);
                    if (borderRight != null) borderRight.BackColor = Color.FromArgb((int)(alpha * (baseBorderColor.A / 255.0)), baseBorderColor.R, baseBorderColor.G, baseBorderColor.B);
                    if (borderTop != null) borderTop.BackColor = Color.FromArgb((int)(alpha * (baseBorderColor.A / 255.0)), baseBorderColor.R, baseBorderColor.G, baseBorderColor.B);
                    if (borderBottom != null) borderBottom.BackColor = Color.FromArgb((int)(alpha * (baseBorderColor.A / 255.0)), baseBorderColor.R, baseBorderColor.G, baseBorderColor.B);
                    if (borderBottomRight != null)
                    {
                        borderBottomRight.BackColor = Color.FromArgb(
                            (int)(currentOpacity * baseCornerColor.A),
                            baseCornerColor.R,
                            baseCornerColor.G,
                            baseCornerColor.B);
                    }

                    // Container background (WinForms) - use content background to avoid magenta tint
                    this.BackColor = Color.FromArgb(alpha, currentTheme.ContentBackgroundColor);

                    // ElementHost background
                    if (elementHost != null)
                    {
                        elementHost.BackColor = Color.FromArgb(alpha, currentTheme.ContentBackgroundColor);
                    }

                    // WPF content - recursively fade all backgrounds
                    if (elementHost?.Child != null)
                    {
                        FadeWpfElementBackgrounds(elementHost.Child, (byte)alpha);

                        // Content opacity: Keep at 80% when fully faded
                        if (currentOpacity <= 0.01)
                        {
                            elementHost.Child.Opacity = FadedContentOpacity;
                        }
                        else
                        {
                            // Gradually fade content as well
                            elementHost.Child.Opacity = Math.Max(FadedContentOpacity, currentOpacity);
                        }
                    }
                }
                else
                {
                    // Fade in: Restore everything
                    // Title panel background and text
                    if (titlePanel != null)
                    {
                        titlePanel.BackColor = Color.FromArgb(alpha, currentTheme.TitleBackgroundColor);
                        titlePanel.Invalidate(); // Force repaint for custom Paint event
                    }

                    // Help button - show and restore opacity during fade in
                    if (helpButton != null)
                    {
                        // Show button as opacity increases
                        if (alpha < 10)
                        {
                            helpButton.Visible = false;
                        }
                        else
                        {
                            helpButton.Visible = true;
                            helpButton.ForeColor = Color.FromArgb(alpha, currentTheme.TitleTextColor);
                            helpButton.BackColor = Color.FromArgb(alpha, Color.Transparent);
                            int mouseOverAlpha = Math.Min(50, alpha);
                            helpButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(mouseOverAlpha, currentTheme.TitleTextColor);
                        }
                    }

                    // Borders - apply fade to base border colors (respecting hover state)
                    Color baseBorderColor = GetBorderColor(isBorderHovered);
                    Color baseCornerColor = GetCornerGripColor(isBorderHovered);

                    if (borderLeft != null) borderLeft.BackColor = Color.FromArgb((int)(alpha * (baseBorderColor.A / 255.0)), baseBorderColor.R, baseBorderColor.G, baseBorderColor.B);
                    if (borderRight != null) borderRight.BackColor = Color.FromArgb((int)(alpha * (baseBorderColor.A / 255.0)), baseBorderColor.R, baseBorderColor.G, baseBorderColor.B);
                    if (borderTop != null) borderTop.BackColor = Color.FromArgb((int)(alpha * (baseBorderColor.A / 255.0)), baseBorderColor.R, baseBorderColor.G, baseBorderColor.B);
                    if (borderBottom != null) borderBottom.BackColor = Color.FromArgb((int)(alpha * (baseBorderColor.A / 255.0)), baseBorderColor.R, baseBorderColor.G, baseBorderColor.B);
                    if (borderBottomRight != null)
                    {
                        borderBottomRight.BackColor = Color.FromArgb(
                            (int)(currentOpacity * baseCornerColor.A),
                            baseCornerColor.R,
                            baseCornerColor.G,
                            baseCornerColor.B);
                    }

                    // Container background (WinForms) - use content background to avoid magenta tint
                    this.BackColor = Color.FromArgb(alpha, currentTheme.ContentBackgroundColor);

                    // ElementHost background
                    if (elementHost != null)
                    {
                        elementHost.BackColor = Color.FromArgb(alpha, currentTheme.ContentBackgroundColor);
                    }

                    // WPF content - recursively fade all backgrounds
                    if (elementHost?.Child != null)
                    {
                        FadeWpfElementBackgrounds(elementHost.Child, (byte)alpha);

                        // Content: Restore to full opacity
                        elementHost.Child.Opacity = Math.Max(FadedContentOpacity, currentOpacity);
                    }
                }

                // Force update of all controls
                this.Refresh();
            }
            catch (Exception ex)
            {
                log.Error($"Error applying fade opacity: {ex.Message}", ex);
            }
        }

        private void FenceContainer_Resize(object sender, EventArgs e)
        {
            // Recalculate rounded corners for new size
            roundedCornersBehavior.Apply();

            throttledResize.Run(() =>
            {
                fenceInfo.Width = this.Width;
                fenceInfo.Height = minifyBehavior.GetSaveHeight();
                NotifyChanged();
            });
        }

        /// <summary>
        /// Mouse entered the fence - show resize borders
        /// </summary>
        private void FenceContainer_MouseEnter(object sender, EventArgs e)
        {
            if (!isBorderHovered)
            {
                UpdateBorderHoverState(true);
            }
        }

        /// <summary>
        /// Mouse left the fence - hide resize borders
        /// </summary>
        private void FenceContainer_MouseLeave(object sender, EventArgs e)
        {
            if (isBorderHovered)
            {
                UpdateBorderHoverState(false);
            }
        }

        /// <summary>
        /// Handler for fence content changes (e.g., images rotated, files added/removed).
        /// Triggers auto-height adjustment if enabled.
        /// </summary>
        private void FenceHandler_ContentChanged(object sender, EventArgs e)
        {
            // Only adjust height if auto-height is enabled
            if (fenceInfo.AutoHeight)
            {
                log.Debug($"FenceContainer: Content changed for fence '{fenceInfo.Name}', adjusting height");
                AdjustHeightToContent();
            }
        }

        // OLD resize code - now handled by border panels
        // Kept for reference, can be removed later

        #endregion

        #region Helper Methods

        private void ReloadFonts()
        {
            var family = new FontFamily("Segoe UI");
            titleFont = new Font(family, (int)Math.Floor(logicalTitleHeight / 2.0));
            titleHeight = (int)(logicalTitleHeight * (this.DeviceDpi / 96.0f)); // DPI scaling
        }

        private void NotifyChanged()
        {
            FenceChanged?.Invoke(this, fenceInfo);
        }

        #endregion

        #region Public Methods

        public void UpdateFenceInfo(FenceInfo updatedInfo, FenceHandlerFactoryWpf handlerFactory)
        {
            // Check if CanMinify is being disabled while fence is minified
            bool wasMinifyEnabled = fenceInfo.CanMinify;
            bool willMinifyBeEnabled = updatedInfo.CanMinify;

            // Update properties
            fenceInfo.Name = updatedInfo.Name;
            fenceInfo.TitleHeight = updatedInfo.TitleHeight;
            fenceInfo.Type = updatedInfo.Type;
            fenceInfo.Path = updatedInfo.Path;
            fenceInfo.Filters = updatedInfo.Filters;
            fenceInfo.Interval = updatedInfo.Interval;
            fenceInfo.CanMinify = updatedInfo.CanMinify;

            logicalTitleHeight = updatedInfo.TitleHeight;
            ReloadFonts();

            // Update title panel height
            titlePanel.Height = titleHeight;

            // If CanMinify was disabled and fence is currently minified, expand it
            if (wasMinifyEnabled && !willMinifyBeEnabled && minifyBehavior.IsMinified)
            {
                minifyBehavior.ForceExpand();
                fadeAnimation.ResetOpacity();
                log.Debug($"Fence '{fenceInfo.Name}' expanded because CanMinify was disabled in edit dialog");
            }

            // Recreate WPF content if type changed
            // (In production, you might want to check if type actually changed first)
            fenceHandler?.Cleanup();

            this.Controls.Remove(elementHost);
            elementHost?.Dispose();

            CreateWpfContentArea(handlerFactory);

            minifyBehavior.TryMinify();
            roundedCornersBehavior.Apply();
            NotifyChanged();
            titlePanel.Invalidate();
        }

        public void RefreshContent()
        {
            fenceHandler?.Refresh();

            // Force WPF layout update to ensure visual refresh
            if (elementHost?.Child != null)
            {
                elementHost.Child.UpdateLayout();
                elementHost.Child.InvalidateVisual();
            }
        }

        #endregion

        #region Drag and Drop

        /// <summary>
        /// Attaches WPF event handlers (right-click and drag & drop) to WPF content element
        /// </summary>
        private void AttachWpfEventHandlers(System.Windows.UIElement wpfContent)
        {
            // Right-click handler to show WinForms context menu
            wpfContent.MouseRightButtonDown += (s, e) =>
            {
                // Show WPF context menu at mouse position
                if (contextMenu != null)
                {
                    contextMenu.PlacementTarget = wpfContent;
                    contextMenu.Placement = WpfControls.Primitives.PlacementMode.MousePoint;
                    contextMenu.IsOpen = true;
                }

                e.Handled = true;
            };

            // Note: Drag & drop handled at FenceContainer (WinForms) level, not on WPF content
            // This prevents conflicts between WPF and WinForms drag events

            log.Debug($"WPF event handlers (right-click) attached to content");
        }

        /// <summary>
        /// Generates drop zones based on dropped content and current fence state
        /// </summary>
        private List<DropZoneDefinition> GenerateDropZones(DroppedContentAnalysis analysis)
        {
            var zones = new List<DropZoneDefinition>();
            var currentType = fenceInfo.Type;
            var dominantType = analysis.GetDominantType();

            // Scenario 1: Single folder drop
            if (analysis.Folders.Count == 1 && analysis.TotalCount == 1)
            {
                if (currentType == EntryType.Files.ToString())
                {
                    // Files fence: Offer Monitor folder (Path) OR Static list (Items)
                    zones.Add(new DropZoneDefinition(
                        "monitor_folder",
                        "📁",
                        "Monitor Folder",
                        "Shows all files, updates automatically when folder changes",
                        WpfMedia.Color.FromRgb(76, 175, 80))); // Green

                    zones.Add(new DropZoneDefinition(
                        "static_list",
                        "📋",
                        "Add as Static List",
                        "Shows current files only, no automatic updates",
                        WpfMedia.Color.FromRgb(33, 150, 243))); // Blue

                    return zones;
                }
                else if (currentType == EntryType.Pictures.ToString())
                {
                    // Pictures fence: Offer Convert to Files OR Add folder contents as static images
                    zones.Add(new DropZoneDefinition(
                        "convert_to_files",
                        "📄",
                        "Convert to Files Fence",
                        "Display folder and its contents as file list",
                        WpfMedia.Color.FromRgb(255, 152, 0))); // Orange

                    zones.Add(new DropZoneDefinition(
                        "static_list",
                        "📋",
                        "Add Images from Folder",
                        "Add image files from folder to gallery",
                        WpfMedia.Color.FromRgb(33, 150, 243))); // Blue

                    return zones;
                }
            }

            // Scenario 2: Multiple folders drop
            if (analysis.Folders.Count > 1 && analysis.Folders.Count == analysis.TotalCount)
            {
                // Only offer static list (monitoring multiple folders not supported)
                zones.Add(new DropZoneDefinition(
                    "static_list_multiple",
                    "📋",
                    $"Add {analysis.Folders.Count} Folders as Static Lists",
                    "Current contents from each folder will be added",
                    WpfMedia.Color.FromRgb(33, 150, 243))); // Blue

                return zones;
            }

            // Scenario 3: Type conversion suggestions
            string suggestedType = GetSuggestedFenceType(currentType, analysis, dominantType);

            if (!string.IsNullOrEmpty(suggestedType) && suggestedType != currentType)
            {
                // Offer type conversion
                if (suggestedType == EntryType.Pictures.ToString())
                {
                    zones.Add(new DropZoneDefinition(
                        "convert_to_pictures",
                        "🖼️",
                        "Convert to Pictures Fence",
                        $"Display {analysis.ImageFiles.Count} image(s) as gallery",
                        WpfMedia.Color.FromRgb(156, 39, 176))); // Purple
                }
                else if (suggestedType == EntryType.Video.ToString())
                {
                    zones.Add(new DropZoneDefinition(
                        "convert_to_video",
                        "🎬",
                        "Convert to Video Fence",
                        $"Play {analysis.VideoFiles.Count} video(s) with playlist",
                        WpfMedia.Color.FromRgb(244, 67, 54))); // Red
                }
                else if (suggestedType == EntryType.Files.ToString())
                {
                    zones.Add(new DropZoneDefinition(
                        "convert_to_files",
                        "📄",
                        "Convert to Files Fence",
                        $"Display all {analysis.TotalCount} item(s) as file list",
                        WpfMedia.Color.FromRgb(255, 152, 0))); // Orange
                }

                // Offer to keep current type
                if (currentType == EntryType.Pictures.ToString())
                {
                    zones.Add(new DropZoneDefinition(
                        "keep_pictures",
                        "🖼️",
                        "Add Images Only",
                        $"Add {analysis.ImageFiles.Count} image(s), ignore other files",
                        WpfMedia.Color.FromRgb(33, 150, 243))); // Blue
                }
                else if (currentType == EntryType.Video.ToString())
                {
                    zones.Add(new DropZoneDefinition(
                        "keep_video",
                        "🎬",
                        "Add Videos Only",
                        $"Add {analysis.VideoFiles.Count} video(s), ignore other files",
                        WpfMedia.Color.FromRgb(33, 150, 243))); // Blue
                }
                else if (currentType == EntryType.Files.ToString())
                {
                    zones.Add(new DropZoneDefinition(
                        "keep_files",
                        "📄",
                        "Keep as Files Fence",
                        $"Add all {analysis.TotalCount} item(s) to file list",
                        WpfMedia.Color.FromRgb(33, 150, 243))); // Blue
                }

                return zones;
            }

            // Scenario 4: Simple add to current fence (no conversion needed)
            if (currentType == EntryType.Pictures.ToString())
            {
                // Pictures fence, only images
                if (analysis.ImageFiles.Count > 0)
                {
                    zones.Add(new DropZoneDefinition(
                        "add_images",
                        "🖼️",
                        "Add Images",
                        $"Add {analysis.ImageFiles.Count} image(s) to gallery",
                        WpfMedia.Color.FromRgb(76, 175, 80))); // Green
                }
                return zones;
            }
            else if (currentType == EntryType.Video.ToString())
            {
                // Video fence, only videos
                if (analysis.VideoFiles.Count > 0)
                {
                    zones.Add(new DropZoneDefinition(
                        "add_videos",
                        "🎬",
                        "Add Videos",
                        $"Add {analysis.VideoFiles.Count} video(s) to playlist",
                        WpfMedia.Color.FromRgb(76, 175, 80))); // Green
                }
                return zones;
            }
            else if (currentType == EntryType.Files.ToString())
            {
                // Files fence, accept everything (unless already in Path mode - show warning)
                if (!string.IsNullOrEmpty(fenceInfo.Path))
                {
                    // Warn about Path → Items conversion
                    zones.Add(new DropZoneDefinition(
                        "convert_to_items",
                        "⚠️",
                        "Convert to Static List",
                        $"Stop monitoring folder, add {analysis.TotalCount} item(s) to static list",
                        WpfMedia.Color.FromRgb(255, 193, 7))); // Yellow/Warning
                }
                else
                {
                    zones.Add(new DropZoneDefinition(
                        "add_files",
                        "📄",
                        "Add Files",
                        $"Add {analysis.TotalCount} item(s) to fence",
                        WpfMedia.Color.FromRgb(76, 175, 80))); // Green
                }
                return zones;
            }

            // Scenario 5: Clock/Widget fences don't accept drops (but we shouldn't get here)
            // Handled by the WinForms drop handler

            return zones;
        }

        /// <summary>
        /// Processes dropped files based on the selected drop zone action
        /// </summary>
        private void ProcessDroppedFilesWithAction(string[] droppedPaths, DroppedContentAnalysis analysis, string actionId)
        {
            try
            {
                log.Info($"ProcessDroppedFilesWithAction: action={actionId}, paths={droppedPaths.Length}");

                switch (actionId)
                {
                    case "monitor_folder":
                        // Monitor single folder (Path mode)
                        HandleFolderDropMonitorMode(analysis.Folders[0]);
                        break;

                    case "static_list":
                        // Add single folder as static list (Items mode)
                        // For Pictures fences, only add images from folder
                        if (fenceInfo.Type == EntryType.Pictures.ToString())
                        {
                            HandleFolderDropStaticModeImagesOnly(analysis.Folders[0]);
                        }
                        else
                        {
                            HandleFolderDropStaticMode(analysis.Folders[0]);
                        }
                        break;

                    case "static_list_multiple":
                        // Add multiple folders as static lists
                        foreach (var folder in analysis.Folders)
                        {
                            HandleFolderDropStaticMode(folder);
                        }
                        break;

                    case "convert_to_pictures":
                        // Convert fence to Pictures type
                        log.Info($"Converting fence '{fenceInfo.Name}' from {fenceInfo.Type} to Pictures");
                        fenceInfo.Type = EntryType.Pictures.ToString();

                        // Clear old mode settings (Path, Filter, Items)
                        fenceInfo.Path = null;
                        fenceInfo.Filter = null;
                        fenceInfo.Items = null;

                        RecreateWpfContent();

                        // Add images
                        HandlePicturesFenceDrop(analysis);
                        break;

                    case "convert_to_video":
                        // Convert fence to Video type
                        log.Info($"Converting fence '{fenceInfo.Name}' from {fenceInfo.Type} to Video");
                        fenceInfo.Type = EntryType.Video.ToString();

                        // Clear old mode settings (Path, Filter, Items)
                        fenceInfo.Path = null;
                        fenceInfo.Filter = null;
                        fenceInfo.Items = null;

                        RecreateWpfContent();

                        // Add videos
                        HandleVideoFenceDrop(analysis);
                        break;

                    case "convert_to_files":
                        // Convert fence to Files type
                        log.Info($"Converting fence '{fenceInfo.Name}' from {fenceInfo.Type} to Files");
                        fenceInfo.Type = EntryType.Files.ToString();

                        // Clear old mode settings (Path, Filter, Items)
                        fenceInfo.Path = null;
                        fenceInfo.Filter = null;
                        fenceInfo.Items = null;

                        RecreateWpfContent();

                        // Add all files
                        HandleFilesFenceDrop(analysis);
                        break;

                    case "keep_pictures":
                        // Keep as Pictures fence, add images only
                        HandlePicturesFenceDrop(analysis);
                        break;

                    case "keep_video":
                        // Keep as Video fence, add videos only
                        HandleVideoFenceDrop(analysis);
                        break;

                    case "keep_files":
                        // Keep as Files fence, add all items
                        HandleFilesFenceDrop(analysis);
                        break;

                    case "add_images":
                        // Add images to Pictures fence
                        HandlePicturesFenceDrop(analysis);
                        break;

                    case "add_videos":
                        // Add videos to Video fence
                        HandleVideoFenceDrop(analysis);
                        break;

                    case "add_files":
                        // Add files to Files fence
                        HandleFilesFenceDrop(analysis);
                        break;

                    case "convert_to_items":
                        // Path → Items conversion (with warning handled by zone generation)
                        HandleFilesFenceDrop(analysis);
                        break;

                    default:
                        log.Warn($"Unknown action ID: {actionId}, falling back to default processing");
                        ProcessDroppedFiles(droppedPaths);
                        break;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error processing drop with action '{actionId}': {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to process dropped items: {ex.Message}",
                    "Drag and Drop Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        #region WinForms Drag & Drop Handlers

        /// <summary>
        /// Handles drop completed event from overlay
        /// </summary>
        private void DropZoneOverlay_DropCompleted(object sender, DropCompletedEventArgs e)
        {
            log.Debug($"DropCompleted event fired: zone={e.DropZone?.ActionId ?? "null"}");

            try
            {
                if (e.DropZone != null && e.DroppedPaths != null && currentDragAnalysis != null)
                {
                    log.Info($"Processing overlay drop: zone={e.DropZone.ActionId} - {e.DropZone.Label}");

                    // Process based on zone selection
                    ProcessDroppedFilesWithAction(
                        e.DroppedPaths,
                        currentDragAnalysis,
                        e.DropZone.ActionId);

                    // Force complete UI refresh after drop processing
                    // This ensures new content is immediately visible
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        // Force WPF layout update
                        if (elementHost?.Child != null)
                        {
                            elementHost.Child.InvalidateMeasure();
                            elementHost.Child.InvalidateArrange();
                            elementHost.Child.UpdateLayout();
                            elementHost.Child.InvalidateVisual();
                        }
                    }, System.Windows.Threading.DispatcherPriority.Render);

                    // Force WinForms refresh
                    elementHost?.Refresh();
                    this.PerformLayout();
                    this.Refresh();

                    log.Debug("Forced content refresh after drop processing");
                }
                else
                {
                    log.Warn($"DropCompleted event fired but missing data - Zone={e.DropZone != null}, Paths={e.DroppedPaths != null}, Analysis={currentDragAnalysis != null}");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error processing drop from overlay: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to process dropped items: {ex.Message}",
                    "Drag and Drop Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // Cleanup
                log.Debug("Cleaning up: hiding overlay and clearing results");
                dropZoneOverlay?.Hide();
                dropZoneOverlay?.ClearDropResults();
                currentDragAnalysis = null;
            }
        }

        private void FenceContainer_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;

                // Analyze dropped content and show zones
                var droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (droppedPaths != null && droppedPaths.Length > 0 && dropZoneOverlay != null)
                {
                    currentDragAnalysis = AnalyzeDroppedContent(droppedPaths);
                    log.Debug($"Drag Enter (FenceContainer): {droppedPaths.Length} items, generating drop zones");

                    // Generate and show drop zones
                    var zones = GenerateDropZones(currentDragAnalysis);
                    if (zones != null && zones.Count > 0)
                    {
                        dropZoneOverlay.ShowZones(zones);
                        log.Debug($"Showing {zones.Count} drop zone(s) - overlay will now handle drag events");
                    }
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void FenceContainer_DragLeave(object sender, EventArgs e)
        {
            // Only hide if we're truly leaving the fence (not entering the overlay)
            // Check if mouse is still within fence bounds
            var mousePos = this.PointToClient(Cursor.Position);
            if (!this.ClientRectangle.Contains(mousePos))
            {
                log.Debug("Drag Leave (FenceContainer): hiding drop zones");
                dropZoneOverlay?.Hide();
                dropZoneOverlay?.ClearDropResults();
                currentDragAnalysis = null;
            }
        }

        private void FenceContainer_DragDrop(object sender, DragEventArgs e)
        {
            // Note: Most drops are now handled by DropZoneOverlay.DropCompleted event
            // This handler serves as a fallback if drop happens outside zones (shouldn't happen normally)

            log.Debug($"Drop on FenceContainer (fallback) - overlay processed: {dropZoneOverlay?.LastDropZone != null}");

            // If overlay already processed the drop via DropCompleted event, do nothing
            if (dropZoneOverlay?.LastDropZone != null)
            {
                log.Debug("Drop already processed by overlay via DropCompleted event - skipping fallback");
                return;
            }

            // Fallback: Drop outside zones or overlay didn't show - use default processing
            try
            {
                log.Debug("No zone selected - using default processing (fallback)");

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (droppedPaths != null && droppedPaths.Length > 0)
                    {
                        ProcessDroppedFiles(droppedPaths);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error processing drop (fallback): {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to process dropped items: {ex.Message}",
                    "Drag and Drop Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // Cleanup
                log.Debug("Cleaning up (fallback): hiding overlay and clearing results");
                dropZoneOverlay?.Hide();
                dropZoneOverlay?.ClearDropResults();
                currentDragAnalysis = null;
            }
        }

        /// <summary>
        /// Processes dropped files/folders - called from both WinForms and WPF drag & drop handlers
        /// </summary>
        private void ProcessDroppedFiles(string[] droppedPaths)
        {
            try
            {
                log.Debug($"ProcessDroppedFiles: {droppedPaths.Length} items onto fence '{fenceInfo.Name}' (Type: {fenceInfo.Type})");

                // Analyze what was dropped
                var analysis = AnalyzeDroppedContent(droppedPaths);
                log.Debug($"Content analysis - Images: {analysis.ImageFiles.Count}, Videos: {analysis.VideoFiles.Count}, Executables: {analysis.ExecutableFiles.Count}, Other: {analysis.OtherFiles.Count}, Folders: {analysis.Folders.Count}");

                var currentType = fenceInfo.Type;
                var dominantType = analysis.GetDominantType();

                // Check for type conversion suggestions
                string suggestedType = GetSuggestedFenceType(currentType, analysis, dominantType);

                if (!string.IsNullOrEmpty(suggestedType) && suggestedType != currentType)
                {
                    // Offer to convert fence type
                    var conversionResult = MessageBox.Show(
                        GetTypeConversionMessage(currentType, suggestedType, analysis),
                        "Convert Fence Type?",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (conversionResult == DialogResult.Cancel)
                    {
                        log.Debug("User cancelled drop operation");
                        return;
                    }

                    if (conversionResult == DialogResult.Yes)
                    {
                        // Convert fence type
                        log.Info($"Converting fence '{fenceInfo.Name}' from {currentType} to {suggestedType}");
                        fenceInfo.Type = suggestedType;
                        currentType = suggestedType;

                        // Recreate content with new type
                        RecreateWpfContent();

                        MessageBox.Show(
                            $"Fence converted to {suggestedType} type.\n\nDropped items will now be added.",
                            "Conversion Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    // If No, continue with current type (don't convert, just add items)
                }

                // Handle based on current fence type (possibly converted)
                if (currentType == EntryType.Pictures.ToString())
                {
                    HandlePicturesFenceDrop(analysis);
                }
                else if (currentType == EntryType.Files.ToString())
                {
                    HandleFilesFenceDrop(analysis);
                }
                else if (currentType == EntryType.Clock.ToString() || currentType == EntryType.Widget.ToString())
                {
                    // Clock and Widget fences don't accept drag-and-drop
                    MessageBox.Show(
                        $"Cannot add items to {currentType} fence.\n\nChange fence type to Files or Pictures first.",
                        "Incompatible Fence Type",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    // Unknown type - treat as Files
                    HandleFilesFenceDrop(analysis);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error handling drag-and-drop: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to process dropped items: {ex.Message}",
                    "Drag and Drop Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Gets suggested fence type based on dropped content analysis
        /// </summary>
        private string GetSuggestedFenceType(string currentType, DroppedContentAnalysis analysis, string dominantType)
        {
            // Pictures fence receiving non-images → suggest Files
            if (currentType == EntryType.Pictures.ToString())
            {
                if (dominantType != "Images" && analysis.ImageFiles.Count == 0)
                {
                    // No images at all - suggest Files
                    return EntryType.Files.ToString();
                }
                else if (dominantType == "Mixed" && analysis.ImageFiles.Count < analysis.TotalCount * 0.5)
                {
                    // Less than 50% images - suggest Files
                    return EntryType.Files.ToString();
                }
            }

            // Files fence receiving mostly images → suggest Pictures
            // Files fence receiving mostly videos → suggest Video
            else if (currentType == EntryType.Files.ToString())
            {
                if (dominantType == "Images" && analysis.ImageFiles.Count >= 1)
                {
                    // Images detected - suggest Pictures
                    // Lower threshold (1 instead of 3) for better UX with single-file drops
                    return EntryType.Pictures.ToString();
                }
                else if (dominantType == "Videos" && analysis.VideoFiles.Count >= 1)
                {
                    // Videos detected - suggest Video fence
                    return EntryType.Video.ToString();
                }
            }

            // Clock/Widget/Video fence receiving any droppable content → suggest appropriate type
            else if (currentType == EntryType.Clock.ToString() || currentType == EntryType.Widget.ToString() || currentType == EntryType.Video.ToString())
            {
                if (dominantType == "Images" && analysis.ImageFiles.Count >= 3)
                {
                    return EntryType.Pictures.ToString();
                }
                else if (dominantType == "Videos" && analysis.VideoFiles.Count >= 1)
                {
                    return EntryType.Video.ToString();
                }
                else if (analysis.TotalCount > 0)
                {
                    return EntryType.Files.ToString();
                }
            }

            return null; // No suggestion
        }

        /// <summary>
        /// Gets human-readable message for type conversion dialog
        /// </summary>
        private string GetTypeConversionMessage(string currentType, string suggestedType, DroppedContentAnalysis analysis)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"You dropped {analysis.TotalCount} item(s) onto a {currentType} fence:");
            sb.AppendLine();

            // Show content breakdown
            if (analysis.ImageFiles.Count > 0)
                sb.AppendLine($"  • {analysis.ImageFiles.Count} image(s)");
            if (analysis.VideoFiles.Count > 0)
                sb.AppendLine($"  • {analysis.VideoFiles.Count} video(s)");
            if (analysis.ExecutableFiles.Count > 0)
                sb.AppendLine($"  • {analysis.ExecutableFiles.Count} executable(s)");
            if (analysis.OtherFiles.Count > 0)
                sb.AppendLine($"  • {analysis.OtherFiles.Count} other file(s)");
            if (analysis.Folders.Count > 0)
                sb.AppendLine($"  • {analysis.Folders.Count} folder(s)");

            sb.AppendLine();
            sb.AppendLine($"This content would work better in a {suggestedType} fence.");
            sb.AppendLine();
            sb.AppendLine($"Convert this fence to {suggestedType} type?");
            sb.AppendLine();
            sb.AppendLine("YES = Convert fence type and add items");
            sb.AppendLine($"NO = Keep as {currentType} fence and add compatible items");
            sb.AppendLine("CANCEL = Don't add anything");

            return sb.ToString();
        }

        /// <summary>
        /// Analyzes dropped content and categorizes it by type
        /// </summary>
        private DroppedContentAnalysis AnalyzeDroppedContent(string[] paths)
        {
            var analysis = new DroppedContentAnalysis();

            // File type extension arrays
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico", ".webp", ".svg" };
            var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };
            var executableExtensions = new[] { ".exe", ".msi", ".bat", ".cmd", ".lnk" };

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    analysis.Folders.Add(path);
                }
                else if (File.Exists(path))
                {
                    var ext = Path.GetExtension(path).ToLowerInvariant();

                    if (imageExtensions.Contains(ext))
                    {
                        analysis.ImageFiles.Add(path);
                    }
                    else if (videoExtensions.Contains(ext))
                    {
                        analysis.VideoFiles.Add(path);
                    }
                    else if (executableExtensions.Contains(ext))
                    {
                        analysis.ExecutableFiles.Add(path);
                    }
                    else
                    {
                        analysis.OtherFiles.Add(path);
                    }
                }
            }

            return analysis;
        }

        /// <summary>
        /// Handle drop on Pictures fence - only accepts images
        /// </summary>
        private void HandlePicturesFenceDrop(DroppedContentAnalysis analysis)
        {
            // Pictures fence only accepts image files
            if (analysis.ImageFiles.Count == 0)
            {
                MessageBox.Show(
                    "Pictures fence only accepts image files.\n\n" +
                    "Supported formats: JPG, PNG, GIF, BMP, ICO, WEBP, SVG",
                    "Invalid Content",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Warn about non-images
            if (analysis.OtherFiles.Count > 0 || analysis.Folders.Count > 0)
            {
                MessageBox.Show(
                    $"Only {analysis.ImageFiles.Count} image(s) will be added.\n\n" +
                    $"Ignored: {analysis.OtherFiles.Count} file(s), {analysis.Folders.Count} folder(s)",
                    "Pictures Fence",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            // Add images to fence
            if (fenceInfo.Items == null)
                fenceInfo.Items = new List<string>();

            foreach (var img in analysis.ImageFiles)
            {
                if (!fenceInfo.Items.Contains(img))
                    fenceInfo.Items.Add(img);
            }

            RefreshContent();
            NotifyChanged();
            log.Debug($"Added {analysis.ImageFiles.Count} images to Pictures fence");
        }

        /// <summary>
        /// Handle drop on Video fence - accepts only video files
        /// </summary>
        private void HandleVideoFenceDrop(DroppedContentAnalysis analysis)
        {
            // Video fence only accepts video files
            if (analysis.VideoFiles.Count == 0)
            {
                MessageBox.Show(
                    "Video fence only accepts video files.\n\n" +
                    "Supported formats: MP4, AVI, MKV, MOV, WMV, FLV, WebM, M4V, MPG, MPEG",
                    "Invalid Content",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Warn about non-videos
            if (analysis.ImageFiles.Count > 0 || analysis.OtherFiles.Count > 0 || analysis.Folders.Count > 0)
            {
                MessageBox.Show(
                    $"Only {analysis.VideoFiles.Count} video(s) will be added.\n\n" +
                    $"Ignored: {analysis.ImageFiles.Count} image(s), {analysis.OtherFiles.Count} file(s), {analysis.Folders.Count} folder(s)",
                    "Video Fence",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            // Check if fence had no videos before (need to recreate UI)
            bool wasEmpty = (fenceInfo.Items == null || fenceInfo.Items.Count == 0);

            // Add videos to fence
            if (fenceInfo.Items == null)
                fenceInfo.Items = new List<string>();

            int addedCount = 0;
            foreach (var video in analysis.VideoFiles)
            {
                if (!fenceInfo.Items.Contains(video))
                {
                    fenceInfo.Items.Add(video);
                    addedCount++;
                }
            }

            // If fence was empty, recreate content to create MediaElement
            // Otherwise just refresh
            if (wasEmpty && addedCount > 0)
            {
                log.Debug($"Video fence was empty, recreating content with {addedCount} videos");
                RecreateWpfContent();
            }
            else
            {
                RefreshContent();
            }

            NotifyChanged();
            log.Debug($"Added {addedCount} videos to Video fence (total: {fenceInfo.Items.Count})");
        }

        /// <summary>
        /// Handle drop on Files fence - accepts everything (simplified for zone-based drops)
        /// </summary>
        private void HandleFilesFenceDrop(DroppedContentAnalysis analysis)
        {
            // Note: Folder-only drops are handled by zone selection (monitor_folder or static_list actions)
            // This method handles mixed content or direct file drops

            // Handle mixed content (files + folders) or just files
            var allDropped = analysis.ImageFiles
                .Concat(analysis.VideoFiles)
                .Concat(analysis.ExecutableFiles)
                .Concat(analysis.OtherFiles)
                .Concat(analysis.Folders)
                .ToList();

            // Warn if fence is currently using Path mode (will convert to Items mode)
            if (!string.IsNullOrEmpty(fenceInfo.Path))
            {
                var pathWarning = MessageBox.Show(
                    $"This fence is currently monitoring a folder:\n\n" +
                    $"{fenceInfo.Path}\n\n" +
                    $"Adding these items will convert the fence to a static list.\n" +
                    $"New files in the monitored folder will NO LONGER appear automatically.\n\n" +
                    $"Continue?",
                    "Convert to Static List?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (pathWarning == DialogResult.No)
                {
                    log.Debug("User cancelled drop to avoid Path→Items conversion");
                    return;
                }

                log.Debug($"Files fence converting from Path mode ({fenceInfo.Path}) to Items mode");
                fenceInfo.Path = null;
            }

            // Add all dropped items to the fence
            if (fenceInfo.Items == null)
                fenceInfo.Items = new List<string>();

            int addedCount = 0;
            foreach (var item in allDropped)
            {
                if (!fenceInfo.Items.Contains(item))
                {
                    fenceInfo.Items.Add(item);
                    addedCount++;
                }
            }

            RefreshContent();
            NotifyChanged();
            log.Debug($"Added {addedCount} items to Files fence (total: {fenceInfo.Items.Count})");
        }

        /// <summary>
        /// Handle folder drop in monitor mode - sets Path to monitor folder dynamically (simplified for zone-based drops)
        /// </summary>
        private void HandleFolderDropMonitorMode(string folderPath)
        {
            log.Info($"Setting fence to monitor folder: {folderPath}");

            // Clear existing items if any (user already chose this action via drop zone)
            if (fenceInfo.Items != null && fenceInfo.Items.Count > 0)
            {
                fenceInfo.Items.Clear();
                fenceInfo.Items = null;
            }

            // Set path for monitoring
            fenceInfo.Path = folderPath;

            // Recreate WPF content to switch from Items mode to Path mode
            RecreateWpfContent();
            NotifyChanged();

            log.Info($"Fence '{fenceInfo.Name}' now monitoring path: {folderPath}");
        }

        /// <summary>
        /// Handle folder drop in static mode (images only) - adds only image files from folder to Items list
        /// </summary>
        private void HandleFolderDropStaticModeImagesOnly(string folderPath)
        {
            log.Info($"Adding image files from folder as static list: {folderPath}");

            try
            {
                // Get all files in folder (non-recursive)
                var files = Directory.GetFiles(folderPath);

                // Filter to images only
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico", ".webp", ".svg" };
                var imageFiles = files.Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();

                if (imageFiles.Count == 0)
                {
                    log.Warn($"No image files found in folder: {folderPath}");
                    MessageBox.Show(
                        $"No image files found in folder.\n\nSupported formats: JPG, PNG, GIF, BMP, ICO, WEBP, SVG",
                        "No Images Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // Check if we're switching from Path mode to Items mode
                bool switchingMode = !string.IsNullOrEmpty(fenceInfo.Path);

                if (fenceInfo.Items == null)
                    fenceInfo.Items = new List<string>();

                // Clear Path if it was set
                if (!string.IsNullOrEmpty(fenceInfo.Path))
                {
                    fenceInfo.Path = null;
                }

                int addedCount = 0;
                foreach (var file in imageFiles)
                {
                    if (!fenceInfo.Items.Contains(file))
                    {
                        fenceInfo.Items.Add(file);
                        addedCount++;
                    }
                }

                // If switching modes, recreate content; otherwise just refresh
                if (switchingMode)
                {
                    RecreateWpfContent();
                }
                else
                {
                    RefreshContent();
                }
                NotifyChanged();

                log.Info($"Added {addedCount} image files from folder '{folderPath}' to Pictures fence (total: {fenceInfo.Items.Count})");
            }
            catch (Exception ex)
            {
                log.Error($"Error reading folder contents: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to read folder contents:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handle folder drop in static mode - adds current folder contents to Items list (simplified for zone-based drops)
        /// </summary>
        private void HandleFolderDropStaticMode(string folderPath)
        {
            log.Info($"Adding folder contents as static list: {folderPath}");

            try
            {
                // Get all files in folder (non-recursive)
                var files = Directory.GetFiles(folderPath);

                if (files.Length == 0)
                {
                    log.Warn($"Folder is empty: {folderPath}");
                    return;
                }

                // Check if we're switching from Path mode to Items mode
                bool switchingMode = !string.IsNullOrEmpty(fenceInfo.Path);

                if (fenceInfo.Items == null)
                    fenceInfo.Items = new List<string>();

                // Clear Path if it was set (user already chose to convert via drop zone)
                if (!string.IsNullOrEmpty(fenceInfo.Path))
                {
                    fenceInfo.Path = null;
                }

                int addedCount = 0;
                foreach (var file in files)
                {
                    if (!fenceInfo.Items.Contains(file))
                    {
                        fenceInfo.Items.Add(file);
                        addedCount++;
                    }
                }

                // If switching modes, recreate content; otherwise just refresh
                if (switchingMode)
                {
                    RecreateWpfContent();
                }
                else
                {
                    RefreshContent();
                }
                NotifyChanged();

                log.Info($"Added {addedCount} files from folder '{folderPath}' to fence '{fenceInfo.Name}' (static mode, total: {fenceInfo.Items.Count})");
            }
            catch (Exception ex)
            {
                log.Error($"Error reading folder contents: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to read folder contents:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        #endregion

        /// <summary>
        /// Helper class to analyze dropped content by type
        /// </summary>
        private class DroppedContentAnalysis
        {
            public List<string> ImageFiles { get; } = new List<string>();
            public List<string> VideoFiles { get; } = new List<string>();
            public List<string> ExecutableFiles { get; } = new List<string>();
            public List<string> OtherFiles { get; } = new List<string>();
            public List<string> Folders { get; } = new List<string>();

            /// <summary>
            /// Gets the total count of all items
            /// </summary>
            public int TotalCount => ImageFiles.Count + VideoFiles.Count + ExecutableFiles.Count + OtherFiles.Count + Folders.Count;

            /// <summary>
            /// Gets the dominant content type based on file counts
            /// </summary>
            public string GetDominantType()
            {
                if (Folders.Count > 0 && Folders.Count >= (TotalCount / 2))
                    return "Folders";
                if (ImageFiles.Count > 0 && ImageFiles.Count >= (TotalCount * 0.7))
                    return "Images";
                if (VideoFiles.Count > 0 && VideoFiles.Count >= (TotalCount * 0.7))
                    return "Videos";
                if (ExecutableFiles.Count > 0 && ExecutableFiles.Count >= (TotalCount * 0.7))
                    return "Executables";
                return "Mixed";
            }
        }

        #endregion

        #region Screen Boundary Enforcement

        /// <summary>
        /// Constrains fence position and size to stay within screen bounds.
        /// Ensures at least 50 pixels are visible on screen to prevent fences from being lost.
        /// </summary>
        private Rectangle ConstrainToScreenBounds(int x, int y, int width, int height)
        {
            const int MinVisible = 50; // Minimum pixels that must be visible

            // Get the working area of the screen containing this fence
            Rectangle screenBounds = Screen.FromPoint(new Point(x, y)).WorkingArea;

            // Constrain X position (left edge)
            if (x < screenBounds.Left - width + MinVisible)
                x = screenBounds.Left - width + MinVisible;
            if (x > screenBounds.Right - MinVisible)
                x = screenBounds.Right - MinVisible;

            // Constrain Y position (top edge)
            if (y < screenBounds.Top)
                y = screenBounds.Top;
            if (y > screenBounds.Bottom - MinVisible)
                y = screenBounds.Bottom - MinVisible;

            // Constrain width to fit on screen
            if (x + width > screenBounds.Right + width - MinVisible)
                width = Math.Max(150, screenBounds.Right - x + width - MinVisible);

            // Constrain height to fit on screen
            if (y + height > screenBounds.Bottom)
                height = Math.Max(150, screenBounds.Bottom - y);

            return new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// Re-applies boundary constraints to current fence position.
        /// Call this after display settings change or manual positioning.
        /// </summary>
        public void EnforceBoundaries()
        {
            var constrainedBounds = ConstrainToScreenBounds(
                this.Location.X,
                this.Location.Y,
                this.Width,
                this.Height);

            if (constrainedBounds.X != this.Location.X ||
                constrainedBounds.Y != this.Location.Y ||
                constrainedBounds.Width != this.Width ||
                constrainedBounds.Height != this.Height)
            {
                this.Location = new Point(constrainedBounds.X, constrainedBounds.Y);
                this.Size = new Size(constrainedBounds.Width, constrainedBounds.Height);

                // Update FenceInfo
                fenceInfo.PosX = constrainedBounds.X;
                fenceInfo.PosY = constrainedBounds.Y;
                fenceInfo.Width = constrainedBounds.Width;
                fenceInfo.Height = constrainedBounds.Height;

                log.Debug($"Fence '{fenceInfo.Name}' position adjusted to stay within screen bounds");
            }
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose behaviors
                fadeAnimation?.Dispose();
                dragBehavior?.Detach();
                resizeBehavior?.Detach();
                roundedCornersBehavior?.Dispose();

                // Unsubscribe from handler events
                if (fenceHandler != null)
                {
                    fenceHandler.ContentChanged -= FenceHandler_ContentChanged;
                }

                // Unsubscribe from drop zone overlay events
                if (dropZoneOverlay != null)
                {
                    dropZoneOverlay.DropCompleted -= DropZoneOverlay_DropCompleted;
                }

                fenceHandler?.Cleanup();
                titleFont?.Dispose();
                // Note: WPF ContextMenu doesn't implement IDisposable - managed by GC
                elementHost?.Dispose();

                // Dispose border panels
                borderLeft?.Dispose();
                borderRight?.Dispose();
                borderTop?.Dispose();
                borderBottom?.Dispose();
                borderBottomRight?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }

    /// <summary>
    /// Enum for resize directions
    /// </summary>
    internal enum ResizeDirection
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
        BottomRight
    }
}
