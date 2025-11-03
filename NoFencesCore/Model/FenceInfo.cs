using System;
using System.Collections.Generic;

namespace NoFences.Model
{
    public class FenceInfo
    {
        /* 
         * DO NOT RENAME PROPERTIES. Used for XML serialization.
         */

        public Guid Id { get; set; }

        public string Name { get; set; }

        public int PosX { get; set; }

        public int PosY { get; set; }

        /// <summary>
        /// Gets or sets the DPI scaled window width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the DPI scaled window height.
        /// </summary>
        public int Height { get; set; }

        public bool Locked { get; set; }

        public bool CanMinify { get; set; }

        /// <summary>
        /// Gets or sets the logical window title height.
        /// </summary>
        public int TitleHeight { get; set; } = 25;

        public string Type { get; set; } = EntryType.Files.ToString();

        public string Path { get; set; }

        public List<string> Items { get; set; } = new List<string>();

        public List<string> Filters { get; set; } = new List<string>();

        public int Interval { get; set; } = 30_000;

        public FenceInfo()
        {

        }

        public FenceInfo(Guid id)
        {
            Id = id;
        }
    }
}
