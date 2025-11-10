using NoFences.Core.Model;
using System.Windows;
using System.Windows.Controls;

namespace NoFences.View.Canvas.TypeEditors
{
    /// <summary>
    /// Properties panel for Video fence type
    /// </summary>
    public class VideoPropertiesPanel : TypePropertiesPanel
    {
        private CheckBox chkVideoLoop;
        private CheckBox chkVideoShuffle;
        private CheckBox chkVideoPlayOnHover;
        private CheckBox chkVideoShowPlaylist;
        private Slider sliderVideoVolume;
        private TextBlock lblVolumeValue;

        public VideoPropertiesPanel()
        {
            var stack = new StackPanel();

            // Video Loop
            chkVideoLoop = new CheckBox
            {
                Content = "Loop video(s)",
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 4)
            };
            stack.Children.Add(chkVideoLoop);
            stack.Children.Add(new TextBlock
            {
                Text = "When enabled, video or playlist will loop continuously",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Video Shuffle
            chkVideoShuffle = new CheckBox
            {
                Content = "Shuffle playlist",
                IsChecked = false,
                Margin = new Thickness(0, 0, 0, 4)
            };
            stack.Children.Add(chkVideoShuffle);
            stack.Children.Add(new TextBlock
            {
                Text = "Play videos in random order (only applies to playlists with multiple videos)",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Play on Hover
            chkVideoPlayOnHover = new CheckBox
            {
                Content = "Play on hover only (Live Photo mode)",
                IsChecked = false,
                Margin = new Thickness(0, 0, 0, 4)
            };
            stack.Children.Add(chkVideoPlayOnHover);
            stack.Children.Add(new TextBlock
            {
                Text = "Video will pause by default and only play when mouse hovers over the fence",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Show Playlist
            chkVideoShowPlaylist = new CheckBox
            {
                Content = "Show playlist panel",
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 4)
            };
            stack.Children.Add(chkVideoShowPlaylist);
            stack.Children.Add(new TextBlock
            {
                Text = "Display a side panel with the list of videos (only shown if multiple videos)",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Separator
            stack.Children.Add(new System.Windows.Controls.Separator { Margin = new Thickness(0, 12, 0, 12) });

            // Volume Control
            stack.Children.Add(new TextBlock { Text = "Volume:", Margin = new Thickness(0, 0, 0, 4) });

            var volumePanel = new StackPanel { Orientation = Orientation.Horizontal };

            sliderVideoVolume = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Width = 200,
                TickFrequency = 10,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center
            };
            sliderVideoVolume.ValueChanged += (s, e) =>
            {
                lblVolumeValue.Text = $"{(int)sliderVideoVolume.Value}%";
            };
            volumePanel.Children.Add(sliderVideoVolume);

            lblVolumeValue = new TextBlock
            {
                Text = "0%",
                Width = 40,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            volumePanel.Children.Add(lblVolumeValue);

            stack.Children.Add(volumePanel);
            stack.Children.Add(new TextBlock
            {
                Text = "Video volume (0 = muted, 100 = full volume)\nDefault is 0 (muted)",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 4, 0, 8)
            });

            // Info note
            stack.Children.Add(new TextBlock
            {
                Text = "Note: Videos use streaming playback to minimize memory usage.\nSupported formats: MP4, AVI, MKV, MOV, WMV, FLV, WebM, M4V, MPG, MPEG",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });

            Content = stack;
        }

        public override void LoadFromFenceInfo(FenceInfo fenceInfo)
        {
            chkVideoLoop.IsChecked = fenceInfo.VideoLoop;
            chkVideoShuffle.IsChecked = fenceInfo.VideoShuffle;
            chkVideoPlayOnHover.IsChecked = fenceInfo.VideoPlayOnHover;
            chkVideoShowPlaylist.IsChecked = fenceInfo.VideoShowPlaylist;
            sliderVideoVolume.Value = fenceInfo.VideoVolume;
            lblVolumeValue.Text = $"{fenceInfo.VideoVolume}%";
        }

        public override void SaveToFenceInfo(FenceInfo fenceInfo)
        {
            fenceInfo.VideoLoop = chkVideoLoop.IsChecked ?? true;
            fenceInfo.VideoShuffle = chkVideoShuffle.IsChecked ?? false;
            fenceInfo.VideoPlayOnHover = chkVideoPlayOnHover.IsChecked ?? false;
            fenceInfo.VideoShowPlaylist = chkVideoShowPlaylist.IsChecked ?? true;
            fenceInfo.VideoVolume = (int)sliderVideoVolume.Value;
        }
    }
}
