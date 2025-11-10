using NoFences.Core.Model;
using NoFences.Model;
using System;
using System.Windows;

namespace NoFences.View.Canvas.Handlers
{
    /// <summary>
    /// WPF-based fence handler interface for the NEW canvas-based architecture.
    /// Implementations provide WPF UIElements instead of WinForms painting.
    ///
    /// This is completely separate from the original IFenceHandler interface.
    /// For the original WinForms handler interface, see View/Fences/Handlers/IFenceHandler.cs
    /// </summary>
    public interface IFenceHandlerWpf
    {
        /// <summary>
        /// Raised when the content of the fence changes in a way that may affect layout/height.
        /// Examples: images rotate in slideshow, files added/removed, grid items change.
        /// FenceContainer subscribes to this for auto-height adjustment.
        /// </summary>
        event EventHandler ContentChanged;

        /// <summary>
        /// Initializes the handler with fence information.
        /// </summary>
        void Initialize(FenceInfo fenceInfo);

        /// <summary>
        /// Creates and returns the WPF UIElement that represents the fence content.
        /// This will be hosted in an ElementHost within the FenceContainer.
        /// </summary>
        /// <param name="titleHeight">Height of the title bar</param>
        /// <param name="theme">The theme definition to apply to the content</param>
        UIElement CreateContentElement(int titleHeight, FenceThemeDefinition theme);

        /// <summary>
        /// Called when the fence is being disposed.
        /// Use this to clean up resources.
        /// </summary>
        void Cleanup();

        /// <summary>
        /// Called when fence data changes (e.g., files added/removed via drag-drop).
        /// The handler should refresh its content.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Returns true if the fence has content to display.
        /// Used to determine if fade animations should be enabled.
        /// </summary>
        bool HasContent();
    }
}
