using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Threading;

namespace FFXI_ME_v2
{
    static class FFXI_ME_v2_Program
    {
        static private Mutex m;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool instantiated;

            m = new Mutex(false, "Local\\" + "<x_ffxime_x> One Program At A Time!", out instantiated);

            if (!instantiated)
            {
                MessageBox.Show("FFXI ME! is already running!", "I can't let you do that, Dave.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            try
            {

                if (Preferences.PathToOpen != null)
                    Preferences.PathToOpen.Clear();
            }
            catch
            {
                if (MessageBox.Show("Did you remove the Yekyaa.FFXIEncoding.dll file from the program's directory?", "WTF?!", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                {
                    MessageBox.Show("You should probably get that fixed...", "Yeah, about that...", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("Then I have no idea what you did to crash this program...", "I still think you shouldn't use this program.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "/debug") || (args[i] == "-debug"))
                    Preferences.ShowDebugInfo = true;
                else if ((args[i] == "/options") || (args[i] == "-options") || 
                    (args[i] == "-o") || (args[i] == "/o"))
                    MainForm.ShowOptionsDialog = true;
                else if (args[i] != String.Empty)
                {
                    Preferences.AddLocation(args[i]);
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.DoEvents();
            Application.Run(new MainForm());
        }
    }
}