using log4net;
using NoFences.Core.Model;
using NoFences.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoFences.View.Canvas.Handlers
{
    /// <summary>
    /// WPF-based handler for displaying videos in a fence with playlist support.
    /// Features:
    /// - Streaming playback (doesn't load entire video into memory)
    /// - Muted playback by default
    /// - Playlist panel showing all videos
    /// - Shuffle mode
    /// - Loop current video or entire playlist
    /// - Play-on-hover mode (like Live Photo)
    /// </summary>
    public class VideoFenceHandlerWpf : IFenceHandlerWpf
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(VideoFenceHandlerWpf));

        private FenceInfo fenceInfo;
        private MediaElement mediaElement;
        private Grid mainContainer;
        private Border playlistPanel;
        private StackPanel playlistItems;
        private List<string> videoFiles;
        private List<string> playOrder; // Actual play order (may be shuffled)
        private int currentVideoIndex = -1;
        private bool isHovering = false;

        private readonly string[] validExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };

        // Event raised when content changes (for auto-height)
        public event EventHandler ContentChanged;

        public void Initialize(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo ?? throw new ArgumentNullException(nameof(fenceInfo));
            log.Debug($"VideoFenceHandler initialized for fence: {fenceInfo.Name}");
        }

        public UIElement CreateContentElement(int titleHeight, FenceThemeDefinition theme)
        {
            log.Debug("Creating video content element");

            // Get video files from Items list
            videoFiles = GetVideoFiles();

            if (videoFiles.Count == 0)
            {
                log.Warn("No video files found");
                return CreateEmptyState(theme);
            }

            // Initialize play order
            InitializePlayOrder();

            // Main container grid
            mainContainer = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(
                    theme.ContentBackgroundColor.A,
                    theme.ContentBackgroundColor.R,
                    theme.ContentBackgroundColor.G,
                    theme.ContentBackgroundColor.B))
            };

            // Define columns: Video player | Playlist panel
            if (fenceInfo.VideoShowPlaylist && videoFiles.Count > 0)
            {
                mainContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                mainContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); // Playlist width
            }
            else
            {
                mainContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Create media element
            mediaElement = new MediaElement
            {
                LoadedBehavior = MediaState.Manual,  // Manual control for streaming
                ScrubbingEnabled = false,             // Reduces memory usage
                IsMuted = true,                       // Always start muted
                Stretch = Stretch.Uniform,            // Maintain aspect ratio
                Volume = fenceInfo.VideoVolume / 100.0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Wire up media events
            mediaElement.MediaOpened += MediaElement_MediaOpened;
            mediaElement.MediaEnded += MediaElement_MediaEnded;
            mediaElement.MediaFailed += MediaElement_MediaFailed;

            // Add media element to container
            Grid.SetColumn(mediaElement, 0);
            mainContainer.Children.Add(mediaElement);

            // Create playlist panel if enabled (show even for single video)
            if (fenceInfo.VideoShowPlaylist && videoFiles.Count > 0)
            {
                CreatePlaylistPanel(theme);
                Grid.SetColumn(playlistPanel, 1);
                mainContainer.Children.Add(playlistPanel);
            }

            // Wire up mouse events for play-on-hover
            if (fenceInfo.VideoPlayOnHover)
            {
                mainContainer.MouseEnter += Container_MouseEnter;
                mainContainer.MouseLeave += Container_MouseLeave;
            }

            // Load and play first video
            if (currentVideoIndex == -1 && playOrder.Count > 0)
            {
                LoadVideo(0);
            }

            return mainContainer;
        }

        private void CreatePlaylistPanel(FenceThemeDefinition theme)
        {
            // Playlist panel (right side)
            playlistPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 30, 30, 40)), // Semi-transparent dark
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, theme.BorderColor.R, theme.BorderColor.G, theme.BorderColor.B)),
                BorderThickness = new Thickness(1, 0, 0, 0), // Left border only
                Padding = new Thickness(5)
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            playlistItems = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // Add playlist items
            for (int i = 0; i < playOrder.Count; i++)
            {
                var videoPath = playOrder[i];
                var videoName = Path.GetFileNameWithoutExtension(videoPath);

                var itemButton = new Button
                {
                    Content = videoName,
                    Tag = i, // Store index
                    Margin = new Thickness(0, 2, 0, 2),
                    Padding = new Thickness(5),
                    Background = new SolidColorBrush(Colors.Transparent),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Cursor = Cursors.Hand,
                    ToolTip = videoPath
                };

                itemButton.Click += PlaylistItem_Click;
                playlistItems.Children.Add(itemButton);
            }

            scrollViewer.Content = playlistItems;
            playlistPanel.Child = scrollViewer;
        }

        private UIElement CreateEmptyState(FenceThemeDefinition theme)
        {
            var grid = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(
                    theme.ContentBackgroundColor.A,
                    theme.ContentBackgroundColor.R,
                    theme.ContentBackgroundColor.G,
                    theme.ContentBackgroundColor.B))
            };

            var textBlock = new TextBlock
            {
                Text = "No videos\n\nDrag video files here to add them",
                FontSize = 16,
                Foreground = new SolidColorBrush(Colors.Gray),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            grid.Children.Add(textBlock);
            return grid;
        }

        private List<string> GetVideoFiles()
        {
            var videos = new List<string>();

            if (fenceInfo.Items != null && fenceInfo.Items.Count > 0)
            {
                foreach (var item in fenceInfo.Items)
                {
                    if (File.Exists(item) && IsVideoFile(item))
                    {
                        videos.Add(item);
                    }
                }
            }

            log.Debug($"Found {videos.Count} video files");
            return videos;
        }

        private bool IsVideoFile(string path)
        {
            var ext = Path.GetExtension(path)?.ToLowerInvariant();
            return !string.IsNullOrEmpty(ext) && validExtensions.Contains(ext);
        }

        private void InitializePlayOrder()
        {
            playOrder = new List<string>(videoFiles);

            if (fenceInfo.VideoShuffle)
            {
                // Shuffle the playlist
                var rnd = new Random();
                playOrder = playOrder.OrderBy(x => rnd.Next()).ToList();
                log.Debug("Playlist shuffled");
            }
        }

        private void LoadVideo(int index)
        {
            if (index < 0 || index >= playOrder.Count)
            {
                log.Warn($"Invalid video index: {index}");
                return;
            }

            if (mediaElement == null)
            {
                log.Error("MediaElement is null, cannot load video");
                return;
            }

            currentVideoIndex = index;
            var videoPath = playOrder[index];

            try
            {
                log.Debug($"Loading video: {videoPath}");

                mediaElement.Source = new Uri(videoPath, UriKind.Absolute);
                mediaElement.Stop(); // Stop any current playback

                // Highlight current item in playlist
                UpdatePlaylistSelection();

                // Don't auto-play if play-on-hover is enabled and mouse is not hovering
                if (!fenceInfo.VideoPlayOnHover || isHovering)
                {
                    mediaElement.Play();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to load video: {videoPath}", ex);
            }
        }

        private void UpdatePlaylistSelection()
        {
            if (playlistItems == null) return;

            // Unhighlight all
            foreach (var child in playlistItems.Children)
            {
                if (child is Button btn)
                {
                    btn.Background = new SolidColorBrush(Colors.Transparent);
                    btn.FontWeight = FontWeights.Normal;
                }
            }

            // Highlight current
            if (currentVideoIndex >= 0 && currentVideoIndex < playlistItems.Children.Count)
            {
                var currentButton = playlistItems.Children[currentVideoIndex] as Button;
                if (currentButton != null)
                {
                    currentButton.Background = new SolidColorBrush(Color.FromArgb(100, 76, 175, 80)); // Semi-transparent green
                    currentButton.FontWeight = FontWeights.Bold;
                }
            }
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            log.Debug($"Video opened: {playOrder[currentVideoIndex]}");
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            log.Debug("Video ended");

            if (fenceInfo.VideoLoop && playOrder.Count == 1)
            {
                // Loop current video
                mediaElement.Position = TimeSpan.Zero;
                mediaElement.Play();
            }
            else
            {
                // Play next video in playlist
                int nextIndex = currentVideoIndex + 1;

                if (nextIndex >= playOrder.Count)
                {
                    // Reached end of playlist
                    if (fenceInfo.VideoLoop)
                    {
                        // Loop playlist
                        nextIndex = 0;
                    }
                    else
                    {
                        // Stop at end
                        return;
                    }
                }

                LoadVideo(nextIndex);
            }
        }

        private void MediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            log.Error($"Video playback failed: {e.ErrorException?.Message}");
        }

        private void PlaylistItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                log.Debug($"Playlist item clicked: index {index}");
                LoadVideo(index);
            }
        }

        private void Container_MouseEnter(object sender, MouseEventArgs e)
        {
            isHovering = true;

            if (fenceInfo.VideoPlayOnHover && mediaElement != null)
            {
                log.Debug("Mouse entered - playing video");
                mediaElement.Play();
            }
        }

        private void Container_MouseLeave(object sender, MouseEventArgs e)
        {
            isHovering = false;

            if (fenceInfo.VideoPlayOnHover && mediaElement != null)
            {
                log.Debug("Mouse left - pausing video");
                mediaElement.Pause();
            }
        }

        public void Refresh()
        {
            log.Debug("Refreshing video fence");

            // Reload video list
            videoFiles = GetVideoFiles();
            InitializePlayOrder();

            // Restart from first video
            if (playOrder.Count > 0)
            {
                LoadVideo(0);
            }
        }

        public bool HasContent()
        {
            return videoFiles != null && videoFiles.Count > 0;
        }

        public void Cleanup()
        {
            log.Debug("Cleaning up video fence");

            if (mediaElement != null)
            {
                mediaElement.Stop();
                mediaElement.Close();
                mediaElement.MediaOpened -= MediaElement_MediaOpened;
                mediaElement.MediaEnded -= MediaElement_MediaEnded;
                mediaElement.MediaFailed -= MediaElement_MediaFailed;
            }

            if (mainContainer != null && fenceInfo.VideoPlayOnHover)
            {
                mainContainer.MouseEnter -= Container_MouseEnter;
                mainContainer.MouseLeave -= Container_MouseLeave;
            }
        }
    }
}
