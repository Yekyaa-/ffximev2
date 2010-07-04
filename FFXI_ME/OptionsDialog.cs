using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FFXI_ME_v2
{
    public partial class OptionsDialog : Form
    {
        public OptionsDialog(String title)
            : this()
        {
            this.Text = title;
        }
        public OptionsDialog()
        {
            InitializeComponent();
        }

        private void SaveOptions()
        {
            if (this.comboBoxLanguage.SelectedIndex == (this.comboBoxLanguage.Items.Count - 1))
                Preferences.Language = Yekyaa.FFXIEncoding.FFXIATPhraseLoader.ffxiLanguages.LANG_ALL;
            else Preferences.Language = this.comboBoxLanguage.SelectedIndex + 1;
            Preferences.Program_Language = this.comboBoxProgLanguage.SelectedIndex + 1;
            Preferences.Max_Menu_Items = (int)this.numericUpDownMaxMenuItems.Value;
            Preferences.Include_Header = this.checkBoxIncludeHeader.Checked;
            Preferences.UseExplorerViewOnFolderOpen = this.checkBoxExplorerView.Checked;
            Preferences.UseFolderAsRoot = this.checkBoxFolderAsRoot.Checked;
            Preferences.LoadAutoTranslatePhrases = this.checkBoxAtPhrases.Checked;
            Preferences.LoadItems = this.checkBoxItems.Checked;
            Preferences.LoadKeyItems = this.checkBoxKeyItems.Checked;
            Preferences.MinimizeToTray = this.checkBoxMinimize.Checked;
            Preferences.ShowBlankBooks = this.checkBoxShowBlankBooks.Checked;
            Preferences.EnterCreatesNewLine = this.comboBoxEnterKeyOption.SelectedIndex;
            this.buttonApply.Enabled = false;
        }

        private void LoadOptions()
        {
            if (Preferences.Language != Yekyaa.FFXIEncoding.FFXIATPhraseLoader.ffxiLanguages.LANG_ALL)
            {
                this.comboBoxLanguage.SelectedIndex = Preferences.Language - 1;
            }
            else this.comboBoxLanguage.SelectedIndex = this.comboBoxLanguage.Items.Count - 1;

            this.comboBoxProgLanguage.SelectedIndex = Preferences.Program_Language - 1;
            this.numericUpDownMaxMenuItems.Value = Preferences.Max_Menu_Items;
            this.checkBoxIncludeHeader.Checked = Preferences.Include_Header;
            this.checkBoxExplorerView.Checked = Preferences.UseExplorerViewOnFolderOpen;
            this.checkBoxFolderAsRoot.Checked = Preferences.UseFolderAsRoot;
            this.checkBoxAtPhrases.Checked = Preferences.LoadAutoTranslatePhrases;
            this.checkBoxItems.Checked = Preferences.LoadItems;
            this.checkBoxKeyItems.Checked = Preferences.LoadKeyItems;
            this.checkBoxMinimize.Checked = Preferences.MinimizeToTray;
            this.checkBoxShowBlankBooks.Checked = Preferences.ShowBlankBooks;
            if ((Preferences.EnterCreatesNewLine >= 0) && (Preferences.EnterCreatesNewLine < this.comboBoxEnterKeyOption.Items.Count))
                this.comboBoxEnterKeyOption.SelectedIndex = Preferences.EnterCreatesNewLine;
            else this.comboBoxEnterKeyOption.SelectedIndex = Preferences.EnterCreatesNewLine % this.comboBoxEnterKeyOption.Items.Count;

            this.buttonApply.Enabled = false;
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            if (((this.comboBoxLanguage.SelectedIndex == (this.comboBoxLanguage.Items.Count-1)) && 
                    (Preferences.Language != Yekyaa.FFXIEncoding.FFXIATPhraseLoader.ffxiLanguages.LANG_ALL)) ||
                ((this.comboBoxLanguage.SelectedIndex+1) != Preferences.Language) || 
                ((this.comboBoxProgLanguage.SelectedIndex+1) != Preferences.Program_Language))
            {
                MessageBox.Show("Changing the Language Settings will not take\r\neffect until after restarting the program.", "Language Update Notification");
            }
            SaveOptions();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (((this.comboBoxLanguage.SelectedIndex + 1) != Preferences.Language) ||
                ((this.comboBoxProgLanguage.SelectedIndex + 1) != Preferences.Program_Language))
            {
                MessageBox.Show("Changing the Language Settings will not take\r\neffect until after restarting the program.", "Language Update Notification");
            }
            SaveOptions();
            this.Close();
        }

        private void OptionsDialog_Load(object sender, EventArgs e)
        {
            LoadOptions();
        }

        private void ValueChanged(object sender, EventArgs e)
        {
            this.buttonApply.Enabled = true;
        }

        /*
        private void checkBoxExplorerView_CheckedChanged(object sender, EventArgs e)
        {
            this.buttonApply.Enabled = true;
            if (checkBoxExplorerView.Checked == true)
                checkBoxFolderAsRoot.Enabled = true;
            else if (checkBoxExplorerView.Checked == false)
                checkBoxFolderAsRoot.Enabled = false;
            this.buttonApply.Enabled = true;
        }*/
    }
}