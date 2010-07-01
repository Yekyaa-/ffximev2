using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace FFXI_ME_v2
{
    static class FFXI_ME_v2_Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Preferences.PathToOpen.Clear();

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "/debug") || (args[i] == "-debug"))
                    Preferences.ShowDebugInfo = true;
                else if ((args[i] == "/options") || (args[i] == "-options") || 
                    (args[i] == "-o") || (args[i] == "/o"))
                    MainForm.ShowOptionsDialog = true;
                else if (args[i] != String.Empty)
                {
                    if (!Preferences.PathToOpen.Contains(args[i]))
                        Preferences.PathToOpen.Add(args[i]);
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.DoEvents();
            Application.Run(new MainForm());
        }
    }
}