using NoFences.Core.Model;

namespace NoFences.ViewModels
{
    /// <summary>
    /// ViewModel for Clock fence type properties.
    /// </summary>
    public class ClockPropertiesViewModel : ViewModelBase
    {
        private readonly FenceInfo fenceInfo;

        // Add clock-specific properties here as needed
        // (currently Clock fence doesn't have additional properties)

        public ClockPropertiesViewModel(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo;
            LoadFromFenceInfo();
        }

        private void LoadFromFenceInfo()
        {
            // Load clock-specific properties when implemented
        }

        public void SaveToFenceInfo(FenceInfo fenceInfo)
        {
            // Save clock-specific properties when implemented
        }
    }
}
