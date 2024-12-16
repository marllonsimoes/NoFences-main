using NoFences.Model;
using NoFences.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace NoFences
{
    public partial class EditDialog : Form
    {
        public EditDialog(FenceInfo fenceInfo)
        {
            InitializeComponent();
            List<String> typeValues = new List<string>();
            foreach (var entry in Enum.GetValues(typeof(EntryType)))
            {
                typeValues.Add(entry.ToString());
            }
            cbType.Items.AddRange(typeValues.ToArray());

            tbName.Text = fenceInfo.Name;
            titleSize.Value = fenceInfo.TitleHeight;
            cbType.SelectedItem = fenceInfo.Type.ToString();
            linkLabel1.Text = fenceInfo.Folder != null && fenceInfo.Folder != "" ? fenceInfo.Folder : "(click to select)";
            interval.Value = fenceInfo.Interval < 60000 ? fenceInfo.Interval/1000 : fenceInfo.Interval/60_000;
            intervalTimeUnit.SelectedIndex = fenceInfo.Interval < 60000 ? 0 : 1;

            toolTip1.IsBalloon = true;
            toolTip1.SetToolTip(button3, "The filter migh be a file name or part of the name. \r\nIt will also work or shortcuts where if the shortcut is a link that matches the name, it will be listed.\r\nE.g.: \r\n- steamapps://* - will list all steam application from the given folder.\r\n- *.(doc|docx) - will list all word documents from the given folder.");
            intervalTimeUnit.SelectedIndex = 0;

            list = new BindingList<String>();
            list.AllowNew = true;
            list.AllowRemove = true;
            list.AllowEdit = false;
            list.RaiseListChangedEvents = true;

            list.AddingNew += List_AddingNew;

            if (fenceInfo.Patterns != null && fenceInfo.Patterns.Count > 0)
            {
                delPatternButton.Enabled = true;
            }
            foreach (var pattern in fenceInfo.Patterns)
            {
                list.Add(pattern);
            }

            patternList.DataSource = list;

        }

        private BindingList<string> list;

        public FenceInfo GetFenceInfoEdited()
        {
            var patterns = new List<string>();
            foreach (var p in list.ToList())
            {
                if (p.StartsWith("*."))
                {
                    patterns.Add(".*\\." + p.Substring(2));
                } else if (p.StartsWith("*")) {
                    patterns.Add(".*" + p.Substring(1));
                }
                else
                {
                    patterns.Add(p);
                }
            }

            return new FenceInfo()
            {
                Name = tbName.Text,
                Folder = linkLabel1.Text.Equals("(click to select)") ? "" : linkLabel1.Text,
                Type = cbType.SelectedItem.ToString(),
                TitleHeight = ((int)titleSize.Value),
                Patterns = patterns,
                Interval = interval.Value * intervalTimeUnit.SelectedIndex == 0 ? 1000 : 60_000
            };
        }

        private void List_AddingNew(object sender, AddingNewEventArgs e)
        {
            e.NewObject = pattern.Text;
            pattern.Clear();
            delPatternButton.Enabled = true;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var selectFolder = selectFolderDialog.ShowDialog(this);
            if (selectFolder == DialogResult.OK)
            {
                linkLabel1.Text = selectFolderDialog.SelectedPath;
            }
        }

        private void cbType_SelectedValueChanged(object sender, EventArgs e)
        {
            var selectedItem = cbType.SelectedItem.ToString();

            if (selectedItem == EntryType.Folder.ToString()) {
                folderPanel.Visible = true;
                interval.Value = 20;
                intervalTimeUnit.SelectedIndex = 0;
            } else { 
                folderPanel.Visible = false;
            }

            if (selectedItem == EntryType.Picture.ToString())
            {
                intervalPanel.Visible = true;
                linkLabel1.Text = "(click to select)";
                if (list != null) list.Clear();
            }
            else { 
                intervalPanel.Visible = false; 
            }
        }

        private void delPatternButton_Click(object sender, EventArgs e)
        {
            foreach (int index in patternList.SelectedIndices)
            {
                list.RemoveAt(index);
            }

            if (list.Count == 0)
            {
                delPatternButton.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            list.AddNew();
        }

        private void EditDialog_Load(object sender, EventArgs e)
        {

        }
    }
}
