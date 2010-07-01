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
        public static bool Deleteable(String x)
        {
            if (x == "<x_ffxime_x> Delete Me")
                return true;
            return false;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Preferences.PathToOpen.Clear();
            bool Delete = false, Skip = false;

            for (int i = 0; i < args.Length; i++)
            {
                Delete = false;
                Skip = false;
                if ((args[i] == "/debug") || (args[i] == "-debug"))
                    Preferences.ShowDebugInfo = true;
                else if ((args[i] == "/options") || (args[i] == "-options") || 
                    (args[i] == "-o") || (args[i] == "/o"))
                    MainForm.ShowOptionsDialog = true;
                else if (args[i] != String.Empty)
                {
                    DirectoryInfo di = new DirectoryInfo(args[i]);
                    if (Preferences.PathToOpen.Contains(di.FullName)) // if we already have this exact folder, skip it
                    {
                        continue;
                    }
                    else
                    {
                        for (int c = 0; c < Preferences.PathToOpen.Count; c++)
                        {
                            if (Preferences.PathToOpen[c].Contains(di.FullName))
                            {
                                // if args[i] is a Parent Folder of an existing Path
                                Preferences.PathToOpen[c] = "<x_ffxime_x> Delete Me";
                                Delete = true;
                            }
                            else if (di.FullName.Contains(Preferences.PathToOpen[c]))
                            {
                                Skip = true;
                            }
                        }
                        if (Delete)
                        {
                            Preferences.PathToOpen.RemoveAll(Deleteable);
                            //while (Preferences.PathToOpen.Contains("<x_ffxime_x> Delete Me"))
                            //{
                            //    Preferences.PathToOpen.Remove("<x_ffxime_x> Delete Me");
                            //}
                            
                        }
                        if (Skip)
                            continue;

                        if (!Preferences.PathToOpen.Contains(di.FullName))
                            Preferences.PathToOpen.Add(di.FullName);
                    }
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.DoEvents();
            Application.Run(new MainForm());
        }
    }
}