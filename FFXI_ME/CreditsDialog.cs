using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FFXI_ME_v2
{
    public partial class CreditsDialog : Form
    {
        public CreditsDialog()
        {
            InitializeComponent();
            this.listBox1.SelectedIndex = 0;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://forums.windower.net/topic/11409-yekyaas-ffxi-me-v2-offline-macro-editor/");
            }
            catch
                (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.contactlabel.Text = this.listBox1.SelectedItem as String;
            switch (this.listBox1.SelectedIndex)
            {
                case 0:
                    this.linkLabel.Text = "";
                    this.informationLabel.Text = "Many people were involved with the creation of this program, although I was the creator of the program.  Without the help of some of the people listed here, I probably would never have come this far...";
                    break;
                case 1:
                    this.linkLabel.Text = "http://www.windower.net/";
                    this.informationLabel.Text = "For being the main location to get my program since forever now! Thanks, guys!";
                    break;
                case 2:
                    this.linkLabel.Text = "http://www.faservers.net/";
                    this.informationLabel.Text = "For giving hosting space through my second attempt! It's GREATLY appreciated!";
                    break;
                case 3:
                    this.linkLabel.Text = "http://www.darkmystics.com/";
                    this.informationLabel.Text = "For originally giving me hosting space for the program while I was working on it.";
                    break;
                case 4:
                    this.linkLabel.Text = "http://irc.windower.net/client/";
                    this.informationLabel.Text = "For working tirelessly on a mapping of a large majority of the supported FFXI characters to UTF-16, without which I probably still wouldn't be able to support some basic French, German, or Japanese characters.";
                    break;
                case 5:
                    this.linkLabel.Text = "http://polutils.7.forumer.com/";
                    this.informationLabel.Text = "For providing the item, key item, and auto translate phrase information necessary to make my program fully complete!  Without you, I'd be lost!";
                    break;
                case 6:
                    this.linkLabel.Text = "http://ffxi.archbell.com/";
                    this.informationLabel.Text = "For sharing the original Macro File format with me, so that I could get started on the original console version a LOOOONG time ago!\r\n\r\nPS -- Yes, the link forwards to www.windower.net, I don't know if he can be reached there anymore or not.";
                    break;
                case 7:
                    this.linkLabel.Text = "";
                    this.informationLabel.Text = "";
                    break;
                case 8:
                    this.linkLabel.Text = "http://www.youtube.com/watch?v=XkKEGW2P3ZI";
                    this.informationLabel.Text = "Made way back in September of 2003 I think. This was my first ever attempt at doing the Shadow Lord solo.  I just hated trying to get people to go up there, and ended up soloing him on a few occassions.  I hope you enjoy watching it.";
                    break;
                case 9:
                    this.linkLabel.Text = "mailto:doomer18@gmail.com";
                    this.informationLabel.Text = "Email me information regarding bugs about the program that you encounter.  If you need a debug log, create a shortcut with -debug after the name of the program to get an in-depth log.";
                    break;
                default:
                    this.contactlabel.Text = "Error...";
                    this.linkLabel.Text = "";
                    this.informationLabel.Text = "Unknown error occurred, one of the selections was not considered. Please report as a bug and let me know which one you selected.";
                    break;
            }
        }

        private void linkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (this.linkLabel.Text != String.Empty)
            {
                try
                {
                    System.Diagnostics.Process.Start(this.linkLabel.Text);
                }
                catch
                    (
                     System.ComponentModel.Win32Exception noBrowser)
                {
                    if (noBrowser.ErrorCode == -2147467259)
                        MessageBox.Show(noBrowser.Message);
                }
                catch (System.Exception other)
                {
                    MessageBox.Show(other.Message);
                }
            }
        }
    }
}
