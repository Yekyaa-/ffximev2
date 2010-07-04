using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Yekyaa.FFXIEncoding;

namespace FFXI_ME_v2
{
    public partial class SearchForPhrase : Form
    {
        #region SearchForPhrase Variables
        FFXIATPhraseLoader ATPhrasesReference = null;
        private bool _sfpvisible;
        #endregion

        #region SearchForPhrase Properties
        public bool sfpvisible
        {
            get { return _sfpvisible; }
            set { _sfpvisible = value; }
        }
        #endregion

        #region SearchForPhrase Methods
        private void searchButton_Click(object sender, EventArgs e)
        {
            if (this.ATPhrasesReference == null)
            {
                resultsBox.Text = "No auto-translate phrases loaded!";
                return;
            }
            string s = searchBox.Text, x = "No results found.";
            if ((s.Trim() == String.Empty) || (s == null))
            {
                resultsBox.Text = "What is this, a wild goose chase? Type something in.";
                return;
            }

            FFXIATPhrase[] atp = null;

            try
            {
                atp = this.ATPhrasesReference.GetPhraseViaRegEx(s, true);
            }
            catch
            {
                atp = null;
            }
            finally
            {
                if ((atp == null) || (atp.Length <= 1))
                    atp = this.ATPhrasesReference.GetPhrases(s, true);
            }
                
            if ((atp != null) && (atp.Length > 0))
            {
                x = String.Empty;
                for (int i = 0; i < atp.Length; i++)
                {
                    if (atp[i].value.Contains("similar phrase"))
                        x += String.Format("{0}\r\n", atp[i].value);
                    else x += String.Format("{0}\r\n", atp[i].ToString());
                }
            }
            resultsBox.Text = x;
        }

        void SearchForPhrase_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.sfpvisible = false;
            this.Hide();
        }

