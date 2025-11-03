using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;
using Peter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NoFences.View.Fences.Handlers;

namespace NoFences
{
    public partial class FenceWindow : Form
    {
        private int logicalTitleHeight;
        public int titleHeight;
        private const int titleOffset = 3;

        public readonly FenceInfo fenceInfo;
        
        private Font titleFont;

        private bool isMinified;
        private int prevHeight;

        public int scrollHeight;
        public int scrollOffset;

        private readonly object saveLock = new object();

        private readonly ThrottledExecution throttledMove = new ThrottledExecution(TimeSpan.FromSeconds(4));
        private readonly ThrottledExecution throttledResize = new ThrottledExecution(TimeSpan.FromSeconds(4));

        private IFenceHandler _fenceHandler;

        private void ReloadFonts()
        {
            var family = new FontFamily("Segoe UI");
            titleFont = new Font(family, (int)Math.Floor(logicalTitleHeight / 2.0));
        }

        public FenceWindow(FenceInfo fenceInfo, FenceHandlerFactory fenceHandlerFactory)
        {
            InitializeComponent();
            DropShadow.ApplyShadows(this);
            BlurUtil.EnableBlur(Handle);
            WindowUtil.HideFromAltTab(Handle);
            DesktopUtil.GlueToDesktop(Handle);
            DesktopUtil.PreventMinimize(Handle);
            logicalTitleHeight = (fenceInfo.TitleHeight < 16 || fenceInfo.TitleHeight > 100) ? 35 : fenceInfo.TitleHeight;
            titleHeight = LogicalToDeviceUnits(logicalTitleHeight);

            MouseWheel += FenceWindow_MouseWheel;

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

            _fenceHandler = fenceHandlerFactory.CreateFenceHandler(fenceInfo);
            _fenceHandler.Initialize(this);
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
            if ((m.Msg == WindowUtil.WM_SYSCOMMAND) && m.WParam.ToInt32() == 0xF032)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            // Prevent foreground
            if (m.Msg == WindowUtil.WM_SETFOCUS)
            {
                WindowUtil.SetWindowPos(Handle, WindowUtil.HWND_BOTTOM, 0, 0, 0, 0, WindowUtil.SWP_NOSIZE | WindowUtil.SWP_NOMOVE | WindowUtil.SWP_NOACTIVATE);
                return;
            }

            // Other messages
            base.WndProc(ref m);

            // If not locked and using the left mouse button
            if (MouseButtons == MouseButtons.Right || lockedToolStripMenuItem.Checked)
                return;

            // Then, allow dragging and resizing
            if (m.Msg == WindowUtil.WM_NCHITTEST)
            {
                var pt = PointToClient(new Point(m.LParam.ToInt32()));

                if ((int)m.Result == WindowUtil.HTCLIENT && pt.Y < titleHeight) // drag the form
                {
                    m.Result = (IntPtr)WindowUtil.HTCAPTION;
                    FenceWindow_MouseEnter(null, null);
                }

                if (pt.X < 10 && pt.Y < 10)
                    m.Result = new IntPtr(WindowUtil.HTTOPLEFT);
                else if (pt.X > (Width - 10) && pt.Y < 10)
                    m.Result = new IntPtr(WindowUtil.HTTOPRIGHT);
                else if (pt.X < 10 && pt.Y > (Height - 10))
                    m.Result = new IntPtr(WindowUtil.HTBOTTOMLEFT);
                else if (pt.X > (Width - 10) && pt.Y > (Height - 10))
                    m.Result = new IntPtr(WindowUtil.HTBOTTOMRIGHT);
                else if (pt.Y > (Height - 10))
                    m.Result = new IntPtr(WindowUtil.HTBOTTOM);
                else if (pt.X < 10)
                    m.Result = new IntPtr(WindowUtil.HTLEFT);
                else if (pt.X > (Width - 10))
                    m.Result = new IntPtr(WindowUtil.HTRIGHT);
            }
        }

