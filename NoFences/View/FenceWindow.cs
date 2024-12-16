using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;
using NoFencesCore;
using NoFencesCore.Util;
using Peter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static NoFences.Win32.WindowUtil;

namespace NoFences
{
    public partial class FenceWindow : Form
    {
        private int logicalTitleHeight;
        private int titleHeight;
        private const int titleOffset = 3;
        private const int itemWidth = 75;
        private const int itemHeight = 32 + itemPadding + textHeight;
        private const int textHeight = 35;
        private const int itemPadding = 15;
        private const float shadowDist = 1.5f;

        private readonly FenceInfo fenceInfo;
        private string[] validExtensionsForSlideshow = new string[] { ".jpg", ".png", ".gif", ".jpeg", ".bpm" };

        private Font titleFont;
        private Font iconFont;

        private Timer timer;
        private string currentPicture;

        private string selectedItem;
        private string hoveringItem;
        private bool shouldUpdateSelection;
        private bool shouldRunDoubleClick;
        private bool hasSelectionUpdated;
        private bool hasHoverUpdated;
        private bool isMinified;
        private int prevHeight;

        private int scrollHeight;
        private int scrollOffset;

        private readonly ThrottledExecution throttledMove = new ThrottledExecution(TimeSpan.FromSeconds(4));
        private readonly ThrottledExecution throttledResize = new ThrottledExecution(TimeSpan.FromSeconds(4));
        private readonly ShellContextMenu shellContextMenu = new ShellContextMenu();
        private readonly ThumbnailProvider thumbnailProvider = new ThumbnailProvider();

        private List<string> allowedDropEntryTypes = new List<string>(new string[] {EntryType.Picture.ToString(), EntryType.File.ToString(), EntryType.Folder.ToString(), EntryType.SlideShow.ToString() });

        private void ReloadFonts()
        {
            var family = new FontFamily("Segoe UI");
            titleFont = new Font(family, (int)Math.Floor(logicalTitleHeight / 2.0));
            iconFont = new Font(family, 9);
        }

        public FenceWindow(FenceInfo fenceInfo)
        {
            InitializeComponent();
            DropShadow.ApplyShadows(this);
            BlurUtil.EnableBlur(Handle);
            WindowUtil.HideFromAltTab(Handle);
            DesktopUtil.GlueToDesktop(Handle);
            DesktopUtil.PreventMinimize(Handle);
            logicalTitleHeight = (fenceInfo.TitleHeight < 16 || fenceInfo.TitleHeight > 100) ? 35 : fenceInfo.TitleHeight;
            titleHeight = LogicalToDeviceUnits(logicalTitleHeight);
            
            this.MouseWheel += FenceWindow_MouseWheel;
            thumbnailProvider.IconThumbnailLoaded += ThumbnailProvider_IconThumbnailLoaded;

            ReloadFonts();

            AllowDrop = true;

            this.fenceInfo = fenceInfo;
            Text = fenceInfo.Name;
            Location = new Point(fenceInfo.PosX, fenceInfo.PosY);

            Width = fenceInfo.Width;
            Height = fenceInfo.Height;

            prevHeight = Height;
            lockedToolStripMenuItem.Checked = fenceInfo.Locked;
            minifyToolStripMenuItem.Checked = fenceInfo.CanMinify;
            Minify();

            timer = new Timer();
            InitializePictureFence();
            InitializeFolderFence();

        }

        private void InitializeFolderFence()
        {
            if (fenceInfo.Type == EntryType.Folder.ToString())
            {
                timer = new Timer();
                timer.Interval = 30_000;
                timer.Tick += Timer_Tick;
                timer.Enabled = true;
                timer.Start();
            }
        }

        private void InitializePictureFence()
        {
            if (fenceInfo.Type == EntryType.Picture.ToString())
            {
                SetNextSlidePicture();
                if (fenceInfo.Files.Count > 1)
                {
                    timer.Interval = fenceInfo.Interval;
                    timer.Tick += Timer_Tick;
                    timer.Enabled = true;
                    timer.Start();
                }
            }
        }

