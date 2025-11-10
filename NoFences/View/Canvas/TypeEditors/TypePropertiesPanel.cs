using NoFences.Core.Model;
using System.Windows.Controls;

namespace NoFences.View.Canvas.TypeEditors
{
    /// <summary>
    /// Base class for type-specific property panels.
    /// Each fence type has its own panel that implements these methods
    /// to load and save type-specific properties.
    /// </summary>
    public abstract class TypePropertiesPanel : UserControl
    {
        /// <summary>
        /// Load properties from FenceInfo into the panel's controls
        /// </summary>
        public abstract void LoadFromFenceInfo(FenceInfo fenceInfo);

        /// <summary>
        /// Save properties from the panel's controls back to FenceInfo
        /// </summary>
        public abstract void SaveToFenceInfo(FenceInfo fenceInfo);
    }
}