        #region Drag and drop event handlers for things inside the fences
        
        private void FenceWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (lockedToolStripMenuItem.Checked || !e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            _fenceHandler.DragEnter(sender, e);            
        }

        private void FenceWindow_DragDrop(object sender, DragEventArgs e)
        {
            _fenceHandler.DragDrop(sender, e);
            Save();
            Refresh();
        }
        
        #endregion

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

        private void FenceWindow_Click(object sender, EventArgs e)
        {
            _fenceHandler.Click(sender, e);
            Refresh();
        }

        private void FenceWindow_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            _fenceHandler.MouseClick(sender, e);
            appContextMenu.Show(this, e.Location);
        }

        private void FenceWindow_MouseMove(object sender, MouseEventArgs e)
        {
            _fenceHandler.MouseMove(sender, e);
            Refresh();
        }

        private void FenceWindow_MouseEnter(object sender, EventArgs e)
        {
            _fenceHandler.MouseEnter(sender, e);
            if (minifyToolStripMenuItem.Checked && isMinified)
            {
                isMinified = false;
                Height = prevHeight;
            }
        }

        private void FenceWindow_MouseLeave(object sender, EventArgs e)
        {   
            _fenceHandler.MouseLeave(sender, e);
            Minify();
            Refresh();
        }

        private void FenceWindow_DoubleClick(object sender, EventArgs e)
        {
            _fenceHandler.MouseDoubleClick(sender, e);
            Refresh();
        }

        private void FenceWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            if (scrollHeight < 1)
                return;

            scrollOffset -= Math.Sign(e.Delta) * 30;
            if (scrollOffset < 0)
                scrollOffset = 0;
            if (scrollOffset > scrollHeight)
                scrollOffset = scrollHeight;

            Invalidate();
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

            scrollHeight = 0;
            e.Graphics.Clip = new Region(new Rectangle(0, titleHeight, Width, Height - titleHeight));

            _fenceHandler?.Paint(e);

            scrollHeight -= (ClientRectangle.Height - titleHeight);

            // Scroll bars
            if (scrollHeight > 0)
            {
                var contentHeight = Height - titleHeight;
                var scrollbarHeight = contentHeight - scrollHeight;
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(150, Color.Black)), new Rectangle(Width - 5, titleHeight + scrollOffset, 5, scrollbarHeight));
                scrollOffset = Math.Min(scrollOffset, scrollHeight);
            }
        }

        private void FenceWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Application.OpenForms.Count == 0)
                Application.Exit();
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

        private void FenceWindow_Load(object sender, EventArgs e)
        {

        }

        #region Strip menu event handlers 

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
                fenceInfo.Path = editedFence.Path;
                fenceInfo.Filters = editedFence.Filters;
                fenceInfo.Interval = editedFence.Interval;

                titleHeight = LogicalToDeviceUnits(logicalTitleHeight);

                ReloadFonts();
                Minify();
                if (isMinified)
                {
                    Height = titleHeight;
                }

                var fenceHandlerFactory = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<FenceHandlerFactory>();
                _fenceHandler = fenceHandlerFactory.CreateFenceHandler(fenceInfo);
                _fenceHandler.Initialize(this);

                Save();
                Refresh();
            }
        }

        private void newFenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fenceManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<FenceManager>();
            fenceManager.CreateFence("New fence");
        }

        private void lockedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fenceInfo.Locked = lockedToolStripMenuItem.Checked;
            Save();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Really remove this fence? Your files will not be deleted.", "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var fenceManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<FenceManager>();
                fenceManager.RemoveFence(fenceInfo);
                Close();
            }
        }

        #endregion

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

        private void Save()
        {
            lock (saveLock)
            {
                var fenceManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<FenceManager>();
                fenceManager.UpdateFence(fenceInfo);
            }
        }

        private void FenceWindow_KeyUp(object sender, KeyEventArgs e)
        {
            _fenceHandler.KeyUp(sender, e);
            Save();
            Refresh();
        }
    }
}

