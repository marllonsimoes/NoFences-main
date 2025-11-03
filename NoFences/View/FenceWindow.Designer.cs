namespace NoFences
{
    partial class FenceWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.appContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.lockedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.minifyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editFenceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.newFenceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.titleSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.appContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // appContextMenu
            // 
            this.appContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lockedToolStripMenuItem,
            this.minifyToolStripMenuItem,
            this.editFenceToolStripMenuItem,
            this.toolStripSeparator1,
            this.newFenceToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.appContextMenu.Name = "contextMenuStrip1";
            this.appContextMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.appContextMenu.Size = new System.Drawing.Size(152, 120);
            // 
            // lockedToolStripMenuItem
            // 
            this.lockedToolStripMenuItem.CheckOnClick = true;
            this.lockedToolStripMenuItem.Name = "lockedToolStripMenuItem";
            this.lockedToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.lockedToolStripMenuItem.Text = "Lock";
            this.lockedToolStripMenuItem.Click += new System.EventHandler(this.lockedToolStripMenuItem_Click);
            // 
            // minifyToolStripMenuItem
            // 
            this.minifyToolStripMenuItem.CheckOnClick = true;
            this.minifyToolStripMenuItem.Name = "minifyToolStripMenuItem";
            this.minifyToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.minifyToolStripMenuItem.Text = "Minify";
            this.minifyToolStripMenuItem.Click += new System.EventHandler(this.minifyToolStripMenuItem_Click);
            // 
            // editFenceToolStripMenuItem
            // 
            this.editFenceToolStripMenuItem.Name = "editFenceToolStripMenuItem";
            this.editFenceToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.editFenceToolStripMenuItem.Text = "Edit fence...";
            this.editFenceToolStripMenuItem.Click += new System.EventHandler(this.editFenceToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(148, 6);
            // 
            // newFenceToolStripMenuItem
            // 
            this.newFenceToolStripMenuItem.Name = "newFenceToolStripMenuItem";
            this.newFenceToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.newFenceToolStripMenuItem.Text = "New Fence";
            this.newFenceToolStripMenuItem.Click += new System.EventHandler(this.newFenceToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.exitToolStripMenuItem.Text = "Remove Fence";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // titleSizeToolStripMenuItem
            // 
            this.titleSizeToolStripMenuItem.Name = "titleSizeToolStripMenuItem";
            this.titleSizeToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.titleSizeToolStripMenuItem.Text = "Title size...";
            // 
            // FenceWindow
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(338, 110);
            this.DoubleBuffered = true;
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MinimizeBox = false;
            this.Name = "FenceWindow";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "New Fence";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FenceWindow_FormClosed);
            this.Load += new System.EventHandler(this.FenceWindow_Load);
            this.LocationChanged += new System.EventHandler(this.FenceWindow_LocationChanged);
            this.Click += new System.EventHandler(this.FenceWindow_Click);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.FenceWindow_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.FenceWindow_DragEnter);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.FenceWindow_Paint);
            this.DoubleClick += new System.EventHandler(this.FenceWindow_DoubleClick);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FenceWindow_KeyUp);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.FenceWindow_MouseClick);
            this.MouseEnter += new System.EventHandler(this.FenceWindow_MouseEnter);
            this.MouseLeave += new System.EventHandler(this.FenceWindow_MouseLeave);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FenceWindow_MouseMove);
            this.Resize += new System.EventHandler(this.FenceWindow_Resize);
            this.appContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip appContextMenu;
        private System.Windows.Forms.ToolStripMenuItem lockedToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minifyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editFenceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newFenceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem titleSizeToolStripMenuItem;
    }
}

