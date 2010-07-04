using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FFXI_ME_v2
{
    public partial class ExitAndSaveBox : Form
    {
        public ExitAndSaveBox(String title, String LabelText, String YesText, String NoText, String CancelText):this()
        {
            this.Text = title;
            this.YesButton.Text = YesText;
            this.NoButton.Text = NoText;
            this.CancelExitButton.Text = CancelText;
            if (CancelText == String.Empty)
            {
                this.CancelExitButton.Visible = false;
            }
            if (NoText == String.Empty)
            {
                this.NoButton.Visible = false;
            }

            this.label1.Text = LabelText;
        }

        public ExitAndSaveBox()
        {
            InitializeComponent();
        }

        public void SelectAll()
        {
            for (int i = 0; i < this.checkedListBox1.Items.Count; i++)
            {
                this.checkedListBox1.SetItemChecked(i, true);
            }
        }

        public void DeSelectAll()
        {
            for (int i = 0; i < this.checkedListBox1.Items.Count; i++)
            {
                this.checkedListBox1.SetItemChecked(i, false);
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SelectAll();
        }

        private void deselectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DeSelectAll();
        }

        private void selectMacroFilesOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DeSelectAll();
            for (int i = 0; i < this.checkedListBox1.Items.Count; i++)
            {
                MainForm.TagInfo cb = this.checkedListBox1.Items[i] as MainForm.TagInfo;
                if ((cb.Type == "Save_File") || (cb.Type == "Delete_File"))
                    this.checkedListBox1.SetItemChecked(i, true);
            }
        }

        private void selectBooksOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DeSelectAll();
            for (int i = 0; i < this.checkedListBox1.Items.Count; i++)
            {
                MainForm.TagInfo cb = this.checkedListBox1.Items[i] as MainForm.TagInfo;
                if ((cb.Type == "Overwrite_TTL") || (cb.Type == "Save_TTL") ||
                    (cb.Type == "Copy_TTL"))
                    this.checkedListBox1.SetItemChecked(i, true);
            }
        }

        private void selectFoldersOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DeSelectAll();
            for (int i = 0; i < this.checkedListBox1.Items.Count; i++)
            {
                MainForm.TagInfo cb = this.checkedListBox1.Items[i] as MainForm.TagInfo;
                if (cb.Type == "Delete_Folder")
                    this.checkedListBox1.SetItemChecked(i, true);
            }
        }
    }
}
