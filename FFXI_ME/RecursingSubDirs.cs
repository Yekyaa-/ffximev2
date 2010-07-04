using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FFXI_ME_v2
{
    public partial class RecursingSubDirs : Form
    {
        public String UpdateInfo
        {
            get { return label1.Text; }
            set { label1.Text = value; }
        }

        public String UpdateName
        {
            set { this.Name = value; }
        }

        public void UpdateUI(string text)
        {
            if (!this.label1.Disposing)
            {
                lock (this.label1.Text)
                {
                    this.label1.Text = text;
                }
            }
        }

        public RecursingSubDirs()
        {
            InitializeComponent();
        }
    }
}