using NoFences.Model;
using System;
using System.Collections.Generic;

namespace NoFences
{
    partial class EditDialog
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
            this.lbName = new System.Windows.Forms.Label();
            this.tbName = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.labelTitleHeight = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.titleSize = new System.Windows.Forms.NumericUpDown();
            this.cbType = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.selectFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.folderPanel = new System.Windows.Forms.Panel();
            this.button3 = new System.Windows.Forms.Button();
            this.delPatternButton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.pattern = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.patternList = new System.Windows.Forms.ListBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.label3 = new System.Windows.Forms.Label();
            this.selectFileDialot = new System.Windows.Forms.OpenFileDialog();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.intervalPanel = new System.Windows.Forms.Panel();
            this.intervalTimeUnit = new System.Windows.Forms.ComboBox();
            this.interval = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.titleSize)).BeginInit();
            this.folderPanel.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.intervalPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.interval)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbName
            // 
            this.lbName.AutoSize = true;
            this.lbName.BackColor = System.Drawing.Color.Transparent;
            this.lbName.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lbName.Location = new System.Drawing.Point(96, 18);
            this.lbName.Name = "lbName";
            this.lbName.Size = new System.Drawing.Size(30, 13);
            this.lbName.TabIndex = 0;
            this.lbName.Text = "Title:";
            // 
            // tbName
            // 
            this.tbName.Location = new System.Drawing.Point(139, 15);
            this.tbName.Name = "tbName";
            this.tbName.Size = new System.Drawing.Size(228, 20);
            this.tbName.TabIndex = 1;
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnOk.Location = new System.Drawing.Point(261, 3);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(56, 23);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(323, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(56, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // labelTitleHeight
            // 
            this.labelTitleHeight.AutoSize = true;
            this.labelTitleHeight.Location = new System.Drawing.Point(214, 42);
            this.labelTitleHeight.Name = "labelTitleHeight";
            this.labelTitleHeight.Size = new System.Drawing.Size(18, 13);
            this.labelTitleHeight.TabIndex = 9;
            this.labelTitleHeight.Text = "px";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label1.Location = new System.Drawing.Point(83, 44);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Title size:";
            // 
            // titleSize
            // 
            this.titleSize.Location = new System.Drawing.Point(139, 40);
            this.titleSize.Minimum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.titleSize.Name = "titleSize";
            this.titleSize.Size = new System.Drawing.Size(60, 20);
            this.titleSize.TabIndex = 12;
            this.titleSize.Value = new decimal(new int[] {
            16,
            0,
            0,
            0});
            // 
            // cbType
            // 
            this.cbType.FormattingEnabled = true;
            this.cbType.Location = new System.Drawing.Point(139, 66);
            this.cbType.Name = "cbType";
            this.cbType.Size = new System.Drawing.Size(102, 21);
            this.cbType.TabIndex = 13;
            this.cbType.SelectedValueChanged += new System.EventHandler(this.cbType_SelectedValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label2.Location = new System.Drawing.Point(100, 69);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "Type:";
            // 
            // folderPanel
            // 
            this.folderPanel.Controls.Add(this.button3);
            this.folderPanel.Controls.Add(this.delPatternButton);
            this.folderPanel.Controls.Add(this.button1);
            this.folderPanel.Controls.Add(this.pattern);
            this.folderPanel.Controls.Add(this.label5);
            this.folderPanel.Controls.Add(this.patternList);
            this.folderPanel.Controls.Add(this.linkLabel1);
            this.folderPanel.Controls.Add(this.label3);
            this.folderPanel.Location = new System.Drawing.Point(3, 36);
            this.folderPanel.Name = "folderPanel";
            this.folderPanel.Size = new System.Drawing.Size(398, 161);
            this.folderPanel.TabIndex = 15;
            this.folderPanel.Visible = false;
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.Color.CornflowerBlue;
            this.button3.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.button3.FlatAppearance.BorderSize = 2;
            this.button3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button3.ForeColor = System.Drawing.Color.White;
            this.button3.Location = new System.Drawing.Point(369, 25);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(19, 23);
            this.button3.TabIndex = 21;
            this.button3.Text = "?";
            this.button3.UseVisualStyleBackColor = false;
            // 
            // delPatternButton
            // 
            this.delPatternButton.Enabled = false;
            this.delPatternButton.Location = new System.Drawing.Point(323, 54);
            this.delPatternButton.Name = "delPatternButton";
            this.delPatternButton.Size = new System.Drawing.Size(56, 23);
            this.delPatternButton.TabIndex = 20;
            this.delPatternButton.Text = "Remove";
            this.delPatternButton.UseVisualStyleBackColor = true;
            this.delPatternButton.Click += new System.EventHandler(this.delPatternButton_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(323, 26);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(40, 23);
            this.button1.TabIndex = 19;
            this.button1.Text = "Add";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // pattern
            // 
            this.pattern.Location = new System.Drawing.Point(135, 27);
            this.pattern.Name = "pattern";
            this.pattern.Size = new System.Drawing.Size(182, 20);
            this.pattern.TabIndex = 18;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label5.Location = new System.Drawing.Point(42, 26);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(84, 13);
            this.label5.TabIndex = 18;
            this.label5.Text = "File filter pattern:";
            // 
            // patternList
            // 
            this.patternList.FormattingEnabled = true;
            this.patternList.Location = new System.Drawing.Point(135, 54);
            this.patternList.Name = "patternList";
            this.patternList.Size = new System.Drawing.Size(182, 82);
            this.patternList.TabIndex = 17;
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(132, 7);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(78, 13);
            this.linkLabel1.TabIndex = 16;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "(click to select)";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label3.Location = new System.Drawing.Point(91, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(39, 13);
            this.label3.TabIndex = 15;
            this.label3.Text = "Folder:";
            // 
            // selectFileDialot
            // 
            this.selectFileDialot.FileName = "openFileDialog1";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.intervalPanel);
            this.flowLayoutPanel1.Controls.Add(this.folderPanel);
            this.flowLayoutPanel1.Controls.Add(this.panel1);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(1, 91);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(404, 237);
            this.flowLayoutPanel1.TabIndex = 17;
            // 
            // intervalPanel
            // 
            this.intervalPanel.Controls.Add(this.intervalTimeUnit);
            this.intervalPanel.Controls.Add(this.interval);
            this.intervalPanel.Controls.Add(this.label4);
            this.intervalPanel.Location = new System.Drawing.Point(3, 3);
            this.intervalPanel.Name = "intervalPanel";
            this.intervalPanel.Size = new System.Drawing.Size(398, 27);
            this.intervalPanel.TabIndex = 16;
            this.intervalPanel.Visible = false;
            // 
            // intervalTimeUnit
            // 
            this.intervalTimeUnit.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.intervalTimeUnit.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.intervalTimeUnit.FormattingEnabled = true;
            this.intervalTimeUnit.Items.AddRange(new object[] {
            "Second(s)",
            "Minute(s)"});
            this.intervalTimeUnit.Location = new System.Drawing.Point(201, 2);
            this.intervalTimeUnit.Name = "intervalTimeUnit";
            this.intervalTimeUnit.Size = new System.Drawing.Size(102, 21);
            this.intervalTimeUnit.TabIndex = 14;
            // 
            // interval
            // 
            this.interval.Location = new System.Drawing.Point(135, 3);
            this.interval.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.interval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.interval.Name = "interval";
            this.interval.Size = new System.Drawing.Size(60, 20);
            this.interval.TabIndex = 13;
            this.interval.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label4.Location = new System.Drawing.Point(35, 5);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(95, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Slideshow interval:";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnOk);
            this.panel1.Controls.Add(this.btnCancel);
            this.panel1.Location = new System.Drawing.Point(3, 203);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(398, 31);
            this.panel1.TabIndex = 17;
            // 
            // EditDialog
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(405, 347);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cbType);
            this.Controls.Add(this.titleSize);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.labelTitleHeight);
            this.Controls.Add(this.tbName);
            this.Controls.Add(this.lbName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Rename";
            this.Load += new System.EventHandler(this.EditDialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.titleSize)).EndInit();
            this.folderPanel.ResumeLayout(false);
            this.folderPanel.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.intervalPanel.ResumeLayout(false);
            this.intervalPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.interval)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbName;
        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label labelTitleHeight;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown titleSize;
        private System.Windows.Forms.ComboBox cbType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.FolderBrowserDialog selectFolderDialog;
        private System.Windows.Forms.Panel folderPanel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.OpenFileDialog selectFileDialot;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Panel intervalPanel;
        private System.Windows.Forms.NumericUpDown interval;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox intervalTimeUnit;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button delPatternButton;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox pattern;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox patternList;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button button3;
    }
}