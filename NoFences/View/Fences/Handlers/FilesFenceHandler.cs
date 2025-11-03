using NoFences.Model;
using NoFences.Util;
using NoFencesCore.Util;
using Peter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NoFences.View.Fences.Handlers
{
    internal class FilesFenceHandler : IFenceHandler
    {
        private Form _window;
        private FenceInfo _fenceInfo;

        private const int itemPadding = 15;
        private const int itemWidth = 75;
        private const int itemHeight = 32 + itemPadding + textHeight;
        private const int textHeight = 35;
        private const float shadowDist = 1.5f;

        private string selectedItem;
        private string hoveringItem;
        private bool shouldUpdateSelection;
        private bool shouldRunDoubleClick;
        private bool hasSelectionUpdated;
        private bool hasHoverUpdated;

        private Font iconFont;

        private readonly ThumbnailProvider thumbnailProvider = new ThumbnailProvider();
        private readonly ShellContextMenu shellContextMenu = new ShellContextMenu();

        public void Initialize(Form window)
        {
            _window = window;
            _fenceInfo = ((FenceWindow)window).fenceInfo;

            var family = new FontFamily("Segoe UI");
            iconFont = new Font(family, 9);
            thumbnailProvider.IconThumbnailLoaded += ThumbnailProvider_IconThumbnailLoaded;
        }

        private List<string> GetFiles()
        {
            var fileList = new List<string>();

            if (_fenceInfo.Path != null && _fenceInfo.Path != "")
            {
                var directoryFilesList = Directory.GetFileSystemEntries(_fenceInfo.Path).ToList();
                if (_fenceInfo.Filters?.Count > 0)
                {
                    foreach (string pattern in _fenceInfo.Filters)
                    {
                        fileList.AddRange(directoryFilesList.Where(p =>
                        {
                            string filename = Path.GetFileNameWithoutExtension(p);
                            var dirInfo = new DirectoryInfo(p);
                            string dirName = dirInfo.Exists ? dirInfo.Name : null;

                            bool patternIsFileName = filename.Equals(pattern);
                            bool patternIsDirName = false;
                            if (dirName != null)
                            {
                                patternIsDirName = dirName.Equals(pattern);
                            }

                            ShortcutInfo shortcutInfo = FileUtils.GetShortcutInfo(p);
                            bool patterIsShortcutName = false;
                            bool patterIsUrl = false;
                            if (shortcutInfo != null)
                            {
                                patterIsShortcutName = shortcutInfo.Name.Equals(pattern);
                                if (shortcutInfo.Url != null)
                                {
                                    patterIsUrl = Regex.IsMatch(shortcutInfo.Url, pattern);
                                }
                            }
                            return Regex.IsMatch(p, pattern) || patternIsFileName || patternIsDirName || patterIsShortcutName || patterIsUrl;
                        }
                        ));
                    }
                }
                else
                {
                    fileList.AddRange(directoryFilesList);
                }
            }
            else if ( _fenceInfo.Items?.Count > 0)
            {
                fileList.AddRange(_fenceInfo.Items.Where((item) => Directory.Exists(item) || File.Exists(item)));
            }
            return fileList;
        }

        public void Paint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var x = itemPadding;
            var y = itemPadding;

            foreach (var file in GetFiles())
            {
                var entry = FenceEntry.FromPath(file);
                if (entry == null)
                    continue;

                RenderEntry(g, entry, x, y + ((FenceWindow)_window).titleHeight - ((FenceWindow)_window).scrollOffset, Control.MousePosition);

                var itemBottom = y + itemHeight;
                if (itemBottom > ((FenceWindow)_window).scrollHeight)
                    ((FenceWindow)_window).scrollHeight = itemBottom;

                x += itemWidth + itemPadding;
                if (x + itemWidth > ((FenceWindow)_window).Width)
                {
                    x = itemPadding;
                    y += itemHeight + itemPadding;
                }
            }
            // Click handlers
            if (shouldUpdateSelection && !hasSelectionUpdated)
                selectedItem = null;

            if (!hasHoverUpdated)
                hoveringItem = null;

            shouldRunDoubleClick = false;
            shouldUpdateSelection = false;
            hasSelectionUpdated = false;
            hasHoverUpdated = false;
        }

        public void RenderEntry(Graphics g, FenceEntry entry, int x, int y, Point mousePosition)
        {

            var icon = entry.ExtractIcon(thumbnailProvider);
            var name = entry.Name;

            var textPosition = new PointF(x, y + icon.Height + 5);
            var textMaxSize = new SizeF(itemWidth, textHeight);

            var stringFormat = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

            var textSize = g.MeasureString(name, iconFont, textMaxSize, stringFormat);
            var outlineRect = new Rectangle(x - 2, y - 2, itemWidth + 2, icon.Height + (int)textSize.Height + 5 + 2);
            var outlineRectInner = outlineRect.Shrink(1);

            var mousePos = _window.PointToClient(mousePosition);
            var mouseOver = mousePos.X >= x && mousePos.Y >= y && mousePos.X < x + outlineRect.Width && mousePos.Y < y + outlineRect.Height;

            if (mouseOver)
            {
                hoveringItem = entry.Path;
                hasHoverUpdated = true;
            }

            if (mouseOver && shouldUpdateSelection)
            {
                selectedItem = entry.Path;
                shouldUpdateSelection = false;
                hasSelectionUpdated = true;
            }

            if (mouseOver && shouldRunDoubleClick)
            {
                shouldRunDoubleClick = false;
                Open(entry);
            }

            if (selectedItem == entry.Path)
            {
                if (mouseOver)
                {
                    g.DrawRectangle(new Pen(Color.FromArgb(120, SystemColors.ActiveBorder)), outlineRectInner);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(100, SystemColors.GradientActiveCaption)), outlineRect);
                }
                else
                {
                    g.DrawRectangle(new Pen(Color.FromArgb(120, SystemColors.ActiveBorder)), outlineRectInner);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(80, SystemColors.GradientInactiveCaption)), outlineRect);
                }
            }
            else
            {
                if (mouseOver)
                {
                    g.DrawRectangle(new Pen(Color.FromArgb(120, SystemColors.ActiveBorder)), outlineRectInner);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(80, SystemColors.ActiveCaption)), outlineRect);
                }
            }

            g.DrawIcon(icon, x + itemWidth / 2 - icon.Width / 2, y);
            g.DrawString(name, iconFont, new SolidBrush(Color.FromArgb(180, 15, 15, 15)), new RectangleF(textPosition.Move(shadowDist, shadowDist), textMaxSize), stringFormat);
            g.DrawString(name, iconFont, Brushes.White, new RectangleF(textPosition, textMaxSize), stringFormat);
        }

        public void Open(FenceEntry entry)
        {
            Task.Run(() =>
            {
                // start asynchronously
                try
                {
                    Process.Start("explorer.exe", entry.Path);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to start: {e}");
                }
            });
        }

        private void ThumbnailProvider_IconThumbnailLoaded(object sender, EventArgs e)
        {
            _window.Invalidate();
        }

        public void MouseClick(object sender, EventArgs e)
        {
            if (hoveringItem != null && !Control.ModifierKeys.HasFlag(Keys.Shift))
            {
                shellContextMenu.ShowContextMenu(new[] { new FileInfo(hoveringItem) }, Control.MousePosition);
            }
        }

        public void MouseDoubleClick(object sender, EventArgs e)
        {
            shouldRunDoubleClick = true;
        }

        public void MouseLeave(object sender, EventArgs e)
        {
            selectedItem = null;
        }

        public void Click(object sender, EventArgs e)
        {
            shouldUpdateSelection = true;
        }

        public void MouseEnter(object sender, EventArgs e)
        {
        }

        public void MouseMove(object sender, MouseEventArgs e)
        {
        }

        public void DragDrop(object sender, DragEventArgs e)
        {
            var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (dropped.Count() == 1)
            {
                if (File.Exists(dropped[0]))
                {
                    if (_fenceInfo.Path != null && !_fenceInfo.Path.Equals(""))
                    {
                        var confirmation = MessageBox.Show("This is a folder fence, showing files from it.\r\n " +
                            "YES = List all the files from this folder (will not be updated) + file being added right now.\r\n" +
                            "NO = Show only the files being added right now.\r\n" +
                            "CANCEL = Cancel the action.", 
                            "Change Show Folder to Show Files?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                        if (confirmation == DialogResult.Cancel)
                        {
                            return;
                        } 
                        else if (confirmation == DialogResult.No)
                        {
                            _fenceInfo.Path = null;
                            if (_fenceInfo.Items == null)
                            {
                                _fenceInfo.Items = new List<string>();
                            }
                            foreach (var file in dropped)
                            {
                                if (!_fenceInfo.Items.Contains(file))
                                {
                                    _fenceInfo.Items.Add(file);
                                }
                            }
                        }
                        else if (confirmation == DialogResult.Yes)
                        {
                            var path = _fenceInfo.Path;
                            _fenceInfo.Items = new List<string>();
                            _fenceInfo.Items.AddRange(Directory.GetFiles(path));
                            _fenceInfo.Items.AddRange(Directory.GetDirectories(path));
                            _fenceInfo.Path = null;

                            if (_fenceInfo.Items == null)
                            {
                                _fenceInfo.Items = new List<string>();
                            }

                            foreach (var file in dropped)
                            {
                                if (!_fenceInfo.Items.Contains(file))
                                {
                                    _fenceInfo.Items.Add(file);
                                }
                            }
                        }
                    } else
                    {
                        if (_fenceInfo.Items == null)
                        {
                            _fenceInfo.Items = new List<string>();
                        }
                        foreach (var file in dropped)
                        {
                            if (!_fenceInfo.Items.Contains(file))
                            {
                                _fenceInfo.Items.Add(file);
                            }
                        }
                    }
                } 
                else if (Directory.Exists(dropped[0]))
                {
                    if (_fenceInfo.Items?.Count() > 0)
                    {
                        var confirmation = MessageBox.Show("This is a file list fence.\r\n " +
                            "YES = Keep the files and add the files from the folder (will not be updated).\r\n" +
                            "NO = Show only the files from the folder.\r\n" +
                            "CANCEL = Cancel the action.", 
                            "Change File Fence to Folder?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        if (confirmation == DialogResult.Cancel)
                        {
                            return;
                        }
                        else if (confirmation == DialogResult.No)
                        {
                            _fenceInfo.Items = null;
                            _fenceInfo.Path = dropped[0];
                        }
                        else if (confirmation == DialogResult.Yes)
                        {
                            _fenceInfo.Items = new List<string>();
                            _fenceInfo.Items.AddRange(Directory.GetFiles(dropped[0]));
                            _fenceInfo.Items.AddRange(Directory.GetDirectories(dropped[0]));
                        }
                    } 
                    else if (_fenceInfo.Items?.Count() == 0 && (_fenceInfo.Path == null || "".Equals(_fenceInfo.Path)))
                    {
                        _fenceInfo.Path = dropped[0];
                    }
                    else
                    {
                        var confirmation = MessageBox.Show("This is a folder fence.\r\n " +
                            "YES = Keep the files from the current folder and add the files from the new folder (will not updated).\r\n" +
                            "NO = Show only the files from the new folder.\r\n" +
                            "CANCEL = Cancel the action.",
                            "Change File Fence to Folder?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        if (confirmation == DialogResult.Cancel)
                        {
                            return;
                        }
                        else if (confirmation == DialogResult.No)
                        {
                            _fenceInfo.Items = null;
                            _fenceInfo.Path = dropped[0];
                        }
                        else if (confirmation == DialogResult.Yes)
                        {
                            var path = _fenceInfo.Path;
                            _fenceInfo.Items = new List<string>();
                            _fenceInfo.Items.AddRange(Directory.GetFiles(path));
                            _fenceInfo.Items.AddRange(Directory.GetDirectories(path));
                            _fenceInfo.Items.AddRange(Directory.GetFiles(dropped[0]));
                            _fenceInfo.Items.AddRange(Directory.GetDirectories(dropped[0]));
                            _fenceInfo.Path = null;
                        }
                    }
                }
            } 
            else
            {
                if (_fenceInfo.Path != null && !_fenceInfo.Path.Equals(""))
                {
                    var confirmation = MessageBox.Show("This is a folder fence, showing files from it.\r\n " +
                            "YES = Show files from the folder (will not be updated) + file being added right now.\r\n" +
                            "NO = Show only the files being added right now.\r\n" +
                            "CANCEL = Cancel the action.", 
                            "Change Show Folder to Show Files?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (confirmation == DialogResult.Cancel)
                    {
                        return;
                    } 
                    else if (confirmation == DialogResult.No)
                    {
                        _fenceInfo.Path = null;
                        if (_fenceInfo.Items == null)
                        {
                            _fenceInfo.Items = new List<string>();
                        }
                        foreach (var file in dropped)
                        {
                            if (!_fenceInfo.Items.Contains(file))
                            {
                                _fenceInfo.Items.Add(file);
                            }
                        }
                    }
                    else if (confirmation == DialogResult.Yes)
                    {
                        var path = _fenceInfo.Path;
                        _fenceInfo.Items = new List<string>();
                        _fenceInfo.Items.AddRange(Directory.GetFiles(path));
                        _fenceInfo.Items.AddRange(Directory.GetDirectories(path));
                        _fenceInfo.Path = null;

                        foreach (var file in dropped)
                        {
                            if (!_fenceInfo.Items.Contains(file))
                            {
                                _fenceInfo.Items.Add(file);
                            }
                        }
                    }
                }
            }
        }

        public void DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        public void KeyUp(object sender, KeyEventArgs e)
        {
            if (selectedItem != null)
            {
                if (e.KeyCode == Keys.Delete)
                {
                    _fenceInfo.Items.Remove(selectedItem);
                }
            }
        }
    }
}