        private void SetNextSlidePicture()
        {
            if (fenceInfo.Files.Count != 0)
            {
                var nextPic = fenceInfo.Files.Count > 1 ? new Random().Next(0, fenceInfo.Files.Count() - 1) : 0;
                currentPicture = fenceInfo.Files.ElementAt(nextPic);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (fenceInfo.Type == EntryType.Picture.ToString())
            {
                SetNextSlidePicture();
            }
            Refresh();
        }

        protected override void WndProc(ref Message m)
        {

            // Remove border
            if (m.Msg == 0x0083)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            // Mouse leave
            var myrect = new Rectangle(Location, Size);
            if (m.Msg == 0x02a2 && !myrect.IntersectsWith(new Rectangle(MousePosition, new Size(1, 1))))
            {
                Minify();
            }

            // Prevent maximize
            if ((m.Msg == WM_SYSCOMMAND) && m.WParam.ToInt32() == 0xF032)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            // Prevent foreground
            if (m.Msg == WM_SETFOCUS)
            {
                SetWindowPos(Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                return;
            }

            // Other messages
            base.WndProc(ref m);

            // If not locked and using the left mouse button
            if (MouseButtons == MouseButtons.Right || lockedToolStripMenuItem.Checked)
                return;

            // Then, allow dragging and resizing
            if (m.Msg == WM_NCHITTEST)
            {

                var pt = PointToClient(new Point(m.LParam.ToInt32()));

                if ((int)m.Result == HTCLIENT && pt.Y < titleHeight)     // drag the form
                {
                    m.Result = (IntPtr)HTCAPTION;
                    FenceWindow_MouseEnter(null, null);
                }

                if (pt.X < 10 && pt.Y < 10)
                    m.Result = new IntPtr(HTTOPLEFT);
                else if (pt.X > (Width - 10) && pt.Y < 10)
                    m.Result = new IntPtr(HTTOPRIGHT);
                else if (pt.X < 10 && pt.Y > (Height - 10))
                    m.Result = new IntPtr(HTBOTTOMLEFT);
                else if (pt.X > (Width - 10) && pt.Y > (Height - 10))
                    m.Result = new IntPtr(HTBOTTOMRIGHT);
                else if (pt.Y > (Height - 10))
                    m.Result = new IntPtr(HTBOTTOM);
                else if (pt.X < 10)
                    m.Result = new IntPtr(HTLEFT);
                else if (pt.X > (Width - 10))
                    m.Result = new IntPtr(HTRIGHT);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Really remove this fence? Your files will not be deleted.", "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (timer.Enabled)
                {
                    timer.Enabled = false;
                    timer.Stop();
                    timer.Dispose();
                }
                FenceManager.Instance.RemoveFence(fenceInfo);
                Close();
            }
        }

        private void deleteItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fenceInfo.Files.Remove(hoveringItem);
            hoveringItem = null;
            Save();
            Refresh();
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            deleteItemToolStripMenuItem.Visible = hoveringItem != null;
        }

        private void FenceWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (allowedDropEntryTypes.Contains(fenceInfo.Type) && e.Data.GetDataPresent(DataFormats.FileDrop) && !lockedToolStripMenuItem.Checked) {

                var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (fenceInfo.Type == EntryType.Picture.ToString() && dropped.Length > 1)
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }
                foreach (var file in dropped)
                {
                    if (fenceInfo.Type == EntryType.Picture.ToString())
                    {
                        var fileLowercase = Path.GetExtension(file).ToLower();
                        if (!File.Exists(file) || 
                            !validExtensionsForSlideshow.Contains(fileLowercase))
                        {
                            e.Effect = DragDropEffects.None;
                            return;
                        }
                    }
                    if (fenceInfo.Type == EntryType.Folder.ToString() || fenceInfo.Type == EntryType.SlideShow.ToString())
                    {
                        if (!Directory.Exists(file)) {
                            e.Effect = DragDropEffects.None; 
                            return; 
                        }
                    }
                }
                e.Effect = DragDropEffects.Move;
            } else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void FenceWindow_DragDrop(object sender, DragEventArgs e)
        {
            var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (fenceInfo.Files.Count == 0)
            {
                if (dropped.Count() == 1)
                {

                    if (Directory.Exists(dropped[0]) && !fenceInfo.Type.Equals(EntryType.Folder.ToString()))
                    {
                        if (MessageBox.Show("It seems you are dropping a folder to this fence.\n Would you like to see it's contents here?", "Show folder's content?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            fenceInfo.Type = EntryType.Folder.ToString();
                            fenceInfo.Folder = dropped[0];
                        }
                    }
                }
                int countImages = dropped.Count(filename => File.Exists(filename) && validExtensionsForSlideshow.Contains(Path.GetExtension(filename).ToLower()));
                if (countImages == dropped.Count() && !fenceInfo.Type.Equals(EntryType.Picture.ToString()))
                {
                    if (MessageBox.Show("It seems you are dropping only images to this fence, but this fence will display only icons.\nWould you like to see the pictures instead?", "Change Fence type?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        fenceInfo.Type = EntryType.Picture.ToString();
                    }
                }
            }

            if (!fenceInfo.Type.Equals(EntryType.Folder.ToString()))
            {
                foreach (var file in dropped)
                {
                    if (!fenceInfo.Files.Contains(file) && ItemExists(file))
                    {
                        fenceInfo.Files.Add(file);
                    }
                }
            }
            Save();
            Refresh();
            InitializePictureFence();
            InitializeFolderFence();
        }

        private void FenceWindow_Resize(object sender, EventArgs e)
        {
            throttledResize.Run(() =>
            {
                fenceInfo.Width = Width;
                fenceInfo.Height = isMinified ? prevHeight : Height;
                Save();
            });

            Refresh();
        }

        private void FenceWindow_MouseMove(object sender, MouseEventArgs e)
        {
            Refresh();
        }

        private void FenceWindow_MouseEnter(object sender, EventArgs e)
        {
            if (minifyToolStripMenuItem.Checked && isMinified)
            {
                isMinified = false;
                Height = prevHeight;
            }
        }

        private void FenceWindow_MouseLeave(object sender, EventArgs e)
        {
            Minify();
            selectedItem = null;            
            Refresh();
        }

        private void Minify()
        {
            if (minifyToolStripMenuItem.Checked && !isMinified)
            {
                isMinified = true;
                prevHeight = Height;
                Height = titleHeight;
                Refresh();
            }
        }

        private void minifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isMinified)
            {
                Height = prevHeight;
                isMinified = false;
            }
            fenceInfo.CanMinify = minifyToolStripMenuItem.Checked;
            Save();

        }

