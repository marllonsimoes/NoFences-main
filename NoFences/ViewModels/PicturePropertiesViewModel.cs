using NoFences.Core.Model;
using NoFences.Model;

namespace NoFences.ViewModels
{
    /// <summary>
    /// ViewModel for Picture fence type properties.
    /// </summary>
    public class PicturePropertiesViewModel : ViewModelBase
    {
        private readonly FenceInfo fenceInfo;

        private string _displayMode;
        public string DisplayMode
        {
            get => _displayMode;
            set => SetProperty(ref _displayMode, value);
        }

        private int _masonryMinColumnWidth;
        public int MasonryMinColumnWidth
        {
            get => _masonryMinColumnWidth;
            set => SetProperty(ref _masonryMinColumnWidth, value);
        }

        private int _masonryMaxColumnWidth;
        public int MasonryMaxColumnWidth
        {
            get => _masonryMaxColumnWidth;
            set => SetProperty(ref _masonryMaxColumnWidth, value);
        }

        private int _masonryMaxImages;
        public int MasonryMaxImages
        {
            get => _masonryMaxImages;
            set => SetProperty(ref _masonryMaxImages, value);
        }

        public PicturePropertiesViewModel(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo;
            LoadFromFenceInfo();
        }

        private void LoadFromFenceInfo()
        {
            DisplayMode = fenceInfo.PictureDisplayMode ?? "Masonry";
            MasonryMinColumnWidth = fenceInfo.MasonryMinColumnWidth;
            MasonryMaxColumnWidth = fenceInfo.MasonryMaxColumnWidth;
            MasonryMaxImages = fenceInfo.MasonryMaxImages;
        }

        public void SaveToFenceInfo(FenceInfo fenceInfo)
        {
            fenceInfo.PictureDisplayMode = DisplayMode;
            fenceInfo.MasonryMinColumnWidth = MasonryMinColumnWidth;
            fenceInfo.MasonryMaxColumnWidth = MasonryMaxColumnWidth;
            fenceInfo.MasonryMaxImages = MasonryMaxImages;
        }
    }
}
