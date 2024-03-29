﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;
using Yekyaa.FFXIEncoding;

namespace FFXI_ME_v2
{
    /// <summary>
    /// Static class for printing debugging and informational messages to a logfile.
    /// Call the Initialize() function as early as possible and Close() before exiting the program.
    /// </summary>
    public static class LogMessage
    {
        #region LogMessage Variables
        private static Object locker = new Object();
        private static string logName = String.Empty;
        private static StreamWriter logfile = null;
        private static bool initialized = false;
        #endregion

        #region LogMessage Properties
        /// <summary>
        /// Returns the status of the logfile.
        /// </summary>
        public static bool Initialized
        {
            get { return initialized; }
        }
        #endregion

        #region LogMessage Methods
        /// <summary>
        /// Initializes the LogMessage class by opening or creating the file as necessary.
        /// </summary>
        static public void Initialize(String name)
        {
            lock (locker)
            {
                try
                {
                    DateTime dt = DateTime.Now;

                    logName = String.Format("{0}-{1}.log", name, dt.ToString("yyyy.MM.dd.HH.mm.ss"));

                    if (!Directory.Exists(Preferences.LogFileDirectory))
                    {
                        Directory.CreateDirectory(Preferences.LogFileDirectory);
                    }
                    logfile = File.CreateText(Preferences.LogFileDirectory + Path.DirectorySeparatorChar + logName);
                }
                catch { initialized = false; logfile = null; return; }
                finally
                {
                    if (logfile != null)
                        initialized = true;
                }
            }
        }

        /// <summary>
        /// Closes the logfile.
        /// </summary>
        static public void Close()
        {
            lock (locker)
            {
                if (initialized && (logfile != null))
                {
                    try
                    {
                        logfile.Flush();
                    }
                    catch
                    {
                    }
                    finally
                    {
                        try
                        {
                            logfile.Close();
                        }
                        catch { }
                    }
                }
                initialized = false;
                logfile = null;
            }
        }

        /// <summary>
        /// Base function for logging messages for debugging or informational purposes.
        /// </summary>
        /// <param name="logMessage">A string containing the message you want printed to the logfile.</param>
        static public void Log(String logMessage)
        {
            Log(false, logMessage);
        }

        /// <summary>
        /// Base function for logging messages for debugging or informational purposes that forces the information to be written to the logfile.
        /// </summary>
        /// <param name="logMessage">A string containing the message you want printed to the logfile.</param>
        static public void LogF(String logMessage)
        {
            Log(true, logMessage);
        }