        private void FenceWindow_Click(object sender, EventArgs e)
        {
            shouldUpdateSelection = true;
            Refresh();
        }

        private void FenceWindow_DoubleClick(object sender, EventArgs e)
        {
            shouldRunDoubleClick = true;
            Refresh();
        }

        private void FenceWindow_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clip = new Region(ClientRectangle);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Background
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(100, Color.Black)), ClientRectangle);

            // Title
            e.Graphics.DrawString(Text, titleFont, Brushes.White, new PointF(Width / 2, titleOffset), new StringFormat { Alignment = StringAlignment.Center });
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(50, Color.Black)), new RectangleF(0, 0, Width, titleHeight));

            // Items
            var x = itemPadding;
            var y = itemPadding;
            scrollHeight = 0;
            e.Graphics.Clip = new Region(new Rectangle(0, titleHeight, Width, Height - titleHeight));
            if (fenceInfo.Type == EntryType.File.ToString() || fenceInfo.Type == EntryType.Folder.ToString())
            {
                var files = fenceInfo.Type == EntryType.Folder.ToString() ? Directory.GetFileSystemEntries(fenceInfo.Folder) : fenceInfo.Files.ToArray();

                var patternFilteredList = new List<string>();

                if (fenceInfo.Patterns != null && fenceInfo.Patterns.Count > 0)
                {
                    foreach (string pattern in fenceInfo.Patterns)
                    {
                        patternFilteredList.AddRange(files.Where(p =>
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
                files = patternFilteredList.Count > 0 ? patternFilteredList.ToArray() : files;

                foreach (var file in files)
                {
                    var entry = FenceEntry.FromPath(file);
                    if (entry == null)
                        continue;

                    RenderEntry(e.Graphics, entry, x, y + titleHeight - scrollOffset);

                    var itemBottom = y + itemHeight;
                    if (itemBottom > scrollHeight)
                        scrollHeight = itemBottom;

                    x += itemWidth + itemPadding;
                    if (x + itemWidth > Width)
                    {
                        x = itemPadding;
                        y += itemHeight + itemPadding;
                    }
                }
            }
            if (fenceInfo.Type == EntryType.Picture.ToString())
            {
                RenderImage(e.Graphics, currentPicture);
            }

            scrollHeight -= (ClientRectangle.Height - titleHeight);

            // Scroll bars
            if (scrollHeight > 0)
            {
                var contentHeight = Height - titleHeight;
                var scrollbarHeight = contentHeight - scrollHeight;
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(150, Color.Black)), new Rectangle(Width - 5, titleHeight + scrollOffset, 5, scrollbarHeight));

                scrollOffset = Math.Min(scrollOffset, scrollHeight);
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

        private void RenderImage(Graphics g, string pictureUrl)
        {
            if (pictureUrl != null && pictureUrl.Length > 0)
            {
                g.Clear(Color.Transparent);
                Image img = Image.FromFile(pictureUrl);
                ExifRotate(img);

                var ratioX = (double)Width / img.Width;
                var ratioY = (double)Height / img.Height;
                var ratio = Math.Min(ratioX, ratioY);

                var newWidth = (int)(img.Width * ratio);
                var newHeight = (int)(img.Height * ratio);
                var newX = (Width - newWidth) / 2;
                var newY = (Height - newHeight) / 2;

                g.DrawImage(img, newX, newY + fenceInfo.TitleHeight, newWidth, newHeight);
                img.Dispose();
            }
        }

        // stackoverflow :https://stackoverflow.com/questions/27835064/get-image-orientation-and-rotate-as-per-orientation
        private readonly int exifOrientationID = 0x112; //274
        private void ExifRotate(Image img)
        {
            if (!img.PropertyIdList.Contains(exifOrientationID))
                return;

            var prop = img.GetPropertyItem(exifOrientationID);
            int val = BitConverter.ToUInt16(prop.Value, 0);
            var rot = RotateFlipType.RotateNoneFlipNone;

            if (val == 3 || val == 4)
                rot = RotateFlipType.Rotate180FlipNone;
            else if (val == 5 || val == 6)
                rot = RotateFlipType.Rotate90FlipNone;
            else if (val == 7 || val == 8)
                rot = RotateFlipType.Rotate270FlipNone;

            if (val == 2 || val == 4 || val == 5 || val == 7)
                rot |= RotateFlipType.RotateNoneFlipX;

            if (rot != RotateFlipType.RotateNoneFlipNone)
            {
                img.RotateFlip(rot);
                img.RemovePropertyItem(exifOrientationID);
            }
        }

        private void RenderEntry(Graphics g, FenceEntry entry, int x, int y)
        {

            var icon = entry.ExtractIcon(thumbnailProvider);
            var name = entry.Name;

            var textPosition = new PointF(x, y + icon.Height + 5);
            var textMaxSize = new SizeF(itemWidth, textHeight);

            var stringFormat = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

            var textSize = g.MeasureString(name, iconFont, textMaxSize, stringFormat);
            var outlineRect = new Rectangle(x - 2, y - 2, itemWidth + 2, icon.Height + (int)textSize.Height + 5 + 2);
            var outlineRectInner = outlineRect.Shrink(1);

            var mousePos = PointToClient(MousePosition);
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
                entry.Open();
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

        private void editFenceToolStripMenuItem_Click(object sender, EventArgs e)
        {

            var dialog = new EditDialog(fenceInfo);


            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                FenceInfo editedFence = dialog.GetFenceInfoEdited();

                fenceInfo.Name = editedFence.Name;

                fenceInfo.TitleHeight = editedFence.TitleHeight;
                logicalTitleHeight = editedFence.TitleHeight;
                fenceInfo.Type = editedFence.Type;
                fenceInfo.Folder = editedFence.Folder;
                fenceInfo.Patterns = editedFence.Patterns;
                fenceInfo.Interval = editedFence.Interval;

                titleHeight = LogicalToDeviceUnits(logicalTitleHeight);

                ReloadFonts();
                Minify();
                if (isMinified)
                {
                    Height = titleHeight;
                }

                InitializePictureFence();
                InitializeFolderFence();
                Refresh();
                Save();
            }
        }

        private void newFenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FenceManager.Instance.CreateFence("New fence");
        }

        private void FenceWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Application.OpenForms.Count == 0)
                Application.Exit();
        }

        private readonly object saveLock = new object();
        private void Save()
        {
            lock (saveLock)
            {
                FenceManager.Instance.UpdateFence(fenceInfo);
            }
        }

        private void FenceWindow_LocationChanged(object sender, EventArgs e)
        {
            throttledMove.Run(() =>
            {
                fenceInfo.PosX = Location.X;
                fenceInfo.PosY = Location.Y;
                Save();
            });
        }

        private void lockedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fenceInfo.Locked = lockedToolStripMenuItem.Checked;
            Save();
        }

        private void FenceWindow_Load(object sender, EventArgs e)
        {

        }

        private void FenceWindow_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            if (hoveringItem != null && !ModifierKeys.HasFlag(Keys.Shift))
            {
                shellContextMenu.ShowContextMenu(new[] { new FileInfo(hoveringItem) }, MousePosition);
            }
            else
            {
                appContextMenu.Show(this, e.Location);
            }
        }

        private void FenceWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            if (scrollHeight < 1)
                return;

            scrollOffset -= Math.Sign(e.Delta) * 10;
            if (scrollOffset < 0)
                scrollOffset = 0;
            if (scrollOffset > scrollHeight)
                scrollOffset = scrollHeight;

            Invalidate();
        }

        private void ThumbnailProvider_IconThumbnailLoaded(object sender, EventArgs e)
        {
            Invalidate();
        }

        private bool ItemExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        private void splitContainer1_Paint(object sender, PaintEventArgs e)
        {
            this.FenceWindow_Paint(sender, e);
        }
    }
}