        private void searchBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (sender is TextBox)
                {
                    TextBox ttb = sender as TextBox;
                    if ((ttb != null) && (ttb.ContextMenuStrip != null))
                    {
                        if (!ttb.Focused)
                        {
                            int ss = ttb.SelectionStart, sl = ttb.SelectionLength;
                            ttb.SelectionStart = ss;
                            ttb.SelectionLength = sl;
                            ttb.Focus();
                        }
                        ToolStripItem paster = ttb.ContextMenuStrip.Items["pasteToolStripMenuItem"];
                        ToolStripItem deleter = ttb.ContextMenuStrip.Items["deleteToolStripMenuItem"];
                        ToolStripItem cutter = ttb.ContextMenuStrip.Items["cutToolStripMenuItem"];
                        ToolStripItem copier = ttb.ContextMenuStrip.Items["copyToolStripMenuItem"];
                        ToolStripItem copier2 = ttb.ContextMenuStrip.Items["copy2ToolStripMenuItem"];
                        copier2.Visible = false;
                        //ToolStripItem selecter = ttb.ContextMenuStrip.Items["selectAllToolStripMenuItem"];
                        if (ttb == resultsBox)
                        {
                            #region Handle Right-Click in Results Box
                            #region Handle the Cut menu item
                            if (cutter != null)
                                cutter.Visible = false;
                            #endregion

                            #region Handle the Copy Menu Item
                            if ((copier != null) && (copier2 != null))
                            {
                                if (ttb.SelectionLength > 0)
                                {
                                    copier.Text = String.Format("Copy \'{0}{1}\'",
                                        (ttb.SelectedText.Length > 33) ? ttb.SelectedText.Substring(0, 30) : ttb.SelectedText,
                                        (ttb.SelectedText.Length > 33) ? "..." : "");
                                    copier.Enabled = true;
                                    copier2.Visible = false;
                                }
                                else
                                {
                                    int linenumber = ttb.GetLineFromCharIndex(ttb.GetCharIndexFromPosition(e.Location));
                                    String line = String.Empty;
                                    if (linenumber < ttb.Lines.Length)
                                    {
                                        line = ttb.Lines[linenumber];
                                    }
                                    if ((line != String.Empty) && (line.IndexOf(FFXIEncoding.StartMarker) == 0))
                                    {
                                        //ttb.Select(ttb.GetFirstCharIndexFromLine(linenumber), line.Length);
                                        copier.Text = String.Format("Copy \'{0}{1}\'",
                                            (line.Length > 43) ? line.Substring(10, 30) : line,
                                            (line.Length > 43) ? "..." : "");
                                        copier.Tag = line;
                                        copier.Enabled = true;

                                        int mid_marker = line.IndexOf(FFXIEncoding.MiddleMarker);
                                        int end_marker = line.IndexOf(FFXIEncoding.EndMarker);
                                        String the_text = String.Empty;
                                        if ((mid_marker < end_marker) && (mid_marker != -1) && (end_marker != -1))
                                            the_text = line.Substring(mid_marker + 1, end_marker - (mid_marker + 1));

                                        copier2.Text = String.Format("Copy \'{0}{1}\'",
                                            (the_text.Length > 43) ? the_text.Substring(10, 30) : the_text,
                                            (the_text.Length > 43) ? "..." : "");

                                            copier2.Tag = the_text;
                                        if (the_text != String.Empty)
                                        {
                                            copier2.Visible = true;
                                        }
                                        else copier2.Visible = false;
                                    }
                                    else
                                    {
                                        if (copier.Text != "Copy")
                                            copier.Text = "Copy";
                                        copier.Enabled = false;
                                        copier2.Visible = false;
                                    }
                                }
                            }
                            #endregion

                            #region Handle the Paste Menu item
                            if (paster != null)
                                paster.Visible = false;
                            #endregion

                            #region Handle the Delete Menu item
                            if (deleter != null)
                                deleter.Visible = false;
                            #endregion
                            #endregion
                        }
                        else if (ttb == searchBox)
                        {
                            #region Handle Right-Click in Search Box
                            #region Handle the Cut Menu item
                            if (cutter != null)
                            {
                                cutter.Visible = true;
                                if (ttb.SelectionLength > 0)
                                    cutter.Enabled = true;
                                else cutter.Enabled = false;
                            }
                            #endregion

                            #region Handle the Copy Menu item
                            if (copier != null)
                            {
                                if (ttb.SelectionLength > 0)
                                {
                                    copier.Text = String.Format("Copy \'{0}{1}\'",
                                        (ttb.SelectedText.Length > 33) ? ttb.SelectedText.Substring(0, 30) : ttb.SelectedText,
                                        (ttb.SelectedText.Length > 33) ? "..." : "");
                                    copier.Enabled = true;
                                }
                                else
                                {
                                    copier.Enabled = false;
                                    if (copier.Text != "Copy")
                                        copier.Text = "Copy";
                                }
                            }
                            #endregion

                            #region Handle the Paste Menu item
                            if (paster != null)
                            {
                                paster.Visible = true;
                                if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                                    paster.Enabled = true;
                                else paster.Enabled = false;
                            }
                            #endregion

                            #region Handle the Delete Menu item
                            if (deleter != null)
                            {
                                deleter.Visible = true;
                                if (ttb.SelectionLength > 0)
                                    deleter.Enabled = true;
                                else deleter.Enabled = false;
                            }
                            #endregion
                            #endregion
                        }
                    } // ttb != null
                } // sender
            } // e.Button
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox ttb = null;
            if (this.ActiveControl is TextBox)
                ttb = this.ActiveControl as TextBox;
            if (ttb != null)
            {
                if (ttb == searchBox)
                {
                    ttb.Cut();
                }
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox ttb = null;
            if (this.ActiveControl is TextBox)
                ttb = this.ActiveControl as TextBox;
            if (ttb != null)
            {
                if (ttb == searchBox)
                {
                    ttb.Copy();
                }
                else if (ttb == resultsBox)
                {
                    if (ttb.SelectionLength <= 0)
                    {
                        ToolStripMenuItem tsmi = null;
                        if (sender is ToolStripMenuItem)
                            tsmi = sender as ToolStripMenuItem;

                        String line = String.Empty;

                        if ((tsmi != null) && (tsmi.Tag != null))
                            line = tsmi.Tag as String;
                        if (line != String.Empty)
                        {
                            Clipboard.SetText(line, TextDataFormat.UnicodeText);
                        }
                    }
                    else
                    {
                        ttb.Copy();
                        ttb.SelectionLength = 0;
                    }
                }
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox ttb = null;
            if (this.ActiveControl is TextBox)
                ttb = this.ActiveControl as TextBox;
            if (ttb != null)
            {
                if (ttb == searchBox)
                {
                    ttb.Paste();
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox ttb = null;
            if (this.ActiveControl is TextBox)
                ttb = this.ActiveControl as TextBox;
            if (ttb != null)
            {
                if ((ttb == searchBox) && (ttb.SelectionLength > 0))
                {
                    int ss = ttb.SelectionStart, sl = ttb.SelectionLength;
                    if (ss == ttb.Text.Length)
                    {
                        ttb.Text = ttb.Text.Remove(ss - sl, sl);
                    }
                    else
                    {
                        ttb.Text = ttb.Text.Remove(ss, sl);
                    }
                    if (ttb.Text.Length >= ss)
                        ttb.SelectionStart = ss;
                    else ttb.SelectionStart = ttb.Text.Length;
                }
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox ttb = null;
            if (this.ActiveControl is TextBox)
                ttb = this.ActiveControl as TextBox;
            if (ttb != null)
            {
                if ((ttb == searchBox) || (ttb == resultsBox))
                {
                    ttb.SelectAll();
                }
            }
        }
        #endregion

        #region SearchForPhrase Constructor
        public SearchForPhrase(FFXIATPhraseLoader reference)
        {
            InitializeComponent();
            sfpvisible = false; // default value
            this.ATPhrasesReference = reference;
        }
        #endregion
    }
}
