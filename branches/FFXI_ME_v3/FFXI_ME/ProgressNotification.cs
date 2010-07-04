using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FFXI_ME_v2
{
    public partial class ProgressNotification : Form
    {
        public int NotifyBarMax
        {
            get { return this.notifyBar.Maximum; }
        }

        public int NotifyBarMin
        {
            get { return this.notifyBar.Minimum; }
        }

        public int NotifyBarStep
        {
            get { return this.notifyBar.Step; }
        }

        public int NotifyBarValue
        {
            get { return this.notifyBar.Value; }
            set
            {
                if ((value >= this.notifyBar.Minimum) && (value <= this.notifyBar.Maximum))
                {
                    this.notifyBar.Value = value;
                    this.countLabel.Text = String.Format("{0}/{1}", value, this.notifyBar.Maximum);
                }
            }
        }

        public String NotifyLabelText
        {
            get { return this.notifyLabel.Text; }
            set { if (value != String.Empty) this.notifyLabel.Text = value; }
        }

        public ProgressNotification(String title, String text, Int32 max) : this()
        {
            if (title != String.Empty)
                this.Text = title;
            if (text != String.Empty)
                this.notifyLabel.Text = text;
            this.notifyBar.Maximum = (int)(max * this.notifyBar.Step);
        }

        public ProgressNotification()
        {
            InitializeComponent();
            this.Text = "Processing...";
            this.notifyLabel.Text = "Please wait...";
        }
    }
}
