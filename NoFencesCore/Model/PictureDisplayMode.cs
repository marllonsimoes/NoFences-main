namespace NoFences.Core.Model
{
    /// <summary>
    /// Display mode for picture fences.
    /// </summary>
    public enum PictureDisplayMode
    {
        /// <summary>
        /// Single image at a time, rotating through images
        /// </summary>
        Slideshow,

        /// <summary>
        /// Multiple images in a masonry/grid layout
        /// </summary>
        MasonryGrid,

        /// <summary>
        /// Hybrid: Grid with random selection that rotates
        /// Good for large fences with many images
        /// </summary>
        Hybrid
    }
}
