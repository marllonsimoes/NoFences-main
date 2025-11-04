using System;
using System.Windows.Forms;

namespace NoFences.Behaviors
{
    /// <summary>
    /// Manages minify/expand behavior for fence containers.
    /// When minified, fences collapse to just the title bar height.
    /// Automatically expands when mouse enters and collapses when mouse leaves.
    /// </summary>
    public class FenceMinifyBehavior
    {
        #region Private Fields

        private readonly Control containerControl;
        private readonly Func<int> getTitleHeight;
        private readonly Func<bool> canMinifyCheck;

        private bool isMinified;
        private int previousHeight;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the minified state changes
        /// </summary>
        public event EventHandler<MinifyStateChangedEventArgs> StateChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new minify behavior
        /// </summary>
        /// <param name="containerControl">The control to apply minify behavior to</param>
        /// <param name="getTitleHeight">Function to get the current title bar height</param>
        /// <param name="canMinifyCheck">Function to check if minify is enabled</param>
        public FenceMinifyBehavior(
            Control containerControl,
            Func<int> getTitleHeight,
            Func<bool> canMinifyCheck)
        {
            this.containerControl = containerControl ?? throw new ArgumentNullException(nameof(containerControl));
            this.getTitleHeight = getTitleHeight ?? throw new ArgumentNullException(nameof(getTitleHeight));
            this.canMinifyCheck = canMinifyCheck ?? throw new ArgumentNullException(nameof(canMinifyCheck));

            this.previousHeight = containerControl.Height;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the fence is currently minified (collapsed to title bar)
        /// </summary>
        public bool IsMinified => isMinified;

        /// <summary>
        /// The height the fence had before being minified
        /// </summary>
        public int PreviousHeight => previousHeight;

        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to minify (collapse) the fence to title bar height.
        /// Returns true if minified, false if already minified or cannot minify.
        /// </summary>
        public bool TryMinify()
        {
            if (!canMinifyCheck())
                return false;

            if (isMinified)
                return false; // Already minified

            // Store current height
            previousHeight = containerControl.Height;

            // Collapse to title height
            int titleHeight = getTitleHeight();
            containerControl.Height = titleHeight;

            isMinified = true;

            // Raise event
            RaiseStateChanged(MinifyState.Minified, previousHeight, titleHeight);

            return true;
        }

        /// <summary>
        /// Attempts to expand the fence to its previous height.
        /// Returns true if expanded, false if already expanded.
        /// </summary>
        public bool TryExpand()
        {
            if (!isMinified)
                return false; // Not minified

            // Restore previous height
            int titleHeight = containerControl.Height;
            containerControl.Height = previousHeight;

            isMinified = false;

            // Raise event
            RaiseStateChanged(MinifyState.Expanded, titleHeight, previousHeight);

            return true;
        }

        /// <summary>
        /// Toggles between minified and expanded states
        /// </summary>
        public bool Toggle()
        {
            if (isMinified)
                return TryExpand();
            else
                return TryMinify();
        }

        /// <summary>
        /// Forces the fence to be expanded, even if already expanded.
        /// Useful when minify is disabled while the fence is minified.
        /// </summary>
        public void ForceExpand()
        {
            if (isMinified)
            {
                containerControl.Height = previousHeight;
                isMinified = false;
                RaiseStateChanged(MinifyState.ForcedExpand, getTitleHeight(), previousHeight);
            }
        }

        /// <summary>
        /// Updates the stored previous height without changing minify state.
        /// Call this when the fence is resized while expanded.
        /// </summary>
        /// <param name="newHeight">The new height to store</param>
        public void UpdatePreviousHeight(int newHeight)
        {
            if (!isMinified)
            {
                previousHeight = newHeight;
            }
        }

        /// <summary>
        /// Gets the appropriate height for the fence based on minify state.
        /// Used when saving fence configuration.
        /// </summary>
        public int GetSaveHeight()
        {
            return isMinified ? previousHeight : containerControl.Height;
        }

        #endregion

        #region Private Methods

        private void RaiseStateChanged(MinifyState newState, int oldHeight, int newHeight)
        {
            StateChanged?.Invoke(this, new MinifyStateChangedEventArgs(
                newState,
                isMinified,
                oldHeight,
                newHeight));
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Describes the type of minify state change
    /// </summary>
    public enum MinifyState
    {
        /// <summary>Fence was collapsed to title bar</summary>
        Minified,

        /// <summary>Fence was expanded to full height</summary>
        Expanded,

        /// <summary>Fence was forcibly expanded (e.g., when CanMinify disabled)</summary>
        ForcedExpand
    }

    /// <summary>
    /// Event arguments for minify state changes
    /// </summary>
    public class MinifyStateChangedEventArgs : EventArgs
    {
        public MinifyState State { get; }
        public bool IsMinified { get; }
        public int OldHeight { get; }
        public int NewHeight { get; }

        public MinifyStateChangedEventArgs(MinifyState state, bool isMinified, int oldHeight, int newHeight)
        {
            State = state;
            IsMinified = isMinified;
            OldHeight = oldHeight;
            NewHeight = newHeight;
        }
    }

    #endregion
}