        /// <summary>
        /// Base function for logging messages for debugging or informational purposes.
        /// </summary>
        /// <param name="force_show">True: Log the following message to file regardless of ShowDebugInfo status.
        /// False: Print to the logfile only if ShowDebugInfo is set to true (for those not so important messages).</param>
        /// <param name="logMessage">A string containing the message you want printed to the logfile.</param>
        static public void Log(bool force_show, String logMessage)
        {
            // force_show overrides ShowDebugInfo
            // so if ShowDebugInfo is false, it suppresses Logs
            // if force_show is true however it will not (for those REALLY important ones)
            if ((Preferences.ShowDebugInfo == false) && (force_show == false))
                return;

            lock (locker)
            {
                if ((logfile == null) || (!initialized))
                {
                    try
                    {
                        logfile = File.AppendText(logName);
                    }
                    catch
                    {
                        initialized = false;
                        logfile = null;
                        return;
                    }
                    initialized = true;
                }
                try
                {

                    logfile.WriteLine("{0}:{1}", DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss.fff tt"), logMessage);
                    logfile.Flush();
                }
                catch
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Base function for logging messages for debugging or informational purposes. This variant functions like String.Format
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        static public void Log(string format, params object[] args)
        {
            Log(false, String.Format(format, args));
        }

        /// <summary>
        /// Base function for logging messages for debugging or informational purposes that forces the information to be written to the logfile. This variant functions like String.Format
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        static public void LogF(string format, params object[] args)
        {
            Log(true, format, args);
        }

        /// <summary>
        /// Base function for logging messages for debugging or informational purposes. This variant functions like String.Format
        /// </summary>
        /// <param name="force_show">True: Log the following message to file regardless of ShowDebugInfo status.
        /// False: Print to the logfile only if ShowDebugInfo is set to true (for those not so important messages).</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        static public void Log(bool force_show, string format, params object[] args)
        {
            Log(force_show, String.Format(format, args));
        }
        #endregion
    }

    /// <summary>
    /// System Preferences Some User-defined/modifiable, others intended for internal use only and stored here for simplification.
    /// </summary>
    static public class Preferences
    {
        #region Preferences Variables
        /// <summary>
        /// List of Paths requested to be opened upon startup (can be files, folders, directories) (Internal)
        /// </summary>
        /// 
        static public List<String> PathToOpen = new List<string>();

        static private String DeleteNotifier = "<x_ffxime_x> Delete Me";

        /// <summary>
        /// Stores Full Path of the Program's default User Save location. (Not intended to be saved!) (Internal)
        /// NOTE: DO NOT USE Settings.PutSetting() with this variable!
        /// </summary>
        static public String AppMyDocsFolderName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + "FFXI ME!";

        static public String MenuXMLFile = Preferences.AppMyDocsFolderName + Path.DirectorySeparatorChar + "menu.xml";

        static public String SettingsXMLFile = Preferences.AppMyDocsFolderName + Path.DirectorySeparatorChar + "settings.xml";

        static public String LogFileDirectory = Preferences.AppMyDocsFolderName + Path.DirectorySeparatorChar + "Logs";

        static public String TasksDirectory = Preferences.AppMyDocsFolderName + Path.DirectorySeparatorChar + "Tasks";

        /// <summary>
        /// Default Language for what files will be loaded is set to English. (All, Jp, En, Fr, De are all valid) (User Setting)
        /// </summary>
        static public int Language = FFXIATPhraseLoader.ffxiLanguages.LANG_ENGLISH; // English

        /// <summary>
        /// Default value for categorizing Auto-Translate menu if there's more items than this amount in the list requested. (User Setting)
        /// </summary>
        static public int Max_Menu_Items = 30; // Default value for categorizing AT Menu if more than this many in list

        /// <summary>
        /// Maximum number of macro sets per folder (Setup in case of future expansions). (Internal)
        /// </summary>
        static public int Max_Macro_Sets = 200; // Currently maximum setup for FFXI, for future expansion

        /// <summary>
        /// Default program language (not fully implemented).
        /// </summary>
        static public int Program_Language = FFXIATPhraseLoader.ffxiLanguages.LANG_ENGLISH; // English, not yet implemented

        /// <summary>
        /// Default Templates folder name (based on location of the "default User Save" location.  This can be saved/modified. (Not implemented as User Setting, yet)
        /// </summary>
        static public String TemplatesFolderName = AppMyDocsFolderName + Path.DirectorySeparatorChar + "Templates";

        /// <summary>
        /// Does pressing the Enter key in the Macro Editor create a new line or just skip to the next line? (User Setting)
        /// </summary>
        static public int EnterCreatesNewLine = 0;

        /// <summary>
        /// User Explorer view by default when opening an Explorer window. (runs explorer with /e option: Functions like File Manager) (User Setting)
        /// </summary>
        static public bool UseExplorerViewOnFolderOpen = true;

        /// <summary>
        /// If true, when opening the explorer window, you won't be able to navigate to any folder above the one opened. (Can't go "Up") (User Setting)
        /// </summary>
        static public bool UseFolderAsRoot = true;

        /// <summary>
        /// Include a descriptive header on Auto-Translate phrase Tab and right-click menus. (User Setting)
        /// </summary>
        static public bool Include_Header = true; // By default include descriptive header on at phrase menu and right-click context menu 

        /// <summary>
        /// Force ALL log messages to be written, not just one's explicitly requested. Good for debugging purposes. (Command-Line Option: /debug)
        /// </summary>
        static public bool ShowDebugInfo = false;

        /// <summary>
        /// True: Load Items information, False: Skip loading of Item information. (User Setting)
        /// </summary>
        static public bool LoadItems = true;

        /// <summary>
        /// True: Load Key Items information, False: Skip loading of Key Items information. (User Setting)
        /// </summary>
        static public bool LoadKeyItems = true;

        /// <summary>
        /// True: Load Auto-Translate Phrase information, False: Skip loading of Auto-Translate Phrase information. (User Setting)
        /// </summary>
        static public bool LoadAutoTranslatePhrases = true;

        /// <summary>
        /// When minimized, should it minimize to system tray? (User Setting)
        /// </summary>
        static public bool MinimizeToTray = true;

        /// <summary>
        /// Modified only when the program is minimized/restored/maximized (For Saving Last State). (Internal)
        /// </summary>
        static public bool IsMaximized = false;

        /// <summary>
        /// Show Books that may be empty.
        /// </summary>
        static public bool ShowBlankBooks = true;

        // Node Color for visual confirmation that a file has been modified/not saved
        static private Color _changed = Color.Red;
        // Node Color for visual confirmation that a file has been saved or not modified
        static private Color _notchanged = Color.Black;
        #endregion

        #region Preferences Properties
        /// <summary>
        /// The Color used to show a file has changed in the TreeView (Read-Only)
        /// </summary>
        static public Color ShowFileChanged
        {
            get { return _changed; }
        }
        /// <summary>
        /// The default Color used for files in the TreeView (Read-Only)
        /// </summary>
        static public Color FileNotChanged
        {
            get { return _notchanged; }
        }
        #endregion

        private static bool Deleteable(String x)
        {
            if (x == DeleteNotifier)
                return true;
            return false;
        }

        public static bool IsFile(FileAttributes fa)
        {
            if ((fa & FileAttributes.Directory) != FileAttributes.Directory)
                return true;
            return false;
        }

        public static bool IsDirectory(FileAttributes fa)
        {
            if ((fa & FileAttributes.Directory) == FileAttributes.Directory)
                return true;
            return false;
        }

        public static int ReverseComparePaths(String p1, String p2)
        {
            if (p1 == null)
            {
                if (p2 == null)
                    return 0;
                else return 1;
            }
            else if (p2 == null)
            {
                    return -1;
            }

            if (Directory.Exists(p1))
            {
                if (Directory.Exists(p2))
                {
                    return p2.CompareTo(p1);
                }
                else if (File.Exists(p2))
                {
                    return -1;
                }
            }
            else if (File.Exists(p1))
            {
                if (Directory.Exists(p2))
                    return 1;
                else if (File.Exists(p2))
                    return p2.CompareTo(p1);
            }
            return 0;
        }

        static public bool AddLocation(String location)
        {

            FileInfo fi = new FileInfo(location);
            DirectoryInfo di = new DirectoryInfo(location);
            String dir = di.FullName;

            // Scenario #1, PathToOpen is Empty, add it no matter what.
            if (Preferences.PathToOpen.Count == 0)
            {
                if (IsFile(di.Attributes))
                    Preferences.PathToOpen.Add(fi.FullName);
                else Preferences.PathToOpen.Add(di.FullName.TrimEnd('\\'));
                return true;
            }

            if (IsFile(di.Attributes))
            {
                if (fi.Length != 7624)
                    return false;

                dir = fi.DirectoryName.TrimEnd('\\');
            }

            if (Preferences.PathToOpen.Contains(dir)) // if we already have this exact location, skip it
            {
                return false;
            }

            bool Skip = false, Delete = false;
            String prefDir = String.Empty;
            DirectoryInfo prefdi = null;
            FileInfo preffi = null;

            for (int c = 0; c < Preferences.PathToOpen.Count; c++)
            {
                prefdi = new DirectoryInfo(Preferences.PathToOpen[c]);
                preffi = new FileInfo(Preferences.PathToOpen[c]);
                prefDir = prefdi.FullName.TrimEnd('\\');

                // scenarios
                // 1. PathToOpen contains nothing, add whatever it is
                // 2. PathToOpen contains a fileName F1 (C:\Tools.dat), new fileName (F2) (C:\Tool1.dat; C:\Tool\tool1.dat) read in... Keep both, eventually a folder will contain one or other -- solution, add F2, keep F1
                // 3. PathToOpen contains a fileName F1 (C:\Tools.dat), new directory (D1) (C:\) read in, F1 will be found reading D1 -- solution, delete F1, add D1
                // 4. PathToOpen contains a fileName F1 (C:\Tools.dat), new directory D1 (C:\Tools\File), F1 won't be found when searching D1 -- solution, add D1, keep F1
                // 5. D1 (C:\tools) loaded, F1 (C:\tools\blah.dat) will be found when searching D1 -- solution skip F1, keep D1
                // 6. D1 (C:\tools) loaded, F1 (C:\blah.dat) will not be found when searching D1 -- solution add F1, continue
                // 7. D1 (C:\) loaded, D2 (C:\tools) read in that will be found when searching D1 -- solution, skip D2, keep D1
                // 8. D1 (C:\tools) loaded, D2 (C:\) read in but D1 will be found when searching D2 -- solution delete D1, add D2

                if (IsFile(prefdi.Attributes))
                {
                    // Preferences is a File
                    prefDir = preffi.DirectoryName.TrimEnd('\\');

                    if (IsFile(di.Attributes))
                    {
                        // Scenario #2, keep em both.

                        // to be sure, don't add the exact same file
                        if (preffi.FullName == fi.FullName)
                            return false;
                    }
                    else
                    {
                        // Preferences is a File, challenger is directory
                        if (Preferences.PathToOpen[c].Contains(dir + "\\"))
                        {
                            // Scenario #3, Delete F1, Add D1
                            Preferences.PathToOpen[c] = DeleteNotifier;
                            Delete = true;
                            // Don't return here, continue looping through looking for others to delete
                            continue;
                        }
                        else
                        {
                            // Scenario #4, Keep F1, Add D1
                            continue;
                        }
                    }
                }
                else
                {
                    // Preferences is a Directory
                    if (IsFile(di.Attributes))
                    {
                        if (fi.FullName.Contains(prefDir + "\\"))
                        {
                            // Scenario #5, skip F1, keep D1, may as well return since i can't add the file definitely
                            return false;
                        }
                        else
                        {
                            // Scenario #6, can't definitely add it, so continue on
                            continue;
                        }
                    }
                    else
                    {
                        // Both are directories
                        // If they're the same, ignore the new one
                        if (di.FullName == prefdi.FullName)
                            return false;
                        else if (di.FullName.Contains(prefDir + "\\"))
                        {
                            // Scenario #7, we'll search D1 and find D2, so do not add at all
                            return false;
                        }
                        else if (prefDir.Contains(di.FullName.TrimEnd('\\') + "\\"))
                        {
                            // Scenario #8
                            Preferences.PathToOpen[c] = DeleteNotifier;
                            Delete = true;
                            continue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }

            if (Delete)
                Preferences.PathToOpen.RemoveAll(Preferences.Deleteable);

            if (!Skip)
            {
                if (IsFile(di.Attributes))
                    Preferences.PathToOpen.Add(fi.FullName);
                else Preferences.PathToOpen.Add(di.FullName);

                PathToOpen.Sort(ReverseComparePaths);
            }
            else return false;

            return true;
        }
    }

    static public class Icons
    {
        #region Icons Variables
        #region Icons (Main Form Buttons)
        #region Icons (Main Form Buttons Enabled)
        static private Image _usa_enabled = global::FFXI_ME_v2.Properties.Resources.usa;
        static private Image _deutsch_enabled = global::FFXI_ME_v2.Properties.Resources.germany;
        static private Image _france_enabled = global::FFXI_ME_v2.Properties.Resources.france;
        static private Image _japan_enabled = global::FFXI_ME_v2.Properties.Resources.japan;
        #endregion

        #region Icons (Main Form Buttons Disabled)
        static private Image _usa_disabled = global::FFXI_ME_v2.Properties.Resources.usa_disabled;
        static private Image _deutsch_disabled = global::FFXI_ME_v2.Properties.Resources.germany_disabled;
        static private Image _france_disabled = global::FFXI_ME_v2.Properties.Resources.france_disabled;
        static private Image _japan_disabled = global::FFXI_ME_v2.Properties.Resources.japan_disabled;
        #endregion
        #endregion

        #region Icons (English)
        static private Image _englishicon = global::FFXI_ME_v2.Properties.Resources.English_Icon;
        static private Image _englishkeyitem = global::FFXI_ME_v2.Properties.Resources.EnglishKeyItem;
        static private Image _englisharmor = global::FFXI_ME_v2.Properties.Resources.EnglishArmor;
        static private Image _englishweapon = global::FFXI_ME_v2.Properties.Resources.EnglishWeapon;
        static private Image _englishpuppetitem = global::FFXI_ME_v2.Properties.Resources.EnglishPuppetItem;
        static private Image _englishitem = global::FFXI_ME_v2.Properties.Resources.EnglishItemIcon;
        #endregion

        #region Icons (Japanese)
        static private Image _japaneseicon = global::FFXI_ME_v2.Properties.Resources.Japanese_Icon;
        static private Image _japanesekeyitem = global::FFXI_ME_v2.Properties.Resources.JapaneseKeyItem;
        static private Image _japanesearmor = global::FFXI_ME_v2.Properties.Resources.JapaneseArmor;
        static private Image _japaneseweapon = global::FFXI_ME_v2.Properties.Resources.JapaneseWeapon;
        static private Image _japanesepuppetitem = global::FFXI_ME_v2.Properties.Resources.JapanesePuppetItem;
        static private Image _japaneseitem = global::FFXI_ME_v2.Properties.Resources.JapaneseItemIcon;
        #endregion

        #region Icons (Deutsch/German)
        static private Image _deutschicon = global::FFXI_ME_v2.Properties.Resources.Deutsch_Icon;
        static private Image _deutschkeyitem = global::FFXI_ME_v2.Properties.Resources.DeutschKeyItem;
        static private Image _deutscharmor = global::FFXI_ME_v2.Properties.Resources.DeutschArmor;
        static private Image _deutschweapon = global::FFXI_ME_v2.Properties.Resources.DeutschWeapon;
        static private Image _deutschpuppetitem = global::FFXI_ME_v2.Properties.Resources.DeutschPuppetItem;
        static private Image _deutschitem = global::FFXI_ME_v2.Properties.Resources.DeutschItemIcon;
        #endregion

        #region Icons (French)
        static private Image _frenchicon = global::FFXI_ME_v2.Properties.Resources.French_Icon;
        static private Image _frenchkeyitem = global::FFXI_ME_v2.Properties.Resources.FrenchKeyItem;
        static private Image _frencharmor = global::FFXI_ME_v2.Properties.Resources.FrenchArmor;
        static private Image _frenchweapon = global::FFXI_ME_v2.Properties.Resources.FrenchWeapon;
        static private Image _frenchpuppetitem = global::FFXI_ME_v2.Properties.Resources.FrenchPuppetItem;
        static private Image _frenchitem = global::FFXI_ME_v2.Properties.Resources.FrenchItemIcon;
        #endregion

        #region Icons (Non-Language Specific)
        static private Image _keyitemicon = global::FFXI_ME_v2.Properties.Resources.KeyItemIcon;
        static private Image _itemicon = global::FFXI_ME_v2.Properties.Resources.ItemIcon;
        static private Image _armoricon = global::FFXI_ME_v2.Properties.Resources.ArmorIcon;
        static private Image _weaponicon = global::FFXI_ME_v2.Properties.Resources.WeaponIcon;
        static private Image _puppeticon = global::FFXI_ME_v2.Properties.Resources.PuppetIcon;
        #endregion

        #region Icons (Sorted By Type)
        static private Image[] _keyitem = { _keyitemicon, _japanesekeyitem, _englishkeyitem, _deutschkeyitem, _frenchkeyitem, _keyitemicon };
        static private Image[] _item = { _itemicon, _japaneseitem, _englishitem, _deutschitem, _frenchitem, _itemicon };
        static private Image[] _armor = { _armoricon, _japanesearmor, _englisharmor, _deutscharmor, _frencharmor, _armoricon };
        static private Image[] _weapon = { _weaponicon, _japaneseweapon, _englishweapon, _deutschweapon, _frenchweapon, _weaponicon };
        static private Image[] _puppet = { _puppeticon, _japanesepuppetitem, _englishpuppetitem, _deutschpuppetitem, _frenchpuppetitem, _puppeticon };
        static private Image[] _icon = { null, _japaneseicon, _englishicon, _deutschicon, _frenchicon, null };
        #endregion

        #region Icons (Color Specific)
        // Item Color for the non-categorized auto-translate menu items
        static private Color _itemcolor = Color.DarkGreen;
        // Key Item Color for the non-categorized auto-translate menu items
        static private Color _keyitemcolor = Color.Blue;
        #endregion
        #endregion

        #region Icons Properties
        #region Icons Properties (Main Form Buttons)
        #region Icons Properties (Main Form Buttons Enabled)
        static public Image UsaEnabled
        {
            get { return _usa_enabled; }
        }
        static public Image DeutschEnabled
        {
            get { return _deutsch_enabled; }
        }
        static public Image FranceEnabled
        {
            get { return _france_enabled; }
        }
        static public Image JapanEnabled
        {
            get { return _japan_enabled; }
        }
        #endregion

        #region #region Icons Properties (Main Form Buttons Disabled)
        static public Image UsaDisabled
        {
            get { return _usa_disabled; }
        }
        static public Image DeutschDisabled
        {
            get { return _deutsch_disabled; }
        }
        static public Image FranceDisabled
        {
            get { return _france_disabled; }
        }
        static public Image JapanDisabled
        {
            get { return _japan_disabled; }
        }
        #endregion
        #endregion

        #region Icons Properties (Arrays of Type Icons by Language)
        static public Image[] KeyItemIcon
        {
            get { return _keyitem; }
        }
        static public Image[] ItemIcon
        {
            get { return _item; }
        }
        static public Image[] ArmorIcon
        {
            get { return _armor; }
        }
        static public Image[] WeaponIcon
        {
            get { return _weapon; }
        }
        static public Image[] PuppetIcon
        {
            get { return _puppet; }
        }
        static public Image[] GeneralIcon
        {
            get { return _icon; }
        }
        #endregion

        #region Icons Properties (Color Specific)
        static public Color ItemColor
        {
            get { return _itemcolor; }
        }
        static public Color KeyItemColor
        {
            get { return _keyitemcolor; }
        }
        #endregion
        #endregion
    }

    static public class Utilities
    {
        #region Utilities Methods
        /// <summary>
        /// Internal function for Ellipsifying a string to simplify path ellipsification.
        /// </summary>
        /// <param name="s">String to Ellipsify.</param>
        /// <param name="length">Length that the returned String should not be longer than.</param>
        /// <returns>A String with ellipsis starting at 1 character from the start. ie "F...Whatever was left after shortening the String to Length"</returns>
        static private string Ellipsify(string s, int length)
        {
            return Ellipsify(s, length, 0);
        }

        /// <summary>
        /// Internal function for Ellipsifying a string to simplify path ellipsification.
        /// </summary>
        /// <param name="s">String to Ellipsify.</param>
        /// <param name="length">Length that the returned String should not be longer than.</param>
        /// <param name="startindex">Location in the string to apply the ellipses.</param>
        /// <returns>A String with ellipsis beginning at (startindex + 1). ie "F...Whatever was left after shortening the String to Length"</returns>
        static private string Ellipsify(string s, int length, int startindex)
        {
            if (s.Length <= (length - startindex))
                return s;
            return (s.Replace(s.Substring(startindex + 1, s.Length - length - (startindex + 1)), "..."));
        }

        /// <summary>
        /// Function for Ellipsifying a path to a shortened length (no matter how long the path)
        /// </summary>
        /// <param name="s">Path to Ellipsify.</param>
        /// <param name="length">Length that the returned String should not be longer than.</param>
        /// <returns>A String with ellipsis beginning after the drive or folder. ie "C:\...\Whatever was left after shortening the String to Length"</returns>
        static public string EllipsifyPath(string s, int length)
        {
            if (s.Length <= length)
                return s;

            String[] sPath = s.Split('\\');
            String ret = String.Empty;
            int count = 0;
            if (sPath.Length > 0)
            {
                ret = sPath[0] + "\\...\\"; // get starting dir/folder no matter what
                count = ret.Length + sPath[sPath.Length - 1].Length; // acct for filename at the end

                if (count == length)
                    return String.Format("{0}{1}", ret, sPath[sPath.Length - 1]);

                int i = 0;
                // work backwards b/c we want to include the most significant path
                for (i = sPath.Length - 2; (i > 1) && ((count + sPath[i].Length + 1) <= length); i--) count += (sPath[i].Length + 1);

                // once we found the limit
                for (; i < (sPath.Length - 2); i++)
                    ret += sPath[i + 1] + "\\";

                ret += sPath[sPath.Length - 1]; // include file name
            }
            return ret;
        }

        /// <summary>
        /// Function for Ellipsifying a path to a shortened length (no matter how long the path). Default of 45 characters.
        /// </summary>
        /// <param name="s">Path to Ellipsify.</param>
        /// <returns>A String with ellipsis beginning after the drive or folder. ie "C:\...\Whatever was left after shortening the String to Length"</returns>
        static public string EllipsifyPath(string s)
        {
            return EllipsifyPath(s, 45);
        }
        #endregion
    }

    public class Settings
    {
        #region Settings Variables
        XmlDocument xmlDocument = new XmlDocument();
        string documentPath = String.Empty; //Preferences.SettingsXMLFile;
        String MainNode = String.Empty; // "settings";
        #endregion

        #region Settings Methods
        #region Settings Methods (ConvertTo/From)
        // ConvertFromLanguage, take a value, 1, 2, 3, 4,
        // return Language in String equivalent
        private string ConvertFromLanguage(int lang)
        {
            if (lang == FFXIATPhraseLoader.ffxiLanguages.LANG_JAPANESE)
                return "Japanese";
            else if (lang == FFXIATPhraseLoader.ffxiLanguages.LANG_ENGLISH)
                return "English";
            else if (lang == FFXIATPhraseLoader.ffxiLanguages.LANG_DEUTSCH)
                return "Deutsch";
            else if (lang == FFXIATPhraseLoader.ffxiLanguages.LANG_FRENCH)
                return "French";
            else if (lang == FFXIATPhraseLoader.ffxiLanguages.LANG_ALL)
                return "Load All";
            // Default To English
            return "English";
        }
        // ConvertToLanguage, based on string received, 
        // return int version (1, 2, 3, 4)
        private int ConvertToLanguage(string dv)
        {
            string lc = dv.ToLower();
            if ((lc == "2") || (lc == "en") || (lc == "e") || (lc == "english") || (lc == "american"))
                return FFXIATPhraseLoader.ffxiLanguages.LANG_ENGLISH; // 0x02 English
            else if ((lc == "1") || (lc == "jp") || (lc == "jap") || (lc == "japanese") || (lc == "j"))
                return FFXIATPhraseLoader.ffxiLanguages.LANG_JAPANESE;
            else if ((lc == "3") || (lc == "de") || (lc == "german") || (lc == "deutsch") || (lc == "d") || (lc == "g") || (lc == "gr"))
                return FFXIATPhraseLoader.ffxiLanguages.LANG_DEUTSCH;
            else if ((lc == "4") || (lc == "fr") || (lc == "f") || (lc == "french"))
                return FFXIATPhraseLoader.ffxiLanguages.LANG_FRENCH;
            else if ((lc == "jedf") || (lc == "all") || (lc == "everything") || (lc == "loadall") || (lc == "load all"))
                return FFXIATPhraseLoader.ffxiLanguages.LANG_ALL;
            // Default to English
            return FFXIATPhraseLoader.ffxiLanguages.LANG_ENGLISH;
        }
        // ConvertToBoolean, preferred boolean conversion
        // I like, 1, true (lower case), t, and y to be equal
        // to True, not just "True"
        // no ConvertFromBoolean as it will output True or False only
        // using the default conversion.
        private bool ConvertToBoolean(string dv)
        {
            string lc = dv.ToLower();
            if ((lc == "1") || (lc == "true") || (lc == "yes") || (lc == "y") || (lc == "t"))
                return true;
            // everything else is false
            return false;
        }
        #endregion

        #region Settings Methods (DeleteSetting)
        /// <summary>
        /// Used for deleting a node all all children below it.  Do NOT forget to call SaveSettings() after doing this.
        /// </summary>
        /// <param name="xPath">The XML path to the node you wish to remove (underneath the 'settings' node).</param>
        /// <returns>TRUE if Node exists and was deleted; FALSE if not found or the NodeType has no Parent to delete it from.</returns>
        public bool DeleteSetting(string xPath)
        {
            XmlNode xmlNode = xmlDocument.SelectSingleNode(this.MainNode + "/" + xPath);
            
            if ((xmlNode != null) && (xmlNode.ParentNode != null))
            { 
                xmlNode.ParentNode.RemoveChild(xmlNode);
                return true;
            }
            return false;  // No Setting by that name, or it's NodeType is Attribute, Document, DocumentFragment, Entity, Notation
        }
        #endregion

        #region Settings Methods (GetSetting() overloads)
        public bool GetSetting(string xPath, bool defaultValue)
        { return ConvertToBoolean(GetSetting(xPath, Convert.ToString(defaultValue))); }
        public int GetSettingLanguage(string xPath, int defaultValue)
        {
            return ConvertToLanguage(GetSetting(xPath, ConvertFromLanguage(defaultValue)));
        }
        public int GetSetting(string xPath, int defaultValue)
        {
            return Convert.ToInt16(GetSetting(xPath, Convert.ToString(defaultValue)));
        }

        // Base GetSetting() overload
        public string GetSetting(string xPath, string defaultValue)
        {
            XmlNode xmlNode = xmlDocument.SelectSingleNode(this.MainNode + "/" + xPath);
            if (xmlNode != null) { return xmlNode.InnerText; }
            else { return defaultValue; }
        }
        #endregion

        #region Settings Methods (PutSetting() overloads)
        public void PutSetting(string xPath, bool value)
        { PutSetting(xPath, Convert.ToString(value)); }
        public void PutSettingLanguage(string xPath, int value)
        { PutSetting(xPath, ConvertFromLanguage(value)); }
        public void PutSetting(string xPath, int value)
        { PutSetting(xPath, Convert.ToString(value)); }

        // Base PutSetting() overload
        public void PutSetting(string xPath, string value)
        {
            XmlNode xmlNode = xmlDocument.SelectSingleNode(this.MainNode + "/" + xPath);
            if (xmlNode == null) { xmlNode = createMissingNode(this.MainNode + "/" + xPath); }
            xmlNode.InnerText = value;
        }
        #endregion

        #region Settings Methods (SaveSettings, Close)
        /// <summary>
        /// "Closes" the XML file, does NOT save.
        /// </summary>
        public void Close()
        {
            this.MainNode = String.Empty;
            this.documentPath = String.Empty;
        }

        /// <summary>
        /// SaveSettings: Needed an implicit call as too many PutSettings close together gave me user-mapped errors.
        /// Required if you expect to write the XML successfully.
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                xmlDocument.Save(this.documentPath);
            }
            catch (IOException)
            {
                try
                {
                    System.Threading.Thread.Sleep(250);
                    xmlDocument.Save(this.documentPath);
                }
                catch (IOException)
                {
                    LogMessage.LogF("Unable to save '{0}' successfully due to some user-mapped error issue.", this.documentPath);
                }
            }
        }
        #endregion

        #region Settings Methods (Node-specific)
        /// <summary>
        /// Gets a list of nodes that are below the given path.
        /// </summary>
        /// <param name="xPath">Path to search under for child nodes.</param>
        /// <returns>Array of strings with the name of each node under xmlNode xPath.</returns>
        public List<String> GetNodeList(string xPath)
        {
            List<String> return_list = new List<string>();
            XmlNode xmlNode = xmlDocument.SelectSingleNode(this.MainNode + "/" + xPath);
            if ((xmlNode != null) && xmlNode.HasChildNodes)
            {
                foreach (XmlNode testNode in xmlNode.ChildNodes)
                {
                    return_list.Add(testNode.Name);
                }
            }
            return return_list;
        }

        private XmlNode createMissingNode(string xPath)
        {
            string[] xPathSections = xPath.Split('/');
            string currentXPath = "";
            XmlNode testNode = null;
            XmlNode currentNode = xmlDocument.SelectSingleNode(this.MainNode);
            foreach (string xPathSection in xPathSections)
            {
                currentXPath += xPathSection;
                testNode = xmlDocument.SelectSingleNode(currentXPath);
                if (testNode == null)
                {
                    currentNode.InnerXml += "<" +
                                xPathSection + "></" +
                                xPathSection + ">";
                }
                currentNode = xmlDocument.SelectSingleNode(currentXPath);
                currentXPath += "/";
            }
            return currentNode;
        }
        #endregion
        #endregion

        #region Settings Constructor
        public Settings() : this(Preferences.SettingsXMLFile, "settings", true) { }

        public Settings(String docPath, String mNode, bool Create)
        {
            this.documentPath = docPath;
            this.MainNode = mNode;
            {
                try { xmlDocument.Load(docPath); }
                catch
                {
                    if (Create)
                        xmlDocument.LoadXml(String.Format("<{0}></{0}>", mNode));
                    else
                    {
                        this.documentPath = String.Empty;
                        this.MainNode = String.Empty;
                    }
                }
            }
        }

        #endregion
    }
}
