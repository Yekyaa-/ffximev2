using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Data;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Reflection;
using Microsoft.Win32;
using Yekyaa.FFXIEncoding;
using FFXI_ME_v2.Properties; // for easier access to Resources

namespace FFXI_ME_v2
{
    public partial class MainForm : Form
    {
        #region MainForm Delegates & Externs
        public delegate void RecurseCloseHandler();

        public delegate void UpdateUIHandler(string text);
        #endregion

        #region MainForm Variables
        static public bool ShowOptionsDialog = false;
        // Tracks Changes of Renames and Deletes

        TagInfo[] NodeUpdatesToDo = new TagInfo[0];
        List<TagInfo> DeleteRenameChanges = new List<TagInfo>();
        List<CBook> BookList = new List<CBook>();
        /// <summary>
        /// Array of CMacroFiles that contains all Macrofiles loaded into memory.
        /// </summary>
        List<CMacroFile> MacroFiles = new List<CMacroFile>();

        const int FILES_TO_HIDE = 6;

        Cursor CursorCopy = null;
        Cursor CursorLink = null;
        Bitmap linkbmp = FFXI_ME_v2.Properties.Resources.defaultlink;

        RecursingSubDirs recurse_form = null;
        ProgressNotification notifyForm = null;

        /// <summary>
        /// FFXIATPhraseLoader instance for Loading the Auto-Translate Phrases.
        /// </summary>
        private FFXIATPhraseLoader _ATPhraseLoader = null;

        /// <summary>
        /// This is the backupCursor information I keep so when I'm Macro(File) copies, I can restore the Cursor.
        /// </summary>
        private Cursor backupCursor = Cursors.Default;

        /// <summary>
        /// Used for running the BuildATMenu() function in the background. This is so there's no loadtime the first time you press the Tab Key.
        /// </summary>
        private BackgroundWorker atphraseBW = new BackgroundWorker();

        /// <summary>
        /// Used for running the BuildSpecialsMenu() function in the background. This is so there's no loadtime the first time you bring up a Context Menu.
        /// </summary>
        private BackgroundWorker specialsBW = new BackgroundWorker();

        /// <summary>
        /// Used for locking access to the ATPhraseStrip when Building the Auto-Translate Phrase Menu.
        /// </summary>
        private Object atphraseStripLocker = new Object();

        /// <summary>
        /// Used for locking access to the SpecialStrip when Building the Special Characters Menu.
        /// </summary>
        private Object specialsStripLocker = new Object();

        /// <summary>
        /// Main Programs settings saved to an XML file.
        /// </summary>
        public Settings settings = new Settings();

        /// <summary>
        /// This is for storing a copy of the Cut/Copied Macro for Paste functions.
        /// </summary>
        CMacro clipboard_macro = null;

        /// <summary>
        /// TabTextBox (a derivation on TextBox) reference to the TextBox in which the TAB key was pressed.
        /// </summary>
        private TabTextBox caller = null;

        /// <summary>
        /// Character &lt;-&gt; Folder array.
        /// </summary>
        TagInfo[] characterList = null;

        ContextMenuStrip backupPhraseStrip = null;
        
        /// <summary>
        /// Storage variable for the default Auto-Translate context menu.
        /// </summary>
        ContextMenuStrip ATPhraseStrip = null;

        /// <summary>
        /// Storage variable for the default Specials character context menu.
        /// </summary>
        ContextMenuStrip Specials = null;

        #region Variables Specific to Drag & Drop FolderTreeView
        /// <summary>
        /// Variable which suppresses the Node.Text being updated due to TextBoxesChanged event being fired.
        /// </summary>
        private bool SuppressNodeUpdates = false;

        /// <summary>
        /// Suppresses the BeforeSelect event manually (returns success when entered). Used in RawSelect()
        /// </summary>
        private bool SuppressBeforeSelect = false;

        /// <summary>
        /// For DragLeave event to know when to cancel the actual drag action.
        /// </summary>
        private bool CancelOnLeave = false;

        /// <summary>
        /// Mouse Modifier definition for DragOver event (Right mouse button being held)
        /// </summary>
        /// <remarks>leftmouse is 0x01, middlemouse is 0x10</remarks>
        private int rightmouse = 0x02;

        /// <summary>
        /// Key Modifier definition for DragOver event (Ctrl key held)
        /// </summary>
        /// <remarks>shift is 0x04, alt is 0x20</remarks>
        private int ctrl = 0x08;

        /// <summary>
        /// The node that's being dragged. Removed during OnLeave and DragDrop events.
        /// </summary>
        //private TreeNode dragNode = null;

        /// <summary>
        /// The original node that was selected BEFORE the drag & drop operation began.
        /// </summary>
        private TreeNode originalNode = null;

        /// <summary>
        /// A temporary drop node for selection purposes.
        /// </summary>
        //private TreeNode tempDropNode = null;

        /// <summary>
        /// Timer for scrolling and expanding nodes while hovering.
        /// </summary>
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        /// <summary>
        /// FIX: The node that's currently being hovered over, maybe I could use tempDropNode instead?
        /// </summary>
        private TreeNode HoverNode = null;

        /// <summary>
        /// The interval for the timer function. (Refers to number of passes)
        /// </summary>
        private int interval = 0;
        #endregion

        /// <summary>
        /// Array of buttons for for() loops
        /// </summary>
        private Button[] buttons = new Button[20];

        /// <summary>
        /// Used to break out of the directory search Loop during recursion.
        /// </summary>
        private bool exitLoop = false;

        /// <summary>
        /// String with location of the FFXI Installation Path (found via Registry or manual selection)
        /// </summary>
        public string _FFXIInstallPath;

        /// <summary>
        /// The "Search for phrase" modeless dialog box. (Uses Show() instead of ShowDialog())
        /// </summary>
        private SearchForPhrase sfp = null;

        /// <summary>
        /// Used in the TabTextBox OnKeyDown and OnKeyPress event handler to keep certain characters from entering the TabTextBox.
        /// </summary>
        private bool nonAlphaNumEntered = false;
        #endregion

        #region MainForm Properties
        private FFXIATPhraseLoader ATPhraseLoader
        {
            get
            {
                return _ATPhraseLoader;
            }
        }

        /// <summary>
        /// Property for returning the Installation Path of FFXI if set.
        /// </summary>
        /// <value>Accesses the FFXI Installation Path private variable (get only).</value>
        public string FFXIInstallPath
        {
            get { return _FFXIInstallPath; }
        }
        #endregion

        #region MainForm Methods
        #region MainForm Methods (OnLoad, OnMinimize, OnResize, FormClosing Event Handlers)
        private void MainForm_Load(object sender, EventArgs e)
        {
            string s = Environment.CommandLine;
            if (!File.Exists("FFXI_ME_v2.chm"))
            {
                this.helpMainToolStripMenuItem.Visible = false;
                this.toolStripSeparator3.Visible = false;
            }

            int i = s.IndexOf('\"', 1); // file names are probably in quotes when run from Windows.
            if ((i != -1) && (i < (s.Length - 1)))
                s = s.Remove(i + 1);
            LogMessage.LogF("## Running {0} on {1} {2}", s,
                DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
            LogMessage.LogF("{0}", (Preferences.ShowDebugInfo == true) ? "Debugging Active!" : "Normal Operation Active.");
            LogMessage.LogF("Checking if East Asian Support is installed.");

            RegistryKey codepages = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Nls\CodePage");
            if (codepages == null)
            {
                LogMessage.LogF("..No Code Pages In The Registry Could Be Found, Attempting to continue");
            }
            else
            {
                string _932 = (string)codepages.GetValue("932"); // check for shift-jis
                string _1251 = (string)codepages.GetValue("1251");
                string _1252 = (string)codepages.GetValue("1252");
                bool notfound = false;
                if ((_932 == null) || (_932 == String.Empty))
                {
                    LogMessage.LogF("..Japanese (Shift-JIS) support not found");
                    notfound = true;
                }
                else LogMessage.LogF("..Japanese (Shift-JIS) support found");
                if ((_1251 == null) || (_1251 == String.Empty))
                {
                    LogMessage.LogF("..Cyrillic (Windows) support not found");
                    notfound = true;
                }
                else LogMessage.LogF("..Cyrillic (Windows) support found");

                if ((_1252 == null) || (_1252 == String.Empty))
                {
                    LogMessage.LogF("..Western European (Windows) support not found");
                    notfound = true;
                }
                else LogMessage.LogF("..Western European (Windows) support found");
                if (notfound)
                {
                    if (DialogResult.Cancel == MessageBox.Show(
                        "Japanese, Cyrillic, or Western European support not found.\r\n" +
                        "Please install East Asian Language Support via the Control Panel\r\n" +
                        "under \"Regional and Language Options\" on the \"Language\" tab.\r\n" +
                        "Hit OK to run FFXI ME!, Cancel to Exit.", "Language Support not installed.", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2))
                    {
                        LogMessage.LogF("..East Asian support not found, User chose to exit");
                        this.Close();
                        return;
                    }
                }
            }

            this.CursorCopy = null;
            this.CursorLink = null;
            this.CreateCursors();
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(SystemEvents_UserPreferenceChanged);

            //MessageBox.Show("Download .NET 2.0 here\r\n" + @"http://www.microsoft.com/downloads/details.aspx?FamilyID=0856EACB-4362-4B0D-8EDD-AAB15C5E04F5&displaylang=en");

            LogMessage.LogF("Loading Preferences...");
            LoadPreferences();
            LogMessage.LogF("..Done Loading Preferences.");

            if (MainForm.ShowOptionsDialog)
            {
                LogMessage.LogF("Options Requested...");
                OptionsDialog od = new OptionsDialog("FFXI ME! v2 Options");
                od.StartPosition = FormStartPosition.CenterScreen;
                if (od.ShowDialog() == DialogResult.OK)
                {
                    SavePreferences();
                    LogMessage.LogF("Options screen loaded and saved");
                }
                LogMessage.LogF("Exiting application");
                this.Close();
                return;
            }

            try
            {
                _ATPhraseLoader = new FFXIATPhraseLoader(Preferences.Language,
                    Preferences.LoadItems,
                    Preferences.LoadKeyItems,
                    Preferences.LoadAutoTranslatePhrases,
                    String.Format("Yekyaa's FFXI ME! v{0}",
                        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            }
            catch (FileNotFoundException exception)
            {
                LogMessage.LogF("Missing file in FFXI directory installation: ", exception.FileName);
                LogMessage.LogF("...Attempting to continue without the Auto-Translate Phrases.");
            }

            atphraseBW.DoWork += BuildATBackground;
            atphraseBW.RunWorkerAsync("buildATMenu");

            specialsBW.DoWork += BuildSpecialsBackground;
            specialsBW.RunWorkerAsync("buildSpecialsMenu");


            if (_ATPhraseLoader != null)
            {
                this._FFXIInstallPath = this.ATPhraseLoader.GetRegistryKey();// MainFormGetRegistryKey();
            }

            if (Preferences.PathToOpen.Count <= 0)
            {
                if (this.FFXIInstallPath != String.Empty)
                {
                    this.OpenFolderDialog.SelectedPath = String.Format("{0}\\USER\\", this.FFXIInstallPath.Trim('\\'));
                }
                else
                {
                    if (MessageBox.Show("FINAL FANTASY XI Installation not found, Continue?", "FFXI Not Found!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1) == DialogResult.No)
                        this.Close();
                    else
                    {
                        // set a default
                        this._FFXIInstallPath = @"C:\Program Files\PlayOnline\SquareEnix\FINAL FANTASY XI\";
                        this.OpenFolderDialog.SelectedPath = @"C:\Program Files\PlayOnline\SquareEnix\FINAL FANTASY XI\USER\";
                    }

                }
            }
            
            if (buttons == null)
                buttons = new Button[20];
            buttons[0] = buttonCtrl1;
            buttons[1] = buttonCtrl2;
            buttons[2] = buttonCtrl3;
            buttons[3] = buttonCtrl4;
            buttons[4] = buttonCtrl5;
            buttons[5] = buttonCtrl6;
            buttons[6] = buttonCtrl7;
            buttons[7] = buttonCtrl8;
            buttons[8] = buttonCtrl9;
            buttons[9] = buttonCtrl0;
            buttons[10] = buttonAlt1;
            buttons[11] = buttonAlt2;
            buttons[12] = buttonAlt3;
            buttons[13] = buttonAlt4;
            buttons[14] = buttonAlt5;
            buttons[15] = buttonAlt6;
            buttons[16] = buttonAlt7;
            buttons[17] = buttonAlt8;
            buttons[18] = buttonAlt9;
            buttons[19] = buttonAlt0;

            if (Preferences.PathToOpen.Count > 0)
            {
                OpenFolderMethod(Preferences.PathToOpen);
            }
            else OpenFolderMethod();

            this.BringToFront();
            //RestoreFFXI_ME();
        }

        private void MainForm_Closing(object sender, FormClosingEventArgs e)
        {
            LogMessage.LogF("Program Close requested...");
            bool Error = false;
            bool foundBook = false, foundFolder = false, foundFile = false;
            int original_drc_length = DeleteRenameChanges.Count;
            ExitAndSaveBox esb = new ExitAndSaveBox();

            foreach (TagInfo t in DeleteRenameChanges)
            {
                if (t.Type == "Delete_Folder")
                {
                    foundFolder = true;
                    break;
                }
            }

            esb.selectFoldersOnlyToolStripMenuItem.Visible = foundFolder;

            foreach (CBook cb in BookList)
            {
                if (cb.IsDeleted)
                {
                    if (File.Exists(cb.fName))
                    {
                        DeleteRenameChanges.Add(new TagInfo("Delete_File", cb.fName));
                        foundBook = true;
                    }
                }
                else if (cb.Changed)
                {
                    DeleteRenameChanges.Add(new TagInfo("Save_TTL", (Object)cb));
                    foundBook = true;
                }
            }

            esb.selectBooksOnlyToolStripMenuItem.Visible = foundBook;

            foreach (CMacroFile x in MacroFiles)
            {
                if (x.IsDeleted)
                {
                    if (File.Exists(x.fName))
                    {
                        DeleteRenameChanges.Add(new TagInfo("Delete_File", x.fName));
                        foundFile = true;
                    }
                }
                else if (x.Changed == true)
                {
                    DeleteRenameChanges.Add(new TagInfo("Save_File", x));
                    foundFile = true;
                }
            }

            esb.selectMacroFilesOnlyToolStripMenuItem.Visible = foundFile;

            esb.toolStripSeparator1.Visible = (foundFolder || foundFile || foundBook);

            if (DeleteRenameChanges.Count > 1)
            {
                #region If there's more than 1 item to save, show CheckedListBox
                esb.checkedListBox1.Items.Clear();
                if (this.WindowState == FormWindowState.Minimized)
                    esb.StartPosition = FormStartPosition.CenterScreen;

                foreach (TagInfo tiLoop in DeleteRenameChanges)
                {
                    #region for each item in the DRC array, add it to the CheckedListBox
                    if (tiLoop.Type != "Skip")
                    {
                        // don't add Deletion of Folders or Files
                        // if the directory doesn't exist
                        // Files should be handled above, but I may not have
                        // counted on some old code lying around somewhere.
                        if ((tiLoop.Type == "Delete_Folder") &&
                            !Directory.Exists(tiLoop.Text))
                            continue;
                        else if ((tiLoop.Text == "Delete_File") &&
                            !File.Exists(tiLoop.Text))
                            continue;
                        esb.checkedListBox1.Items.Add(tiLoop, true);
                    }
                    #endregion
                }

                DialogResult dr;
                LogMessage.LogF("..Save All Changes Requested");
                exitLoop = false;
                while (exitLoop == false)
                {
                    dr = esb.ShowDialog();
                    exitLoop = true;
                    if (dr == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                        LogMessage.LogF("...Exit & Save cancelled");
                        // Remove the newly added Files that need to be saved.
                        DeleteRenameChanges.RemoveRange(original_drc_length, DeleteRenameChanges.Count - original_drc_length);
                        return;
                    }
                    else if (dr == DialogResult.Yes)
                    {
                        #region If we choose to Save
                        if (esb.checkedListBox1.CheckedItems.Count > 0)
                        {
                            foreach (object x in esb.checkedListBox1.CheckedItems)
                            {
                                Error = SaveTagInfo(x as TagInfo);
                            }
                            LogMessage.LogF("...Save All Changes Completed {0}.", (Error == true) ? "With Errors" : "Successfully.");
                        }
                        else
                        {
                            exitLoop = false;
                            MessageBox.Show("You need to pick something if you want to Save & Exit!", "Select 'Just Exit' or 'Cancel!' instead.");
                        }
                        #endregion
                    }
                    else
                    {
                        LogMessage.LogF("...Save All Declined, Exiting.");
                    }
                }
                #endregion
            }
            else if (DeleteRenameChanges.Count == 1)
            {
                #region If there's exactly one item, use a MessageBox instead
                DialogResult dr = MessageBox.Show(DeleteRenameChanges[0].ToString(), "Save before exiting?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button2);
                if (dr == DialogResult.Cancel)
                {
                    #region Cancel the close, reset the Changes array, and return
                    e.Cancel = true;
                    LogMessage.LogF("...Exit & Save cancelled");
                    // Remove the newly added Files that need to be saved.
                    DeleteRenameChanges.RemoveRange(original_drc_length, DeleteRenameChanges.Count - original_drc_length);
                    return;
                    #endregion
                }
                else if (dr == DialogResult.Yes)
                {
                    Error = SaveTagInfo(DeleteRenameChanges[0]);
                    LogMessage.LogF("...Save All Changes Completed {0}.", (Error == true) ? "With Errors" : "Successfully.");
                }
                else
                {
                    LogMessage.LogF("...Save All Declined, Exiting.");
                }
                #endregion
            }
            notifyIcon.Visible = false;
            LogMessage.LogF("..Saving Preferences...");
            SavePreferences();
            LogMessage.LogF("...Done Saving Preferences.");
            SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(SystemEvents_UserPreferenceChanged);
            LogMessage.LogF("..Unloaded Event Handler");
            LogMessage.LogF("Program Closing Reason:");
            CloseReason clr = e.CloseReason;

            if (clr == CloseReason.ApplicationExitCall)
                LogMessage.LogF(".." + clr.ToString() + " : Application Error");
            else if (clr == CloseReason.None)
                LogMessage.LogF(".." + clr.ToString() + " : Reason Unknown");
            else if (clr == CloseReason.TaskManagerClosing)
                LogMessage.LogF(".." + clr.ToString() + " : Forced Close");
            else LogMessage.LogF(".." + clr.ToString() + " : Pretty self-explanatory");
            LogMessage.Close();
        }

        private bool SaveTagInfo(TagInfo ti)
        {
            bool Error = false;
            #region Save the one item there is, based on Type
            if (ti.Type == "Save_File")
            {
                #region Saving a regular Macro File
                CMacroFile cmf = ti.Object1 as CMacroFile;
                if ((cmf != null) && (cmf.Changed == true))
                {
                    if (cmf.Save() != true)
                    {
                        LogMessage.LogF("...Error while saving {0}, skipping.", cmf.fName);
                        Error = true;
                    }
                }
                #endregion
            }
            else if (ti.Type == "Save_TTL")
            {
                CBook cb = ti.Object1 as CBook;
                if (cb != null)
                {
                    if (cb.Save())
                        LogMessage.LogF("...Saved book {0}", cb.fName);
                    else Error = true;
                }
            }
            else if (ti.Type == "Delete_Folder")
            {
                if (Directory.Exists(ti.Text))
                {
                    Directory.Delete(ti.Text, true);
                    LogMessage.LogF("...Removed directory {0} and all sub-directories and files.", ti.Text);
                }
            }
            else if (ti.Type == "Delete_File")
            {
                if (File.Exists(ti.Text))
                {
                    File.Delete(ti.Text);
                    LogMessage.LogF("...Deleted File {0}", ti.Text);
                }
            }
            #endregion
            return Error;
        }

        private void MainForm_Resize(object sender, System.EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Preferences.IsMaximized = false;
                if (this.sfp != null)
                {
                    if (this.sfp.sfpvisible == true)
                        this.sfp.Visible = false;
                }

                if (Preferences.MinimizeToTray)
                {
                    //Show the Icon in the system tray:
                    this.notifyIcon.Visible = true;
                    //Remove the Program from the Task Bar
                    this.ShowInTaskbar = false;
                    //Set the Form to InVisible to remove the ability to Alt-Tab to it.
                    this.Visible = false;
                }
            }
            else if (this.WindowState != FormWindowState.Minimized)
            {
                // in case it's minimized immediately after a resize/restore
                // I want to store the last size state
                // and considering OnMove is called right as form is loading
                // I didn't want to take a chance that Resize is done as well.
                if (this.WindowState == FormWindowState.Normal)
                {
                    Preferences.IsMaximized = false;
                }
                else if (this.WindowState == FormWindowState.Maximized)
                {
                    Preferences.IsMaximized = true;
                }

                //Check to see if the window has been Minimized:
                if (this.sfp != null)
                {
                    if (this.sfp.sfpvisible == true)
                    {
                        this.sfp.Visible = true;
                        this.sfp.BringToFront();
                        this.sfp.Focus(); // STILL DOES NOT FOCUS IT! WTF!
                    }
                }
            }
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {

            if (e.Category == Microsoft.Win32.UserPreferenceCategory.Mouse)
            {
                // UserPreference Changed is fired 3 times on mouse cursor
                // change and most other mouse mods, which is the only one I care about.
                LogMessage.LogF("...Mouse Preferences Changed, recreating Cursors", e.Category);
                if (this.CursorCopy != null)
                {
                    this.CursorCopy.Dispose();
                    this.CursorCopy = null;
                }
                if (this.CursorLink != null)
                {
                    this.CursorLink.Dispose();
                    this.CursorLink = null;
                }
                this.CreateCursors();
            }
        }
        #endregion

        #region MainForm Methods (Default Auto-Translate ContextMenuStrip Builder)
        private ToolStripMenuItem[] MakeMenuGroup(int language)
        {
            ToolStripMenuItem newitem = null;
            //english = null, japanese = null, deutsch = null, french = null
            ToolStripMenuItem[] target_category = null; // categories_en = null, categories_jp = null, 
            FFXIATPhrase[] atp = this.ATPhraseLoader.ATPhrases;
            int category_cnt = 0; // en_cat_cnt = 0, jp_cat_cnt = 0, 
            string GroupName = "Unknown Group";
            string ItemName = String.Empty;

            MenuCompare mcompare = new MenuCompare();
            if (atp != null)
            {
                for (int i = 0; i < atp.Length; i++)
                {
                    if ((atp[i] == null) || (atp[i].MessageID == 0x00)) continue;
                    else if (atp[i].Language != language) continue;
                    else if (atp[i].value.Trim() == String.Empty) continue;

                    byte b1 = atp[i].StringResource,
                         b2 = atp[i].Language,
                         b3 = atp[i].GroupID,
                         b4 = atp[i].MessageID;

                    ItemName = atp[i].value;

                    if (ItemName == " ") continue;

                    FFXIATPhrase grp = this.ATPhraseLoader.GetPhraseByID(b1, b2, b3);

                    if (grp != null)
                        GroupName = grp.value;
                    else GroupName = "Unknown Group";

                    if (target_category == null)
                    {
                        target_category = new ToolStripMenuItem[1];
                        if ((b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX) &&
                            (b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN))
                            target_category[0] = new ToolStripMenuItem(GroupName, Icons.GeneralIcon[b2]);
                        //else target_category[0] = new ToolStripMenuItem(GroupName);
                        category_cnt = 0;
                    }
                    else
                    {
                        for (category_cnt = 0; category_cnt < target_category.Length; category_cnt++)
                        {
                            if (target_category[category_cnt].Text == GroupName)
                                break;
                        }
                        if (category_cnt == target_category.Length)
                        {
                            Array.Resize(ref target_category, target_category.Length + 1);
                            if ((b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX) &&
                                (b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN))
                                target_category[target_category.Length - 1] = new ToolStripMenuItem(GroupName, Icons.GeneralIcon[b2]);
                        }
                    }
                    newitem = new ToolStripMenuItem(ItemName, null, ContextAT_Click);
                    if ((b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX) &&
                        (b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN))
                        newitem.Image = Icons.GeneralIcon[b2];
                    newitem.Name = atp[i].ToString(); // FFXIEncoding.atp_String(atp[i]);
                    target_category[category_cnt].DropDownItems.Add(newitem);
                }
            }

            if (target_category != null)
            {
                // Sorting
                for (int _cnt = 0; _cnt < target_category.Length; _cnt++)
                {
                    if (target_category[_cnt].DropDownItems.Count < 19) continue;
                    ToolStripItem[] ts = new ToolStripItem[target_category[_cnt].DropDownItems.Count];
                    target_category[_cnt].DropDownItems.CopyTo(ts, 0);
                    Array.Sort(ts, mcompare);
                    target_category[_cnt].DropDownItems.Clear();
                    target_category[_cnt].DropDownItems.AddRange(ts);
                }

                return target_category;
            }
            return (null);
        }
        private ToolStripMenuItem[] MakeMenuGroup()
        {
            return MakeMenuGroup(Preferences.Language);
        }

        private ContextMenuStrip BuildATMenu(int language)
        {
            ContextMenuStrip cms = new ContextMenuStrip();
            ToolStripMenuItem[] target_category = null;

            ToolStripMenuItem target = null;
            cms.SuspendLayout();
            cms.Padding = new Padding(cms.Padding.Left, cms.Padding.Top - 4, cms.Padding.Right, cms.Padding.Bottom - 4);
            //cms.ShowImageMargin = false;
            //cms.ShowCheckMargin = false;
            ToolStripLabel header = new ToolStripLabel("Auto-Translate Menu");
            Font f = new Font(header.Font, FontStyle.Bold);
            header.Font = f;

            cms.Items.Add(header);
            cms.Items.Add(new ToolStripSeparator());

            if (language == FFXIATPhraseLoader.ffxiLanguages.LANG_ALL) // ffxi.FileTypes.LANG_ALL
            {
                for (int i = FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN;
                    i <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX; i++)
                {
                    target_category = MakeMenuGroup(i);
                    if (target_category != null)
                    {
                        target = new ToolStripMenuItem(FFXIATPhraseLoader.Languages[i], Icons.GeneralIcon[i]);
                        target.DropDownItems.AddRange(target_category);
                        if (target != null)
                        cms.Items.Add(target);
                    }
                    else
                    {
                        cms.Items.Add(new ToolStripLabel(String.Format("No {0} Phrases Loaded.",
                        FFXIATPhraseLoader.Languages[i])));
                    }
                    target = null;
                    target_category = null;
                }
            }
            else
            {
                target_category = MakeMenuGroup(language);
                if (target_category != null)
                {
                    cms.Items.AddRange(target_category);
                }
                else
                {
                    cms.Items.Add(new ToolStripLabel(String.Format("No {0}Phrases Loaded.",
                      ((language <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX) &&
                       (language >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN)) ?
                       FFXIATPhraseLoader.Languages[language] + " " : "")));
                }
                target = null;
                target_category = null;
            }
            cms.ResumeLayout(true);
            return cms;
        }

        void BuildATBackground(object sender, DoWorkEventArgs e)
        {
            // Do not access the form's BackgroundWorker reference directly.
            // Instead, use the reference provided by the sender parameter.
            BackgroundWorker bw = sender as BackgroundWorker;

            // If the operation was canceled by the user, 
            // set the DoWorkEventArgs.Cancel property to true.
            if (bw.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            lock (atphraseStripLocker)
            {
                if (this.ATPhraseStrip == null)
                {
                    LogMessage.LogF("...BuildATBackground(): Auto-Translate Phrase Context Menu Creation Begun.");
                    this.backupPhraseStrip = BuildATMenu(Preferences.Language);
                    this.ATPhraseStrip = BuildATMenu(Preferences.Language);
                    LogMessage.LogF("...BuildATBackground(): Auto-Translate Phrase Context Menu Creation Completed.");
                }
            }
        }

        private char GetChar(String x)
        {
            if ((x == String.Empty) || (x.Length != 4))
                return ' ';

            if (!System.Text.RegularExpressions.Regex.IsMatch(x, "[^a-fA-F0-9]"))
            {
                int i = Int32.Parse(x, System.Globalization.NumberStyles.AllowHexSpecifier);
                char c = (char)i;
                return c;
            }
            return '?';
        }

        private ToolStripMenuItem ProcessXML(XmlNode x)
        {
            if (x == null)
                return null;
            ToolStripMenuItem retValue = null;
            if (x.NodeType == XmlNodeType.Element)
            {
                retValue = new ToolStripMenuItem();

                if ((x.Name == "menu") || (x.Name == "main"))
                {
                    #region Process "menu" or "main" tags
                    String text = "<Menu Name>";
                    if (x.Attributes != null)
                    {
                        foreach (XmlAttribute a in x.Attributes)
                        {
                            if ((a.Name == "text") || (a.Name == "name") || (a.Name == "value"))
                                text = a.Value;
                        }
                    }
                    retValue.Text = text;
                    if (x.HasChildNodes)
                    {
                        ToolStripMenuItem tmp = null;
                        foreach (XmlNode xN in x.ChildNodes)
                        {
                            tmp = ProcessXML(xN);
                            if (tmp != null)
                                retValue.DropDownItems.Add(tmp);
                        }
                    }
                    #endregion
                }
                else if (x.Name == "insertphrase")
                {
                    #region Process "insertphrase" tag
                    String texttoconvert = "<Item Name>";
                    #region Check Attributes for the Values
                    if (x.Attributes != null)
                    {
                        foreach (XmlAttribute a in x.Attributes)
                        {
                            if ((a.Name == "value") || 
                                (a.Name == "text") || 
                                (a.Name == "phrase"))
                            {
                                texttoconvert = a.Value;
                            }
                        }
                    }
                    // setup for ContextAT_Click
                    // set Name equal to what we want to replace with.
                    retValue.Name = texttoconvert; // else use text as given
                    #endregion

                    #region Process for end-user viewable text
                    // only time we have child nodes is if there were closing brackets and text between them
                    if (!x.HasChildNodes || ((x.HasChildNodes) && (x.ChildNodes.Count == 1)))
                    {
                        retValue.Text = "<Unknown Phrase>";
                        if (x.InnerText == String.Empty)
                        {
                            if (!x.HasChildNodes)
                                retValue.Text = texttoconvert;
                        }
                        else
                        {
                            if (x.HasChildNodes && (x.ChildNodes.Count == 1))
                            {
                                retValue.Text = x.InnerText;
                                // if we wanted to insert a character
                                // but wanted to give it a different name
                                // set the Image to a graphic of the character to be
                                // inserted.
                                if (texttoconvert.Length == 1)
                                {
                                    Bitmap bmp = new Bitmap(32, 32);
                                    Graphics g = Graphics.FromImage(bmp);
                                    g.DrawString(texttoconvert, new Font(retValue.Font.FontFamily, 20.0f, FontStyle.Bold), new SolidBrush(Color.Black), -7.5f, -2.5f);
                                    retValue.Image = bmp;
                                }
                            }
                        }
                        retValue.Click += this.ContextAT_Click;
                    }
                    else return null;
                    #endregion
                    #endregion
                }
                else if (x.Name == "insertchar")
                {
                    #region InsertChar only
                    String texttoconvert = "<Item Name>";
                    #region Process Attributes for value to insert
                    if (x.Attributes != null)
                    {
                        foreach (XmlAttribute a in x.Attributes)
                        {
                            if ((a.Name == "item") || (a.Name == "value"))
                            {
                                texttoconvert = a.Value;
                            }
                        }
                    }

                    string cvt_string = String.Format("{0}", GetChar(texttoconvert));
                    #endregion
                    #region Process text node if any for user-viewable menu item text
                    // setup for ContextAT_Click by setting
                    // Name equal to what we want to replace with.
                    retValue.Name = cvt_string;
                    retValue.Text = "<Unknown Char>";
                    // only time we have child nodes is if there were closing brackets and text between them
                    if ((!x.HasChildNodes) || ((x.HasChildNodes) && (x.ChildNodes.Count == 1)))
                    {
                        if (x.InnerText == String.Empty)
                        {
                            if (!x.HasChildNodes)
                                retValue.Text = cvt_string;
                        }
                        else
                        {
                            if (x.HasChildNodes && (x.ChildNodes.Count == 1))
                            {
                                retValue.Text = x.InnerText;

                            // if we wanted to insert a character
                            // but wanted to give it a different name
                            // set the Image to a graphic of the character to be
                            // inserted.
                            if (cvt_string.Length == 1)
                            {
                                Bitmap bmp = new Bitmap(32, 32);
                                Graphics g = Graphics.FromImage(bmp);
                                g.DrawString(cvt_string, new Font(retValue.Font.FontFamily, 20.0f, FontStyle.Bold), new SolidBrush(Color.Black), -7.5f, -2.5f);
                                //g.DrawString(cvt_string, retValue.Font, new SolidBrush(Color.Black), 0.1f, 0.1f);
                                retValue.Image = bmp;
                            }
                            }
                        }
                        retValue.Click += this.ContextAT_Click;
                    }
                    else return null;
                    #endregion
                    #endregion
                }
                #region Old Code
                    /*
                else if (x.Name == "insert")
                {
                    #region InsertOnly
                    String texttoconvert = "<Item Name>";
                    bool Convert = false;
                    if (x.Attributes != null)
                    {
                        foreach (XmlAttribute a in x.Attributes)
                        {
                            if ((a.Name == "item") || (a.Name == "value"))
                            {
                                Convert = true;
                                texttoconvert = a.Value;
                            }
                            else if ((a.Name == "name") || (a.Name == "text") || (a.Name == "phrase"))
                            {
                                Convert = false;
                                texttoconvert = a.Value;
                            }
                        }
                    }
                    // setup for ContextAT_Click
                    char cvt = GetChar(texttoconvert);
                    string cvt_string = String.Format("{0}", cvt);
                    // set Name equal to what we want to replace with.
                    if (Convert) // if convertable chosen
                        retValue.Name = cvt_string;
                    else retValue.Name = texttoconvert; // else use text as given

                    // only time we have child nodes is if there were closing brackets and text between them
                    if ((x.HasChildNodes) && (x.ChildNodes.Count == 1))
                    {
                        if (x.InnerText == String.Empty)
                        {
                            LogMessage.Log("..... HasChildNodes true, ChildNodes.Count == 1, but no InnerText! {0}", x.Value);
                        }
                        else
                        {
                            retValue.Text = x.InnerText;
                            // if we wanted to insert a character
                            // but wanted to give it a different name
                            // set the Image to a graphic of the character to be
                            // inserted.
                            if (Convert && (cvt_string.Length == 1))
                            {
                                Bitmap bmp = new Bitmap(32, 32);
                                Graphics g = Graphics.FromImage(bmp);
                                g.DrawString(cvt_string, new Font(retValue.Font.FontFamily, 20.0f, FontStyle.Bold), new SolidBrush(Color.Black), -7.5f, -2.5f);
                                //g.DrawString(cvt_string, retValue.Font, new SolidBrush(Color.Black), 0.1f, 0.1f);
                                retValue.Image = bmp;
                            }
                        }
                        retValue.Click += this.ContextAT_Click;
                    }
                    else if (x.HasChildNodes && (x.ChildNodes.Count > 1))
                        return null;
                    else if (!x.HasChildNodes)
                    {
                        // if we don't have childnodes, use the text given as the Menu Name
                        if (x.InnerText == String.Empty)
                        {
                            if (Convert)
                                retValue.Text = cvt_string;
                            else retValue.Text = texttoconvert;
                        }
                        else
                        {
                            LogMessage.Log("No ChildNodes but innerText is there: {0}", x.InnerText);
                            //retValue.Text = x.InnerText;
                        }
                        retValue.Click += this.ContextAT_Click;
                    }
                #endregion
                }*/
                #endregion
            }
            return retValue;
        }
        private ContextMenuStrip BuildSpecialsMenu()
        {
            ContextMenuStrip cms = new ContextMenuStrip();

            cms.SuspendLayout();
            cms.Padding = new Padding(cms.Padding.Left, cms.Padding.Top - 4, cms.Padding.Right, cms.Padding.Bottom - 4);

            //cms.Items.Add(new ToolStripLabel("Undo Test", FFXI_ME_v2.Properties.Resources.UndoMacro));
            #region Add new Special characters here
            String filename = Preferences.MenuXMLFile;
            if (File.Exists(filename))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    try { doc.Load(filename); }
                    catch { doc.LoadXml("<main></main>"); }
                    XmlNode xN = doc.SelectSingleNode("main");

                    if (xN != null)
                    {
                        ToolStripLabel header = null;
                        String text = String.Empty;
                        if (xN.Attributes != null)
                        {
                            foreach (XmlAttribute a in xN.Attributes)
                            {
                                if ((a.Name == "text") || (a.Name == "name") || (a.Name == "value") || (a.Name == "item"))
                                    text = a.Value;
                            }
                        }
                        if (text == String.Empty)
                            header = new ToolStripLabel("Special Characters Menu");
                        else header = new ToolStripLabel(text);
                        Font f = new Font(header.Font, FontStyle.Bold);
                        header.Font = f;
                        cms.Items.Add(header);
                        cms.Items.Add(new ToolStripSeparator());

                        ToolStripMenuItem[] tsmi = null;
                        ToolStripMenuItem tmp = null;
                        if (xN.HasChildNodes) // should ALWAYS be the case.
                        {
                            tsmi = new ToolStripMenuItem[xN.ChildNodes.Count];
                            int cnt = 0;
                            for (int i = 0; i < xN.ChildNodes.Count; i++)
                            {
                                tmp = ProcessXML(xN.ChildNodes[i]);
                                if (tmp != null)
                                    tsmi[cnt++] = tmp;
                            }
                            if (cnt < xN.ChildNodes.Count)
                                Array.Resize(ref tsmi, cnt);
                        }
                        else
                        {
                            tsmi = new ToolStripMenuItem[1];
                            tmp = ProcessXML(xN);
                            if (tmp != null)
                                tsmi[0] = tmp;
                        }
                        if (xN.NextSibling != null)
                            LogMessage.Log("main has siblings?!");
                        if (tsmi != null)
                            cms.Items.AddRange(tsmi);
                    }
                    else LogMessage.Log(" main has no Nodes!");
                }
                catch (Exception e)
                {
                    LogMessage.Log("...Error loading Specials Menu: Exception e: " + e);
                    cms.Items.Add(new ToolStripLabel("Error loading menu"));
                }
                finally
                {

                }
            }
            else // if file does NOT exist, we don't even want it here.
            {
                return null;
                //   cms.Items.Add(new ToolStripLabel("Unable to load menu"));
            }
            #endregion
            cms.ResumeLayout(true);
            return cms;
        }

        void BuildSpecialsBackground(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            if (bw.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            lock (specialsStripLocker)
            {
                LogMessage.LogF("...BuildSpecialsMenu(): Special Character Context Menu Creation Begun.");
                this.Specials = BuildSpecialsMenu();
                LogMessage.LogF("...BuildSpecialsMenu(): Special Character Context Menu Creation Completed.");
            }
        }
        #endregion

        #region MainForm Methods (Main Menu functions)
        #region File Menu
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Preferences.PathToOpen.Clear();
            OpenFolderMethod();
        }
        #region File/Backup Submenu Items
        private void backupCurrentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CMacroFile cmf = FindMacroFileByNode(treeView.SelectedNode);
            CMacro cm = FindMacroByNode(treeView.SelectedNode);

            if ((treeView.SelectedNode != treeView.Nodes[0]) && (treeView.SelectedNode.Parent != treeView.Nodes[0]))
            {
                if (cmf == null)
                {
                    MessageBox.Show("You must select a folder or file first in order to Backup!", "Error while backing up");
                    return;
                }
            }
            else
            {
                MessageBox.Show(treeView.SelectedNode.Name);
                return;
            }

            if ((cm == null) && (cmf != null))
                cm = GetCurrentMacro(cmf);

            if (cm != null)
                SaveToMemory(cm);

            // if this MacroFile node has a parent
            // who does it belong to?
            if ((cmf.thisNode.Parent != null) && (cmf.thisNode.Parent.Parent != null) &&
                (cmf.thisNode.Parent.Parent == treeView.Nodes[0]))
            {
                MessageBox.Show(cmf.thisNode.Parent.Name);
            }

            String folder = PickBackupFolder();
            /*if (cmf != null)
            {
                if (cmf.Save() == true)
                {
                    MessageBox.Show(cmf.fName + " saved successfully.", "You have been Saved!");
                }
                else MessageBox.Show(cmf.fName + " could NOT be saved!", "Error while saving");
            }
            else MessageBox.Show("You must select a folder or file first in order to Save!", "Error while saving");
             */
        }

        private void backupAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Backup ALL Not Yet Implemented!");
        }
        #endregion
        #region File/Save Submenu Items (also includes the SaveAllChanges() function
        private void saveCurrentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CMacroFile cmf = FindMacroFileByNode(treeView.SelectedNode);
            CMacro cm = FindMacroByNode(treeView.SelectedNode);

            this.SetWaitCursor();

            if ((cm == null) && (cmf != null))
                cm = GetCurrentMacro(cmf);

            if (cm != null)
                SaveToMemory(cm);

            if (cmf != null)
            {
                if (cmf.Save() == true)
                {
                    MessageBox.Show(cmf.fName + " saved successfully.", "You have been Saved!");
                }
                else MessageBox.Show(cmf.fName + " could NOT be saved!", "Error while saving");
            }
            else MessageBox.Show("You must select a folder or file first in order to Save!", "Error while saving");

            this.RestoreCursor();
        }

        private void SaveAllMacroSets(bool Exit, bool ChangesOnly)
        {
            if (MacroFiles.Count < 1)
            {
                MessageBox.Show("You must open a folder or file first in order to Save All!", "Error while saving");
                return;
            }
            ExitAndSaveBox esb = new ExitAndSaveBox("Save which Macro Files?", "Select which files to save:", "Save!", String.Empty, "Forget It!");
            this.SetWaitCursor();
            bool Error = false;
            int original_drc_length = DeleteRenameChanges.Count;
            TagInfo[] original = DeleteRenameChanges.ToArray();

            DeleteRenameChanges.Clear();

            esb.selectFoldersOnlyToolStripMenuItem.Visible = false;

            esb.selectBooksOnlyToolStripMenuItem.Visible = false;

            foreach (CMacroFile x in MacroFiles)
            {
                if (x.IsDeleted)
                {
                    if (File.Exists(x.fName))
                    {
                        DeleteRenameChanges.Add(new TagInfo("Delete_File", x.fName));
                    }
                }
                else if (x.Changed == true)
                {
                    DeleteRenameChanges.Add(new TagInfo("Save_File", x));
                }
            }

            esb.selectMacroFilesOnlyToolStripMenuItem.Visible = false;

            esb.toolStripSeparator1.Visible = false;

            if (DeleteRenameChanges.Count > 1)
            {
                #region If there's more than 1 item to save, show CheckedListBox
                esb.checkedListBox1.Items.Clear();
                if (this.WindowState == FormWindowState.Minimized)
                    esb.StartPosition = FormStartPosition.CenterScreen;

                foreach (TagInfo tiLoop in DeleteRenameChanges)
                {
                    #region for each item in the DRC array, add it to the CheckedListBox
                    if (tiLoop.Type != "Skip")
                    {
                        // don't add Deletion of Folders or Files
                        // if the directory doesn't exist
                        // Files should be handled above, but I may not have
                        // counted on some old code lying around somewhere.
                        if ((tiLoop.Type == "Delete_Folder") &&
                            !Directory.Exists(tiLoop.Text))
                            continue;
                        else if ((tiLoop.Text == "Delete_File") &&
                            !File.Exists(tiLoop.Text))
                            continue;
                        esb.checkedListBox1.Items.Add(tiLoop, true);
                    }
                    #endregion
                }

                DialogResult dr;
                LogMessage.LogF("..Save All Changes Requested");
                exitLoop = false;
                while (exitLoop == false)
                {
                    dr = esb.ShowDialog();
                    exitLoop = true;
                    if (dr == DialogResult.Cancel)
                    {
                        LogMessage.LogF("...Exit & Save cancelled");
                        // Remove the newly added Files that need to be saved.
                        DeleteRenameChanges.Clear();
                        for (int xi = 0; xi < original.Length; xi++)
                            DeleteRenameChanges.Add(original[xi]);
                        this.RestoreCursor();
                        return;
                    }
                    else if (dr == DialogResult.Yes)
                    {
                        #region If we choose to Save
                        if (esb.checkedListBox1.CheckedItems.Count > 0)
                        {
                            foreach (object x in esb.checkedListBox1.CheckedItems)
                            {
                                Error = SaveTagInfo(x as TagInfo);
                            }
                            LogMessage.LogF("...Save All Changes Completed {0}.", (Error == true) ? "With Errors" : "Successfully.");
                        }
                        else
                        {
                            exitLoop = false;
                            MessageBox.Show("You need to pick something if you want to Save & Exit!", "Select 'Just Exit' or 'Cancel!' instead.");
                        }
                        #endregion
                    }
                    else
                    {
                        LogMessage.LogF("...Save All Declined, Exiting.");
                    }
                }
                #endregion
            }
            else if (DeleteRenameChanges.Count == 1)
            {
                #region If there's exactly one item, use a MessageBox instead
                DialogResult dr = MessageBox.Show(DeleteRenameChanges[0].ToString(), "Save before exiting?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button2);
                if (dr == DialogResult.Cancel)
                {
                    #region Cancel the close, reset the Changes array, and return
                    LogMessage.LogF("...Exit & Save cancelled");
                    // Remove the newly added Files that need to be saved.
                    DeleteRenameChanges.Clear();
                    for (int xi = 0; xi < original.Length; xi++)
                        DeleteRenameChanges.Add(original[xi]);
                    this.RestoreCursor();
                    return;
                    #endregion
                }
                else if (dr == DialogResult.Yes)
                {
                    Error = SaveTagInfo(DeleteRenameChanges[0]);
                    LogMessage.LogF("...Save All Changes Completed {0}.", (Error == true) ? "With Errors" : "Successfully.");
                }
                else
                {
                    LogMessage.LogF("...Save All Declined, Exiting.");
                }
                #endregion
            }
            else if (DeleteRenameChanges.Count == 0)
            {
                MessageBox.Show("No Macrofiles need saving.", "No files to save.");
            }
            this.RestoreCursor();
            DeleteRenameChanges.Clear();
            for (int xi = 0; xi < original.Length; xi++)
                DeleteRenameChanges.Add(original[xi]);
        }

        private void SaveAllChanges()
        {
            if (MacroFiles.Count < 1)
            {
                MessageBox.Show("You must open a folder or file first in order to Save All!", "Error while saving");
                return;
            }
            ExitAndSaveBox esb = new ExitAndSaveBox("Save which changes?", "Select which changes to save:", "Save!", String.Empty, "Forget It!");
            this.SetWaitCursor();
            bool Error = false;
            bool foundBook = false, foundFolder = false, foundFile = false;
            int original_drc_length = DeleteRenameChanges.Count;

            foreach (TagInfo t in DeleteRenameChanges)
            {
                if (t.Type == "Delete_Folder")
                {
                    foundFolder = true;
                    break;
                }
            }

            esb.selectFoldersOnlyToolStripMenuItem.Visible = foundFolder;

            foreach (CBook cb in BookList)
            {
                if (cb.IsDeleted)
                {
                    if (File.Exists(cb.fName))
                    {
                        DeleteRenameChanges.Add(new TagInfo("Delete_File", cb.fName));
                        foundBook = true;
                    }
                }
                else if (cb.Changed)
                {
                    DeleteRenameChanges.Add(new TagInfo("Save_TTL", (Object)cb));
                    foundBook = true;
                }
            }

            esb.selectBooksOnlyToolStripMenuItem.Visible = foundBook;

            foreach (CMacroFile x in MacroFiles)
            {
                if (x.IsDeleted)
                {
                    if (File.Exists(x.fName))
                    {
                        DeleteRenameChanges.Add(new TagInfo("Delete_File", x.fName));
                        foundFile = true;
                    }
                }
                else if (x.Changed == true)
                {
                    DeleteRenameChanges.Add(new TagInfo("Save_File", x));
                    foundFile = true;
                }
            }

            esb.selectMacroFilesOnlyToolStripMenuItem.Visible = foundFile;

            esb.toolStripSeparator1.Visible = (foundFolder || foundFile || foundBook);

            if (DeleteRenameChanges.Count > 1)
            {
                #region If there's more than 1 item to save, show CheckedListBox
                esb.checkedListBox1.Items.Clear();
                if (this.WindowState == FormWindowState.Minimized)
                    esb.StartPosition = FormStartPosition.CenterScreen;

                foreach (TagInfo tiLoop in DeleteRenameChanges)
                {
                    #region for each item in the DRC array, add it to the CheckedListBox
                    if (tiLoop.Type != "Skip")
                    {
                        // don't add Deletion of Folders or Files
                        // if the directory doesn't exist
                        // Files should be handled above, but I may not have
                        // counted on some old code lying around somewhere.
                        if ((tiLoop.Type == "Delete_Folder") &&
                            !Directory.Exists(tiLoop.Text))
                            continue;
                        else if ((tiLoop.Text == "Delete_File") &&
                            !File.Exists(tiLoop.Text))
                            continue;
                        esb.checkedListBox1.Items.Add(tiLoop, true);
                    }
                    #endregion
                }

                DialogResult dr;
                LogMessage.LogF("..Save All Changes Requested");
                exitLoop = false;
                while (exitLoop == false)
                {
                    dr = esb.ShowDialog();
                    exitLoop = true;
                    if (dr == DialogResult.Cancel)
                    {
                        LogMessage.LogF("...Exit & Save cancelled");
                        // Remove the newly added Files that need to be saved.
                        DeleteRenameChanges.RemoveRange(original_drc_length, DeleteRenameChanges.Count - original_drc_length);
                        this.RestoreCursor();
                        return;
                    }
                    else if (dr == DialogResult.Yes)
                    {
                        #region If we choose to Save
                        if (esb.checkedListBox1.CheckedItems.Count > 0)
                        {
                            foreach (object x in esb.checkedListBox1.CheckedItems)
                            {
                                Error = SaveTagInfo(x as TagInfo);
                            }
                            LogMessage.LogF("...Save All Changes Completed {0}.", (Error == true) ? "With Errors" : "Successfully.");
                        }
                        else
                        {
                            exitLoop = false;
                            MessageBox.Show("You need to pick something if you want to Save & Exit!", "Select 'Just Exit' or 'Cancel!' instead.");
                        }
                        #endregion
                    }
                    else
                    {
                        LogMessage.LogF("...Save All Declined, Exiting.");
                    }
                }
                #endregion
            }
            else if (DeleteRenameChanges.Count == 1)
            {
                #region If there's exactly one item, use a MessageBox instead
                DialogResult dr = MessageBox.Show(DeleteRenameChanges[0].ToString(), "Save before exiting?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button2);
                if (dr == DialogResult.Cancel)
                {
                    #region Cancel the close, reset the Changes array, and return
                    LogMessage.LogF("...Exit & Save cancelled");
                    // Remove the newly added Files that need to be saved.
                    DeleteRenameChanges.RemoveRange(original_drc_length, DeleteRenameChanges.Count - original_drc_length);
                    this.RestoreCursor();
                    return;
                    #endregion
                }
                else if (dr == DialogResult.Yes)
                {
                    Error = SaveTagInfo(DeleteRenameChanges[0]);
                    LogMessage.LogF("...Save All Changes Completed {0}.", (Error == true) ? "With Errors" : "Successfully.");
                }
                else
                {
                    LogMessage.LogF("...Save All Declined, Exiting.");
                }
                #endregion
            }

            this.RestoreCursor();
        }

        private void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAllChanges();
        }
        #endregion
        #region File/Exit Submenu Items
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion
        #endregion
        #region Edit Menu
        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.treeView.SelectedNode == null)
                return;

            CMacroFile cmf = FindMacroFileByNode(this.treeView.SelectedNode);
            if (cmf == null)
                return;
            CMacro cm = GetCurrentMacro(cmf);
            if (cm == null)
                return;

            if (clipboard_macro == null)
                clipboard_macro = new CMacro();
            if (this.pasteToolStripMenuItem.Enabled == false)
                this.pasteToolStripMenuItem.Enabled = true;
            clipboard_macro.CopyFrom(cm);
            cm.Clear();
            //TreeNode tn = cm.thisNode;
            //int mn = cm.MacroNumber;
            //cm = new CMacro();
            //cm.thisNode = tn;
            //cm.MacroNumber = mn;
            cmf.Changed = true;
            FillForm(cm);
        }
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.treeView.SelectedNode == null)
                return;

            CMacroFile cmf = FindMacroFileByNode(this.treeView.SelectedNode);
            if (cmf == null)
                return;
            CMacro cm = GetCurrentMacro(cmf);
            if (cm == null)
                return;

            if (clipboard_macro == null)
                clipboard_macro = new CMacro();
            if (this.pasteToolStripMenuItem.Enabled == false)
                this.pasteToolStripMenuItem.Enabled = true;
            clipboard_macro.CopyFrom(cm);
            // Copy doesn't require a Changed flag...
            //cmf.Changed = true;
        }
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.treeView.SelectedNode == null)
                return;

            CMacroFile cmf = FindMacroFileByNode(this.treeView.SelectedNode);
            if (cmf == null)
                return;

            CMacro cm = GetCurrentMacro(cmf);
            if (cm == null)
                return;

            if (clipboard_macro == null)
                return;
            //clipboard_macro = new CMacro();
            cm.CopyFrom(clipboard_macro); //.CopyFrom(cm);
            cmf.Changed = true;
            FillForm(cm);
        }
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                CMacro cm = null;
                CMacroFile cmf = null;
                ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
                if (tsmi.Name == "Delete_Macro")
                {
                    cm = tsmi.Tag as CMacro;
                    cmf = FindMacroFileByNode(cm.thisNode);
                }
                else
                {
                    cmf = FindMacroFileByNode(this.treeView.SelectedNode);
                    if (cmf != null)
                    {
                        cm = GetCurrentMacro(cmf);
                    }
                }
                if ((cmf != null) && (cm != null))
                {
                    cm.Clear();
                    cmf.Changed = true;
                    FillForm(cm);
                }
            }
        }
        #endregion
        #region Tools Menu
        private void toolsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode[] tN = this.treeView.Nodes.Find("Templates <x_ffxi_me_x>", false);

            if ((tN != null) && (tN.Length >= 1))
            {
                if (!Directory.Exists(TagInfo.GetTagInfo(tN[0].Tag).Text))
                {
                    Open_Template_Folder.Enabled = false;
                }
                else
                {
                    Open_Template_Folder.Enabled = true;
                }
            }
            else
            {
                Open_Template_Folder.Enabled = false;
            }
        }

        private void importMacroToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Import Macro Not Yet Implemented!");
        }
        private void saveasXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //PickBackupFolder();
            MessageBox.Show("Save as XML Not Yet Implemented!");
        }
        // Separator
        private void searchforPhraseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.sfp == null)
                this.sfp = new SearchForPhrase(this.ATPhraseLoader);
            this.sfp.Show();
            if (this.sfp.sfpvisible == true)
                this.sfp.Focus();
            this.sfp.sfpvisible = true;
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionsDialog od = new OptionsDialog();
            od.StartPosition = FormStartPosition.CenterParent;
            if (od.ShowDialog() == DialogResult.OK)
            {
                SavePreferences();
            }
        }
        #endregion
        #region Help Menu
        private void creditsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreditsDialog f = new CreditsDialog();
            f.ShowDialog(this);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox f = new AboutBox();
            f.ShowDialog();
        }

        private void helpMainToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists("FFXI_ME_v2.chm"))
                Help.ShowHelp(this, "file://" + Path.GetFullPath("FFXI_ME_v2.chm"));
        }
        #endregion
        #endregion

        #region MainForm Methods (Folder Open Methods, TreeView Builder Methods, Recursing Form)
        #region MainForm Methods (Notify Form)
        private void ShowNotifyForm(Object obj)
        {
            TagInfo ti = obj as TagInfo;
            if (ti != null)
            {
                int max = (Int32)ti.Object1;
                notifyForm = new ProgressNotification((ti.Text == String.Empty) ? "Processing Information..." : ti.Text, "Please wait...", max);
                exitLoop = false;
                if (notifyForm.ShowDialog() == DialogResult.Abort)
                    exitLoop = true;
            }
        }
        private void VisibleForm()
        {
            if (notifyForm != null)
            {
                if (notifyForm.InvokeRequired)
                {
                    try
                    {
                        notifyForm.Invoke((MethodInvoker)delegate
                        {
                            notifyForm.Visible = true; Thread.Sleep(20);
                        });
                    }
                    catch (Exception e)
                    {
                        LogMessage.Log(e.Message);
                    }
                }
                else
                {
                    notifyForm.Visible = true;
                    Thread.Sleep(20);
                    //notifyForm.Focus();
                    //notifyForm.BringToFront();
                }
            }
        }

        private void InvisibleForm()
        {
            if (notifyForm != null)
            {
                if (notifyForm.InvokeRequired)
                {
                    try
                    {
                        notifyForm.Invoke((MethodInvoker)delegate { notifyForm.Visible = false; });
                    }
                    catch (Exception e)
                    {
                        LogMessage.Log(e.Message);
                    }
                }
                else notifyForm.Visible = false;
            }
        }

        private void CloseNotifyForm()
        {
            if (notifyForm != null)
            {
                if (notifyForm.InvokeRequired)
                {
                    try
                    {
                        notifyForm.Invoke((MethodInvoker)delegate { notifyForm.Close(); });
                    }
                    catch (Exception e)
                    {
                        LogMessage.Log(e.Message);
                    }
                }
                else
                {
                    notifyForm.Close();
                }
                notifyForm = null;
            }
        }

        private void UpdateNotifyUI(String text, int value)
        {
            if (notifyForm != null)
            {
                if (notifyForm.InvokeRequired)
                {
                    try
                    {
                        notifyForm.Invoke((MethodInvoker)delegate { notifyForm.NotifyLabelText = text; notifyForm.NotifyBarValue = value; });
                    }
                    catch (Exception e)
                    {
                        LogMessage.Log(e.Message);
                    }
                }
                else
                {
                    notifyForm.NotifyLabelText = text;
                    notifyForm.NotifyBarValue = value;
                }
            }
        }
        private void UpdateNotifyProgress(int value)
        {
            if (notifyForm != null)
            {
                if (notifyForm.InvokeRequired)
                {
                    try
                    {
                        notifyForm.Invoke((MethodInvoker)delegate { notifyForm.NotifyBarValue = value; });
                    }
                    catch (Exception e)
                    {
                        LogMessage.Log(e.Message);
                    }
                }
                else notifyForm.NotifyBarValue = value;
            }
        }
        private void UpdateNotifyLabel(String text)
        {
            if (notifyForm != null)
            {
                if (notifyForm.InvokeRequired)
                {
                    try
                    {
                        notifyForm.Invoke((MethodInvoker)delegate { notifyForm.NotifyLabelText = text; });
                    }
                    catch (Exception e)
                    {
                        LogMessage.Log(e.Message);
                    }
                }
                else notifyForm.NotifyLabelText = text;
            }
        }
        #endregion

        #region MainForm Methods (Recursing Form and Pick Backup Folder?)
        private String PickBackupFolder()
        {
            FolderBrowserDialog x = new FolderBrowserDialog();
            x.SelectedPath = this.FFXIInstallPath;
            DialogResult result = x.ShowDialog();
            if (result == DialogResult.OK)
            {
                MessageBox.Show(x.SelectedPath); // selectedpath never ends with '\\'
                return x.SelectedPath;
            }
            else LogMessage.Log("...Pick Backup Folder cancelled.");
            return String.Empty;
        }
        private void ShowRecursingForm()
        {
            recurse_form.Handle.ToInt32();
            recurse_form.UpdateInfo = "Preparing...";
            recurse_form.Name = "Recursing Subdirectories...";
            exitLoop = false;
            if (recurse_form.ShowDialog() == DialogResult.Abort)
            {
                exitLoop = true;
                Thread.Sleep(1);
            }
        }
        #endregion

        #region MainForm Methods (OpenFolderMethod() overloads)
        private void OpenFolderMethod()
        {
            bool loop = true;
            DialogResult result;
            do
            {

                result = OpenFolderDialog.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    if (!Preferences.PathToOpen.Contains(OpenFolderDialog.SelectedPath))
                        Preferences.PathToOpen.Add(OpenFolderDialog.SelectedPath);
                }
                String Message = String.Format("Would you like to select{0} folders to search for Macro files (Currently {1} selected)?", (Preferences.PathToOpen.Count > 0) ? " more" : "", (Preferences.PathToOpen.Count == 0) ? "none" : Preferences.PathToOpen.Count.ToString());
                String Caption = String.Format("Search{0} folders...", (Preferences.PathToOpen.Count > 0) ? " more" : "");
                DialogResult yesnocancel = MessageBox.Show(Message, Caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

                if (yesnocancel == DialogResult.Cancel)
                {
                    if (MacroFiles == null)
                        FillForm((CMacro)null);
                    loop = false;
                }
                else if (yesnocancel == DialogResult.No)
                {
                    if (Preferences.PathToOpen.Count > 0)
                        OpenFolderMethod(Preferences.PathToOpen);
                    else if (MacroFiles == null)
                        FillForm((CMacro)null);
                    loop = false;
                }

            } while (loop);
        }

        private void OpenFolderMethod(string path)
        {
            if (path == String.Empty)
                return;

            List<string> xpaths = new List<string>();
            xpaths.Clear();
            xpaths.Add(path);
            OpenFolderMethod(xpaths);
        }

        public void OpenFolderMethod(List<string> paths)
        {
            LogMessage.Log("Loading Macro Files");
            recurse_form = new RecursingSubDirs();
            Thread myThread = new Thread(new ThreadStart(this.ShowRecursingForm));
            string tempFolderName = String.Empty;
            MacroFiles.Clear();
            BookList.Clear();
            DeleteRenameChanges.Clear();
            DirectoryInfo temp_di = new DirectoryInfo(Preferences.TemplatesFolderName);
            bool TemplateFolderOK = true;
            try
            {
                List<string> tempfileList = null;
                List<string> mcrfileList = null;
                myThread.Start();
                Thread.Sleep(500);

                tempFolderName = temp_di.FullName;

                foreach (string x in paths)
                {
                    String path = x;

                    if (path == String.Empty)
                        continue;

                    DirectoryInfo di = new DirectoryInfo(path);
                    FileInfo fi = new FileInfo(path);

                    if (di.Exists && ((di.Attributes & FileAttributes.Directory) == FileAttributes.Directory))
                    {
                        List<string> temp = GetFilesNonRecursive(di, "*.*", false);
                        if (mcrfileList == null)
                        {
                            mcrfileList = new List<string>();
                            mcrfileList.Clear();
                        }
                        if (temp != null)
                            mcrfileList.AddRange(temp);
                    }
                    else if (fi.Exists && ((fi.Attributes & FileAttributes.Directory) != FileAttributes.Directory))
                    {
                        if (IsMacroFile(fi))
                        {
                            try
                            {
                                if (!recurse_form.Disposing)
                                    recurse_form.Invoke(new UpdateUIHandler(recurse_form.UpdateUI), new object[] { "Examining File :\r\n'" + Utilitiies.EllipsifyPath(fi.FullName, 35) + "'" });
                            }
                            catch
                            {
                                // (System.ObjectDisposedException) && (System.InvalidOperationException)
                            }
                            if (mcrfileList == null)
                            {
                                mcrfileList = new List<string>();
                                mcrfileList.Clear();
                            }
                            mcrfileList.Add(fi.FullName);
                        }
                    }

                    if ((temp_di.Exists) && (!path.Contains(temp_di.FullName)) && (!temp_di.FullName.Contains(path))) // Templates path isn't contained in main path.
                    {
                        //foreach (string s in tempfileList)
                        //    Log("tempfileList item: " + s);
                    }
                    else if (!temp_di.Exists)
                    {

                    }
                    else
                    {
                        if (TemplateFolderOK)
                        {
                            TemplateFolderOK = false;
                            LogMessage.Log("..Templates folder exists as part of the Selected directory, ignoring");
                        }
                    }
                }

                if (!temp_di.Exists)
                {
                    LogMessage.Log(".." + temp_di.FullName + " doesn't exist at all and doesn't conflict with existing selected paths (creating empty node)");
                }

                if ((mcrfileList != null) && (mcrfileList.Count > 0))
                {
                    LogMessage.Log("..Generating List of Selected Directories Macro Files");
                    foreach (String mcr in mcrfileList)
                    //for (int i = 0; i < mcrfileList.Count; i++)
                    {
                        //if (File.Exists(mcrfileList[i]) && IsMacroFile(mcrfileList[i]))
                        //{
                        //    MacroFiles.Add(new CMacroFile(mcrfileList[i], this.ATPhraseLoader));
                        //}
                        MacroFiles.Add(new CMacroFile(mcr, this.ATPhraseLoader));
                    }
                }

                if (TemplateFolderOK)
                {
                    // Only one templates folder, all templates should be a sub-folder of that one
                    // This way, I don't have to do what I did above (mcrfileList.AddRange())
                    LogMessage.Log("..Templates folder name: '" + temp_di.FullName + "'");
                    tempfileList = GetFilesNonRecursive(temp_di, "*.*", true);
                }

                if ((tempfileList != null) && (tempfileList.Count > 0))
                {
                    LogMessage.Log("..Generating List of Template Directories Macro Files");
                    foreach(String tempfn in tempfileList)
                    //for (int i = 0; i < tempfileList.Count; i++)
                    {
                        // LOAD TEMPLATES HERE
                        //if (File.Exists(tempfileList[i]) && IsMacroFile(tempfileList[i]))
                        //{
                        //    MacroFiles.Add(new CMacroFile(tempfileList[i], this.ATPhraseLoader));
                        //}
                        MacroFiles.Add(new CMacroFile(tempfn, this.ATPhraseLoader));
                    }
                }
                LogMessage.Log("..Building TreeView Nodes from file lists");
                BuildTree(paths, tempFolderName);
                LogMessage.Log("..Building TreeView Nodes completed");
            }
            catch (OutOfMemoryException e)
            {
                LogMessage.Log("Out of Memory Exception: " + e.Message);
                this.Close();
            }

            try
            {
                recurse_form.Invoke(new RecurseCloseHandler(recurse_form.Close));
            }
            catch
            {
                // (System.ObjectDisposedException) && (System.InvalidOperationException)
            }
            recurse_form = null;
            FillForm((CMacro)null);
        }
        #endregion

        #region MainForm Methods (Cursor Specific)
        void SaveCursor()
        {
            this.backupCursor = this.Cursor;
        }
        void SetWaitCursor()
        {
            SaveCursor();
            this.Cursor = Cursors.WaitCursor;
        }
        void RestoreCursor()
        {
            this.Cursor = this.backupCursor;
        }
        #endregion

        #region MainForm Methods (BuildTree, BuildMacroNode, BuildMacroFileNodes, etc)
        private List<string> GetFilesNonRecursive(DirectoryInfo dirInfo, string searchPattern, bool IncludeEmptyDirs)
        {
            if (searchPattern == String.Empty)
                return null;

            // set loop handler
            exitLoop = false;

            int index = 0;
            List<string> dirList = new List<string>();
            List<string> fileList = null;  // keep list null, we'll initialize it if we need it, handle null if no files
            FileInfo fi;
            int trim_len = 35;

            // Initialize the list
            dirList.Clear();
            dirList.Add(dirInfo.FullName);

            try
            {
                recurse_form.Invoke(new UpdateUIHandler(recurse_form.UpdateUI), new object[] { "Searching Directory :\r\n'" + Utilitiies.EllipsifyPath(dirList[index], trim_len) + "'" });
            }
            catch
            {
                // (System.ObjectDisposedException) && (System.InvalidOperationException)
            }

            //foreach (String currentdirList in dirList) // Since I'm adding to the list as I'm iterating through the list, I don't think this works the way I want it to.
            while (index < dirList.Count)
            {
                try
                {
                    // Pull subdirectories for the current directory at "index" location.
                    foreach (string dir in Directory.GetDirectories(dirList[index]))
                    {

                        try
                        {
                            recurse_form.Invoke(new UpdateUIHandler(recurse_form.UpdateUI), new object[] { "Searching Directory :\r\n'" + Utilitiies.EllipsifyPath(dir, trim_len) + "'" });
                        }
                        catch
                        {
                            // (System.ObjectDisposedException) && (System.InvalidOperationException)
                            exitLoop = true;
                            break;
                        }

                        // add the subdirectory to the end of the list so that we can continue to non-recursively get ALL files
                        dirList.Add(dir);

                        // include the subdirectories in the actual file list (so even if it's empty, it still shows up)
                        if (IncludeEmptyDirs)
                        {
                            if (fileList == null)
                            {
                                fileList = new List<string>();
                                fileList.Clear();
                            }
                            fileList.Add(dir);
                        }
                        if (exitLoop == true)
                        {
                            break;
                        }
                    }

                    // Now, pull the file list for this existing directory
                    foreach (string file in Directory.GetFiles(dirList[index], searchPattern))
                    {
                        fi = new FileInfo(file);
                        // If it's a potential macro file, add it to the file list.
                        if (IsMacroFile(fi))
                        {
                            try
                            {
                                if (!recurse_form.Disposing)
                                    recurse_form.Invoke(new UpdateUIHandler(recurse_form.UpdateUI), new object[] { "Examining File :\r\n'" + Utilitiies.EllipsifyPath(file, trim_len) + "'" });
                            }
                            catch
                            {
                                // (System.ObjectDisposedException) && (System.InvalidOperationException)
                            }
                            if (fileList == null)
                            {
                                fileList = new List<string>();
                                fileList.Clear();
                            }
                            fileList.Add(file);
                        }
                        if (exitLoop == true)
                        {
                            break;
                        }
                    }
                }
                catch (UnauthorizedAccessException e)
                {
                    // Not allowed access to the folder/file, so Log it and Skip it
                    LogMessage.Log("Unauthorized Access : " + e.Message);
                }
                catch (IOException e)
                {
                    // Something wrong with a file or directory that wasn't handled previously, Skip it
                    LogMessage.Log("I/O Exception : " + dirList[index] + " '" + e.Message.TrimEnd('\n') + "'");
                }
                index++;
                if (exitLoop == true)
                {
                    exitLoop = false;
                    break;
                }
            }
            return fileList;
        }

        /// <summary>
        /// Builds the treeview recursively utilizing other Build() functions.
        /// </summary>
        /// <param name="paths">List of paths to build as main nodes.</param>
        /// <param name="temppath">Templates folder path.</param>
        private void BuildTree(List<string> paths, string temppath)
        {
            // Build TreeView here
            String s1 = temppath.TrimEnd('\\');
            String tempNodeName = String.Empty;
            TreeNode mainNode = null;
            String IsTempDrive = Path.GetPathRoot(temppath).TrimEnd('\\');
            DirectoryInfo temp_di;

            int x = 0;
            bool Enable_Open_Folders = false;

            if (treeView.Nodes != null)
                this.treeView.Nodes.Clear();

            this.treeView.BeginUpdate();

            ToolStripItem[] tsmi = new ToolStripItem[paths.Count];
            
            foreach (String mainpath in paths)
            {
                String s = mainpath.TrimEnd('\\');
                String mainNodeName = String.Empty;

                mainNode = null;

                String IsItDrive = Path.GetPathRoot(mainpath).TrimEnd('\\');

                FileInfo fi = new FileInfo(s);
                DirectoryInfo di = new DirectoryInfo(s);
                String directory = di.FullName;

                if (Preferences.IsFile(di.Attributes))
                {
                    directory = fi.DirectoryName;
                }

                if (s == IsItDrive)
                {
                    DriveInfo drv = new DriveInfo(IsItDrive);
                    try
                    {
                        if (drv.VolumeLabel.Trim() == String.Empty)
                            mainNodeName = "Local Disk (" + IsItDrive + ")";
                        else mainNodeName = drv.VolumeLabel + " (" + IsItDrive + ")";
                    }
                    catch (IOException e)
                    {
                        LogMessage.Log("I/O Exception: " + s + " '" + e.Message + "'");
                        mainNodeName = "Local Disk (" + IsItDrive + ")";
                    }
                    s = IsItDrive;
                }
                else
                {
                    if (Preferences.IsFile(di.Attributes))
                    {
                        // If it's a file, strip the filename with (GetDirectoryName) and then Get the first directory from that)
                         mainNodeName = Path.GetFileName(fi.DirectoryName);

                        // If it's a filename and it got THIS far, it's separate from the regular directories that were chosen
                        // special case requires pulling just the root directory and going from there.
                        //mainNodeName = Path.GetPathRoot(s).TrimEnd('\\');
                    }
                    else if (Preferences.IsDirectory(di.Attributes))
                    {
                        mainNodeName = Path.GetFileName(di.FullName); // s.Remove(0, pos + 1); // if it doesn't end in a \\ it's a directory
                    }
                }

                if ((this.treeView.Nodes != null) && (this.treeView.Nodes.Count > 0))
                {
                    if (this.treeView.Nodes.ContainsKey(mainNodeName))
                        continue;
                }

                #region Setup main nodes
                if (mainpath == String.Format("{0}\\USER\\{1}", this.FFXIInstallPath.TrimEnd('\\'), mainNodeName))
                {
                    #region Apply special names to character folders that are requested to be named
                    if (characterList != null)
                    {
                        for (int folder_x = 0; folder_x < characterList.Length; folder_x++)
                        {
                            // if the folder == mainNodeName
                            // and the Character Name is != mainNodeName (ie someone named the character same as the folder)
                            if ((characterList[folder_x].Type == mainNodeName) &&
                                (characterList[folder_x].Text != mainNodeName) &&
                                (characterList[folder_x].Text.Trim() != String.Empty))
                            {
                                mainNode = this.treeView.Nodes.Add(mainNodeName, mainNodeName + " <" + characterList[folder_x].Text + ">", "CharFolderClosed", "CharFolderOpen");
                                break;
                            }
                        }
                    }
                    #endregion

                    #region Even if no names are setup, character folders get special icons anyway
                    if (mainNode == null)
                        mainNode = this.treeView.Nodes.Add(mainNodeName, mainNodeName, "CharFolderClosed", "CharFolderOpen");
                    #endregion
                }
                else mainNode = this.treeView.Nodes.Add(mainNodeName, mainNodeName, "ClosedFolder", "OpenFolder");
                TagInfo tI = new TagInfo("main", directory);
                mainNode.Tag = tI;
                tsmi[x] = new ToolStripMenuItem("Open " + mainNodeName + " Folder", Resources.openHS, DynamicMenu_Click);
                tsmi[x].Tag = mainNode as Object;
                tsmi[x].Name = "Open_Folder";
                if (Directory.Exists(directory))
                {
                    tsmi[x].Enabled = true;
                    Enable_Open_Folders = true;
                }
                else tsmi[x].Enabled = false;
                x++;

            //tmsi[2] = new ToolStripMenuItem("Open " + e.Node.Text + " Folder", Resources.openHS, DynamicMenu_Click);
            //tmsi[2].Tag = e.Node as Object;
            //tmsi[2].Name = "Open_Folder";
            //tmsi[2].Enabled = false;

            //if (!Directory.Exists(TagInfo.GetTagInfo(e.Node.Tag).Text))
            //{
            //    tmsi[2].Enabled = false;

                #endregion
            }

            if (x < tsmi.Length)
                Array.Resize(ref tsmi, x);

            if (Open_Main_Folder.DropDownItems.Count > 0)
                Open_Main_Folder.DropDownItems.Clear();

            if (Enable_Open_Folders)
                Open_Main_Folder.Enabled = true;

            if (tsmi.Length == 1)
            {
                Open_Main_Folder.Tag = tsmi[0].Tag;
                Open_Main_Folder.Text = tsmi[0].Text;
                Open_Main_Folder.Enabled = tsmi[0].Enabled;
                Open_Main_Folder.Name = tsmi[0].Name;

            }
            else
            {
                Open_Main_Folder.Text = "Open Folders";
                Open_Main_Folder.Tag = null;
                Open_Main_Folder.Name = "Open Folders";
                Open_Main_Folder.DropDownItems.AddRange(tsmi);
            }

            #region Build Template Node
            #region Set Templates Node name
            temp_di = new DirectoryInfo(s1);

            if (temppath != String.Empty)
            {

                if (s1 == IsTempDrive) // temppath[temppath.Length - 1] == '\\') // if it ends in '\\' it's a drive
                {
                    DriveInfo drv = new DriveInfo(IsTempDrive);
                    try
                    {
                        if (drv.VolumeLabel.Trim() == String.Empty)
                            tempNodeName = "Local Disk (" + IsTempDrive + ")";
                        else tempNodeName = drv.VolumeLabel + " (" + IsTempDrive + ")";
                    }
                    catch (IOException e)
                    {
                        LogMessage.Log("I/O Exception: " + IsTempDrive + " '" + e.Message + "'");
                        tempNodeName = IsTempDrive;
                    }
                }
                else
                {
                    tempNodeName = Path.GetFileName(temp_di.FullName); // if it doesn't end in a \\ it's a directory
                }
            }
            #endregion

            #region Setup Template Node with icon and enable/disable "Open Templates Folder"
            TreeNode templateNode = null;
            if (temppath != String.Empty)
                templateNode = this.treeView.Nodes.Add("Templates <x_ffxi_me_x>", tempNodeName, "ClosedFolder", "OpenFolder");

            if (templateNode != null)
            {
                TagInfo tI2 = new TagInfo("template", temp_di.FullName);
                templateNode.Tag = tI2;
                if (temp_di.Exists)
                    Open_Template_Folder.Enabled = true;
                else Open_Template_Folder.Enabled = false;
                Open_Template_Folder.Text = "Open " + templateNode.Text + " Folder";
            }
            else
            {
                Open_Template_Folder.Enabled = false;
            }
            #endregion
            #endregion

            #region Loop through all files to copy.
            foreach (CMacroFile cmf in MacroFiles)
            {
                BuildMacroFileNodes(cmf, this.treeView.Nodes, templateNode);
            }
            #endregion

            if (exitLoop == true)
            {
                exitLoop = false;
                this.treeView.Nodes.Clear();
                MacroFiles.Clear();
                FillForm((CMacro)null);
            }

            treeView.EndUpdate();
        }

        private void BuildMacroFileNodes(CMacroFile cmf, TreeNodeCollection maintreeviewNodes, TreeNode templateNode)
        {
            if ((cmf == null) || (cmf.fName == null))
                return;
    
            // mainpath is C:\Program Files\blahblah
            // fName is C:\Program Files\blahblah\filename.dat

            TreeNode tempTreeNode = null;
            bool Found = false;
            string s = String.Empty;
            string temppath = String.Empty;
            if (templateNode != null)
            {
                TagInfo tItemp = templateNode.Tag as TagInfo;
                temppath = tItemp.Text;
            }

            foreach (TreeNode mainNode in maintreeviewNodes)
            {

                if (mainNode == templateNode)
                {
                    continue;
                }

                TagInfo tI = mainNode.Tag as TagInfo;
                string mainpath = tI.Text;

                if ((mainpath != String.Empty) && cmf.fName.Contains(mainpath))
                {
                    s = cmf.fName.Remove(0, mainpath.Length); // remove the common start path
                    tempTreeNode = mainNode;
                    if (s.Trim('\\') == String.Empty)
                        s = mainpath;
                    Found = true;
                }
                if (Found)
                    break;
            }

            if (!Found && (temppath != String.Empty) && cmf.fName.Contains(temppath))
            {
                s = cmf.fName.Remove(0, temppath.Length);
                tempTreeNode = templateNode;
                if (s.Trim('\\') == String.Empty)
                    s = temppath;
            }

            if (tempTreeNode != null)
            {
                cmf.thisNode = BuildTreeRecursive(tempTreeNode, s.Trim('\\'), cmf.FileNumber, cmf.fName);
                TagInfo xTag = new TagInfo("macrofile");
                cmf.thisNode.Tag = xTag;
                cmf.ctrlNode = cmf.thisNode.Nodes.Add("Ctrl Macros", "Ctrl Macros", "Bars", "Bars");
                xTag = new TagInfo("ctrlmacro");
                cmf.ctrlNode.Tag = xTag;
                cmf.altNode = cmf.thisNode.Nodes.Add("Alt Macros", "Alt Macros", "Bars", "Bars");
                xTag = new TagInfo("altmacro");
                cmf.altNode.Tag = xTag;
                for (int x = 0; x < 20; x++)
                {
                    if (x < 10)
                        BuildMacroNode(ref cmf.Macros[x], cmf.ctrlNode);
                    else if (x >= 10)
                        BuildMacroNode(ref cmf.Macros[x], cmf.altNode);
                }
            }
        }

        private void BuildMacroNode(ref CMacro cm, TreeNode parent)
        {
            string cmName = cm.DisplayName();
            cm.thisNode = parent.Nodes.Add(CMacro.DisplayName(cm.MacroNumber) + "<" + cmName + ">", cmName, "Macro", "EditMacro");
            TagInfo xTag = new TagInfo("macro");
            cm.thisNode.Tag = xTag;

        }
        private TreeNode BuildTreeRecursive(TreeNode parent, string path, int filenumber, string cmfName)
        {
            if (parent == null)
                return null;
            int i = path.IndexOf('\\'); // Locate next folder/file separator
            if (i == -1) // Found a file.
            {
                string mcrFileName = String.Empty;
                FileInfo fi = new FileInfo(Path.GetDirectoryName(cmfName) + "\\mcr.ttl");
                if ((filenumber >= 0) && (filenumber < Preferences.Max_Macro_Sets))
                {
                    int index = -1;
                    for (index = 0; index < BookList.Count; index++)
                    {
                        if (BookList[index].fName == fi.FullName)
                        {
                            if (BookList[index].IsDeleted)
                                BookList[index].Restore();
                            break;
                        }
                    }
                    if (index >= BookList.Count) // not found, create new
                    {
                        BookList.Add(new CBook(fi.FullName));
                        if (Preferences.ShowBlankBooks)
                        {
                            TagInfo tgInfo = null;
                            TreeNode newBook = null;
                            for (int bknum = 0; bknum < 20; bknum++)
                            {
                                newBook = parent.Nodes.Add((bknum + 1).ToString(), BookList[index].GetBookName(bknum), "ClosedBook", "OpenBook");
                                tgInfo = new TagInfo("book", BookList[index].GetBookName(bknum));
                                newBook.Tag = tgInfo;
                            }
                        }
                    }
                    else if (Preferences.ShowBlankBooks)
                    {
                        TagInfo tgInfo = null;
                        TreeNode newBook = null;
                        for (int bknum = 0; bknum < 20; bknum++)
                        {
                            if (parent.Nodes.Find((bknum + 1).ToString(), true).Length == 0)
                            {
                                newBook = parent.Nodes.Add((bknum + 1).ToString(), BookList[index].GetBookName(bknum), "ClosedBook", "OpenBook");
                                tgInfo = new TagInfo("book", BookList[index].GetBookName(bknum));
                                newBook.Tag = tgInfo;
                            }
                        }
                    }
                    // REPLACE_BOOK_INFO
                    // byte[] test_string;
                    int booknumber = ((int)Math.Truncate((decimal)((filenumber) / 10.0))) + 1;
                    String test_str = BookList[index].GetBookName(booknumber - 1);
                    //GetBookName(fi.FullName, (int)Math.Truncate((decimal)(filenumber / 10)) + 1);
                    if (test_str != String.Empty)
                    {
                        TreeNode[] tempNode = parent.Nodes.Find(booknumber.ToString(), false);
                        TreeNode tNode = null;
                        bool found = false;
                        for (int x = 0; x < tempNode.Length; x++)
                            if (tempNode[x].Text == test_str)
                            {
                                parent = tempNode[x];
                                found = true;
                                break;
                            }
                        if (found != true)
                        {
                            TagInfo tAG = new TagInfo("book", test_str);
                            tNode = parent.Nodes.Add(booknumber.ToString(), test_str, "ClosedBook", "OpenBook");
                            tNode.Tag = tAG;
                            parent = tNode;
                        }
                    }
                    mcrFileName = String.Format("Macro Set #{0}", (filenumber % 10) + 1);
                }
                else if ((filenumber >= 0) && (filenumber < 10))
                    mcrFileName = String.Format("Macro Set #{0}", filenumber + 1);
                else mcrFileName = path;

                return parent.Nodes.Add(mcrFileName, mcrFileName, "Macrofile", "EditMacrofile");
            }
            else
            {
                string next_folderName = path.Trim('\\').Split('\\')[0]; //SubStringSlash(path); // remove the file we just added.
                TreeNode tmp = parent.Nodes[next_folderName];
                //GetNodeByName(parent, next_folderName);
                TagInfo tIparent = parent.Tag as TagInfo;
                TagInfo tItmp = new TagInfo();
                if (tmp == null)
                {
                    if (tIparent.Text == String.Format("{0}USER", this.FFXIInstallPath))
                    {
                        if (characterList != null)
                        {
                            for (int folder_x = 0; folder_x < characterList.Length; folder_x++)
                            {
                                // if foldername is == next_folderName &&
                                // the Character name is not set to the same as the foldername.
                                if ((characterList[folder_x].Type == next_folderName) &&
                                    (characterList[folder_x].Text != next_folderName) &&
                                    (characterList[folder_x].Text.Trim() != String.Empty))

                                {
                                    tmp = parent.Nodes.Add(next_folderName, next_folderName + " <" + characterList[folder_x].Text + ">", "CharFolderClosed", "CharFolderOpen"); //added the new directory
                                    tItmp.Type = "char";
                                    break;
                                }
                            }
                        }

                        if (tmp == null)
                        {
                            tmp = parent.Nodes.Add(next_folderName, next_folderName, "CharFolderClosed", "CharFolderOpen"); //added the new directory
                            tItmp.Type = "char";
                        }
                    }
                    else
                    {
                        tmp = parent.Nodes.Add(next_folderName, next_folderName, "ClosedFolder", "OpenFolder");
                        tItmp.Type = "folder";
                    }
                }
                if (tmp.Tag == null)
                {
                    tItmp.Text = String.Format("{0}\\{1}", tIparent.Text, next_folderName);
                    tmp.Tag = tItmp as Object;
                    //LogMessage.Log("tmp.Tag = '" + (String)tmp.Tag + "'");
                }
                return BuildTreeRecursive(tmp, path.Remove(0, i).Trim('\\'), filenumber, cmfName); // send next path
            }
        }
        #endregion
        #endregion

        #region MainForm Methods (TaskBar Menu Items and Notify Icon OnClick Event Handlers)
        private void TaskBarMenuExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void TaskBarMenuRestore_Click(object sender, EventArgs e)
        {
            RestoreFFXI_ME();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            RestoreFFXI_ME();
        }

        private void RestoreFFXI_ME()
        {
            this.Visible = true;
            //Show the application in the task bar again:
            this.ShowInTaskbar = true;

            //Hide The icon in the System tray:
            notifyIcon.Visible = false;

            //Set the Form to be Visible again (allow Alt-Tab back to the program)
            if (this.sfp != null)
            {
                if (this.sfp.sfpvisible == true)
                    this.sfp.Show();
            }
            //Set the window state back to normal:
            if (this.WindowState != FormWindowState.Normal)
                this.WindowState = FormWindowState.Normal;


            //this.Activate();
            this.SendToBack();
            this.BringToFront();
            int oldStart;
            int oldLength;
            // this part is to make sure that the textBoxes scroll
            // appropriately after a minimize/restore
            // apparently if there's a selection, it scrolls to
            // the END of the selection, and puts that end near
            // the front of the box instead of near the end where it
            // would belong.  No matter how short/long that line may be.
            oldStart = this.textBoxLine1.SelectionStart;
            oldLength = this.textBoxLine1.SelectionLength;
            this.textBoxLine1.SelectionStart = 0;
            this.textBoxLine1.SelectionLength = 0;
            this.textBoxLine1.ScrollToCaret();
            this.textBoxLine1.SelectionStart = oldStart;
            this.textBoxLine1.SelectionLength = oldLength;
            this.textBoxLine1.Refresh();
            oldStart = this.textBoxLine2.SelectionStart;
            oldLength = this.textBoxLine2.SelectionLength;
            this.textBoxLine2.SelectionStart = 0;
            this.textBoxLine2.ScrollToCaret();
            this.textBoxLine2.SelectionStart = oldStart;
            this.textBoxLine2.SelectionLength = oldLength;
            this.textBoxLine2.Refresh();
            oldStart = this.textBoxLine3.SelectionStart;
            oldLength = this.textBoxLine3.SelectionLength;
            this.textBoxLine3.SelectionStart = 0;
            this.textBoxLine3.ScrollToCaret();
            this.textBoxLine3.SelectionStart = oldStart;
            this.textBoxLine3.SelectionLength = oldLength;
            this.textBoxLine3.Refresh();
            oldStart = this.textBoxLine4.SelectionStart;
            oldLength = this.textBoxLine4.SelectionLength;
            this.textBoxLine4.SelectionStart = 0;
            this.textBoxLine4.ScrollToCaret();
            this.textBoxLine4.SelectionStart = oldStart;
            this.textBoxLine4.SelectionLength = oldLength;
            this.textBoxLine4.Refresh();
            oldStart = this.textBoxLine5.SelectionStart;
            oldLength = this.textBoxLine5.SelectionLength;
            this.textBoxLine5.SelectionStart = 0;
            this.textBoxLine5.ScrollToCaret();
            this.textBoxLine5.SelectionStart = oldStart;
            this.textBoxLine5.SelectionLength = oldLength;
            this.textBoxLine5.Refresh();
            oldStart = this.textBoxLine6.SelectionStart;
            oldLength = this.textBoxLine6.SelectionLength;
            this.textBoxLine6.SelectionStart = 0;
            this.textBoxLine6.ScrollToCaret();
            this.textBoxLine6.SelectionStart = oldStart;
            this.textBoxLine6.SelectionLength = oldLength;
            this.textBoxLine6.Refresh();
        }
        #endregion

        #region MainForm Methods (Timer function)
        private void timer_Tick(object sender, EventArgs e)
        {
            // get node at mouse position
            Point ptcmp = this.treeView.PointToClient(Control.MousePosition);
            TreeNode node = this.treeView.GetNodeAt(ptcmp);

            if (node != null)
            {
                // if HoverNode hasn't been set yet
                // or if node is not the same, set new HoverNode and start
                // Timer Interval back over.
                if ((this.HoverNode == null) || (node != this.HoverNode))
                {
                    this.HoverNode = node; // start interval now
                    this.interval = 0;
                    return;
                }
                else if (this.interval <= 4) // node is still the same as HoverNode, but interval is not 1250
                {
                    this.interval++;
                    return;
                }

                DragHelper.ImageList_DragShowNolock(false);
                this.interval = 0; // start interval over
                this.HoverNode.Expand(); // expand the correct node
                this.HoverNode = null;
                DragHelper.ImageList_DragShowNolock(true);
            }
        }

        private void NodeUpdateTimer_Tick(object sender, EventArgs e)
        {
            lock (NodeUpdatesToDo)
            {
                if (NodeUpdatesToDo.Length > 0)
                {
                    this.treeView.BeginUpdate();
                    foreach (TagInfo t in NodeUpdatesToDo)
                    {
                        TreeNode tN = t.Object1 as TreeNode;
                        tN.Text = t.Text;
                    }
                    NodeUpdatesToDo = new TagInfo[0];
                    this.treeView.EndUpdate();
                }
            }
        }
        #endregion

        #region MainForm Methods (Preferences Loading & Saving)
        private void LoadPreferences()
        {
            if (!File.Exists(Preferences.SettingsXMLFile))
            {
                try
                {
                    if (!Directory.Exists(Preferences.AppMyDocsFolderName))
                        Directory.CreateDirectory(Preferences.AppMyDocsFolderName);
                    if (File.Exists("settings.xml"))
                    {
                        File.Copy("settings.xml", Preferences.SettingsXMLFile);
                    }
                }
                catch
                {
                    LogMessage.Log("Error in LoadPreferences():");
                    LogMessage.Log("..settings.xml didn't exist, failed somewhere to create the path or file or even to restore old settings, ignoring.");
                }
            }

            Preferences.MenuXMLFile = settings.GetSetting("MainProgram/MenuXMLFile", Preferences.MenuXMLFile);

            if (!File.Exists(Preferences.MenuXMLFile))
            {
                try
                {
                    if (!Directory.Exists(Preferences.AppMyDocsFolderName))
                        Directory.CreateDirectory(Preferences.AppMyDocsFolderName);
                    if (File.Exists("menu.xml"))
                    {
                        File.Copy("menu.xml", Preferences.MenuXMLFile);
                    }
                }
                catch
                {
                    LogMessage.Log("Error in LoadPreferences():");
                    LogMessage.Log("..menu.xml didn't exist, failed somewhere to create the path or file or even to restore old menu, ignoring.");
                }
            }

            this.Location = new Point(settings.GetSetting("MainProgram/Left", 0), settings.GetSetting("MainProgram/Top", 0));
            this.Size = new Size(settings.GetSetting("MainProgram/Width", 640), settings.GetSetting("MainProgram/Height", 480));
            Preferences.TemplatesFolderName = settings.GetSetting("MainProgram/TemplatesFolderName", Preferences.TemplatesFolderName);
            Preferences.Language = settings.GetSettingLanguage("MainProgram/Language", Preferences.Language);
            Preferences.Program_Language = settings.GetSettingLanguage("MainProgram/ProgramLanguage", Preferences.Program_Language);
            Preferences.EnterCreatesNewLine = settings.GetSetting("MainProgram/EnterCreatesNewLine", Preferences.EnterCreatesNewLine);

            if (Preferences.Program_Language == FFXIATPhraseLoader.ffxiLanguages.LANG_JAPANESE)
            {
                japan.Visible = true;
                japan.Enabled = false;
                japan.BackgroundImage = Icons.JapanDisabled;
            }
            else if (Preferences.Program_Language == FFXIATPhraseLoader.ffxiLanguages.LANG_ENGLISH)
            {
                usa.Visible = true;
                usa.Enabled = false;
                usa.BackgroundImage = Icons.UsaDisabled;
            }
            else if (Preferences.Program_Language == FFXIATPhraseLoader.ffxiLanguages.LANG_DEUTSCH)
            {
                deutsch.Visible = true;
                deutsch.Enabled = false;
                deutsch.BackgroundImage = Icons.DeutschDisabled;
            }
            else if (Preferences.Program_Language == FFXIATPhraseLoader.ffxiLanguages.LANG_FRENCH)
            {
                france.Visible = true;
                france.Enabled = false;
                france.BackgroundImage = Icons.FranceDisabled;
            }

            Preferences.ShowBlankBooks = settings.GetSetting("MainProgram/ShowBlankBooks", Preferences.ShowBlankBooks);
            Preferences.IsMaximized = settings.GetSetting("MainProgram/IsMaximized", Preferences.IsMaximized);

            if (Preferences.IsMaximized)
                this.WindowState = FormWindowState.Maximized;

            //String header = Preferences.Include_Header.ToString();
            Preferences.Include_Header = settings.GetSetting("MainProgram/Include_Header", Preferences.Include_Header);
            Preferences.UseExplorerViewOnFolderOpen = settings.GetSetting("MainProgram/UseExplorerViewForFolderOpen", Preferences.UseExplorerViewOnFolderOpen);
            //Preferences.Include_Header = (header == 1) ? true : false;
            Preferences.Max_Menu_Items = settings.GetSetting("MainProgram/MaxMenuItems", Preferences.Max_Menu_Items);
            Preferences.UseFolderAsRoot = settings.GetSetting("MainProgram/UseFolderAsRoot", Preferences.UseFolderAsRoot);
            Preferences.Max_Macro_Sets = settings.GetSetting("MainProgram/MaxMacroSets", Preferences.Max_Macro_Sets);
            Preferences.LoadItems = settings.GetSetting("MainProgram/LoadItems", Preferences.LoadItems);
            Preferences.LoadKeyItems = settings.GetSetting("MainProgram/LoadKeyItems", Preferences.LoadKeyItems);
            Preferences.LoadAutoTranslatePhrases = settings.GetSetting("MainProgram/LoadAutoTranslatePhrases", Preferences.LoadAutoTranslatePhrases);
            Preferences.MinimizeToTray = settings.GetSetting("MainProgram/MinimizeToTray", Preferences.MinimizeToTray);

            #region Load Character Names, if any
            String[] CharNodes = settings.GetNodeList("CharacterNames");
            if ((CharNodes != null) && (CharNodes.Length > 0))
            {
                //for (int cnt = 0; cnt < CharNodes.Length; cnt++)
                foreach (String CharName in CharNodes)
                {
                    if (CharName.Trim() == String.Empty)
                        continue;

                    String char_name = settings.GetSetting("CharacterNames/" + CharName, "(No Name)");

                    if (char_name.Trim() != String.Empty)
                    {
                        if (characterList == null)
                            characterList = new TagInfo[1];
                        else Array.Resize(ref characterList, characterList.Length + 1);
                        int index = characterList.Length - 1;

                        characterList[index] = new TagInfo(CharName, char_name);
                        // characterList.Type == folder name
                        // characterList.Text == Character name
                        LogMessage.Log(".. {0} <==> {1}", characterList[index].Type, characterList[index].Text);
                    }
                    else LogMessage.Log(".. {0} skipped, Name is empty!", CharName);
                }

            }
            #endregion
            #region Load Last Opened Folders, if any

            //respect original setting
            String oldsetting = settings.GetSetting("MainProgram/LastLoaded", String.Empty);

            // backward compatibility for old "node name"
            // new node name "LastLoadedLocations" also accepts files.
            String nodename = "LastLoadedFolders";
            String[] FolderNodes = settings.GetNodeList(nodename);
            if (FolderNodes == null)
            {
                nodename = "LastLoadedLocations";
                FolderNodes = settings.GetNodeList(nodename);
            }

            if (oldsetting != String.Empty)
            {
                if (FolderNodes == null)
                    FolderNodes = new String[1];
                else Array.Resize(ref FolderNodes, FolderNodes.Length + 1);
                FolderNodes[FolderNodes.Length - 1] = oldsetting;
            }

            if ((FolderNodes != null) && (FolderNodes.Length > 0))
            {
                foreach (String Folder in FolderNodes)
                {
                    if (Folder.Trim() == String.Empty)
                        continue;
                    String folder_name = settings.GetSetting(nodename + "/" + Folder, String.Empty);
                    if (folder_name.Trim() == String.Empty)
                        continue;

                    Preferences.AddLocation(folder_name);
                }
            }
            #endregion

            // To Clear out old Registry Keys for anyone using the older versions of the program.
            string Yekyaa_Key = @"SOFTWARE\Yekyaa";
            try
            {
                RegistryKey deletekey = Registry.LocalMachine.OpenSubKey(Yekyaa_Key + "\\Preferences");
                if (deletekey != null)
                    Registry.LocalMachine.DeleteSubKey(Yekyaa_Key + "\\Preferences", false);
            }
            catch
            {
            }
            try
            {
                RegistryKey deletekey = Registry.LocalMachine.OpenSubKey(Yekyaa_Key);
                if (deletekey != null)
                    Registry.LocalMachine.DeleteSubKey(Yekyaa_Key, false);
            }
            catch
            {
            }
        }

        private void SavePreferences()
        {
            if (!Directory.Exists(Preferences.AppMyDocsFolderName))
                Directory.CreateDirectory(Preferences.AppMyDocsFolderName);

            // only save if not minimized or maximized
            if (this.WindowState == FormWindowState.Normal)
            {
                settings.PutSetting("MainProgram/Top", this.Location.Y);
                settings.PutSetting("MainProgram/Left", this.Location.X);
                settings.PutSetting("MainProgram/Width", this.Size.Width);
                settings.PutSetting("MainProgram/Height", this.Size.Height);
            }

            if (Preferences.MenuXMLFile != String.Empty)
                settings.PutSetting("MainProgram/MenuXMLFile", Preferences.MenuXMLFile);

            settings.PutSetting("MainProgram/TemplatesFolderName", Preferences.TemplatesFolderName);
            settings.PutSetting("MainProgram/IsMaximized", Preferences.IsMaximized);
            settings.PutSetting("MainProgram/EnterCreatesNewLine", Preferences.EnterCreatesNewLine);
            settings.PutSetting("MainProgram/ShowBlankBooks", Preferences.ShowBlankBooks);
            settings.PutSetting("MainProgram/MaxMacroSets", Preferences.Max_Macro_Sets);
            settings.PutSetting("MainProgram/UseExplorerViewForFolderOpen", Preferences.UseExplorerViewOnFolderOpen);
            settings.PutSettingLanguage("MainProgram/Language", Preferences.Language);
            settings.PutSetting("MainProgram/UseFolderAsRoot", Preferences.UseFolderAsRoot);
            settings.PutSettingLanguage("MainProgram/ProgramLanguage", Preferences.Program_Language);
            settings.PutSetting("MainProgram/Include_Header", Preferences.Include_Header);
            settings.PutSetting("MainProgram/MaxMenuItems", Preferences.Max_Menu_Items);
            settings.PutSetting("MainProgram/LoadItems", Preferences.LoadItems);
            settings.PutSetting("MainProgram/LoadKeyItems", Preferences.LoadKeyItems);
            settings.PutSetting("MainProgram/LoadAutoTranslatePhrases", Preferences.LoadAutoTranslatePhrases);
            settings.PutSetting("MainProgram/MinimizeToTray", Preferences.MinimizeToTray);

            settings.DeleteSetting("CharacterNames");

            if (characterList != null)
            {
                for (int i = 0; i < characterList.Length; i++)
                {
                    settings.PutSetting("CharacterNames/" + characterList[i].Type, characterList[i].Text.Trim());
                }
            }

            settings.DeleteSetting("LastLoadedFolders");
            settings.DeleteSetting("LastLoadedLocations");

            if (Preferences.PathToOpen.Count > 0)
            {
                for (int i = 0; i < Preferences.PathToOpen.Count; i++)
                {
                    if (Preferences.PathToOpen[i].Trim() == String.Empty)
                        continue;

                    settings.PutSetting(String.Format("LastLoadedLocations/Location{0:X4}", i), Preferences.PathToOpen[i].Trim());
                }
            }

            settings.SaveSettings();
        }
        #endregion

        #region MainForm Methods (MacroFile related)
        #region MainForm Methods (NewFileInMemory() overloads)
        private CMacroFile NewFileInMemory(CMacroFile src, FileInfo fi)
        {
            CMacroFile ret = NewFileInMemory(fi);
            ret.CopyFrom(src);
            return (ret);
        }

        private CMacroFile NewFileInMemory(CMacroFile src, String s)
        {
            FileInfo fi = new FileInfo(s);
            CMacroFile ret = NewFileInMemory(fi);
            ret.CopyFrom(src);
            return (ret);
        }

        private CMacroFile NewFileInMemory(String s)
        {
            FileInfo fi = new FileInfo(s);
            return NewFileInMemory(fi);
        }

        private CMacroFile NewFileInMemory(FileInfo fi)
        {
            // Search for one with same filename that may be deleted
            CMacroFile cmf = FindMacroFileExactByFileName(fi.FullName, true);

            // if not found
            if (cmf == null)
            {
                cmf = new CMacroFile(this._ATPhraseLoader);
                MacroFiles.Add(cmf);
            }

            cmf.fName = fi.FullName;

            int first_index = -1, last_index = -1;
            first_index = fi.FullName.LastIndexOf('r');
            last_index = fi.FullName.LastIndexOf('.');
            cmf.FileNumber = -1;
            if (fi.FullName.Contains("\\mcr") && fi.FullName.Contains(".dat"))
            {
                if ((first_index != -1) && (last_index != -1))
                {
                    string number = fi.FullName.Substring(first_index + 1, last_index - (first_index + 1));
                    if (number == String.Empty)
                        cmf.FileNumber = 0;
                    else cmf.FileNumber = Int32.Parse(number);
                }
            }
            if (cmf.FileNumber >= Preferences.Max_Macro_Sets)
                cmf.FileNumber = -1;
            LogMessage.Log("..Created a new file '" + fi.FullName + "' in memory");

            int index = this.treeView.Nodes.IndexOfKey("Templates <x_ffxi_me_x>");

            BuildMacroFileNodes(cmf, this.treeView.Nodes, (index > -1) ? this.treeView.Nodes[index] : null);
            cmf.Changed = true;
            if (cmf.IsDeleted)
                cmf.Restore();
            return cmf;
        }
        #endregion

        #region MainForm Methods (Swap() overloads)
        private bool Swap(ref CMacroFile cmfe_drop, ref CMacroFile cmfe_drag)
        {
            if ((cmfe_drop == null) || (cmfe_drag == null))
                return false;

            CMacroFile tmp = new CMacroFile(this._ATPhraseLoader);
            tmp.CopyFrom(cmfe_drop);
            cmfe_drop.CopyFrom(cmfe_drag);
            cmfe_drag.CopyFrom(tmp);
            return true;
        }

        private bool Swap(ref CMacro cm_drop, ref CMacro cm_drag)
        {
            if ((cm_drop == null) || (cm_drag == null))
                return false;

            CMacro tmp = new CMacro();
            tmp.CopyFrom(cm_drop);
            cm_drop.CopyFrom(cm_drag);
            cm_drag.CopyFrom(tmp);
            CMacroFile cmf_drop = FindMacroFileByNode(cm_drop.thisNode),
                        cmf_drag = FindMacroFileByNode(cm_drag.thisNode);
            if (cmf_drop != null)
            {
                cmf_drop.Changed = true;
            }
            if (cmf_drag != null)
            {
                cmf_drag.Changed = true;
            }
            return true;
        }
        #endregion

        #region MainForm Methods (Modify() overloads)
        private void Modify(ref CMacro cm_drag, ref CMacro cm_drop, DragEventArgs e)
        {
            if (cm_drag == cm_drop) // No effect, but just in case
                return;

            #region Swap/Copy Drag & Drop Macros and update
            if ((cm_drag != null) && (cm_drop != null))
            {
                LogMessage.Log("..DragDrop START:{0} Macro src:{1} dest:{2}",
                    e.Effect,
                    cm_drag.Name, cm_drop.Name);

                if (e.Effect == DragDropEffects.Link)
                {
                    if (Swap(ref cm_drop, ref cm_drag))
                    {
                        // if Swap is successful
                        // update node names
                        cm_drag.thisNode.Text = cm_drag.DisplayName();
                        cm_drop.thisNode.Text = cm_drop.DisplayName();
                    }

                }
                else if (e.Effect == DragDropEffects.Copy)
                {
                    if (cm_drop.CopyFrom(cm_drag))
                    {
                        // if Copy is successful
                        // Update node name
                        // and set Changed true on the Drops macrofile (not handled by copy)
                        cm_drop.thisNode.Text = cm_drop.DisplayName();
                        CMacroFile cmf_drop = FindMacroFileByNode(cm_drop.thisNode);
                        if (cmf_drop != null)
                            cmf_drop.Changed = true;
                    }
                }
                LogMessage.Log("..DragDrop END:{0} Macro src:{1} dest:{2}",
                    e.Effect,
                    cm_drag.Name, cm_drop.Name);
            }
            else LogMessage.Log("....Modify(): Swap Macro, one of the two is null");
            #endregion
        }

        private void Modify(ref CMacroFile cmf_drag, ref CMacroFile cmf_drop, DragEventArgs e)
        {
            #region Swap/Copy Drag & Drop MacroFiles and update
            if ((cmf_drag != null) && (cmf_drop != null))
            {
                if (e.Effect == DragDropEffects.Link)
                {
                    Swap(ref cmf_drop, ref cmf_drag);
                }
                else if (e.Effect == DragDropEffects.Copy)
                    cmf_drop.CopyFrom(cmf_drag);
            }
            else LogMessage.Log("....Modify(): cmf_drag: {0}  cmf_drop: {1}",
                (cmf_drag == null) ? "<NULL>" : cmf_drag.thisNode.Text,
                (cmf_drop == null) ? "<NULL>" : cmf_drop.thisNode.Text);
            #endregion
        }
        #endregion
        #endregion

        #region MainForm Methods (Macro-specific, FillForm(), DisplayName(), etc)
        #region DONE: Macro Specific Utilities
        /// <summary>
        /// *** This is an exact search TreeNode > CMacro
        /// </summary>
        /// <param name="tN">Node to search for.</param>
        /// <returns>null if not found, CMacro otherwise.</returns>
        private CMacro FindMacroByNode(TreeNode tN)
        {
            // Sanity checks
            if ((tN == null) || (MacroFiles == null))
                return null;

            // Loop through the MacroFiles list
            for (int i = 0; i < MacroFiles.Count; i++)
            {
                // if no Macros are present continue (unneeded Sanity Check)
                if (MacroFiles[i].Macros == null)
                    continue;
                // if Macros are present, Loop until the correct one is found
                // else keep trying (return to previous loop)
                for (int x = 0; x < 20; x++)
                    if (MacroFiles[i].Macros[x].thisNode == tN)
                        return MacroFiles[i].Macros[x];
            }
            // TreeNode given is not a CMacro selection, return null
            return null;
        }

        private CMacroFile FindMacroFileExactByFileName(String fName)
        {
            return FindMacroFileExactByFileName(fName, false);
        }

        /* FindMacroFileExactByFileName(String) description:
         *  parameter(s) fName: String
         *  returns: CMacroFile if found, null if not found
         *   Attempts to find MacroFile by searching all MacroFiles
         *   for a matching filename, returns file found
         * *** This is an exact search String -> CMacroFile
         */
        private CMacroFile FindMacroFileExactByFileName(String fName, bool includedeleted)
        {
            // Sanity checks
            if ((fName == String.Empty) || (MacroFiles == null))
                return null;

            for (int i = 0; i < MacroFiles.Count; i++)
            {
                if ((includedeleted == false) && (MacroFiles[i].IsDeleted))
                    continue;

                if (MacroFiles[i].fName == fName)
                    return MacroFiles[i];
            }
            // if we got here, we didn't find a matching Node.
            return null;
        }
        /* FindMacroFileExactByNode(TreeNode) description:
         *  parameter(s) mainNode: TreeNode
         *  returns: CMacroFile if found, null if not found
         *   Attempts to find MacroFile by searching all MacroFile
         *   Nodes and their child-nodes, skipping Ctrl and Alt Nodes
         * *** This is an exact search TreeNode -> CMacroFile
         */
        private CMacroFile FindMacroFileExactByNode(TreeNode tN)
        {
            // Sanity checks
            if ((tN == null) || (MacroFiles == null))
                return null;

            for (int i = 0; i < MacroFiles.Count; i++)
            {
                if (MacroFiles[i].IsDeleted)
                    continue;
                // if the Ctrl Macro Node, Alt Macro Node, or main Node for this MacroFile
                // is the same as the given node, we found it.
                if (MacroFiles[i].thisNode == tN)
                    return MacroFiles[i];
                // if this MacroFile has Macros
                /*else if (MacroFiles[i].Macros != null)
                {
                    // Loop through them
                    for (int phrasetoSearch = 0; phrasetoSearch < 20; phrasetoSearch++)
                        // until we find a matching Node
                        if (MacroFiles[i].Macros[phrasetoSearch].thisNode == mainNode)
                            return MacroFiles[i];
                }*/
            }
            // if we got here, we didn't find a matching Node.
            return null;
        }
        /* FindMacroFileByNode(TreeNode) description:
         *  parameter(s) mainNode: TreeNode
         *  returns: CMacroFile if found, null if not found
         *   Attempts to find MacroFile by searching all MacroFile
         *   Nodes and their child-nodes
         */
        private CMacroFile FindMacroFileByNode(TreeNode tN)
        {
            // Sanity checks
            if ((tN == null) || (MacroFiles == null))
                return null;

            for (int i = 0; i < MacroFiles.Count; i++)
            {
                if (MacroFiles[i].IsDeleted)
                    continue;
                // if the Ctrl Macro Node, Alt Macro Node, or main Node for this MacroFile
                // is the same as the given node, we found it.
                if ((MacroFiles[i].thisNode == tN) ||
                    (MacroFiles[i].ctrlNode == tN) ||
                    (MacroFiles[i].altNode == tN))
                    return MacroFiles[i];
                // if this MacroFile has Macros
                else if (MacroFiles[i].Macros != null)
                {
                    // Loop through them
                    for (int x = 0; x < 20; x++)
                        // until we find a matching Node
                        if (MacroFiles[i].Macros[x].thisNode == tN)
                            return MacroFiles[i];
                }
            }
            // if we got here, we didn't find a matching Node.
            return null;
        }

        #region DONE: IsMacroFile() overloads
        static private bool IsMacroFile(string fName)
        {
            FileInfo fi = new FileInfo(fName);
            return IsMacroFile(fi);
        }

        static private bool IsMacroFile(FileInfo fi)
        {
            if (fi.Exists && (fi.Length != 0x1DC8)) // && ((fi.Attributes & FileAttributes.Directory) != FileAttributes.Directory)) // 7624 bytes
                return false;
            return true;
        }
        #endregion
        #endregion
        #region DONE: FillForm overloads
        private void FillForm(CMacro cm)
        {
            this.SuspendLayout();
            if (cm == null)
            {
                //Log("FillForm(CMacro): parameter cm = null, error");
                DisableTextBoxes();
                ChangeButtonNames((CMacroFile)null);
                this.ResumeLayout();
                return;
            }
            /*
            for (int i = 0; i < 20; i++)
                if (radiobuttons[i].Checked == true)
                {
                    if (cm.MacroNumber != i)
                    {
                        radiobuttons[i].Checked = false;
                    }
                    break;
                }
            */
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].Enabled == false)
                {
                    if (cm.MacroNumber != i)
                    {
                        buttons[i].Enabled = true;
                    }
                    break;
                }
            }
            /*
            if (radiobuttons[cm.MacroNumber].Checked == false)
                radiobuttons[cm.MacroNumber].Checked = true;            
             */
            if (buttons[cm.MacroNumber].Enabled == true)
                buttons[cm.MacroNumber].Enabled = false;

            this.labelCurrentEditing.Text = String.Format("Currently Editing Macro Set ~~ {0}{1} : Name : <{2}>",
                    (cm.MacroNumber < 10) ? "Ctrl-" : "Alt-", (cm.MacroNumber + 1) % 10, (cm.Name.Trim() == String.Empty) ? "<Empty>" : cm.Name);

            SuppressNodeUpdates = true;
            textBoxName.Text = cm.Name;
            textBoxLine1.Text = cm.Line[0];
            textBoxLine2.Text = cm.Line[1];
            textBoxLine3.Text = cm.Line[2];
            textBoxLine4.Text = cm.Line[3];
            textBoxLine5.Text = cm.Line[4];
            textBoxLine6.Text = cm.Line[5];
            SuppressNodeUpdates = false;
            // genius, finding the macro file by the macro's node
            ChangeButtonNames(FindMacroFileByNode(cm.thisNode));
            EnableTextBoxes();
            this.ResumeLayout();
        }

        private void FillForm(TreeNode treeNode)
        {
            if (treeNode == null)
                FillForm((CMacro)null);
            else FillForm(GetCurrentMacro(FindMacroFileByNode(treeNode)));
        }

        private void FillForm()
        {
            if (treeView.SelectedNode == null)
            {
                FillForm((CMacro)null);
                return;
            }
            else FillForm(GetCurrentMacro(FindMacroFileByNode(treeView.SelectedNode)));
        }

        #endregion
        #region DONE: DisplayName, SaveToMemory, ChangeButtonNames, Enable/DisableTextBoxes, & Utilities
        /// <summary>
        /// Gets the currently edited Macro for the given Macro File.
        /// </summary>
        /// <param name="cmf">Macro File whose Macro we want to return.</param>
        /// <returns>Returns a Macro assuming the given Macro File is the currently edited one.</returns>
        private CMacro GetCurrentMacro(CMacroFile cmf)
        {
            if (cmf == null)
                return null;

            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i].Enabled == false)
                    return (cmf.Macros[i]);

            return null;
        }

        #region Enable/DisableTextBoxes() utilities
        /// <summary>
        /// Used in FillForm overloads to facilitate a quick way to turn on TextBoxes.
        /// </summary>
        void EnableTextBoxes()
        {
            textBoxName.Enabled = true;
            textBoxLine1.Enabled = true;
            textBoxLine2.Enabled = true;
            textBoxLine3.Enabled = true;
            textBoxLine4.Enabled = true;
            textBoxLine5.Enabled = true;
            textBoxLine6.Enabled = true;

            this.editToolStripMenuItem.Enabled = true;
            this.cutToolStripMenuItem.Enabled = true;
            this.copyToolStripMenuItem.Enabled = true;
            if (clipboard_macro != null)
                this.pasteToolStripMenuItem.Enabled = true;
            this.deleteToolStripMenuItem.Enabled = true;
        }

        private void DisableTextBoxes()
        {
            textBoxLine1.Text = "";
            textBoxLine2.Text = "";
            textBoxLine3.Text = "";
            textBoxLine4.Text = "";
            textBoxLine5.Text = "";
            textBoxLine6.Text = "";
            textBoxName.Text = "";
            labelCurrentEditing.Text = "";

            textBoxName.Enabled = false;
            textBoxLine1.Enabled = false;
            textBoxLine2.Enabled = false;
            textBoxLine3.Enabled = false;
            textBoxLine4.Enabled = false;
            textBoxLine5.Enabled = false;
            textBoxLine6.Enabled = false;

            this.cutToolStripMenuItem.Enabled = false;
            this.copyToolStripMenuItem.Enabled = false;
            this.pasteToolStripMenuItem.Enabled = false;
            this.deleteToolStripMenuItem.Enabled = false;
            this.editToolStripMenuItem.Enabled = false;
        }
        #endregion

        #region DisplayName overloads & utilities
        /*
         * GetNodeFullName: Returns fullpath or "(null Node)" of given node 
         */
        private string GetNodeFullName(TreeNode tN)
        {
            if (tN == null)
                return "(null Node)";
            return tN.FullPath;
        }

        /*
         * GetNodeName: Returns just the char_Name or "(null Node)" of given node 
         */
        private string GetNodeName(TreeNode tN)
        {
            if (tN == null)
                return "(null Node)";
            return tN.Name;
        }
        /*
         * DisplayName(CMacro): Provides a displayable string given a CMacro
         */
        private string DisplayName(CMacro cm)
        {
            if (cm == null)
                return DisplayName(-1);

            if (cm.Name == String.Empty)
                return DisplayName(cm.MacroNumber);
            return cm.Name;
        }

        /*
         * DisplayName(int): Provides a default string given a number.
         */
        private string DisplayName(int num)
        {
            if ((num >= 0) && (num < 20))
                return String.Format("{0}{1}", (num < 10) ? "Ctrl-" : "Alt-",
                            (num + 1) % 10);
            return "<ERROR>";
        }
        #endregion

        #region SaveToMemory overloads
        /// <summary>
        /// Saves the given Macro to memory with very little error checking.
        /// </summary>
        /// <param name="cm">Macro to save to memory.</param>
        private void SaveToMemory(CMacro cm)
        {
            if (cm == null)
            {
                LogMessage.Log("SaveToMemory(CMacro): cm == null, error");
                return;
            }
            if (cm.Line == null)
            {
                LogMessage.Log("SaveToMemory(CMacro): cm.Line == null, error");
                return;
            }
            if ((textBoxName.Enabled == false) ||
                (textBoxLine1.Enabled == false) ||
                (textBoxLine2.Enabled == false) ||
                (textBoxLine3.Enabled == false) ||
                (textBoxLine4.Enabled == false) ||
                (textBoxLine5.Enabled == false) ||
                (textBoxLine6.Enabled == false))
            {
                LogMessage.Log("SaveToMemory(CMacro) cancelled: Status of TextBoxes:\r\n" +
                        "Name: {0}\r\n" +
                        "Line1: {1}\r\n" +
                        "Line2: {2}\r\n" +
                        "Line3: {3}\r\n" +
                        "Line4: {4}\r\n" +
                        "Line5: {5}\r\n" +
                        "Line6: {6}", textBoxName.Enabled, textBoxLine1.Enabled, textBoxLine2.Enabled,
                        textBoxLine3.Enabled, textBoxLine4.Enabled, textBoxLine5.Enabled, textBoxLine6.Enabled);
                return;
            }
            LogMessage.Log("SaveToMemory(CMacro) saved successfully: '{0}'", textBoxName.Text);
            cm.Name = textBoxName.Text;
            cm.Line[0] = textBoxLine1.Text;
            cm.Line[1] = textBoxLine2.Text;
            cm.Line[2] = textBoxLine3.Text;
            cm.Line[3] = textBoxLine4.Text;
            cm.Line[4] = textBoxLine5.Text;
            cm.Line[5] = textBoxLine6.Text;
        }

        /// <summary>
        /// Saves the Macro associated with the given node.
        /// </summary>
        /// <param name="tN">TreeNode whose associated macro we would like to store.</param>
        private void SaveToMemory(TreeNode tN)
        {
            CMacro cm = FindMacroByNode(tN);
            CMacroFile cmf = FindMacroFileByNode(tN);

            if (cm == null)
                cm = GetCurrentMacro(cmf);

            if (cm == null)
            {
                LogMessage.Log("SaveToMemory(TreeNode): mainNode (" + GetNodeName(tN) +
                    ") Macro is null");
                return;
            }

            SaveToMemory(cm);
        }

        /// <summary>
        /// Saves the Macro currently being edited.
        /// </summary>
        /// <remarks>Attempts to check SelectedNode. If that isn't a Macro,
        /// it'll use the MacroFile it's associated with and find the currently edited Macro to save.</remarks>
        private void SaveToMemory()
        {
            CMacro cm = FindMacroByNode(treeView.SelectedNode);
            CMacroFile cmf = FindMacroFileByNode(treeView.SelectedNode);

            if (cm == null)
                cm = GetCurrentMacro(cmf);

            if (cm == null)
            {
                LogMessage.Log("SaveToMemory(): treeView.SelectedNode (" + GetNodeName(treeView.SelectedNode) +
                    ") Macro is null");
                return;
            }

            SaveToMemory(cm);
        }
        #endregion

        #region ChangeButtonNames overloads
        private void ChangeButtonNames(TreeNode treeNode)
        {
            if (treeNode == null)
                ChangeButtonNames((CMacroFile)null);
            else ChangeButtonNames(FindMacroFileByNode(treeNode));
        }
        private void ChangeButtonNames(CMacroFile cmf)
        {
            if ((cmf == null) || (cmf.Macros == null))
                for (int i = 0; i < 20; i++)
                    buttons[i].Text = String.Format("{0}{1}", (i < 10) ? "Ctrl-" : "Alt-",
                                        (i + 1) % 10); // this should be 1 - 9 & 0, not 0 - 9
            //radiobuttons[i].Text = String.Format("{0}{1}", (i < 10) ? "Ctrl-" : "Alt-",
            //                                              (i + 1) % 10);
            else for (int i = 0; i < 20; i++)
                    buttons[i].Text = cmf.Macros[i].DisplayName();
            //radiobuttons[i].Text = cmf.Macros[i].DisplayName();
        }

        private void ChangeButtonNames()
        {
            if (treeView.SelectedNode == null)
                ChangeButtonNames((CMacroFile)null);
            else ChangeButtonNames(FindMacroFileByNode(treeView.SelectedNode));
        }
        #endregion
        #endregion
        #endregion

        #region MainForm Methods (TreeView Event Handlers and Methods)
        private void CreateCursors()
        {
            // I'm going to make a cursor double the size
            // because the cursor I create is always
            // going to have it's hotspot dead center
            // and I need room to work to put hotspot
            // in the correct location
            int width = Cursors.Default.Size.Width * 2;
            int height = Cursors.Default.Size.Height * 2;

            // Get current hotspot locations, so
            // that I may make them center of my new cursors.
            int Hotspot_x = Cursors.Default.HotSpot.X;
            int Hotspot_y = Cursors.Default.HotSpot.Y;

            Bitmap bmp_cursor_link = new Bitmap(width, height);
            Bitmap bmp_cursor_copy = new Bitmap(width, height);
            Graphics gfx_cursor_link = Graphics.FromImage(bmp_cursor_link);
            Graphics gfx_cursor_copy = Graphics.FromImage(bmp_cursor_copy);

            // Draw the Cursor accouting for WHERE the hotspot is.
            // Put the hotspot dead center of my bitmap.
            int center_x = width / 2 - Hotspot_x;
            int center_y = height / 2 - Hotspot_y;
            Rectangle wheretodraw = new Rectangle(center_x, center_y,
                        Cursors.Default.Size.Width, Cursors.Default.Size.Height);

            Cursors.Default.Draw(gfx_cursor_link, wheretodraw);
            Cursors.Default.Draw(gfx_cursor_copy, wheretodraw);

            // Quick and dirty, determine the farthest out 'x' value
            // that doesn't have a transparent value
            // also figure out the lowest setting 'y' value that's not transparent
            int maxx = 0;
            int maxy = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                    if (bmp_cursor_copy.GetPixel(x, y).A != 0) // not transparent
                    {
                        if (x > maxx)
                            maxx = x;
                        if (y > maxy)
                            maxy = y;
                    }
            }
            // take those max values, account for adding a 10-11 pixel box
            // (to figure out where i REALLY want the box at)
            int bottom_x = maxx + 10;
            int bottom_y = maxy + 1;

            // Setup my corners
            Point topleft = new Point(bottom_x - 10, bottom_y - 10);
            Point topright = new Point(bottom_x, bottom_y - 10);
            Point bottomleft = new Point(bottom_x - 10, bottom_y);
            Point bottomright = new Point(bottom_x, bottom_y);

            // Draw the white box for storing the Copy/Link icon
            Rectangle Box = new Rectangle(topleft, new Size(10, 10));
            SolidBrush white_fill_brush = new SolidBrush(Color.White);

            gfx_cursor_link.FillRectangle(white_fill_brush, Box);
            gfx_cursor_copy.FillRectangle(white_fill_brush, Box);

            // Setup points for drawing the two lines for the '+' for CopyCursor
            Point plusmidtop = new Point(topleft.X + 5, topleft.Y + 3);
            Point plusmidbottom = new Point(topleft.X + 5, topleft.Y + 7);
            Point plusmidleft = new Point(topleft.X + 3, topleft.Y + 5);
            Point plusmidright = new Point(topleft.X + 7, topleft.Y + 5);

            // Load the embedded resource for putting in the box for the Swap icon
            Bitmap bmp = new Bitmap(linkbmp);

            // Setup my pens for drawing the lines in the right color
            Pen gray_pen = new Pen(Color.Gray);
            Pen black_pen = new Pen(Color.Black, 1);

            // Do NOT use DrawLines here as that would connect all these points
            // DO draw the gray first considering that's EXACTLY how WinXP does it.
            // also, you want the topleft to have a lesser color
            // and the bottom & right should overlap the top and left
            gfx_cursor_link.DrawLine(gray_pen, bottomleft, topleft);
            gfx_cursor_link.DrawLine(gray_pen, topleft, topright);
            gfx_cursor_link.DrawLine(black_pen, bottomleft, bottomright);
            gfx_cursor_link.DrawLine(black_pen, bottomright, topright);

            gfx_cursor_copy.DrawLine(gray_pen, bottomleft, topleft);
            gfx_cursor_copy.DrawLine(gray_pen, topleft, topright);
            gfx_cursor_copy.DrawLine(black_pen, bottomleft, bottomright);
            gfx_cursor_copy.DrawLine(black_pen, bottomright, topright);

            // Draw the plus with previous coords given for the Copy cursor
            gfx_cursor_copy.DrawLine(black_pen, plusmidtop, plusmidbottom);
            gfx_cursor_copy.DrawLine(black_pen, plusmidleft, plusmidright);

            // Draw the actual image into the box for the Link cursor
            gfx_cursor_link.DrawImage(bmp.GetThumbnailImage(9, 9, null, System.IntPtr.Zero), bottom_x - 9, bottom_y - 9);

            // Save the handle assuming the exact center of
            // our Bitmap is considered the Hotspot
            // so our canvas was 64x64 :)
            // somehow it gets shrunk or something, dunno.
            this.CursorLink = new Cursor(bmp_cursor_link.GetHicon());
            this.CursorCopy = new Cursor(bmp_cursor_copy.GetHicon());
        }

        /// <summary>
        /// Manually selects a given node while having the BeforeSelect event ignored by setting a variable.
        /// </summary>
        /// <param name="tN">TreeNode to be the newly selected node.</param>
        private void RawSelect(TreeNode tN)
        {
            SuppressBeforeSelect = true;
            this.treeView.SelectedNode = tN;
            SuppressBeforeSelect = false;
        }

        /// <summary>
        /// Determines if a given node is the "Alt Macros" node of a Macro File.
        /// </summary>
        /// <param name="node">The TreeNode to check.</param>
        /// <param name="cmf">The Macro File whose "Alt Macros" node we'll compare to.</param>
        /// <returns>Returns true if TreeNode is the "Alt Macros" node of the given MacroFile. Returns false if not.</returns>
        private bool is_altnode(TreeNode node, CMacroFile cmf)
        {
            if ((cmf == null) || (node == null))
                return false;
            return (node == cmf.altNode);
        }

        /// <summary>
        /// Determines if a given node is the "Alt Macros" node of a Macro File.
        /// </summary>
        /// <param name="node">The TreeNode to check.</param>
        /// <param name="cmf">The Macro File whose "Ctrl Macros" node we'll compare to.</param>
        /// <returns>Returns true if TreeNode is the "Ctrl Macros" node of the given MacroFile. Returns false if not.</returns>
        private bool is_ctrlnode(TreeNode node, CMacroFile cmf)
        {
            if ((cmf == null) || (node == null))
                return false;
            return (node == cmf.ctrlNode);
        }

        /// <summary>
        /// Checks if a specific bit is set.
        /// </summary>
        /// <param name="num">The number whose might have the bit set.</param>
        /// <param name="bit">The bit value we want to check for.</param>
        /// <returns>Returns true if set. Returns false if not set.</returns>
        private bool is_set(int num, int bit)
        {
            return ((num & bit) == bit);
        }

        private void treeView_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            TagInfo ti = e.Node.Tag as TagInfo;
            if ((ti.Type != "char") && (ti.Type != "book"))
            {
                e.CancelEdit = true;
            }
        }

        private void treeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            TagInfo ti = e.Node.Tag as TagInfo;

            if ((ti.Type == "char") || (ti.Type == "book"))
            {
                if (ti.Type == "char")
                {
                    // rename character
                    if (e.Label != null)
                    {
                        String namesearchPattern = @"[^a-zA-Z0-9\ \-\.\?\\]";
                        String lbl = System.Text.RegularExpressions.Regex.Replace(e.Label.Trim(), namesearchPattern, "");
                        if ((lbl != String.Empty) && (lbl != e.Node.Name))
                        {
                            e.Node.Text = e.Node.Name + " <" + lbl + ">";
                        }
                        else e.Node.Text = e.Node.Name;
                        RenameCharacter(e.Node.Name, lbl);
                    }
                    else
                    {
                        int folder_x = Int32.MaxValue;
                        if (characterList != null)
                        {
                            for (folder_x = 0; folder_x < characterList.Length; folder_x++)
                            {
                                if (characterList[folder_x].Type == e.Node.Name)
                                    break;
                            }
                            if (folder_x < characterList.Length)
                            {
                                e.Node.Text = e.Node.Name + " <" + characterList[folder_x].Text + ">";
                            }
                            else e.Node.Text = e.Node.Name;
                        }
                        else e.Node.Text = e.Node.Name;
                    }
                }
                else if ((ti.Type == "book") && (e.Label != null)) // rename book
                {
                    String lbl = GetValidName(e.Label);
                    int number = -1;
                    if ((lbl.Trim() == String.Empty) && Int32.TryParse(e.Node.Name, out number))
                    {
                        lbl = String.Format("Book{0:D2}", number);
                    }

                    if (lbl.Length > 15)
                        lbl = lbl.Substring(0, 15);

                    if (lbl != String.Empty)
                    {
                        // this updates the Node text as well
                        RenameBook(e.Node, lbl);
                    }
                }
                e.CancelEdit = true;
            }
            this.treeView.LabelEdit = false;
        }

        /// <summary>
        /// OnNodeMouseClick event handler for the treeView that's activated when a Node is clicked.
        /// </summary>
        /// <param name="sender">The node that was clicked.</param>
        /// <param name="e">The event arguments that contain location, button, and node information.</param>
        private void treeView_NodeMouseClick(object sender, System.Windows.Forms.TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                #region Build Context Menus for Right-click on Node
                this.treeView.SelectedNode = e.Node;
                Thread.Sleep(10);
                ContextMenuStrip cms = new ContextMenuStrip();
                ToolStripItem[] tsmi = new ToolStripItem[1];
                CMacro cm = FindMacroByNode(e.Node);
                CMacroFile cmf = FindMacroFileByNode(e.Node),
                            cmf_e = FindMacroFileExactByNode(e.Node);
                TagInfo tI = null;
                if (e.Node.Tag != null)
                    tI = e.Node.Tag as TagInfo;
                if (tI.Type == "book")
                {
                    #region Book Right-Clicked
                    Array.Resize(ref tsmi, 5);
                    tsmi[0] = new ToolStripLabel("Book Menu");
                    Font f = new Font(tsmi[0].Font, FontStyle.Bold);
                    tsmi[0].Font = f;
                    tsmi[1] = new ToolStripSeparator();

                    tsmi[2] = new ToolStripMenuItem("Rename Book...", Resources.Rename, DynamicMenu_Click);
                    tsmi[2].Tag = e.Node as Object;
                    tsmi[2].Name = "Rename_Book";

                    tsmi[3] = new ToolStripSeparator();

                    tsmi[4] = new ToolStripMenuItem("New File", Resources.NewMacro, DynamicMenu_Click);
                    tsmi[4].Tag = e.Node as Object;
                    tsmi[4].Name = "New_File";
                    if (e.Node.Nodes.Count >= 10)
                    {
                        tsmi[4].Enabled = false;
                    }
                    #endregion
                }
                else if (((cm != null) && (cmf != null)) && (tI.Type == "macro"))
                {
                    #region Macro Right-Clicked
                    // Macro Selected
                    ToolStripItem[] EditMenu = new ToolStripItem[4];
                    Array.Resize(ref tsmi, 8);
                    tsmi[0] = new ToolStripLabel("Macro Menu");
                    Font f = new Font(tsmi[0].Font, FontStyle.Bold);
                    tsmi[0].Font = f;

                    tsmi[1] = new ToolStripSeparator();


                    EditMenu[0] = new ToolStripMenuItem("Cut Macro", cutToolStripMenuItem.Image, cutToolStripMenuItem_Click);
                    EditMenu[1] = new ToolStripMenuItem("Copy Macro", copyToolStripMenuItem.Image, copyToolStripMenuItem_Click);
                    EditMenu[2] = new ToolStripMenuItem("Paste Macro", pasteToolStripMenuItem.Image, pasteToolStripMenuItem_Click);
                    if (clipboard_macro == null)
                        EditMenu[2].Enabled = false;

                    EditMenu[3] = new ToolStripMenuItem("Delete Macro", deleteToolStripMenuItem.Image, deleteToolStripMenuItem_Click);
                    EditMenu[3].Name = "Delete_Macro";
                    EditMenu[3].Tag = cm as Object;


                    tsmi[2] = new ToolStripMenuItem("&Edit Menu", Resources.NavForward, EditMenu);

                    tsmi[3] = new ToolStripSeparator();

                    tsmi[4] = new ToolStripMenuItem("Copy Macro '" + cm.thisNode.Text + "' To Templates", Resources.CopyHS, DynamicMenu_Click);
                    tsmi[4].Tag = cm as Object;
                    tsmi[4].Enabled = false;
                    tsmi[4].Name = "Copy_Macro";
                    tsmi[4].Visible = false;

                    tsmi[5] = new ToolStripSeparator();
                    tsmi[5].Visible = false;

                    tsmi[6] = new ToolStripMenuItem("Save '" + cmf.thisNode.Text.TrimEnd('*') + "'", Resources.saveHS, SaveThistoolStripMenuItem_Click);
                    tsmi[6].Tag = e.Node as Object;

                    tsmi[7] = new ToolStripMenuItem("Reload '" + cmf.thisNode.Text.TrimEnd('*') + "'", Resources.ReloadMacro, ReloadThisToCurrentToolStripMenuItem_Click);
                    tsmi[7].Tag = e.Node as Object;
                    #endregion
                }
                else if (((cm == null) && (cmf != null)) && ((tI.Type == "macrofile") || (tI.Type == "ctrlmacro") || (tI.Type == "altmacro")))
                {
                    #region MacroFile or Ctrl/Alt Macro Right-Clicked
                    // MacroFile || Ctrl/Alt Macro Node selected
                    Array.Resize(ref tsmi, 7);
                    tsmi[0] = new ToolStripLabel("Macro Set Menu");
                    Font f = new Font(tsmi[0].Font, FontStyle.Bold);
                    tsmi[0].Font = f;

                    tsmi[1] = new ToolStripSeparator();

                    tsmi[2] = new ToolStripMenuItem("Delete File '" + cmf.thisNode.Text.TrimEnd('*') + "'", Resources.DeleteHS, DynamicMenu_Click);
                    tsmi[2].Tag = cmf as Object;
                    tsmi[2].Enabled = true;
                    tsmi[2].Name = "Delete_File";
                    tsmi[2].Visible = true;

                    tsmi[3] = new ToolStripMenuItem("Clear File '" + cmf.thisNode.Text.TrimEnd('*') + "'", Resources.NewMacro, DynamicMenu_Click);
                    tsmi[3].Tag = cmf as Object;
                    tsmi[3].Name = "Clear_File";
                    tsmi[3].Visible = true;

                    tsmi[4] = new ToolStripSeparator();
                    tsmi[4].Visible = true;

                    tsmi[5] = new ToolStripMenuItem("Save '" + cmf.thisNode.Text.TrimEnd('*') + "'", Resources.saveHS, SaveThistoolStripMenuItem_Click);
                    tsmi[5].Tag = e.Node as Object;

                    tsmi[6] = new ToolStripMenuItem("Reload '" + cmf.thisNode.Text.TrimEnd('*') + "'", Resources.ReloadMacro, ReloadThisToCurrentToolStripMenuItem_Click);
                    tsmi[6].Tag = e.Node as Object;
                    #endregion
                }
                else if (((tI.Type == "char") || (tI.Type == "main")) && (e.Node.Level == 0))// && (e.Node == this.treeView.Nodes[0])) //Main Folder
                {
                    #region Main Folder Right-Clicked
                    Array.Resize(ref tsmi, 8);
                    tsmi[0] = new ToolStripLabel("Main Character Folder Menu");
                    Font f = new Font(tsmi[0].Font, FontStyle.Bold);
                    tsmi[0].Font = f;

                    tsmi[1] = new ToolStripSeparator();

                    tsmi[2] = new ToolStripMenuItem("Character Name...", Resources.Rename, DynamicMenu_Click);
                    tsmi[2].Tag = e.Node as Object;
                    tsmi[2].Name = "Rename_Character";

                    tsmi[3] = new ToolStripSeparator();

                    tsmi[4] = new ToolStripMenuItem("Open " + e.Node.Text + " Folder", Resources.openHS, DynamicMenu_Click);
                    tsmi[4].Tag = e.Node as Object;
                    tsmi[4].Name = "Open_Folder";
                    tsmi[4].Enabled = true;

                    tsmi[5] = new ToolStripSeparator();

                    tsmi[6] = new ToolStripMenuItem("New Folder", Resources.NewFolderHS, DynamicMenu_Click);
                    tsmi[6].Tag = e.Node as Object;
                    tsmi[6].Name = "New_Folder";

                    tsmi[7] = new ToolStripMenuItem("New Macro File", Resources.NewMacro, DynamicMenu_Click);
                    tsmi[7].Tag = e.Node as Object;
                    tsmi[7].Name = "New_File";
                    #endregion
                    TagInfo tIe = e.Node.Tag as TagInfo;
                    string folder = tIe.Text;
                    if (folder != String.Format("{0}\\USER\\{1}", this.FFXIInstallPath.Trim('\\'), e.Node.Name))
                    {
                        tsmi[0].Text = "Main Folder Menu";
                        tsmi[2].Visible = false;
                        tsmi[3].Visible = false; // separator
                    }
                }
                else if ((tI.Type == "template") && (e.Node.Level == 0)) // && (this.treeView.Nodes.Count > 1) && (e.Node == this.treeView.Nodes[1])) // templates
                {
                    #region Templates Folder Right-Clicked
                    Array.Resize(ref tsmi, 6);
                    tsmi[0] = new ToolStripLabel("Templates Menu");
                    Font f = new Font(tsmi[0].Font, FontStyle.Bold);
                    tsmi[0].Font = f;

                    tsmi[1] = new ToolStripSeparator();

                    tsmi[2] = new ToolStripMenuItem("Open " + e.Node.Text + " Folder", Resources.openHS, DynamicMenu_Click);
                    tsmi[2].Tag = e.Node as Object;
                    tsmi[2].Name = "Open_Folder";
                    tsmi[2].Enabled = false;

                    if (!Directory.Exists(TagInfo.GetTagInfo(e.Node.Tag).Text))
                    {
                        tsmi[2].Enabled = false;
                        Open_Template_Folder.Enabled = false;
                    }
                    else
                    {
                        Open_Template_Folder.Enabled = true;
                        tsmi[2].Enabled = true;
                    }

                    tsmi[3] = new ToolStripSeparator();

                    tsmi[4] = new ToolStripMenuItem("New Folder", Resources.NewFolderHS, DynamicMenu_Click);
                    tsmi[4].Tag = e.Node as Object;
                    tsmi[4].Name = "New_Folder";
                    tsmi[4].Enabled = true;

                    tsmi[5] = new ToolStripMenuItem("New Macro File", Resources.NewMacro, DynamicMenu_Click);
                    tsmi[5].Tag = e.Node as Object;
                    tsmi[5].Name = "New_File";
                    tsmi[5].Enabled = true;
                    #endregion
                }
                else if ((tI.Type == "char") || (tI.Type == "folder"))
                {
                    #region Anything In Between Right-Clicked (folders)
                    Array.Resize(ref tsmi, 8);
                    tsmi[0] = new ToolStripLabel("Character Folder Menu");
                    Font f = new Font(tsmi[0].Font, FontStyle.Bold);
                    tsmi[0].Font = f;

                    tsmi[1] = new ToolStripSeparator();

                    tsmi[2] = new ToolStripMenuItem("Rename Character...", Resources.Rename, DynamicMenu_Click);
                    tsmi[2].Tag = e.Node as Object;
                    tsmi[2].Name = "Rename_Character";

                    tsmi[3] = new ToolStripMenuItem("Open '" + e.Node.Text + "' Folder", Resources.openHS, DynamicMenu_Click);
                    tsmi[3].Tag = e.Node as Object;
                    tsmi[3].Name = "Open_Folder";
                    tsmi[3].Enabled = true;

                    tsmi[4] = new ToolStripSeparator();

                    tsmi[5] = new ToolStripMenuItem("New Folder", Resources.NewFolderHS, DynamicMenu_Click);
                    tsmi[5].Tag = e.Node as Object;
                    tsmi[5].Name = "New_Folder";
                    tsmi[5].Enabled = true;

                    tsmi[6] = new ToolStripMenuItem("New Macro File", Resources.NewMacro, DynamicMenu_Click);
                    tsmi[6].Tag = e.Node as Object;
                    tsmi[6].Name = "New_File";
                    tsmi[6].Enabled = true;

                    tsmi[7] = new ToolStripMenuItem("Delete Folder", Resources.DeleteHS, DynamicMenu_Click);
                    tsmi[7].Tag = e.Node as Object;
                    tsmi[7].Name = "Delete_Folder";
                    tsmi[7].Enabled = false;
                    #endregion
                    TagInfo tIe = e.Node.Tag as TagInfo;
                    string folder = tIe.Text;
                    if (tI.Type != "char") //(folder != String.Format("{0}USER\\{1}", this.FFXIInstallPath, e.Node.Name))
                    {
                        tsmi[0].Text = "Folder Menu";
                        tsmi[2].Visible = false;
                        //tmsi[4].Visible = false;  // separator
                        tsmi[7].Enabled = true; // delete folder
                    }
                }

                if ((tsmi[0] != null) && (tsmi.Length > 2))
                {
                    Array.Resize(ref tsmi, tsmi.Length + 3);
                    int len = tsmi.Length;

                    tsmi[len - 3] = new ToolStripSeparator();

                    tsmi[len - 2] = new ToolStripMenuItem("Save All Macro Sets", Resources.SaveAllHS, saveAllToolStripMenuItem_Click);

                    tsmi[len - 1] = new ToolStripMenuItem("Reload All Macro Sets", Resources.ReloadAll, ReloadAllToolStripMenuItem_Click);
                    if ((Preferences.Include_Header == false) && (tsmi.Length == 5))
                    {
                        // header
                        // separator
                        // separator
                        // Save All
                        // Reload All
                        // minimum is header and first separator are hidden
                        // so hide my separator here as well

                        // basically, only hide my separator if it's the first visible item
                        tsmi[3].Visible = false;
                    }
                }
                if (Preferences.ShowDebugInfo)
                {
                    Array.Resize(ref tsmi, tsmi.Length + 2);
                    tsmi[tsmi.Length - 2] = new ToolStripSeparator();

                    tsmi[tsmi.Length - 1] = new ToolStripMenuItem("Show Node Info", Resources.XMLFileHS, DynamicMenu_Click);
                    tsmi[tsmi.Length - 1].Tag = e.Node as Object;
                    tsmi[tsmi.Length - 1].Name = "Show_Node_Info";
                }
                if (tsmi[0] != null)
                {
                    if (Preferences.Include_Header == false)
                    {
                        // in previous if/else, i devisualized separator index [3]
                        // so as to simplify the header dropping based on preferences.
                        tsmi[0].Visible = false;
                        tsmi[1].Visible = false;
                    }
                    cms.SuspendLayout();
                    cms.Items.AddRange(tsmi);
                    cms.ResumeLayout();
                    cms.Show(e.Node.TreeView, e.Location);
                }
                #endregion
            }
            #region Turn This On to Select Nodes that are expanding/collapsing
            // NodeClick fires even when the +/- is clicked near the Node
            // if I want it to select it even when expanding, I turn this on

            //else if (e.Button == MouseButtons.Left)
            //{
              //  this.treeView.SelectedNode = e.Node;
            //}
            #endregion
        }

        private void treeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            LogMessage.Log("BeforeSelect START: selected {0} e.Node {1} -- Suppressed? {2}", (this.treeView.SelectedNode == null) ? "Unknown" : this.treeView.SelectedNode.Text,
                (e.Node == null) ? "Unknown" : e.Node.Text, SuppressBeforeSelect);
            if (SuppressBeforeSelect)
            {
                LogMessage.Log("BeforeSelect END: first return, Suppressed");
                return;
            }
            else if ((e.Node == null) ||
                (this.treeView.SelectedNode == null)) // || this.dragNode != null
            {
                // e.Node is null, SelectedNode is null, e.Node IS selected node, or we're Dragging
                LogMessage.Log("BeforeSelect END: first return, not changing the form or nodes");
                return;
            }
            if (e.Node == this.treeView.SelectedNode)
            {
                LogMessage.Log("BeforeSelect END: Cancelled (e.Node == this.treeView.SelectedNode) : {0}", e.Node.Text);
                e.Cancel = true;
                return;
            }

            SuppressNodeUpdates = true;
            // LogMessage.Log("BeforeSelect (actual selections)");
            CMacro cm = FindMacroByNode(this.treeView.SelectedNode);
            CMacroFile cmf = FindMacroFileByNode(this.treeView.SelectedNode);

            if ((cm == null) && (cmf != null))
            {
                LogMessage.Log("..BeforeSelect: cm is null and cmf != null");
                cm = GetCurrentMacro(cmf);
            }

            if (cm != null)
            {
                LogMessage.Log("..BeforeSelect: Saving to memory " + cm.DisplayName());
                SaveToMemory(cm);
            }

            cm = FindMacroByNode(e.Node);
            cmf = FindMacroFileByNode(e.Node);

            if (cmf == null)
            {
                FillForm((CMacro)null);
                LogMessage.Log("BeforeSelect END: picked a non Macro[ Bar|File] node, returning");
                return;
            }
            if (cm == null)
                cm = GetCurrentMacro(cmf);

            if (cm == null)
            {
                FillForm(cmf.Macros[0]);
                LogMessage.Log("..BeforeSelect: First selection of a Macro(File| Bar) node");
            }
            else if (cm != null)
            {
                FillForm(cm);
                LogMessage.Log("..BeforeSelect: new node is Macro[ Bar|File] node");
            }
            LogMessage.Log("BeforeSelect: Complete");
            SuppressNodeUpdates = false;
        }
        
        /// <summary>Handles dragging of items.</summary>
        /// <param name="sender" type="object">The source of the event.</param>
        /// <param name="e" type="ItemDragEventArgs">Contains the event data.</param>
        /// <returns type="void">Returns nothing.</returns>
        private void treeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            LogMessage.Log("ItemDrag fired");
            try
            {

                TreeNode tN = (TreeNode)e.Item;

                LogMessage.Log("DoItemDrag: {0}", (tN == null) ? "<NULL>" : tN.Text);
                // if it's one of 2 root nodes cancel
                if ((tN == null) || (tN.Level == 0)) //== this.treeView.Nodes[0].Level) || (tN == this.treeView.Nodes[1]))
                {
                    // they clicked it, so select it
                    //if (e.Button == MouseButtons.Left)
                    //   this.treeView.SelectedNode = tN;
                    // if they click it, NodeMouseClick will handle it
                    LogMessage.Log("DoItemDrag: Base Node attempted to be dragged");
                    return;
                }
                TagInfo tI = tN.Tag as TagInfo;
                if ((tI != null) && (tI.Type == "book") && (tN.Nodes.Count == 0))
                {
                    LogMessage.Log("..DoItemDrag: Premature end, attempt to drag (swap/copy) an empty book");
                    return;
                }

                // Save this info.
                this.originalNode = this.treeView.SelectedNode;

                // Get drag node and select it
                //this.dragNode = tN;

                DragHelper.ImageList_EndDrag();
                DragHelper.ImageList_DragShowNolock(false);

                // Reset image list used for drag image
                this.imageListForDrag.Images.Clear();
                this.imageListForDrag.ImageSize = new Size(tN.Bounds.Size.Width + this.treeView.Indent, tN.Bounds.Height);

                // Create new bitmap
                // This bitmap will contain the tree node image to be dragged
                Bitmap bmp = new Bitmap(tN.Bounds.Width + this.treeView.Indent, tN.Bounds.Height);

                // Get graphics from bitmap
                Graphics gfx = Graphics.FromImage(bmp);

                // Draw node icon into the bitmap
                gfx.DrawImage(this.imageListForTreeView.Images[tN.IsSelected ? tN.SelectedImageKey : tN.ImageKey], 0, 0);

                // Draw node label into bitmap
                gfx.DrawString(tN.Text,
                    this.treeView.Font,
                    new SolidBrush(this.treeView.ForeColor),
                    (float)this.treeView.Indent, 1.0f);

                // Add bitmap to imagelist
                this.imageListForDrag.Images.Add(bmp);

                // Get mouse position in client coordinates
                Point p = this.treeView.PointToClient(Control.MousePosition);

                // Compute delta between mouse position and node bounds
                int dx = p.X + this.treeView.Indent - tN.Bounds.Left;
                int dy = p.Y - tN.Bounds.Top;

                RawSelect(tN);

                // Begin dragging image
                if (!DragHelper.ImageList_BeginDrag(this.imageListForDrag.Handle, 0, dx, dy))
                {
                    DragHelper.ImageList_DragShowNolock(false);
                    DragHelper.ImageList_EndDrag();
                    LogMessage.Log("ItemDrag: Image_ListBeginDrag returned zero! (BAD)");
                }
                else
                {
                    LogMessage.Log("ItemDrag: Image_ListBeginDrag returned true! (GOOD) dx{0} dy{1}", dx, dy);
                }

                if (!timer.Enabled)
                    timer.Start();

                this.treeView.DoDragDrop(tN, DragDropEffects.Link | DragDropEffects.Copy | DragDropEffects.Move);
                // Link = Swap
                // Copy = Copy
                // Move is special (Right-Mouse Drag&Drop)
            }
            catch (Exception ex)
            {
                LogMessage.LogF("ItemDrag event: " + ex.Message);
            }
        }

        private void treeView_GiveFeedback(object sender, System.Windows.Forms.GiveFeedbackEventArgs e)
        {
            LogMessage.Log("GiveFeedback fired");

            try
            {
                e.UseDefaultCursors = false;

                if (e.Effect == DragDropEffects.None)
                    this.Cursor = Cursors.No;
                else if (e.Effect == DragDropEffects.Move)
                    this.Cursor = Cursors.Default;
                else if (e.Effect == DragDropEffects.Link)
                {
                    this.Cursor = this.CursorLink;
                }
                else if (e.Effect == DragDropEffects.Copy)
                {
                    this.Cursor = this.CursorCopy;
                }
            }
            catch (Exception ex)
            {
                LogMessage.LogF("ItemDrag event: " + ex.Message);
            }

        }

        private void treeView_QueryContinueDrag(object sender, System.Windows.Forms.QueryContinueDragEventArgs e)
        {
            //LogMessage.Log("QueryContinueDrag: {0}, e.Action: {1}, CancelOnLeave: {2}", this.dragNode.Text, e.Action, CancelOnLeave);
            // if e.EscapePressed is true, e.Action is auto-set to DragAction.Cancel
            // and if DragAction.Cancel is set, then DragLeave is called on the drop
            // DragLeave is also called if it leaves the control
            // so I have to separate the two.
            LogMessage.Log("QueryContinueDrag fired");

            try
            {
                if (e.Action == DragAction.Cancel)
                {
                    CancelOnLeave = true;
                }
            }
            catch (Exception ex)
            {
                LogMessage.LogF("ItemDrag event: " + ex.Message);
            }
        }

        private void treeView_DragOver(object sender, DragEventArgs e)
        {
            // DragOver is called when a Node is pulled from
            // the treeView, but stays within the same control
            // Sender should always be the same TreeView as the dropnode.treeview
            LogMessage.Log("DragOver fired");

            try
            {
                TreeView tree = sender as TreeView;
                TreeNode dragNode = e.Data.GetData(typeof(TreeNode)) as TreeNode;

                if ((dragNode != null) && (tree != null))
                {
                    //LogMessage.Log("DoDragOver START: {0}", dragNode.Text);

                    // Compute drag position and move image
                    Point pt = new Point(e.X, e.Y);
                    Point formP = this.PointToClient(pt);

                    DragHelper.ImageList_DragMove(formP.X - tree.Left, formP.Y - tree.Top);
                    //LogMessage.Log("DragOver: DragMove x{0} y{1}", formP.X - tree.Left, formP.Y - tree.Top);
                    // Get actual drop node
                    TreeNode dropNode = tree.GetNodeAt(tree.PointToClient(pt));
                    //LogMessage.Log("..DoDragOver: {0} -> {1}", dragNode.Text, (dropNode == null) ? "Unknown" : dropNode.Text);
                    if (dropNode == null)
                        e.Effect = DragDropEffects.None;
                    else
                    {
                        // by default, attempt to swap.
                        CancelOnLeave = false;
                        e.Effect = DragDropEffects.Link; // Swap by default
                        if (is_set(e.KeyState, rightmouse))
                        {
                            e.Effect = DragDropEffects.Move;
                            //LogMessage.Log("..DragOver: KeyState rightmouse is set!");
                            //LogMessage.Log("..DoDragOver: {0} e.Effect: {1}", dragNode.Text, e.Effect);
                        }
                        else if (is_set(e.KeyState, ctrl))
                        {
                            e.Effect = DragDropEffects.Copy;
                            //LogMessage.Log("..DragOver: KeyState ctrl_input key is set!");
                            //LogMessage.Log("..DoDragOver: {0} e.Effect: {1}", dragNode.Text, e.Effect);
                        }
                        // if mouse is on a new node "highlight" it
                        if (tree.SelectedNode != dropNode)
                        {
                            //LogMessage.Log("..DoDragOver: SelectedNode != dropNode");
                            DragHelper.ImageList_DragShowNolock(false);

                            int delta = tree.Height - formP.Y;
                            if ((delta < tree.Height / 2) && (delta > 0))
                            {
                                //LogMessage.Log("..DoDragOver: EnsuringVisible (Next)");
                                if (dropNode.NextVisibleNode != null)
                                    dropNode.NextVisibleNode.EnsureVisible();
                            }
                            if ((delta > tree.Height / 2) && (delta < tree.Height))
                            {
                                //LogMessage.Log("..DoDragOver: EnsuringVisible (Prev)");
                                if (dropNode.PrevVisibleNode != null)
                                    dropNode.PrevVisibleNode.EnsureVisible();
                            }

                            RawSelect(dropNode);
                            //LogMessage.Log("..DoDragOver: SetSelectedNode to dropNode");
                            //LogMessage.Log("..DoDragOver: Removed DragLock");
                            DragHelper.ImageList_DragShowNolock(true);

                            //this.tempDropNode = dropNode;
                            //LogMessage.Log("..DoDragOver: tempDrop set to dropNode");
                        }

                        if (dragNode == dropNode)
                        {
                            e.Effect = DragDropEffects.None;
                            //LogMessage.Log("..DoDragOver: dragNode == dropNode? {0}", (dragNode == dropNode));
                        }
                        // Nowhere in this program can I drop any node
                        // on its immediate parent and be able to figure
                        // out WHAT the user wants me to do there.
                        // So we'll cut it out here.
                        if (dragNode.Parent == dropNode)
                        {
                            e.Effect = DragDropEffects.None;
                        }

                        // if not None yet, it's still assumed valid
                        if ((e.Effect != DragDropEffects.None) && (e.Effect != DragDropEffects.Move))
                        {
                            TagInfo tIdrag = dragNode.Tag as TagInfo,
                                tIdrop = dropNode.Tag as TagInfo;
                            //LogMessage.Log("..DoDragOver: e.Effect is not None yet : {0}", e.Effect);
                            // Add Support for different drop targets based on drag targets here.
                            // Ie, which ones are allowed.  In the DoDragDrop event we've setup
                            // is where we actually add the support itself.
                            // IE: How to handle a Macro drop onto a Macrofile.

                            #region Switch (dragType) Sets Default or None if invalid drop location
                            switch (tIdrag.Type)
                            {
                                case "macro":
                                    if ((tIdrop.Type != "macrofile") &&
                                        (tIdrop.Type != "ctrlmacro") &&
                                        (tIdrop.Type != "altmacro") &&
                                        (tIdrop.Type != "macro"))
                                        e.Effect = DragDropEffects.None;
                                    break;
                                case "macrofile": // Nothing else supported at this time but file->file
                                    if ((tIdrop.Type != "template") &&
                                        (tIdrop.Type != "main") &&
                                        (tIdrop.Type != "folder") && // macrofile -> folder, Copy, Check Overwrite, Confirm, require Save first
                                        (tIdrop.Type != "macrofile") && // this replaces the EXACT macro file it's dropped to.
                                        (tIdrop.Type != "book") && // this should replace the filenumber modified file under the new book
                                        (tIdrop.Type != "char")) // This should replace the exact filename under different char
                                        e.Effect = DragDropEffects.None;
                                    else if ((tIdrop.Type == "char") && (dragNode.Parent != null) && (dragNode.Parent.Parent == dropNode)) // same char....
                                        e.Effect = DragDropEffects.None;
                                    else if (GetDropFileFromInfo(dragNode, dropNode) == null)
                                        e.Effect = DragDropEffects.Copy;
                                    break;
                                case "ctrlmacro":
                                case "altmacro":
                                    if ((tIdrop.Type != "macrofile") &&
                                        (tIdrop.Type != "ctrlmacro") &&
                                        (tIdrop.Type != "altmacro"))
                                        e.Effect = DragDropEffects.None;
                                    break;
                                case "book":
                                    if ((tIdrop.Type != "char") && // book -> char, if not already owned by char, Copy by confirmed overwrite to new char's folder, same Set/Filenames
                                        (tIdrop.Type != "folder") &&  // book -> folder Copy all 10 appropriate Macro Sets, warning of overwrite, require Save first if any Changed
                                        (tIdrop.Type != "book") && // book -> book, Swap, Copy supported, require Saving first. Rename Files, Reload(?).
                                        (tIdrop.Type != "template") && // book -> temp, Copy all 10 Macro Files to the template folder, save first, check for overwrite
                                        (tIdrop.Type != "main"))
                                        e.Effect = DragDropEffects.None;
                                    else if (tIdrop.Type != "book")
                                    {
                                        e.Effect = DragDropEffects.Copy;
                                    }
                                    else if (tIdrop.Type == "book")
                                    {
                                        if ((dropNode.FirstNode == null) || (dropNode.Nodes.Count == 0))
                                            e.Effect = DragDropEffects.Copy; // default action
                                    }
                                    break;
                                case "char":
                                    if ((tIdrop.Type != "char") && // char to char copy/swap, I want them to require saving first
                                        (tIdrop.Type != "folder") &&
                                        (tIdrop.Type != "template") &&
                                        (tIdrop.Type != "main")) // copy an entire character's macro files to anywhere on folders
                                        e.Effect = DragDropEffects.None;
                                    else e.Effect = DragDropEffects.Copy;
                                    break;
                                case "folder":
                                    if ((tIdrop.Type != "main") &&
                                        (tIdrop.Type != "char") &&
                                        (tIdrop.Type != "template") &&
                                        (tIdrop.Type != "folder"))
                                        e.Effect = DragDropEffects.None;
                                    else e.Effect = DragDropEffects.Copy; // default action
                                    break;
                                case "main":     // if you even MANAGE to pull this off...
                                case "template": // or this, you'll be treated to not being able to drop it.
                                default:
                                    e.Effect = DragDropEffects.None;
                                    break;
                            }
                            #endregion
                            //LogMessage.Log("..DoDragOver: e.Effect is : {0}", e.Effect);
                        }
                        else if (e.Effect == DragDropEffects.Move)
                        {
                            TagInfo tIdrag = dragNode.Tag as TagInfo,
                                tIdrop = dropNode.Tag as TagInfo;

                            #region Switch (dragType for Right-Drag Only) Sets None if invalid drop location
                            switch (tIdrag.Type)
                            {
                                case "macro":
                                    if ((tIdrop.Type != "macrofile") &&
                                        (tIdrop.Type != "ctrlmacro") &&
                                        (tIdrop.Type != "altmacro") &&
                                        (tIdrop.Type != "macro"))
                                        e.Effect = DragDropEffects.None;
                                    break;
                                case "macrofile": // Nothing else supported at this time but file->file
                                    if ((tIdrop.Type != "template") &&
                                            (tIdrop.Type != "main") &&
                                            (tIdrop.Type != "folder") && // macrofile -> folder, Copy, Check Overwrite, Confirm, require Save first
                                            (tIdrop.Type != "macrofile") && // this replaces the EXACT macro file it's dropped to.
                                            (tIdrop.Type != "book") && // this should replace the filenumber modified file under the new book
                                            (tIdrop.Type != "char")) // This should replace the exact filename under different char
                                        e.Effect = DragDropEffects.None;
                                    else if ((tIdrop.Type == "char") && (dragNode.Parent != null) && (dragNode.Parent.Parent == dropNode)) // same char....
                                        e.Effect = DragDropEffects.None;
                                    break;
                                case "ctrlmacro":
                                case "altmacro":
                                    if ((tIdrop.Type != "macrofile") &&
                                        (tIdrop.Type != "ctrlmacro") &&
                                        (tIdrop.Type != "altmacro"))
                                        e.Effect = DragDropEffects.None;
                                    break;
                                case "book":
                                    if ((tIdrop.Type != "char") && // book -> char, if not already owned by char, Copy by confirmed overwrite to new char's folder, same Set/Filenames
                                        (tIdrop.Type != "folder") &&  // book -> folder Copy all 10 appropriate Macro Sets, warning of overwrite, require Save first if any Changed
                                        (tIdrop.Type != "book") && // book -> book, Swap, Copy supported, require Saving first. Rename Files, Reload(?).
                                        (tIdrop.Type != "template") && // book -> temp, Copy all 10 Macro Files to the template folder, save first, check for overwrite
                                        (tIdrop.Type != "main"))
                                        e.Effect = DragDropEffects.None;
                                    break;
                                case "char":
                                    if ((tIdrop.Type != "char") && // char to char copy/swap, I want them to require saving first
                                        (tIdrop.Type != "folder") &&
                                        (tIdrop.Type != "template") &&
                                        (tIdrop.Type != "main")) // copy an entire character's macro files to anywhere on folders
                                        e.Effect = DragDropEffects.None;
                                    break;
                                case "folder":
                                    if ((tIdrop.Type != "main") &&
                                        (tIdrop.Type != "char") &&
                                        (tIdrop.Type != "template") &&
                                        (tIdrop.Type != "folder"))
                                        e.Effect = DragDropEffects.None;
                                    break;
                                case "main":     // if you even MANAGE to pull this off...
                                case "template": // or this, you'll be treated to not being able to drop it.
                                default:
                                    e.Effect = DragDropEffects.None;
                                    break;
                            }
                            #endregion
                        }

                        if (e.Effect != DragDropEffects.None)
                        {
                            //LogMessage.Log("..DoDragOver: e.Effect != None still: {0}", e.Effect);
                            // Avoid that drop node is child of drag node 
                            TreeNode tmpNode = dropNode;
                            while (tmpNode.Parent != null)
                            {
                                if (tmpNode.Parent == dragNode)
                                {
                                    e.Effect = DragDropEffects.None;
                                    CancelOnLeave = true;
                                    //LogMessage.Log("..DoDragOver: tempNode.Parent == dragNode e.Effect: {0}", e.Effect);
                                    break;
                                }
                                tmpNode = tmpNode.Parent;
                            }
                        }
                    }
                    if (e.Effect == DragDropEffects.None)
                    {
                        // do same effect as if they had pressed escape to cancel the drag
                        // by clearing the info.
                        // because DragLeave is called instead of DragDrop if effect is "None"
                        CancelOnLeave = true;
                        //LogMessage.Log("..DoDragOver: Setting CancelOnLeave truee, e.Effect: {0}", e.Effect);
                    }
                    //LogMessage.Log("DoDragOver END: {0} e.Effect: {1}", dragNode.Text, e.Effect);
                }
                //else LogMessage.Log("DragOver: dragNode is null! (BAD)");
            }
            catch (Exception ex)
            {
                LogMessage.LogF("ItemDrag event: " + ex.Message);
            }
        }

        private void treeView_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            LogMessage.Log("DragEnter fired");

            try
            {
                TreeNode dragNode = null;
                TreeView tree = null;
                if ((sender != null) && (sender is TreeView))
                    tree = sender as TreeView;

                bool Ignore = false;

                if (!timer.Enabled)
                    timer.Start();

                if (!e.Data.GetDataPresent(typeof(TreeNode)))
                {
                    LogMessage.Log(" DragEnter: GetDataPresent(TreeNode) = false, Drag the right stuff");
                    //e.Effect = DragDropEffects.None;
                    e.Effect = DragDropEffects.Copy;
                    Ignore = true;
                }
                else
                {
                    LogMessage.Log("DragEnter: GetDataPresent(TreeNode) = true (good)");
                    dragNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
                    TreeNode drag2 = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode", false);
                    if (dragNode == null)
                        LogMessage.Log("DragEnter: dragNode is null");
                    else LogMessage.Log("DragEnter: dragNode is NOT null, (good) Name: {0}", dragNode.Text);
                    if (drag2 == null)
                        LogMessage.Log("DragEnter: drag2 is null");
                    else LogMessage.Log("DragEnter: drag2 is NOT null, (good) Name: '{0}'", drag2.Text);
                }

                if (!Ignore)
                    DragHelper.ImageList_DragEnter(tree.Handle, e.X - tree.Left, e.Y - tree.Top);
            }
            catch (Exception ex)
            {
                LogMessage.LogF("ItemDrag event: " + ex.Message);
            }

        }

        private void treeView_DragLeave(object sender, System.EventArgs e)
        {
            LogMessage.Log("DragLeave fired");

            try
            {
                TreeView tree = sender as TreeView;

                //LogMessage.Log("DoDragLeave: {0} : CancelOnLeave {1}", this.dragNode.Text, CancelOnLeave);
                this.Cursor = Cursors.Default;

                if (tree != null)
                {
                    if (CancelOnLeave == true)
                    {
                        this.interval = 0;
                        timer.Stop();
                        CancelOnLeave = false;
                        //DragHelper.ImageList_DragShowNolock(false);
                        DragHelper.ImageList_EndDrag();
                        // go back to previous node if cancelled.
                        RawSelect(this.originalNode);
                        this.treeView.Invalidate();
                    }
                    else DragHelper.ImageList_DragLeave(tree.Handle);
                }
                else LogMessage.Log("DragLeave: sender isn't a TreeView");
            }
            catch (Exception ex)
            {
                LogMessage.LogF("ItemDrag event: " + ex.Message);
            }
        }

        private void treeView_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            LogMessage.Log("DragDrop fired");

            try
            {
                TreeView tree = sender as TreeView;

                // Get drop node
                Point xP = tree.PointToClient(new Point(e.X, e.Y));
                TreeNode dropNode = tree.GetNodeAt(xP);
                TreeNode dragNode = (TreeNode)e.Data.GetData(typeof(TreeNode));

                String[] s = e.Data.GetFormats();
                String msg = String.Empty;
                foreach (String formats in s)
                {
                    msg += String.Format("{0}\r\n", formats);
                }
                //MessageBox.Show("DragDrop Formats Dropped :\r\n" + msg);
                LogMessage.Log("DragDrop Formats Dropped : \r\n" + msg);
                LogMessage.Log("DoDragDrop START: {0}", dragNode.Text);
                LogMessage.Log("..DoDragDrop: Attempting to Drop.");

                // Perform Cleanup first
                //  -- Unlock updates
                DragHelper.ImageList_DragLeave(this.treeView.Handle);
                DragHelper.ImageList_EndDrag();

                //  -- Stop Timer
                this.interval = 0;
                this.timer.Stop();
                this.Cursor = Cursors.Default;
                //  -- Additional Variable
                CancelOnLeave = false;

                if ((dragNode != null) && (dropNode != null))
                {
                    if (dragNode == dropNode)
                    {
                        LogMessage.Log("DoDragDrop: Premature End, dragNode == dropNode?! DragLeave wasn't called, somewhere needs to be Effects.None!");
                        //  -- Reset TreeView
                        RawSelect(this.originalNode);
                        this.originalNode = null;
                        return;
                    }

                    // Duh, I need to MANUALLY SaveToMemory BEFORE Swapping or Copying
                    // This only works if I don't modify selection
                    SaveToMemory(this.originalNode);

                    tree.BeginUpdate(); // this.treeView.SuspendLayout();

                    LogMessage.Log("..DoDragDrop: dragNode != dropNode");
                    CMacro cm_drop = FindMacroByNode(dropNode),
                            cm_drag = FindMacroByNode(dragNode);
                    CMacroFile cmf_drag = FindMacroFileByNode(dragNode),
                                cmf_drop = FindMacroFileByNode(dropNode);
                    TagInfo tIdrag = dragNode.Tag as TagInfo, tIdrop = dropNode.Tag as TagInfo;

                    if (tIdrag == null)
                    {
                        LogMessage.Log("..DragDrop: tIdrag == null: No TagInfo on dragNode!");
                    }

                    if (tIdrop == null)
                    {
                        LogMessage.Log("..DragDrop: tIdrop == null: No TagInfo on dropNode!");
                    }

                    // don't want waitcursor on Right-Click Menu
                    if (e.Effect != DragDropEffects.Move)
                        this.SetWaitCursor();

                    if (e.Effect == DragDropEffects.Move) // Pop right-click menu
                    {
                        BuildAndShowDragAndDropContextMenu(dragNode, dropNode, e);
                    }
                    else if (tIdrag.Type == "macro")
                    {
                        #region DONE: if "macro"
                        if (tIdrop.Type == "macro")
                        {
                            Modify(ref cm_drag, ref cm_drop, e);
                        }
                        #endregion
                        #region DONE: else if "macrofile", "ctrlmacro", "altmacro"
                        else if ((tIdrop.Type == "macrofile") || (tIdrop.Type == "ctrlmacro") ||
                            (tIdrop.Type == "altmacro"))
                        {
                            if ((cmf_drop == cmf_drag) && (tIdrop.Type != "ctrlmacro") &&
                                (tIdrop.Type != "altmacro"))
                            {
                                // if dropping to the same macro file
                                // as where we're dragging from, nothing should change
                                // because it would overwrite itself
                                MessageBox.Show("Dropping a Macro to the same MacroFile it came from has no effect.", "No Effect");
                            }
                            else
                            {
                                #region DONE: Swap/Copy Drag & Drop Macro to Same MacroNumber in new MacroFile and update
                                // if it's dropping to macrofile we already determined
                                // that it's going to a different macrofile
                                if (tIdrop.Type == "macrofile")
                                    cm_drop = cmf_drop.Macros[cm_drag.MacroNumber];
                                else // it's ctrlmacro or altmacro, so swap between the locations
                                {
                                    int number = cm_drag.MacroNumber;
                                    if ((number > 9) && (number < 20)) // 10 - 19
                                        cm_drop = cmf_drop.Macros[number - 10];
                                    else if ((number >= 0) && (number <= 9))
                                        cm_drop = cmf_drop.Macros[number + 10];
                                    else cm_drop = cmf_drop.Macros[cm_drop.MacroNumber];
                                }
                                Modify(ref cm_drag, ref cm_drop, e);
                                #endregion
                            }
                        }
                        #endregion
                    }
                    else if (tIdrag.Type == "macrofile")
                    {
                        #region DONE: if "macrofile" period, handles all cases
                        if (cmf_drag != null)
                        {
                            if (cmf_drop != null) // it's a macrofile, bar, or macro
                            {
                                if (e.Effect == DragDropEffects.Link)
                                {
                                    Swap(ref cmf_drop, ref cmf_drag);
                                }
                                else if (e.Effect == DragDropEffects.Copy)
                                    cmf_drop.CopyFrom(cmf_drag);
                            }
                            else
                            {
                                // it's NOT a macrofile we're dropping to
                                // see if we can find it.
                                cmf_drop = GetDropFileFromInfo(dragNode, dropNode);
                                if ((e.Effect == DragDropEffects.Link) && (cmf_drop != null))
                                {
                                    // it exists and we're swapping, do it
                                    Swap(ref cmf_drop, ref cmf_drag);
                                }
                                else if ((e.Effect == DragDropEffects.Copy) && (cmf_drop != null))
                                {
                                    // if we're copying and it DOES exist
                                    cmf_drop.CopyFrom(cmf_drag);
                                }
                                else if ((e.Effect == DragDropEffects.Copy) && (cmf_drop == null))
                                {
                                    // we're copying and it DOESN'T exist, create it.
                                    cmf_drop = GetDropFileFromInfo(dragNode, dropNode, true);
                                    cmf_drop.CopyFrom(cmf_drag);
                                }
                            }
                        }
                        #endregion
                    }
                    else if ((tIdrag.Type == "altmacro") || (tIdrag.Type == "ctrlmacro"))
                    {
                        #region DONE: if "altmacro", "ctrlmacro", or "macrofile"
                        if ((tIdrop.Type == "altmacro") || (tIdrop.Type == "ctrlmacro") ||
                            (tIdrop.Text == "macrofile"))
                        {
                            int SwapOrCopy = -1;
                            if (e.Effect == DragDropEffects.Link)
                                SwapOrCopy = 1;
                            else if (e.Effect == DragDropEffects.Copy)
                                SwapOrCopy = 2;
                            HandleBarsToBarsOrFile(dragNode, dropNode, SwapOrCopy);
                        }
                        #endregion
                    }
                    else if (tIdrag.Type == "book")
                    {
                        #region if "book"
                        if (tIdrop.Type == "book") // replace or copy over existing book
                        {
                            int SwapOrCopy = -1;
                            if (e.Effect == DragDropEffects.Link)
                                SwapOrCopy = 1;
                            else if (e.Effect == DragDropEffects.Copy)
                                SwapOrCopy = 2;

                            HandleBookToBook(dragNode, dropNode, SwapOrCopy);
                        }
                        #endregion
                        #region if "char", "folder", "template", "main"
                        else if ((tIdrop.Type == "char") || (tIdrop.Type == "main") ||
                                (tIdrop.Type == "folder") || (tIdrop.Type == "template"))
                        {
                            #region If Swapping, cancel (Only can swap books)
                            if (e.Effect == DragDropEffects.Link)
                            {
                                LogMessage.Log("..DragDrop: Attempt to Swap book -> a folder type, unsupported");
                            }
                            #endregion
                            #region If valid Copy to folder, char, template
                            else if (e.Effect == DragDropEffects.Copy)
                            {
                                HandleBookToOthers(dragNode, dropNode);
                            }
                            #endregion
                            #region DEBUG?: Everything else.
                            else
                            {
                                LogMessage.Log("..DragDrop: Unsupported event, e.effect is not Swap or Copy");
                            }
                            #endregion
                        }
                        #endregion
                    }
                    else if ((tIdrag.Type == "char") || (tIdrag.Type == "folder"))
                    {
                        #region DONE -- need Notification Screen? if "char" or "folder"
                        LogMessage.Log("DragDrop: char|folder");
                        if ((tIdrop.Type == "char") || (tIdrop.Type == "folder") ||
                            (tIdrop.Type == "template") || (tIdrop.Type == "main"))
                        {
                            HandleCharOrFolder(dragNode, dropNode);
                        }
                        else LogMessage.Log("Invalid Drop Type {0} for dropping \"char\" node", tIdrop.Type);
                        #endregion
                    }
                    else
                    {
                        #region DONE: Unhandled situations
                        if (e.Effect == DragDropEffects.Link) // Swap it.
                        {
                            MessageBox.Show("You can't swap that with that!", "Drag & Drop: Unsupported Combination", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                            LogMessage.Log("..Drag & Drop Error, Unsupported Source/Target (Link)");
                        }
                        else if (e.Effect == DragDropEffects.Copy)
                        {
                            MessageBox.Show("You can't copy that there!", "Drag & Drop: Unsupported Combination", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                            LogMessage.Log("..Drag & Drop Error, Unsupported Source/Target Combination (Copy)");
                        }
                        else // Everything Else
                        {
                            MessageBox.Show("OMGWTFBBQ! WHAT DID YOU DO?!?!\r\nUmm, You should NOT have received this message!", "Easy BUG: Contact Developer with exact cause.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                            LogMessage.Log("..ERROR: Copy or Swap not the option given. DragDropEffects: " + e.Effect);
                        }
                        #endregion
                    }
                }

                // On Success, reset these back as well.
                ChangeButtonNames(this.originalNode);
                FillForm(this.originalNode); // Fill the Form with original Node just in case
                //  -- Reset TreeView
                RawSelect(this.originalNode);
                this.originalNode = null;
                this.RestoreCursor();
                tree.EndUpdate();
                LogMessage.Log("DragDrop END: Drop Attempt Done.");
            }
            catch (Exception ex)
            {
                LogMessage.LogF("ItemDrag event: " + ex.Message);
            }
        }

        private void BuildAndShowDragAndDropContextMenu(TreeNode dragNode, TreeNode dropNode, DragEventArgs e)
        {
            #region Right-Click Menu for Drag & Drop
            Point xP = this.treeView.PointToClient(new Point(e.X, e.Y));

            CMacroFile cmf_drop = FindMacroFileByNode(dropNode);
            CMacroFile cmf_drag = FindMacroFileByNode(dragNode);
            TagInfo tIdrag = dragNode.Tag as TagInfo, tIdrop = dropNode.Tag as TagInfo;

            ContextMenuStrip cms = new ContextMenuStrip();
            ToolStripItem[] tmsi = new ToolStripItem[1];

            cms.Padding = new Padding(cms.Padding.Left, cms.Padding.Top - 4, cms.Padding.Right, cms.Padding.Bottom - 4);
            #region Build Right-Click Menu
            Array.Resize(ref tmsi, 15);
            tmsi[0] = new ToolStripLabel("Drag And Drop Menu");
            Font f = new Font(tmsi[0].Font, FontStyle.Bold);
            tmsi[0].Font = f;

            tmsi[1] = new ToolStripSeparator();
            if (Preferences.Include_Header == false)
            {
                #region Invisible the header if not wanted
                tmsi[0].Visible = false;
                tmsi[1].Visible = false;
                #endregion
            }

            if (tIdrag.Type == "macro")
            {
                #region if "macro"
                CMacro cm_drop = null,
                        cm_drag = FindMacroByNode(dragNode);
                // if not dropping to a macro, assign the appropriate drop macro
                if (tIdrop.Type != "macro")
                {
                    cm_drop = cmf_drop.Macros[cm_drag.MacroNumber];
                }
                else
                {
                    cm_drop = FindMacroByNode(dropNode);
                }

                tmsi[2] = new ToolStripMenuItem(String.Format("Swap Macros <{0}> and <{1}>", cm_drag.thisNode.Text, cm_drop.thisNode.Text), Resources.SwapMacro, DynamicMenu_Click);
                TagInfo tI = new TagInfo("macro", "swap", cm_drag, cm_drop);
                tmsi[2].Tag = tI as Object;
                tmsi[2].Name = "RightClickModify";

                tmsi[3] = new ToolStripMenuItem(String.Format("Copy Macro <{0}> to <{1}>", cm_drag.thisNode.Text, cm_drop.thisNode.Text), Resources.CopyHS, DynamicMenu_Click);
                tI = new TagInfo("macro", "copy", cm_drag, cm_drop);
                tmsi[3].Tag = tI as Object;
                tmsi[3].Name = "RightClickModify";

                tmsi[4] = new ToolStripSeparator();

                // for "macrofile" "ctrlmacro" "altmacro" where drag & drop macrofiles are same
                if ((tIdrop.Type != "macro") && (cmf_drop == cmf_drag))
                {
                    // Invisible these as they're not valid options.
                    tmsi[2].Visible = false;
                    tmsi[3].Visible = false;
                    tmsi[4].Visible = false;
                }
                #endregion
            }
            else if ((tIdrag.Type == "altmacro") || (tIdrag.Type == "ctrlmacro"))
            {
                #region if "altmacro" or "ctrlmacro"
                tmsi[2] = new ToolStripMenuItem(String.Format("Swap {0} Bar and {1}",
                    (tIdrag.Type == "altmacro") ? "Alt" : "Ctrl",
                    (tIdrop.Type == "altmacro") ? "Alt Bar" :
                    (tIdrop.Type == "ctrlmacro") ? "Ctrl Bar" :
                    (tIdrag.Type == "altmacro") ? "Alt Bar" :
                    (tIdrag.Type == "ctrlmacro") ? "Ctrl Bar" : "This Location"),
                    Resources.SwapMacro, DynamicMenu_Click);
                TagInfo tI = new TagInfo("bars", "swap", dragNode, dropNode);
                tmsi[2].Tag = tI as Object;
                tmsi[2].Name = "RightClickModify";

                tmsi[3] = new ToolStripMenuItem(String.Format("Copy {0} Bar to {1}",
                    (tIdrag.Type == "altmacro") ? "Alt" : "Ctrl",
                    (tIdrop.Type == "altmacro") ? "Alt Bar" :
                    (tIdrop.Type == "ctrlmacro") ? "Ctrl Bar" :
                    (tIdrag.Type == "altmacro") ? "Alt Bar" :
                    (tIdrag.Type == "ctrlmacro") ? "Ctrl Bar" : "This Location"),
                    Resources.CopyHS, DynamicMenu_Click);
                tI = new TagInfo("bars", "copy", dragNode, dropNode);
                tmsi[3].Tag = tI as Object;
                tmsi[3].Name = "RightClickModify";

                tmsi[4] = new ToolStripSeparator();
                #endregion
            }
            else if (tIdrag.Type == "macrofile")
            {
                #region if "macrofile"
                if (tIdrop.Type != "macrofile")
                {
                    cmf_drop = GetDropFileFromInfo(dragNode, dropNode);
                }

                if (cmf_drop != null)
                {
                    tmsi[2] = new ToolStripMenuItem(String.Format("Swap Macrofiles '{0}' and '{1}'", cmf_drag.thisNode.Text.Trim('*'), cmf_drop.thisNode.Text.Trim('*')), Resources.SwapMacro, DynamicMenu_Click);
                    TagInfo swaptI = new TagInfo("macrofile", "swap", dragNode, dropNode);
                    tmsi[2].Tag = swaptI as Object;
                    tmsi[2].Name = "RightClickModify";
                }
                else
                {
                    tmsi[2] = new ToolStripLabel();
                    tmsi[2].Visible = false;
                }

                tmsi[3] = new ToolStripMenuItem(String.Format("Copy Macrofile '{0}' to '{1}'", cmf_drag.thisNode.Text.Trim('*'), dropNode.Text.Trim('*')), Resources.CopyHS, DynamicMenu_Click);
                TagInfo copytI = new TagInfo("macrofile", "copy", dragNode, dropNode);
                tmsi[3].Tag = copytI as Object;
                tmsi[3].Name = "RightClickModify";

                tmsi[4] = new ToolStripSeparator();

                if (cmf_drop == cmf_drag)
                {
                    tmsi[2].Enabled = false;
                    tmsi[3].Enabled = false;
                }
                #endregion
            }
            else if (tIdrag.Type == "book")
            {
                #region if "book"
                tmsi[2] = new ToolStripMenuItem(String.Format("Swap Books <{0}> and <{1}>", dragNode.Text, dropNode.Text), Resources.SwapMacro, DynamicMenu_Click);
                TagInfo tI = new TagInfo("book", "swap", dragNode, dropNode);
                tmsi[2].Tag = tI as Object;
                tmsi[2].Name = "RightClickModify";

                tmsi[3] = new ToolStripMenuItem(String.Format("Copy Contents of Book <{0}> to <{1}>", dragNode.Text, dropNode.Text), Resources.CopyHS, DynamicMenu_Click);
                tI = new TagInfo("book", "copy", dragNode, dropNode);
                tmsi[3].Tag = tI as Object;
                tmsi[3].Name = "RightClickModify";

                tmsi[4] = new ToolStripSeparator();

                if (tIdrop.Text != "book") // swap and copy for book only, copy to others
                    tmsi[2].Visible = false;
                #endregion
            }
            else if ((tIdrag.Type == "char") || (tIdrag.Type == "folder"))
            {
                #region if "char" or "folder"
                if ((tIdrop.Type == "char") || (tIdrop.Type == "folder") ||
                    (tIdrop.Type == "main") || (tIdrop.Type == "template"))
                {
                    tmsi[2] = new ToolStripMenuItem(String.Format("Copy '{0}' to '{1}'", dragNode.Text, dropNode.Text), Resources.CopyHS, DynamicMenu_Click);
                    TagInfo tI = new TagInfo("allfoldertypes", "copy", dragNode, dropNode);
                    tmsi[2].Tag = tI as Object;
                    tmsi[2].Name = "RightClickModify";

                    tmsi[3] = new ToolStripLabel();
                    tmsi[3].Visible = false;

                    tmsi[4] = new ToolStripSeparator();
                }
                #endregion
            }
            else
            {
                #region else create blanks and make them Invisible
                tmsi[2] = new ToolStripMenuItem();
                tmsi[3] = new ToolStripMenuItem();
                tmsi[4] = new ToolStripMenuItem();
                tmsi[2].Visible = false;
                tmsi[3].Visible = false;
                tmsi[4].Visible = false;
                #endregion
            }

            #region if MacroFile add in ability to save and reload specific, if not Invisible it
            tmsi[5] = new ToolStripMenuItem("Save MacroFile", Resources.saveHS, SaveThistoolStripMenuItem_Click);
            tmsi[5].Tag = dropNode as Object;
            tmsi[6] = new ToolStripMenuItem("Reload MacroFile", Resources.ReloadMacro, ReloadThisToCurrentToolStripMenuItem_Click);
            tmsi[6].Tag = dropNode as Object;
            tmsi[7] = new ToolStripSeparator();

            if (cmf_drop == null) // not part a MacroFile or child thereof
            {
                //LogMessage.Log("..cmf_drop == null?!");
                tmsi[5].Visible = false;
                tmsi[6].Visible = false;
                tmsi[7].Visible = false;
            }
            else
            {
                tmsi[5].Text = String.Format("Save '{0}\\{1}'",
                    cmf_drop.thisNode.Parent.Text, cmf_drop.thisNode.Text.TrimEnd('*'));
                tmsi[6].Text = String.Format("Reload '{0}\\{1}'",
                    cmf_drop.thisNode.Parent.Text, cmf_drop.thisNode.Text.TrimEnd('*'));
            }
            #endregion

            if (tIdrop.Type == "template")
            {
                #region if dropNode is template node
                tmsi[8] = new ToolStripMenuItem("Open " + dropNode.Text + " Folder", Resources.openHS, DynamicMenu_Click);
                tmsi[8].Name = "Open_Template_Folder";

                tmsi[9] = new ToolStripSeparator();

                tmsi[10] = new ToolStripMenuItem("New File", Resources.NewMacro, DynamicMenu_Click);
                tmsi[10].Name = "New_File";
                tmsi[10].Tag = dropNode as Object;

                tmsi[11] = new ToolStripMenuItem("New Folder", Resources.NewFolderHS, DynamicMenu_Click);
                tmsi[11].Name = "New_Folder";
                tmsi[11].Tag = dropNode as Object;

                tmsi[12] = new ToolStripSeparator();
                #endregion
            }
            else if (((tIdrop.Type == "main") || (tIdrop.Type == "char")) && (dropNode.Level == 0))
            {
                #region if dropNode is main node (whether it's char or main type)
                tmsi[8] = new ToolStripMenuItem("Open " + dropNode.Text + " Folder", Resources.openHS, DynamicMenu_Click);
                tmsi[8].Name = "Open_Main_Folder";

                tmsi[9] = new ToolStripSeparator();

                tmsi[10] = new ToolStripMenuItem("New File", Resources.NewMacro, DynamicMenu_Click);
                tmsi[10].Name = "New_File";
                tmsi[10].Tag = dropNode as Object;

                tmsi[11] = new ToolStripMenuItem("New Folder", Resources.NewFolderHS, DynamicMenu_Click);
                tmsi[11].Name = "New_Folder";
                tmsi[11].Tag = dropNode as Object;

                tmsi[12] = new ToolStripSeparator();
                #endregion
            }
            else if ((tIdrop.Type == "char") || (tIdrop.Type == "folder"))
            {
                #region if dropNode is "char" or "folder" (and it's not main level)
                tmsi[8] = new ToolStripMenuItem("Rename Character...", Resources.Rename, DynamicMenu_Click);
                tmsi[8].Name = "Rename_Character";
                tmsi[8].Tag = dropNode as Object;

                tmsi[9] = new ToolStripSeparator();

                if (tIdrop.Type != "char")
                {
                    tmsi[8].Visible = false;
                    tmsi[9].Visible = false;
                }

                tmsi[10] = new ToolStripMenuItem("New File", Resources.NewMacro, DynamicMenu_Click);
                tmsi[10].Name = "New_File";
                tmsi[10].Tag = dropNode as Object;

                tmsi[11] = new ToolStripMenuItem("New Folder", Resources.NewFolderHS, DynamicMenu_Click);
                tmsi[11].Name = "New_Folder";
                tmsi[11].Tag = dropNode as Object;

                tmsi[12] = new ToolStripSeparator();
                #endregion
            }
            else
            {
                #region else, create blanks
                tmsi[8] = new ToolStripLabel();
                tmsi[9] = new ToolStripLabel();
                tmsi[10] = new ToolStripLabel();
                tmsi[11] = new ToolStripLabel();
                tmsi[12] = new ToolStripLabel();
                tmsi[8].Visible = false;
                tmsi[9].Visible = false;
                tmsi[10].Visible = false;
                tmsi[11].Visible = false;
                tmsi[12].Visible = false;
                #endregion
            }

            tmsi[13] = new ToolStripMenuItem("Save All Macro Sets", Resources.SaveAllHS, saveAllToolStripMenuItem_Click);
            tmsi[14] = new ToolStripMenuItem("Reload All Macro Sets", Resources.ReloadAll, ReloadAllToolStripMenuItem_Click);

            #endregion
            #region Show Right-Click Menu
            if (tmsi[0] != null)
            {
                if (Preferences.Include_Header == false)
                {
                    // in previous if/else, i devisualized separator index [3]
                    // so as to simplify the header dropping based on preferences.
                    tmsi[0].Visible = false;
                    tmsi[1].Visible = false;
                }
                cms.SuspendLayout();
                cms.Items.AddRange(tmsi);
                cms.ResumeLayout();
                cms.Show(dragNode.TreeView, xP);
            }
            #endregion
            LogMessage.Log("DragDrop END: Drop Attempt Done, Right-Click Menu created, returning.");
            #endregion
        }

        private void HandleBarsToBarsOrFile(TreeNode dragNode, TreeNode dropNode, int SwapOrCopy)
        {
            #region Swap/Copy Drag & Drop Ctrl/Alt Macro Bars and update
            CMacroFile cmf_drop = FindMacroFileByNode(dropNode), 
                cmf_drag = FindMacroFileByNode(dragNode);
            TagInfo tIdrag = dragNode.Tag as TagInfo, tIdrop = dropNode.Tag as TagInfo;
            int destcnt = 0, srccnt = 0, cnt;
            CMacro tmp = new CMacro(), tmp2 = new CMacro();

            if (is_altnode(dragNode, cmf_drag) || (tIdrag.Text == "altmacro"))
                srccnt = 10;

            if (tIdrop.Text == "macrofile") // this covers it if dropping to macrofile
                destcnt = srccnt;
            else if (is_altnode(dropNode, cmf_drop) || (tIdrop.Text == "altmacro"))
                destcnt = 10;

            if (SwapOrCopy == 1)
            {
                for (cnt = 0; cnt < 10; cnt++)
                {
                    Swap(ref cmf_drop.Macros[cnt + destcnt], ref cmf_drag.Macros[cnt + srccnt]);
                }
                //if (cmf_drag != null)
                //{
                //cmf_drag.Changed = true;
                //}
            }
            else if (SwapOrCopy == 2)
            {
                for (cnt = 0; cnt < 10; cnt++)
                {
                    cmf_drop.Macros[cnt + destcnt].CopyFrom(cmf_drag.Macros[cnt + srccnt]);
                }
            }
            if (cmf_drop != null)
            {
                cmf_drop.Changed = true;
            }
            #endregion
        }

        private CMacroFile GetDropFileFromInfo(TreeNode dragNode, TreeNode dropNode)
        {
            return GetDropFileFromInfo(dragNode, dropNode, false);
        }

        private CMacroFile GetDropFileFromInfo(TreeNode dragNode, TreeNode dropNode, bool Create)
        {
            // Determine the file to overwrite based on the node dropped to.
            CMacroFile cmf_drop = FindMacroFileByNode(dropNode),
                        cmf_drag = FindMacroFileByNode(dragNode);

            // if dropNode was macrofile, ctrl/alt bar or macro, send the right one
            if (cmf_drop != null) 
                return cmf_drop;

            // if not dragging a MacroFile, don't bother.
            if (cmf_drag == null)
                return null;

            TagInfo tDrop = dropNode.Tag as TagInfo;
            String fName = String.Empty;
            String dirName = String.Empty;
            String newPathToFile = String.Empty;
            try
            {
                if (tDrop.Type == "book")
                {
                    TagInfo tDropParent = dropNode.Parent.Tag as TagInfo;
                    dirName = tDropParent.Text;
                    if (cmf_drag.FileNumber != -1)
                    {
                        int replacefNumber = ((Int32.Parse(dropNode.Name) - 1) * 10) + (cmf_drag.FileNumber % 10);
                        fName = String.Format("mcr{0}.dat", (replacefNumber == 0) ? "" : replacefNumber.ToString());
                    }
                    else fName = Path.GetFileName(cmf_drag.fName);
                }
                else
                {
                    dirName = tDrop.Text;
                    fName = Path.GetFileName(cmf_drag.fName);
                }
                newPathToFile = Path.GetFullPath(dirName.Trim('\\') + "\\" + fName);
                cmf_drop = FindMacroFileExactByFileName(newPathToFile);
                if (Create && (cmf_drop == null))
                    return NewFileInMemory(newPathToFile);

            }
            catch (PathTooLongException err)
            {
                LogMessage.Log("GetDropFileFromInfo(): Path too long error -- {0}", err.Message);
                cmf_drop = null;
            }
            catch (Exception err)
            {
                LogMessage.Log("GetDropFileFromInfo(): Path too long error -- {0}", err.Message);
                cmf_drop = null;
            }
            return cmf_drop;
        }

        private void HandleBookToOthers(TreeNode dragNode, TreeNode dropNode)
        {
            Thread notifyThread = new Thread(new ParameterizedThreadStart(ShowNotifyForm));
            TagInfo ti = new TagInfo(String.Empty, "Processing book...");

            TreeNode dragchildren = dragNode.FirstNode;
            TagInfo tIdrag = dragNode.Tag as TagInfo;
            TagInfo tIdrop = dropNode.Tag as TagInfo;

            if ((tIdrop.Type == "folder") ||
                (tIdrop.Type == "template") ||
                (tIdrop.Type == "main") ||
                (tIdrop.Type == "char"))
            {
                TagInfo tIp = dragNode.Parent.Tag as TagInfo;
                String newttlfile = tIdrop.Text.Trim('\\') + "\\mcr.ttl";
                String oldttlfile = tIp.Text.Trim('\\') + "\\mcr.ttl";

                #region Catch any error with TTL file manipulation
                try
                {
                    newttlfile = Path.GetFullPath(newttlfile);
                    oldttlfile = Path.GetFullPath(oldttlfile);
                }
                catch (PathTooLongException e)
                {
                    LogMessage.LogF("..HandleBookToOthers(): TTL file path is too long {0}, skipping", e.Message);
                    newttlfile = String.Empty;
                    oldttlfile = String.Empty;
                }
                catch (Exception e)
                {
                    LogMessage.LogF("..HandleBookToOthers(): Unknown Exception {0}, ignoring", e.Message);
                }
                #endregion

                if (newttlfile != oldttlfile)
                {
                    #region If future ttlfile is within system limits on Path Length and is not the SAME FILE
                    CBook cbold = null, cbnew = null;
                    for (int i = 0; i < BookList.Count; i++)
                    {
                        #region Locate any existing book for either file
                        if (BookList[i].fName == oldttlfile)
                        {
                            BookList[i].Restore();
                            cbold = BookList[i];
                        }
                        else if (BookList[i].fName == newttlfile)
                        {
                            BookList[i].Restore();
                            cbnew = BookList[i];
                        }
                        #endregion
                    }

                    if ((cbold == null) && (File.Exists(oldttlfile)))
                    {
                        #region If didn't find the drag mcr.ttl file and it does exist, Load it
                        cbold = new CBook(oldttlfile);
                        BookList.Add(cbold);
                        #endregion
                    }

                    if ((cbnew == null) && (File.Exists(newttlfile)))
                    {
                        #region If didn't find the drop mcr.ttl file and it does exist, Load it
                        cbnew = new CBook(newttlfile);
                        BookList.Add(cbnew);
                        #endregion
                    }

                    if ((cbold != null) && (cbnew != null))
                    {
                        #region If old ttl file and new ttlfile info was found in memory
                        DialogResult dr = MessageBox.Show("Book name file found in both directories, Yes to overwite the destination Book File.",
                            "Overwrite?",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (dr == DialogResult.Yes)
                        {
                            cbnew.CopyFrom(cbold);
                        }
                        else if (dr == DialogResult.Cancel)
                            return;
                        #endregion
                    }
                    else if ((cbold != null) && (cbnew == null))
                    {
                        #region If old ttl file info was found, but newttlfile was not
                        DialogResult dr = MessageBox.Show("Book name file found in source directory, Yes to copy to the destination directory.",
                            "Copy?",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (dr == DialogResult.Yes)
                        {
                            cbnew = new CBook(cbold, newttlfile);
                            BookList.Add(cbnew);
                        }
                        else if (dr == DialogResult.Cancel)
                            return;
                        #endregion
                    }
                    #endregion
                }

                #region Loop through the dragnode's children (macro files) doing copy as necessary
                CMacroFile[] draglist = new CMacroFile[0];

                for (dragchildren = dragNode.FirstNode; dragchildren != null;
                    dragchildren = dragchildren.NextNode)
                {
                    CMacroFile cmf_drag = FindMacroFileExactByNode(dragchildren);

                    if (cmf_drag != null)
                    {
                        Array.Resize(ref draglist, draglist.Length + 1);
                        draglist[draglist.Length - 1] = cmf_drag;
                    }
                }

                if (draglist.Length <= 0)
                {
                    #region If no files to transfer, return
                    LogMessage.Log("..HandleBookToOthers: No valid files to drag, returning");
                    return;
                    #endregion
                }

                #region Show Notify Form
                if (draglist.Length > FILES_TO_HIDE)
                {
                    ti.Object1 = (Object)draglist.Length;
                    notifyThread.Start((object)ti);
                }
                #endregion

                exitLoop = false;
                String[] errorFiles = new String[0];
                String errorString = String.Empty;
                String oldName = String.Empty;
                String newName = String.Empty;
                for (int x = 0; (x < draglist.Length) && (exitLoop == false); x++)
                {
                    #region Loop through draglist attempting to create new files if necessary
                    oldName = Path.GetFileName(draglist[x].fName);
                    if ((oldName != null) && (oldName != String.Empty))
                    {
                        newName = String.Format("{0}\\{1}", tIdrop.Text.Trim('\\'), oldName);
                        try
                        {
                            #region Catch an error if FullPath to newName is too long, otherwise Copy file
                            // if too long this should kick it out automatically
                            newName = Path.GetFullPath(newName);
                            if (draglist.Length > FILES_TO_HIDE)
                            {
                                UpdateNotifyUI(Utilitiies.EllipsifyPath(newName), x);
                                Thread.Sleep(25);
                            }
                            CMacroFile cmf_drop = FindMacroFileExactByFileName(newName);
                            if (cmf_drop != null)
                            {
                                cmf_drop.CopyFrom(draglist[x]);
                            }
                            else
                            {
                                NewFileInMemory(draglist[x], newName);
                            }
                            #endregion
                        }
                        catch (PathTooLongException e)
                        {
                            LogMessage.Log("HandleBookToOthers(): Filename {0} Error -- {1}", newName, e.Message);
                            Array.Resize(ref errorFiles, errorFiles.Length + 1);
                            errorFiles[errorFiles.Length - 1] = Utilitiies.EllipsifyPath(newName);
                            errorString += errorFiles[errorFiles.Length - 1] + "\r\n";
                        }
                        catch (Exception e)
                        {
                            LogMessage.Log("HandleBookToOthers(): Filename {0} Error -- {1}", newName, e.Message);
                        }
                    }
                    #endregion
                }

                #region Close Notify Form
                if (draglist.Length > FILES_TO_HIDE)
                {
                    UpdateNotifyProgress(notifyForm.NotifyBarMax);
                    Thread.Sleep(200);
                    CloseNotifyForm();
                }
                #endregion

                #region Notify of any errors due to long path names
                if ((errorFiles.Length > 0) && (errorString != String.Empty))
                {
                    MessageBox.Show("The following " + errorFiles.Length + " files can not be copied (Path to file is TOO long):\r\n" + errorString, "Error: Path(s) to " + errorFiles.Length + " files are too long!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                #endregion
                #endregion
            }
        }

        /// <summary>
        /// Designed for specifically handling book to book copy and swap.
        /// </summary>
        /// <param name="dragNode">TreeNode that initiated the original Drag and Drop operation.</param>
        /// <param name="dropNode">TreeNode the drag and drop operation was dropped to.</param>
        /// <param name="SwapOrCopy">1 for Swap, 2 for Copy</param>
        private void HandleBookToBook(TreeNode dragNode, TreeNode dropNode, int SwapOrCopy)
        {
            if ((SwapOrCopy != 1) && (SwapOrCopy != 2))
                return;

            #region Setup Variables for BookToBook Swap/Copy
            // Setup Thread for the NotifyForm
            Thread notifyThread = new Thread(new ParameterizedThreadStart(ShowNotifyForm));
            // Information for the notifyForm
            TagInfo ti = new TagInfo(String.Empty, "Processing book...");

            // the actual char|folder of the book location
            TreeNode dP = dropNode.Parent;
            // ... to get the Tag Info
            TagInfo tIdrop = dP.Tag as TagInfo;
            // ... to get the foldername
            String folderName = tIdrop.Text.Trim('\\');

            // Go to the macro sets within the book specifically
            TreeNode dragchildren = dragNode.FirstNode; // go to the MacroSets within the book
            TreeNode dropchildren = dropNode.FirstNode;

            // list variables for storing the drag file and dropfile
            // this should be a 1-1 list.
            CMacroFile[] draglist = new CMacroFile[0];
            CMacroFile[] droplist = new CMacroFile[0];
            #endregion

            for (dragchildren = dragNode.FirstNode; dragchildren != null; dragchildren = dragchildren.NextNode)
            {
                #region Loop through the dragged nodes to setup the list
                CMacroFile cmf_drag = FindMacroFileExactByNode(dragchildren);
                // If not a file to drag, skip it
                if (cmf_drag == null)
                    continue;

                CMacroFile cmf_drop = null;

                if (SwapOrCopy == 1)
                {
                    #region Go through each drop node looking for a file that matches position
                    // go through each drop node looking
                    // for a matching file to swap with
                    // if it exists, add it to the droplist to match
                    // with the draglist
                    for (dropchildren = dropNode.FirstNode; dropchildren != null; dropchildren = dropchildren.NextNode)
                    {
                        cmf_drop = FindMacroFileExactByNode(dropchildren);
                        if (cmf_drop != null)
                        {
                            if ((cmf_drop.FileNumber % 10) == (cmf_drag.FileNumber % 10))
                            {
                                Array.Resize(ref draglist, draglist.Length + 1);
                                draglist[draglist.Length - 1] = cmf_drag;

                                Array.Resize(ref droplist, droplist.Length + 1);
                                droplist[droplist.Length - 1] = cmf_drop;
                                break;
                            }
                        }
                    }
                    #endregion
                }
                else if (SwapOrCopy == 2) // Copy
                {
                    #region Or just parse the location it should go if Copying to make it quicker
                    // booknumber * 10 + matching file number (0-9)
                    int fNumber = ((Int32.Parse(dropNode.Name) - 1) * 10) + (cmf_drag.FileNumber % 10);
                    // format the macrofile name based on this number
                    String name = String.Format("mcr{0}.dat", (fNumber == 0) ? "" : fNumber.ToString());
                    // create the potential filename for the drop file
                    String fName = String.Format("{0}\\{1}", tIdrop.Text.Trim('\\'), name);
                    try
                    {
                        // if it's too long, this should throw an error
                        // and go to the CATCH statement
                        fName = Path.GetFullPath(fName);
                        Array.Resize(ref draglist, draglist.Length + 1);
                        Array.Resize(ref droplist, droplist.Length + 1);

                        draglist[draglist.Length - 1] = cmf_drag;

                        cmf_drop = FindMacroFileExactByFileName(fName);

                        // doesn't matter if the file exists, it could
                        // just be in memory.
                        if (cmf_drop != null)
                        {
                            droplist[droplist.Length - 1] = cmf_drop;
                        }
                        else
                        {
                            droplist[droplist.Length - 1] = NewFileInMemory(fName);
                        }
                    }
                    catch (PathTooLongException e)
                    {
                        LogMessage.LogF("...HandleBookToBook(): Path {0} Error -- {1}", fName, e.Message);
                    }
                    catch (Exception e)
                    {
                        LogMessage.LogF("...HandleBookToBook(): Unexpected error {1} on Path {0}", fName, e.Message);
                    }
                    #endregion
                }
                #endregion
            }

            if ((draglist.Length == 0) ||
                (droplist.Length == 0) ||
                (draglist.Length != droplist.Length))
            {
                #region If the list is NOT 1-1 or there's no list at all, quit with error message.
                if (SwapOrCopy == 1)
                    MessageBox.Show("You can only swap between books for the same number Macro Set!\r\nFor Example: Macro Set #1 and Macro Set #1", "Drag & Drop Error!");
                else if (SwapOrCopy == 2)
                    MessageBox.Show("I would be unable to copy ANY of those files, possibly the path would be too long is my guess", "Drag & Drop Error");
                return;
                #endregion
            }

            DialogResult dr = MessageBox.Show(
                String.Format("{0} Book name As Well?", (SwapOrCopy == 1) ? "Swap" : "Copy"),
                String.Format("{0} book names?", (SwapOrCopy == 1) ? "Swap" : "Copy"),
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (dr == DialogResult.Yes)
            {
                #region Request if we want to copy/swap the booknames as well
                TagInfo tIdrag = dragNode.Parent.Tag as TagInfo;
                String oldttlfile = String.Empty;
                String newttlfile = String.Empty;
                try
                {
                    oldttlfile = Path.GetFullPath(tIdrag.Text.Trim('\\') + "\\mcr.ttl");
                    newttlfile = Path.GetFullPath(tIdrop.Text.Trim('\\') + "\\mcr.ttl");
                    CBook cbold = null, cbnew = null;
                    for (int i = 0; (i < BookList.Count) && (cbold == null) && (cbnew == null); i++)
                    {
                        if (BookList[i].fName == oldttlfile)
                        {
                            BookList[i].Restore();
                            cbold = BookList[i];
                        }
                        if (BookList[i].fName == newttlfile)
                        {
                            BookList[i].Restore();
                            cbnew = BookList[i];
                        }
                    }
                    if ((cbold != null) && (cbnew != null))
                    {
                        int oldindex = Int32.Parse(dragNode.Name) - 1;
                        int newindex = Int32.Parse(dropNode.Name) - 1;
                        if (SwapOrCopy == 1)
                        {
                            String tmp = cbold.GetBookName(oldindex);
                            cbold.SetBookName(oldindex, cbnew.GetBookName(newindex));
                            cbnew.SetBookName(newindex, tmp);
                            tmp = dragNode.Text;
                            dragNode.Text = dropNode.Text;
                            dropNode.Text = tmp;
                        }
                        else if (SwapOrCopy == 2)
                        {
                            cbnew.SetBookName(newindex, cbold.GetBookName(oldindex));
                            dropNode.Text = dragNode.Text;
                        }
                    }
                }
                catch (PathTooLongException e)
                {
                    MessageBox.Show("Unable to comply due to the path length involved is too long", "Path too long error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LogMessage.LogF("..HandleBookToBook(): TTL File: Path too long error -- {0}", e.Message);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Unable to complete the request", "Unexpected error");
                    LogMessage.LogF("..HandleBookToBook(): TTL File: Unexpected error {0}", e.Message);
                }
                #endregion
            }
            else if (dr == DialogResult.Cancel)
                return;

            exitLoop = false;
            #region Show Notify Form
            if (draglist.Length > FILES_TO_HIDE)
            {
                ti.Object1 = draglist.Length as Object;
                notifyThread.Start((object)ti);
                Thread.Sleep(200);
            }
            #endregion

            for (int i = 0; (i < draglist.Length) && (exitLoop == false); i++)
            {
                #region For each item in the draglist, Swap/Copy it with/to the corresponding droplist
                if (draglist.Length > FILES_TO_HIDE)
                {
                    UpdateNotifyUI(Utilitiies.EllipsifyPath(draglist[i].fName), i);
                    Thread.Sleep(25);
                }

                if (SwapOrCopy == 1)
                {
                    Swap(ref draglist[i], ref droplist[i]);
                }
                else if (SwapOrCopy == 2)
                {
                    droplist[i].CopyFrom(draglist[i]);
                }
                #endregion
            }

            #region Close Notify Form
            if ((exitLoop == false) && (draglist.Length > FILES_TO_HIDE))
            {
                UpdateNotifyProgress(notifyForm.NotifyBarMax);
                Thread.Sleep(200);
                CloseNotifyForm();
            }
            #endregion
        }

        private void HandleCharOrFolder(TreeNode dragNode, TreeNode dropNode)
        {
            #region Handle copying files from char to folder
            Thread notifyThread = new Thread(new ParameterizedThreadStart(ShowNotifyForm));
            TagInfo ti = new TagInfo(String.Empty, "Processing book...");

            TagInfo tIdrop = dropNode.Tag as TagInfo;
            TagInfo tIdrag = dragNode.Tag as TagInfo;

            CMacroFile[] draglist = new CMacroFile[0];

            LogMessage.Log("DragDrop: Creating list of valid files");
            for (int i = 0; i < MacroFiles.Count; i++)
            {
                #region Locate all files matching the dragNode's folder path
                if ((MacroFiles[i] != null) &&
                    (MacroFiles[i].fName != null) && 
                    (MacroFiles[i].fName.Contains(tIdrag.Text)))
                {
                    Array.Resize(ref draglist, draglist.Length + 1);
                    draglist[draglist.Length - 1] = MacroFiles[i];
                }
                #endregion
            }

            if (draglist.Length > 0)
            {
                #region If there are valid files for dragging
                CMacroFile tmp_drop = null;
                exitLoop = false;

                List<TagInfo> ttlFileList = new List<TagInfo>();
                for (int i = 0; i < draglist.Length; i++)
                {
                    // remove the common directory, to merge and create a path

                    String PathToRemove = String.Empty;
                    String FileWithFolder = draglist[i].fName;
                    String newpath = String.Empty;
                    try
                    {
                        // draglist[i].fName
                        PathToRemove = Path.GetDirectoryName(tIdrag.Text.Trim('\\'));
                        FileWithFolder = FileWithFolder.Remove(0, PathToRemove.Length);
                        // Attempt to throw an error here.
                        newpath = Path.GetFullPath(tIdrop.Text.Trim('\\') + "\\" + FileWithFolder.Trim('\\'));

                        #region Check for valid TTL files here
                        String newttlfile = String.Empty;
                        String oldttlfile = String.Empty;

                        try
                        {
                            newttlfile = Path.GetDirectoryName(newpath) + "\\mcr.ttl"; // (tIdrop.Text.Trim('\\') + "\\mcr.ttl");
                            oldttlfile = Path.GetDirectoryName(draglist[i].fName) + "\\mcr.ttl";
                        }
                        catch (PathTooLongException e)
                        {
                            // File.Exists(String.Empty) always returns false
                            // This will catch an PathTooLongException errors and ignore transfer
                            LogMessage.LogF("..HandleCharOrFolder(): TTL file path is too long {0}, skipping", e.Message);
                        }
                        catch (Exception e)
                        {
                            LogMessage.LogF("..HandleCharOrFolder(): Unknown exception {0}, ignoring", e.Message);
                        }

                        if (newttlfile != oldttlfile)
                        {
                            #region If future ttlfile is within system limits on Path Length and is not the SAME FILE
                            int oldcnt = 0;
                            for (oldcnt = 0; oldcnt < ttlFileList.Count; oldcnt++)
                            {
                                TagInfo tmpTTL = ttlFileList[oldcnt];
                                CBook cbTTL = tmpTTL.Object1 as CBook;
                                if (cbTTL.fName == oldttlfile)
                                    break;
                            }
                            if (oldcnt >= ttlFileList.Count) // not been handled yet
                            {
                                #region If not handled yet
                                CBook cbold = null, cbnew = null;
                                for (int bcnt = 0; bcnt < BookList.Count; bcnt++)
                                {
                                    #region Locate any existing book for either file
                                    if (BookList[bcnt].fName == oldttlfile)
                                    {
                                        BookList[bcnt].Restore();
                                        cbold = BookList[bcnt];
                                    }
                                    else if (BookList[bcnt].fName == newttlfile)
                                    {
                                        BookList[bcnt].Restore();
                                        cbnew = BookList[bcnt];
                                    }
                                    #endregion
                                }

                                if ((cbold == null) && (File.Exists(oldttlfile)))
                                {
                                    #region If didn't find the drag mcr.ttl file and it does exist, Load it
                                    cbold = new CBook(oldttlfile);
                                    BookList.Add(cbold);
                                    #endregion
                                }

                                if ((cbnew == null) && (File.Exists(newttlfile)))
                                {
                                    #region If didn't find the drop mcr.ttl file and it does exist, Load it
                                    cbnew = new CBook(newttlfile);
                                    BookList.Add(cbnew);
                                    #endregion
                                }

                                if ((cbold != null) && (cbnew != null))
                                {
                                    #region If old ttl file and new ttlfile info was found in memory
                                    ttlFileList.Add(new TagInfo("Overwrite_TTL", cbold, cbnew));
                                    #endregion
                                }
                                else if ((cbold != null) && (cbnew == null))
                                {
                                    #region If old ttl file was found but new ttl file was not
                                    cbnew = new CBook(cbold, newttlfile);
                                    ttlFileList.Add(new TagInfo("Copy_TTL", Path.GetDirectoryName(newttlfile), cbold, cbnew));
                                    #endregion
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                    }
                    catch (PathTooLongException error)
                    {
                        LogMessage.Log("While processing book files, encountered Path Too Long error -- {0}", error.Message);
                    }
                    catch (Exception error)
                    {
                        LogMessage.Log("While processing book files, encountered Unexpected error -- {0}", error.Message);
                    }
                } // end for loop

                #region Test for list
                if (ttlFileList.Count != 0)
                {
                    if (ttlFileList.Count == 1)
                    {
                        #region If only one book file is being processed, show generic MessageBox instead
                        String path = ttlFileList[0].Text;
                        CBook cbold = ttlFileList[0].Object1 as CBook;
                        CBook cbnew = ttlFileList[0].Object2 as CBook;
                        if (ttlFileList[0].Type == "Overwrite_TTL")
                        {
                            DialogResult dr = MessageBox.Show(
                                    String.Format("Book name file found in both directories,\r\nSource: {0}\r\nDestination: {1}\r\n\r\nOverwite the destination Book File?", Utilitiies.EllipsifyPath(cbold.fName, 60), Utilitiies.EllipsifyPath(cbnew.fName, 60)),
                                    "Overwrite?",
                                    MessageBoxButtons.YesNoCancel,
                                    MessageBoxIcon.Question,
                                    MessageBoxDefaultButton.Button2);
                            if (dr == DialogResult.Yes)
                            {
                                cbnew.CopyFrom(cbold);
                            }
                            else if (dr == DialogResult.Cancel)
                            {
                                LogMessage.Log("..Cancelled drop");
                                return;
                            }
                        }
                        else if (ttlFileList[0].Type == "Copy_TTL")
                        {
                            DialogResult dr = MessageBox.Show(
                                    String.Format("Book name file found in source directory:\r\n'{0}',\r\n\r\nCopy to the destination directory:\r\n'{1}'?", Utilitiies.EllipsifyPath(cbold.fName, 60), Utilitiies.EllipsifyPath(path, 60)),
                                    "Copy?",
                                    MessageBoxButtons.YesNoCancel,
                                    MessageBoxIcon.Question,
                                    MessageBoxDefaultButton.Button2);
                            if (dr == DialogResult.Yes)
                            {
                                BookList.Add(cbnew);
                            }
                            else if (dr == DialogResult.Cancel)
                            {
                                LogMessage.Log("..Cancelled drop");
                                return;
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        #region Else go with the CheckedListBox
                        ExitAndSaveBox esb = new ExitAndSaveBox("Confirmation box...", "Select what actions to take:", "Confirm", "No To All", "Cancel Operation");
                        esb.checkedListBox1.Items.Clear();
                        foreach (TagInfo x in ttlFileList)
                            esb.checkedListBox1.Items.Add(x, true);
                        bool ExitTTLLoop = false;
                        do
                        {
                            DialogResult dr = esb.ShowDialog(this);
                            if (dr == DialogResult.Cancel)
                            {
                                LogMessage.Log("..HandleSwapOrCopy(): Copy cancelled");
                                return;
                            }
                            else if (dr == DialogResult.No)
                                ExitTTLLoop = true;
                            else if (dr == DialogResult.Yes)
                            {
                                if (esb.checkedListBox1.CheckedItems.Count == 0)
                                    MessageBox.Show("If you're going to select to save, you must select something!", "Select 'No To All' to ignore this.");
                                else
                                {
                                    foreach (TagInfo x in esb.checkedListBox1.CheckedItems)
                                    {
                                        CBook cbold = x.Object1 as CBook;
                                        CBook cbnew = x.Object2 as CBook;
                                        String type = x.Type;
                                        if (type == "Copy_TTL")
                                        {
                                            BookList.Add(cbnew);
                                        }
                                        else if (type == "Overwrite_TTL")
                                        {
                                            cbnew.CopyFrom(cbold);
                                        }
                                    }
                                    ExitTTLLoop = true;
                                }
                            }

                        } while (ExitTTLLoop == false);
                        #endregion
                    }
                }
                #endregion
                #region Show Notify Form
                if (draglist.Length > FILES_TO_HIDE)
                {
                    ti.Object1 = draglist.Length as Object;
                    notifyThread.Start((object)ti);
                }
                #endregion

                String[] errorFiles = new String[0];
                String errorString = String.Empty;
                for (int i = 0; (i < draglist.Length) && (exitLoop == false); i++)
                {
                    #region Loop through draglist Copying files
                    #region Update NotifyForm UI
                    if (draglist.Length > FILES_TO_HIDE)
                    {
                        UpdateNotifyUI(Utilitiies.EllipsifyPath(draglist[i].fName), i);
                        Thread.Sleep(25);
                    }
                    #endregion
                    String PathToRemove = String.Empty;
                    String FileWithFolder = draglist[i].fName;
                    String newpath = String.Empty;

                    try
                    {
                        #region Copy files or Create new files here
                        // draglist[i].fName
                        PathToRemove = Path.GetDirectoryName(tIdrag.Text.Trim('\\'));
                        FileWithFolder = FileWithFolder.Remove(0, PathToRemove.Length);
                        // Attempt to throw an error here.
                        newpath = Path.GetFullPath(tIdrop.Text.Trim('\\') + "\\" + FileWithFolder.Trim('\\'));

                        tmp_drop = FindMacroFileExactByFileName(newpath);
                        if (tmp_drop != null)
                        {
                            tmp_drop.CopyFrom(draglist[i]);
                        }
                        else
                        {
                            NewFileInMemory(draglist[i], newpath);
                        }
                        #endregion
                    }
                    catch (PathTooLongException e)
                    {
                        #region Setup for error processing for Too long filenames
                        LogMessage.Log("..HandleCharOrFolder(): Path is too long, could not copy: {0}, skipping -- Error: {1}", newpath, e.Message);
                        Array.Resize(ref errorFiles, errorFiles.Length + 1);
                        errorFiles[errorFiles.Length - 1] = Utilitiies.EllipsifyPath(newpath);
                        errorString += errorFiles[errorFiles.Length - 1] + "\r\n";
                        #endregion
                    }
                    catch (Exception e)
                    {
                        #region Generic Error
                        LogMessage.LogF("..HandleCharOrFolder(): Unexpected error -- {0}, ignoring.", e.Message);
                        Array.Resize(ref errorFiles, errorFiles.Length + 1);
                        errorFiles[errorFiles.Length - 1] = Utilitiies.EllipsifyPath(newpath);
                        errorString += errorFiles[errorFiles.Length - 1] + "\r\n";
                        #endregion
                    }
                    #endregion
                }

                #region Close Notify Form
                if (draglist.Length > FILES_TO_HIDE)
                {
                    UpdateNotifyProgress(notifyForm.NotifyBarMax);
                    Thread.Sleep(200);
                    CloseNotifyForm();
                }
                #endregion

                #region Notify of any errors encountered
                if ((errorFiles.Length > 0) && (errorString != String.Empty))
                {
                    MessageBox.Show("The following " + errorFiles.Length + " files can not be saved (Path to file is TOO long):\r\n" + errorString, "Error: Path(s) too long on " + errorFiles.Length + " files!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                #endregion
                #endregion
            }
            else
            {
                LogMessage.Log("...Unable to determine list of files in char directory {0}", tIdrag.Text);
                MessageBox.Show("Unable to complete the request, I'm sorry.", "Drag & Drop Error!");
            }
            #endregion
        }
        #endregion

        #region MainForm Methods (TreeView ContextMenuStrip Methods for Nodes)
        private void DynamicMenu_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem mi = sender as ToolStripMenuItem;
            String namesearchPattern = @"[^a-zA-Z0-9'\.\-_ ]";
            if (mi == null)
                return;
            LogMessage.Log("DynamicMenu_Click");
            if (mi.Name == "RightClickModify")
            {
                #region RightClickModify -- Right-Click Menu when Drag & Drop is active
                TagInfo tI = mi.Tag as TagInfo;

                this.SetWaitCursor();
                this.treeView.BeginUpdate(); //.SuspendLayout();
                if (tI.Type == "allfoldertypes")
                {
                    #region DONE: if "allfoldertypes" (ie char, folder, can't drag main or templates)
                    if (tI.Text == "copy")
                    {
                        TreeNode dragNode = tI.Object1 as TreeNode;
                        TreeNode dropNode = tI.Object2 as TreeNode;
                        HandleCharOrFolder(dragNode, dropNode);
                        ChangeButtonNames();
                        FillForm(); // Fill the Form with original Node just in case
                    }
                    #endregion
                }
                else if (tI.Type == "bars")
                {
                    #region DONE: if "altmacro" or "ctrlmacro"
                    int SwapOrCopy = -1;
                    TreeNode dragNode = tI.Object1 as TreeNode;
                    TreeNode dropNode = tI.Object2 as TreeNode;
                    if (tI.Text == "swap")
                        SwapOrCopy = 1;
                    else if (tI.Text == "copy")
                        SwapOrCopy = 2;
                    HandleBarsToBarsOrFile(dragNode, dropNode, SwapOrCopy);
                    ChangeButtonNames();
                    FillForm(); // Fill the Form with original Node just in case
                    #endregion
                }
                else if (tI.Type == "book")
                {
                    #region DONE: if "book"
                    if (tI.Text == "swap")
                    {
                        TreeNode dragNode = tI.Object1 as TreeNode;
                        TreeNode dropNode = tI.Object2 as TreeNode;
                        TagInfo tdrop = dropNode.Tag as TagInfo;
                        if ((tdrop.Type == "book") && (dragNode != null) && (dropNode != null))
                        {
                            HandleBookToBook(dragNode, dropNode, 1);
                            ChangeButtonNames();
                            FillForm(); // Fill the Form with original Node just in case
                        }
                    }
                    else if (tI.Text == "copy")
                    {
                        TreeNode dragNode = tI.Object1 as TreeNode;
                        TreeNode dropNode = tI.Object2 as TreeNode;
                        TagInfo tdrop = dropNode.Tag as TagInfo;
                        if ((tdrop.Type == "book") && (dragNode != null) && (dropNode != null))
                        {
                            HandleBookToBook(dragNode, dropNode, 2);
                        }
                        else // char, folder, template, main
                        {
                            HandleBookToOthers(dragNode, dropNode);
                        }
                        ChangeButtonNames();
                        FillForm(); // Fill the Form with original Node just in case
                    }
                    #endregion
                }
                else if (tI.Type == "macro")
                {
                    #region DONE: if "macro"
                    CMacro cm_drag = tI.Object1 as CMacro;
                    CMacro cm_drop = tI.Object2 as CMacro;

                    if (tI.Text == "copy")
                    {
                        #region if "copy"
                        if (cm_drop.CopyFrom(cm_drag))
                        {
                            cm_drop.thisNode.Text = cm_drop.DisplayName();
                            LogMessage.Log("..Successfully copied {0} and {1}.", cm_drag.thisNode.Text, cm_drop.thisNode.Text);

                            CMacroFile cmf_drop = FindMacroFileByNode(cm_drop.thisNode),
                                        cmf_drag = FindMacroFileByNode(cm_drag.thisNode);
                            if (cmf_drop != null)
                            {
                                cmf_drop.Changed = true;
                            }
                            // SelectedNode has been set at this point via RawSelect(this.originalNode)
                            // in the Right-Click menu setup function (DoDragDrop)
                            ChangeButtonNames();
                            FillForm(); // Fill the Form with original Node just in case
                            LogMessage.Log("..Copied Macro src:" + cm_drag.Name + " dest:" + cm_drop.Name);
                        }
                        else LogMessage.Log("..Failed to copy {0} and {1}.", cm_drop.thisNode.Text, cm_drag.thisNode.Text);
                        #endregion
                    }
                    else if (tI.Text == "swap")
                    {
                        #region if "swap"
                        if (Swap(ref cm_drop, ref cm_drag))
                        {
                            LogMessage.Log("..Successfully swapped {0} and {1}.", cm_drag.thisNode.Text, cm_drop.thisNode.Text);
                            cm_drop.thisNode.Text = cm_drop.DisplayName();
                            cm_drag.thisNode.Text = cm_drag.DisplayName();
                            // SelectedNode has been set at this point via RawSelect(this.originalNode)
                            // in the Right-Click menu setup function (DoDragDrop)
                            ChangeButtonNames();
                            FillForm(); // Fill the Form with original Node just in case
                            LogMessage.Log("..Swapped Macro src:" + cm_drag.Name + " dest:" + cm_drop.Name);
                        }
                        else LogMessage.Log("..Failed to swap {0} and {1}.", cm_drop.thisNode.Text, cm_drag.thisNode.Text);
                        #endregion
                    }
                    #endregion
                }
                else if (tI.Type == "macrofile")
                {
                    #region DONE: if "macrofile"
                    TreeNode dragNode = tI.Object1 as TreeNode;
                    TreeNode dropNode = tI.Object2 as TreeNode;

                    CMacroFile cmf_drag = FindMacroFileByNode(dragNode);
                    // Get Drop File does a FindMacroFileByNode anyway
                    CMacroFile cmf_drop = GetDropFileFromInfo(dragNode, dropNode);

                    TagInfo tIdrop = dropNode.Tag as TagInfo;
                    if (tI.Text == "swap")
                    {
                        if ((cmf_drag != null) && (cmf_drop != null))
                            Swap(ref cmf_drop, ref cmf_drag);
                    }
                    else if (tI.Text == "copy")
                    {
                        if (cmf_drop == null)
                        {
                            cmf_drop = GetDropFileFromInfo(dragNode, dropNode, true);
                            if (cmf_drop != null) // this should never fail
                                cmf_drop.CopyFrom(cmf_drag);
                        }
                        else cmf_drop.CopyFrom(cmf_drag);
                    }
                    ChangeButtonNames();
                    FillForm(); // Fill the Form with original Node just in case
                    #endregion
                }
                this.treeView.EndUpdate(); //.ResumeLayout();
                this.RestoreCursor();
                #endregion
            }
            else if (mi.Name == "Rename_Book")
            {
                #region Rename_Book
                TreeNode tN = mi.Tag as TreeNode;
                if (tN != null)
                {
                    this.treeView.LabelEdit = true;
                    tN.BeginEdit();
                }
                else LogMessage.Log("..DynamicMenu_Click: Rename_Book: tN is null!");
                #endregion
            }
            else if (Preferences.ShowDebugInfo && (mi.Name == "Show_Node_Info"))
            {
                #region SHOW_DEBUG_INFO
                TreeNode tN = mi.Tag as TreeNode;
                TagInfo tI = tN.Tag as TagInfo;
                MessageBox.Show(String.Format("\r\nName: {9}\r\nText: {0}\r\n" +
                    "Full Path: {1}\r\n" +
                    "ImageIndex: {2}   " +
                    "SelectedImageIndex: {7}\r\n" +
                    "Pos In TreeNode: {4}     " +
                    "Level in TreeView: {5}\r\n" +
                    "Parent: {6}\r\n" +
                    "TagInfo: Text-'{3}' Type-'{8}'\r\n" +
                    "FullName: '{10}'"
                    ,
                    tN.Text, tN.FullPath, tN.ImageIndex, (tI == null) ? "No Text" : tI.Text,
                    tN.Index, tN.Level,
                    (tN.Parent != null) ? tN.Parent.Name : "No Parent",
                    tN.SelectedImageIndex, (tI == null) ? "No Type" : tI.Type, tN.Name, "?"), "Show Node Info For " + tN.Text
                    );
                #endregion
            }
            else if (mi.Name == "Rename_Character")
            {
                #region Rename_Character
                LogMessage.Log("..Renaming a Character Folder");
                TreeNode tN = mi.Tag as TreeNode;
                if (tN != null)
                {
                    // if it's already been renamed
                    // get the index to pull the original name
                    int start = tN.Text.IndexOf('<');
                    int end = tN.Text.IndexOf('>');
                    
                    // if it exists and there's at least one character
                    // as the "alt name" for the char folder
                    // pull it and set the Text BEFORE setting up BeginEdit
                    if (start < end)
                    {
                        tN.Text = tN.Text.Substring(start + 1, end - (start + 1));
                    }
                    else if ((start == -1) && (end == -1) && (tN.Text != tN.Name))
                    {
                        // if it couldn't be found and for some reason
                        // it's NOT set to the original folder name
                        // force that to be so.
                        tN.Text = tN.Name;
                    }
                    // Turn on editing capability
                    this.treeView.LabelEdit = true;
                    tN.BeginEdit(); // AfterLabelEdit handles it from here.
                }
                #endregion
            }
            else if (mi.Name == "Open_Folder")
            {
                #region Open_Folder (9 lines)
                TreeNode tN = mi.Tag as TreeNode;

                if (tN != null)
                {
                TagInfo tI = tN.Tag as TagInfo;
                    LogMessage.Log(".." + mi.Text);
                    System.Diagnostics.Process.Start("explorer.exe", String.Format("{0}{1}{2}",
                        (Preferences.UseExplorerViewOnFolderOpen) ? "/e," : "",
                        (Preferences.UseFolderAsRoot) ? "/root," : "",
                        tI.Text));
                }
                #endregion
            }
            else if (mi.Name == "New_File")
            {
                #region New_File
                LogMessage.Log("..Creating a New File");
                TreeNode tN = mi.Tag as TreeNode;
                TagInfo tI = tN.Tag as TagInfo;

                string folderName = String.Empty;

                if (tI.Type == "book")
                {
                    TagInfo booktI = tN.Parent.Tag as TagInfo;
                    folderName = booktI.Text;
                }
                else folderName = tI.Text;

                if ((tN == null) || (folderName == String.Empty))
                {
                    LogMessage.Log("..Tag was null, Or String Was Empty");
                    return;
                }
                DynamicDialog f = null;

                if (tI.Type == "book") 
                {
                    #region Populate Combo Box with possible values for filenames
                    int bookNumber = Int32.Parse(tN.Name);
                    List<Object> obj_list = new List<object>();
                    if ((bookNumber >= 1) && (bookNumber <= (Preferences.Max_Macro_Sets / 10)))
                    {
                        CMacroFile cmf_tmp = null;
                        TreeNode dropChild = null;
                        bool found = false;
                        int replace_fNumber = -1;
                        for (int i = 0; i < 10; i++)
                        {
                            found = false;
                            replace_fNumber = ((bookNumber - 1) * 10) + i;
                            for (dropChild = tN.FirstNode; dropChild != null; dropChild = dropChild.NextNode)
                            {
                                cmf_tmp = FindMacroFileExactByNode(dropChild);
                                if (cmf_tmp != null)
                                {
                                    if ((cmf_tmp.FileNumber % 10) == i)
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                            }
                            if (!found)
                            {
                                obj_list.Add(
                                    String.Format("mcr{0}.dat (Macro Set #{1})",
                                    (replace_fNumber == 0) ? "" : replace_fNumber.ToString(), i + 1));
                            }
                        }

                    }
                    else
                    {
                        LogMessage.Log("... Book Number is not 1-20, returning!");
                        return;
                    }
                    f = new DynamicDialog("File Name Entry...", obj_list.ToArray(), "Enter a name for the file:");
                    #endregion
                }
                else if (tI.Type == "char")
                {
                    #region Populate Combo Box with possible values for filenames
                    List<Object> obj_list = new List<object>();
                    for (int i = 0; i < Preferences.Max_Macro_Sets; i++)
                    {
                        String tmp_file = String.Format("mcr{0}.dat",
                            (i == 0) ? "" : i.ToString());
                        try
                        {
                            String tmp_fullpath = Path.GetFullPath(folderName + "\\" + tmp_file);
                            if (FindMacroFileExactByFileName(tmp_fullpath) == null)
                                obj_list.Add(tmp_file);
                        }
                        catch (PathTooLongException err)
                        {
                            LogMessage.Log("New_File: Path too long error -- {0}, skipping", err.Message);
                        }
                        catch (Exception err)
                        {
                            LogMessage.LogF("New_File: Unexpected exception -- {0}, skipping", err.Message);
                        }
                    }
                    f = new DynamicDialog("File Name Entry...", obj_list.ToArray(), "Enter a name for the file:", true);
                    #endregion
                }
                else
                {
                    f = new DynamicDialog("File Name Entry...", "Enter a name for the file:", "NewMacroFile.dat");
                }

                bool exitFileLoop = true;
                do
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        string name = f.GetSelection();
                        if (tI.Type == "book")
                        {
                            int start_index = name.IndexOf(' ');
                            if (start_index == -1)
                            {
                                LogMessage.Log("...Did not find a (space) in {0}", name);
                                return;
                            }
                            else
                            {
                                name = name.Remove(start_index);
                            }
                        }
                        name = System.Text.RegularExpressions.Regex.Replace(name, namesearchPattern, "");
                        String newDir = folderName.Trim('\\') + "\\";
                        String newFile = String.Empty;
                        try
                        {
                            #region Check to see if file exists in memory already, if not, create it
                            newFile = Path.GetFullPath(newDir + name.Trim('\\'));
                            if (name.Trim().Trim('.') == String.Empty)
                            {
                                if (MessageBox.Show("The filename given is not valid (empty).\r\nTry again?", "Filename is empty!", MessageBoxButtons.OKCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
                                {
                                    exitFileLoop = true;
                                }
                                else exitFileLoop = false;
                            }
                            else
                            {
                                CMacroFile cmf = FindMacroFileExactByFileName(newFile);

                                if (cmf == null)
                                {
                                    cmf = NewFileInMemory(newFile);
                                    this.treeView.SelectedNode = cmf.thisNode;
                                    cmf.thisNode.EnsureVisible();
                                    LogMessage.Log("....in memory.");
                                    exitFileLoop = true;
                                }
                                else
                                {
                                    MessageBox.Show("That file already exists, Try Again!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                                    exitFileLoop = false;
                                }
                            }
                            #endregion
                        }
                        catch (PathTooLongException err)
                        {
                            LogMessage.Log("..Error in New File: Path too long: {0} -- Error {1}", newDir + name, err.Message);
                            if (MessageBox.Show("The full path for the filename supplied\r\nwould exceed the maximum length allowed.\r\nTry again?", "Path too long error!", MessageBoxButtons.OKCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
                            {
                                exitFileLoop = true;
                            }
                            else exitFileLoop = false;
                        }
                        catch (Exception err)
                        {
                            LogMessage.Log("..Unexpected error in New File: Error {0}", err.Message);
                            MessageBox.Show("I encountered an error, try that again please", "Error!");
                            exitFileLoop = true;
                        }
                    }
                    else exitFileLoop = true;
                } while (exitFileLoop == false);
                #endregion
            }
            else if (mi.Name == "New_Folder")
            {
                #region New_Folder
                LogMessage.Log("..Creating a New Folder");
                TreeNode tN = mi.Tag as TreeNode;
                TagInfo tI = tN.Tag as TagInfo;
                string folderName = tI.Text;
                if ((tN == null) || (folderName == String.Empty))
                {
                    LogMessage.Log("..Tag was null, Or String Was Empty");
                    return;
                }
                DynamicDialog f = new DynamicDialog("Folder Name Entry...", "Enter a name for the folder:", "New Folder");

                DirectoryInfo di = null;
                bool exitDirLoop = true;
                TagInfo tI2;
                do
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        string name = f.GetSelection();
                        name = System.Text.RegularExpressions.Regex.Replace(name, namesearchPattern, "");

                        TreeNode[] tnarray = tN.Nodes.Find(name, false);
                        if ((tnarray != null) && (tnarray.Length >= 1))
                        {
                            MessageBox.Show("That directory already exists!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                            exitDirLoop = false;
                        }
                        else
                        {
                            String newFolderPath = String.Empty;
                            try
                            {
                                newFolderPath = Path.GetFullPath(folderName.Trim('\\') + "\\" + name.Trim('\\').Trim());

                                if (name.Trim().Trim('.') == String.Empty)
                                {
                                    if (MessageBox.Show("The directory name given is not valid (empty).\r\nTry again?", "Filename is empty!", MessageBoxButtons.OKCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
                                    {
                                        exitDirLoop = true;
                                    }
                                    else exitDirLoop = false;
                                }
                                else
                                {
                                    for (int drc_index = 0; drc_index < DeleteRenameChanges.Count; drc_index++)
                                    {
                                        if (DeleteRenameChanges[drc_index].Text == newFolderPath)
                                        {
                                            DeleteRenameChanges[drc_index].Type = "Skip";
                                        }
                                    }

                                    di = new DirectoryInfo(newFolderPath);
                                    TreeNode newNode;
                                    tI2 = tN.Tag as TagInfo;
                                    if ((this.FFXIInstallPath + "USER") == tI2.Text.Trim('\\'))
                                    {
                                        tI2 = new TagInfo("char", di.FullName);
                                        newNode = tN.Nodes.Add(name, name, "CharFolderClosed", "CharFolderOpen");
                                    }
                                    else
                                    {
                                        newNode = tN.Nodes.Add(name, name, "ClosedFolder", "OpenFolder");
                                        tI2 = new TagInfo("folder", di.FullName);
                                    }
                                    newNode.Tag = tI2 as Object;
                                    newNode.Parent.Expand();
                                    this.treeView.SelectedNode = newNode;
                                    LogMessage.Log("..Created new directory '" + di.FullName + "'");
                                    exitDirLoop = true;
                                }
                            }
                            catch (PathTooLongException err)
                            {
                                LogMessage.Log("Path too long: {0}, skipping", err.Message);
                                if (MessageBox.Show("The full path for the directory name supplied\r\nwould exceed the maximum length allowed.\r\nTry again?", "Path too long error!", MessageBoxButtons.OKCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
                                {
                                    exitDirLoop = true;
                                }
                                else exitDirLoop = false;
                            }
                            catch (Exception err)
                            {
                                LogMessage.LogF("..Unexpected error: {0}, attempting to ignore.", err.Message);
                                exitDirLoop = true;
                                MessageBox.Show("I encountered an error, sorry, please try that again.", "Error");
                            }
                        }
                    }
                    else exitDirLoop = true;
                } while (exitDirLoop == false);
                #endregion
            }
            else if (mi.Name == "Delete_Folder")
            {
                #region Delete_Folder
                TreeNode tN = mi.Tag as TreeNode;
                TagInfo tI = tN.Tag as TagInfo;

                if (Directory.Exists(tI.Text)) // if it doesn't exist, don't prompt to delete
                {
                    int drc_index = 0;
                    for (; drc_index < DeleteRenameChanges.Count; drc_index++)
                    {
                        if (DeleteRenameChanges[drc_index].Text == tI.Text)
                        {
                            DeleteRenameChanges[drc_index].Type = mi.Name;
                            break;
                        }
                    }
                    if (drc_index >= DeleteRenameChanges.Count)
                    {
                        DeleteRenameChanges.Add(new TagInfo(mi.Name, tI.Text));
                    }
                }

                if (tN == this.treeView.SelectedNode)
                {
                    // choose a different node
                    if (tN.PrevNode != null)
                        this.treeView.SelectedNode = tN.PrevNode;
                    else if (tN.NextNode != null)
                        this.treeView.SelectedNode = tN.NextNode;
                    else if (tN.Parent != null)
                        this.treeView.SelectedNode = tN.Parent;
                    else this.treeView.SelectedNode = null;
                }

                for (int i = 0; i < MacroFiles.Count; i++)
                {
                    if (MacroFiles[i].fName.StartsWith(tI.Text, true, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        MacroFiles[i].Delete();
                    }
                }
                for (int i = 0; i < BookList.Count; i++)
                {
                    if (BookList[i].fName.StartsWith(tI.Text, true, System.Globalization.CultureInfo.InvariantCulture))
                        BookList[i].Delete();
                }

                tN.Remove();
                #endregion
            }
            else if (mi.Name == "Delete_File")
            {
                #region Delete File
                CMacroFile cmf = mi.Tag as CMacroFile;
                TreeNode tN = cmf.thisNode;

                cmf.Delete(); // set flag, remove node references.

                if (tN == this.treeView.SelectedNode)
                {
                    // choose a different node
                    if (tN.PrevNode != null)
                        this.treeView.SelectedNode = tN.PrevNode;
                    else if (tN.NextNode != null)
                        this.treeView.SelectedNode = tN.NextNode;
                    else if (tN.Parent != null)
                        this.treeView.SelectedNode = tN.Parent;
                    else this.treeView.SelectedNode = null;
                }

                tN.Remove();
                #endregion
            }
            else if (mi.Name == "Clear_File")
            {
                #region Clear File
                CMacroFile cmf = mi.Tag as CMacroFile;
                if (cmf != null)
                {
                    this.treeView.BeginUpdate();
                    cmf.Clear();
                    ChangeButtonNames();
                    FillForm();
                    this.treeView.EndUpdate();
                }
                #endregion
            }
        }

        private void RenameCharacter(string char_folderName, string char_Name)
        {
            int folder_x = Int32.MaxValue;
            if (char_folderName.Trim() == String.Empty)
                return;

            bool Remove = (char_Name.Trim() == String.Empty);

            if (characterList != null)
            {
                for (folder_x = 0; folder_x < characterList.Length; folder_x++)
                {
                    if (characterList[folder_x].Type == char_folderName)
                        break;
                }
                if (folder_x >= characterList.Length)
                {
                    Array.Resize(ref characterList, characterList.Length + 1);
                    folder_x = characterList.Length - 1;
                }
                else if ((Remove) && (folder_x < characterList.Length))
                {
                    LogMessage.Log("..Removed Association of Character Name '{0}' and Folder Name '{1}'",
                        characterList[folder_x].Text, characterList[folder_x].Type);
                    characterList[folder_x] = new TagInfo();
                    return;
                }
            }
            else if (!Remove)
            {
                characterList = new TagInfo[1];
                folder_x = 0;
            }
            else if (Remove)
                return;
            characterList[folder_x] = new TagInfo(char_folderName.Trim(), char_Name.Trim());
            LogMessage.Log("..Associated Character Name '{0}' with a Folder Name '{1}'",
                characterList[folder_x].Text, characterList[folder_x].Type);
        }

        private String GetValidName(String input)
        {
            String namesearchPattern = @"[^a-zA-Z0-9]";
            return System.Text.RegularExpressions.Regex.Replace(input, namesearchPattern, "");
        }

        private void RenameBook(TreeNode tN, string input_name)
        {
            if (tN.Parent == null)
            {
                LogMessage.Log("..Requested Book Node to Rename has a null Parent Node?! ({0})!", tN.Text);
            }
            else
            {
                try
                {
                    TagInfo tA = tN.Parent.Tag as TagInfo;
                    String ttl_name = Path.GetFullPath(tA.Text + "\\mcr.ttl");
                    int booknum = -1;
                    if (Int32.TryParse(tN.Name, out booknum))
                    {
                        CBook c = null;
                        for (int index = 0; index < BookList.Count; index++)
                        {
                            c = BookList[index];

                            if (ttl_name == c.fName)
                            {
                                if (c.IsDeleted)
                                    c.Restore();
                                break;
                            }
                        }

                        if (c != null)
                        {
                            String oldname = tN.Text;
                            c.SetBookName(booknum - 1, input_name);
                            tN.Text = c.GetBookName(booknum - 1);
                            LogMessage.Log(String.Format("..Renamed Book Name '{0}' to '{1}'",
                                oldname, tN.Text));
                        }
                    }
                }
                catch (PathTooLongException e)
                {
                    LogMessage.Log("..RenameBook(): Path would be too long -- {0}", e.Message);
                    MessageBox.Show("Unable to complete that request, Path to BookName file is too long!", "Path too long error!");
                }
                catch (Exception e)
                {
                    LogMessage.LogF("..RenameBook(): Unexpected error {0}", e.Message);
                    MessageBox.Show("Unable to complete the request, unexpected error.", "Unexpected error");
                }
            }
        }

        private void SaveThistoolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogMessage.Log("Save This");
            if (this.treeView == null)
            {
                LogMessage.Log("..treeView = null");
                return;
            }

            this.SetWaitCursor();

            TreeNode tN = ((ToolStripMenuItem)sender).Tag as TreeNode;
            if (tN == null)
                tN = this.treeView.SelectedNode;

            CMacroFile cmf = FindMacroFileByNode(tN);
            CMacro cm = FindMacroByNode(tN);

            if ((cm == null) && (cmf != null))
                cm = GetCurrentMacro(cmf);

            if (cm != null)
                SaveToMemory(cm);

            if (cmf != null)
            {
                if (cmf.Save() == true)
                {
                    MessageBox.Show(cmf.fName + " saved successfully.", "You have been Saved!");
                    LogMessage.Log(".." + cmf.fName + " saved successfully.");
                }
                else
                {
                    MessageBox.Show(cmf.fName + " could NOT be saved!", "Error while saving");
                    LogMessage.Log(".." + cmf.fName + " saved failed.");
                }
            }
            else MessageBox.Show("You must open a folder or file first in order to Save!", "Error while saving");
            
            this.RestoreCursor();
        }
        // Separator
        private void ReloadThisToCurrentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogMessage.Log("Reload This");
            if (this.treeView == null)
            {
                LogMessage.Log("..treeView == null");
                return;
            }
            else if (sender == null)
            {
                LogMessage.Log("..Sender == null");
                return;
            }

            TreeNode tN = ((ToolStripMenuItem)sender).Tag as TreeNode;
            if (tN == null)
                tN = this.treeView.SelectedNode;
            CMacroFile cmf = FindMacroFileByNode(tN);
            CMacro cm = FindMacroByNode(tN);
            if (cmf == null)
            {
                LogMessage.Log("..Unable to find node: " + tN.FullPath);
                return;
            }

            this.SetWaitCursor();
            this.treeView.BeginUpdate();

            if (cmf.Load())
            {
                if (cm == null)
                    cm = GetCurrentMacro(cmf);
                if (cm != null)
                    FillForm(cm);
                else FillForm(cmf.Macros[0]);
                LogMessage.Log("..Load successful for " + cmf.fName);
                cmf.Changed = false;
            }
            else LogMessage.Log("..Load failed for " + cmf.fName);

            this.treeView.EndUpdate();
            this.RestoreCursor();
        }
        private void ReloadAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogMessage.Log("Reload All chosen");
            String tNPath = this.treeView.SelectedNode.Name;
            OpenFolderMethod(Preferences.PathToOpen);

            if ((treeView.Nodes != null) && (treeView.Nodes.Count > 0))
            {
                TreeNode[] tnarray = this.treeView.Nodes.Find(tNPath, true);
                if (tnarray.Length > 0)
                    this.treeView.SelectedNode = tnarray[0];
                else this.treeView.SelectedNode = this.treeView.Nodes[0];
                this.treeView.SelectedNode.EnsureVisible();
                FillForm();
            }
            LogMessage.Log("..Reload All successful");
        }
        #endregion

        #region MainForm Methods (TextBox & TabTextBox event handlers and related)
        #region MainForm Methods (Word/Phrase Selection and associated Utilities for TabTextBox)
        int max(int val, int _max)
        {
            return ((val > _max) ? _max : val);
        }
        int min(int val, int _min)
        {
            return ((val < _min) ? _min : val);
        }
        int max_min(int val, int _min, int _max)
        {
            return max(min(val, _min), _max);
            //return ((val < min) ? min : ((val > max) ? max : val));
        }
        // between   val is somewhere between lwr and upr INCLUSIVE
        bool between(int lwr, int val, int upr)
        {
            return ((lwr <= val) && (val <= upr));
        }
        int get_length(int start_pos, int end_pos)
        {
            if (end_pos < start_pos)
                return 0;
            return (end_pos + 1 - start_pos);
        }
        private start_len GetWordOrPhraseFromSelection(string s, int ndex, int len)
        {
            if ((s.Trim() == String.Empty) || (ndex <= 0))
                return (new start_len(ndex, len));
            else if (s[ndex - 1] == ' ')
            {
                int mid_atp_c = s.LastIndexOf(FFXIEncoding.MiddleMarker, ndex - 1);
                int sta_atp_c = s.LastIndexOf(FFXIEncoding.StartMarker, ndex - 1);
                int end_atp_c = s.IndexOf(FFXIEncoding.EndMarker, ndex - 1);

                // if we found and end marker, a start marker, and a mid-marker
                // and we're behind a space, and the order is start -> mid -> end
                // it's assumed we're behind a space while INSIDE an auto-translate phrase.
                if (!((end_atp_c != -1) && (end_atp_c > mid_atp_c) &&
                    (mid_atp_c > sta_atp_c) && (mid_atp_c != -1) && (sta_atp_c != -1)))
                    return (new start_len(ndex, len));
            }

            // basically at this point, we're not in back of a space and not at start of line
            int index = max_min(ndex, 0, s.Length - 1);

            int end_space = -1, start_space = -1;

            int sta_atp = -1, end_atp = -1,
                sta_atp_2 = -1, end_atp_2 = -1;

            // ndex will not be 0, neither will index, so this will work no matter what
            // Assuming we're between the Start and End marker.
            sta_atp = s.LastIndexOf(FFXIEncoding.StartMarker, ndex - 1);
            end_atp = s.IndexOf(FFXIEncoding.EndMarker, sta_atp + 1);
            // Checks to see if we're between
            sta_atp_2 = s.IndexOf(FFXIEncoding.StartMarker, ndex - 1);
            end_atp_2 = s.IndexOf(FFXIEncoding.EndMarker, sta_atp_2 + 1);
            start_space = s.LastIndexOf(' ', ndex - 1);
            end_space = s.IndexOf(' ', ndex - 1);
            // (end_atp >= (ndex - 1)) && (sta_atp <= (ndex - 1))
            if ((sta_atp != -1) && between(sta_atp, ndex - 1, end_atp))
            {
                return (new start_len(sta_atp, get_length(sta_atp, end_atp)));
            }
            // (end_atp_2 >= (ndex - 1)) && (sta_atp_2 <= (ndex - 1))
            else if ((sta_atp_2 != -1) && between(sta_atp_2, (ndex - 1), end_atp_2))
            {
                return (new start_len(sta_atp_2, get_length(sta_atp_2, end_atp_2)));
            }
            else
            {
                if (start_space == -1)
                    start_space = 0;
                if ((end_atp > start_space) && (end_atp < ndex) && (start_space < ndex))
                {
                    start_space = end_atp;
                }
                if ((end_atp_2 > start_space) && (end_atp_2 < ndex) && (start_space < ndex))
                {
                    start_space = end_atp_2;
                }
                if (end_space == -1)
                    end_space = s.Length - 1;
                if ((sta_atp_2 < end_space) && (end_space > (ndex - 1)) && (sta_atp_2 > (ndex - 1)))
                {
                    end_space = sta_atp_2;
                }
                if ((sta_atp < end_space) && (end_space > ndex) && (sta_atp > ndex))
                {
                    end_space = sta_atp;
                }
                if (start_space >= s.Length)
                    start_space = s.Length - 1;
                else if (start_space < 0)
                    start_space = 0;
                else if (s[start_space] == ' ')
                    start_space++;
                else if (s[start_space] == FFXIEncoding.EndMarker)
                    start_space++;
                if (end_space >= s.Length)
                    end_space = s.Length - 1;
                else if (end_space < 0)
                    end_space = 0;
                else if (s[end_space] == ' ')
                    end_space--;
                else if (s[end_space] == FFXIEncoding.StartMarker)
                    end_space--;
                if (start_space > end_space)
                    return (new start_len(ndex, len));
            }
            return (new start_len(start_space, get_length(start_space, end_space)));
        }

        public string GetPhraseWordsFromString(string p)
        {
            int atp_start = p.IndexOf(FFXIEncoding.StartMarker);
            if (atp_start == -1)
                return p;
            int atp_mid = p.IndexOf(FFXIEncoding.MiddleMarker, atp_start + 1);
            if (atp_mid == -1)
                return p;
            int atp_end = p.IndexOf(FFXIEncoding.EndMarker, atp_mid + 1);
            if (atp_end == -1)
                return p;
            return p.Substring(atp_mid + 1, get_length(atp_mid + 1, atp_end - 1));
        }
        #endregion

        #region MainForm Methods (ContextMenuStrip for TabTextBox OnClick handler)
        /// <summary>
        /// Context Menu Strip Click Handler used for replacing the selected text with the Auto-Translate Phrase selected via the Context Menu.
        /// </summary>
        /// <param name="sender">The ToolStripMenuItem that was clicked.</param>
        /// <param name="e">Generic EventArgs variable, unused.</param>
        private void ContextAT_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
                if (tsmi != null)
                {
                    if (caller != null)
                    {
                        TabTextBox ttb = caller as TabTextBox;
                        caller = null;
                        if (ttb != null)
                        {
                            int caret_pos = ttb.SelectionStart;
                            if (ttb.SelectionLength != 0)
                            {
                                string s = ttb.Text.Remove(ttb.SelectionStart, ttb.SelectionLength);
                                ttb.Text = s.Insert(ttb.SelectionStart, tsmi.Name);
                            }
                            else
                            {
                                string s = ttb.Text.Insert(ttb.SelectionStart, tsmi.Name);
                                ttb.Text = s;
                            }
                            ttb.Select(caret_pos, tsmi.Name.Length);
                        }
                    }
                }
            }
        }
        #endregion

        #region MainForm Methods (TextBox Event Handlers)

        /// <summary>
        /// Occurs after the KeyDown event is fired, and can be used to prevent characters from entering the control.
        /// </summary>
        /// <param name="sender">The TabTextBox that fired the event.</param>
        /// <param name="e">KeyPressEventArgs variable containing info about keypressed and whatnot.</param>
        private void textBoxName_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            // Check for the flag being set in the KeyDown event.
            if (nonAlphaNumEntered == true)
            {
                // Stop the character from being entered into the control since it is non-numerical.
                e.Handled = true;
            }
        }
        private bool textBoxBackspace(TabTextBox to)
        {
            int toLine = getNumfromTTB(to);

            if (toLine <= 0)
                return false;
            for (int i = toLine; i < 6; i++)
            {
                getTTBfromNum(i).Text += getTTBfromNum(i + 1).Text;
                getTTBfromNum(i + 1).Text = String.Empty;
            }
            return true;
        }
        private bool textBoxInsertLine(TabTextBox src, int insertstart, String text, bool WarnOnLoss)
        {
            if ((insertstart < 0) || (insertstart > 6) || (src == null))
                return false;
            if (textBoxLine6.Text != "")
            {
                DialogResult dr = MessageBox.Show("If we insert a line, you'll lose the last line of your macro!\r\n  (To remove this warning, choose change Enter Key Option to 'Insert Line')\r\nIs that ok?", "Possible loss of information warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (dr == DialogResult.No)
                    return false;
            }
            if (src.SelectionLength > 0)
                src.Text = src.Text.Remove(src.SelectionStart, src.SelectionLength);
            if (insertstart < 6)
            {
                textBoxLine6.Text = textBoxLine5.Text;
            }
            if (insertstart < 5)
            {
                textBoxLine5.Text = textBoxLine4.Text;
            }
            if (insertstart < 4)
            {
                textBoxLine4.Text = textBoxLine3.Text;
            }
            if (insertstart < 3)
            {
                textBoxLine3.Text = textBoxLine2.Text;
            }
            if (insertstart < 2)
            {
                textBoxLine2.Text = textBoxLine1.Text;
            }
            if (insertstart < 1)
            {
                textBoxLine1.Text = textBoxName.Text;
            }
            src.Text = text;
            src.SelectionStart = 0;
            src.SelectionLength = 0;
            SendKeys.Send("{DOWN}");
            return true;
        }
        private bool textBoxInsertLine(TabTextBox src, int insertstart, String text)
        {
            return textBoxInsertLine(src, insertstart, text, false);
        }

        private int getNumfromTTB(TabTextBox src)
        {
            if (src == textBoxName)
                return 0;
            else if (src == textBoxLine1)
                return 1;
            else if (src == textBoxLine2)
                return 2;
            else if (src == textBoxLine3)
                return 3;
            else if (src == textBoxLine4)
                return 4;
            else if (src == textBoxLine5)
                return 5;
            else if (src == textBoxLine6)
                return 6;
            return -1;
        }
        private TabTextBox getTTBfromNum(int num)
        {
            switch (num)
            {
                case 0: return textBoxName;
                case 1: return textBoxLine1;
                case 2: return textBoxLine2;
                case 3: return textBoxLine3;
                case 4: return textBoxLine4;
                case 5: return textBoxLine5;
                case 6: return textBoxLine6;
                default: return null;
            }
        }

        private void textBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (sender == null)
                return;

            if (sender is TabTextBox)
            {
                TabTextBox src = sender as TabTextBox;
                if ((e.KeyCode == Keys.Back) && (src.SelectionStart == 0))
                {
                    if ((Preferences.EnterCreatesNewLine == 0) ||
                        ((Preferences.EnterCreatesNewLine != 0) && (src == textBoxName) && (src.SelectionStart == 0)))
                    {
                        SendKeys.Send("{UP}");
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                    }
                    else if (((Preferences.EnterCreatesNewLine == 1) || (Preferences.EnterCreatesNewLine == 2)) && (src.SelectionStart == 0) && (src != textBoxName) && (src != textBoxLine1))
                    {
                        int startPos = getNumfromTTB(src);
                        TabTextBox newsrc = getTTBfromNum(startPos - 1);
                        int oldSP = newsrc.TextLength;
                        textBoxBackspace(newsrc);
                        SendKeys.Send("{UP}");
                        newsrc.SelectionStart = oldSP;
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                    }
                }
                else if ((e.KeyCode == Keys.Delete) && (src.SelectionStart == src.TextLength))
                {
                    if (Preferences.EnterCreatesNewLine != 0)
                    {
                        int oldSP = src.TextLength;
                        textBoxBackspace(src);
                        src.SelectionStart = oldSP;
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                    }
                }
                else if (e.KeyCode == Keys.Enter)// handle enter as if we went DOWN
                {
                    if ((Preferences.EnterCreatesNewLine == 0) ||
                        ((Preferences.EnterCreatesNewLine != 0) && 
                            (((src == textBoxLine6) && (src.SelectionStart == src.TextLength))
                            || (src == textBoxName))))
                        SendKeys.Send("{DOWN}");
                    // Just Insert                          Safe Insert
                    else if ((src != textBoxName) && (Preferences.EnterCreatesNewLine == 1) || (Preferences.EnterCreatesNewLine == 2))
                    {
                        bool WarnOnInsert = (Preferences.EnterCreatesNewLine == 2);
                        int startPos = getNumfromTTB(src);

                        if (src.SelectionStart == 0)
                        {
                            textBoxInsertLine(src, startPos, String.Empty, WarnOnInsert);
                        }
                        else if ((src.SelectionStart == src.TextLength) && (src.TextLength > 0))
                        {
                            startPos++;
                            TabTextBox newsrc = getTTBfromNum(startPos);
                            textBoxInsertLine(newsrc, startPos, String.Empty, WarnOnInsert);
                        }
                        else // somewhere in middle of line
                        {
                            String stufftomove = src.Text.Substring(src.SelectionStart);
                            TabTextBox newsrc = getTTBfromNum(startPos + 1);
                            if (src.SelectionLength > 0)
                                stufftomove = src.Text.Substring(src.SelectionStart + src.SelectionLength);
                            if (textBoxInsertLine(newsrc, startPos + 1, stufftomove, WarnOnInsert))
                            {
                                src.Text = src.Text.Remove(src.SelectionStart);
                                src.SelectionLength = 0;

                            }
                        }
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                    }
                }

                if (((e.KeyCode == Keys.Up) || (e.KeyCode == Keys.Down)) && !(e.Control || e.Alt || e.Shift))
                {
                    #region DONE: ALL TextBox(Name & Line1-6) Up/Down Arrow pressed Move to next
                    TabTextBox upbox = null, downbox = null;

                    if (src == textBoxName)
                    {
                        upbox = textBoxLine6;
                        downbox = textBoxLine1;
                    }
                    else if (src == textBoxLine1)
                    {
                        upbox = textBoxName;
                        downbox = textBoxLine2;
                    }
                    else if (src == textBoxLine2)
                    {
                        upbox = textBoxLine1;
                        downbox = textBoxLine3;
                    }
                    else if (src == textBoxLine3)
                    {
                        upbox = textBoxLine2;
                        downbox = textBoxLine4;
                    }
                    else if (src == textBoxLine4)
                    {
                        upbox = textBoxLine3;
                        downbox = textBoxLine5;
                    }
                    else if (src == textBoxLine5)
                    {
                        upbox = textBoxLine4;
                        downbox = textBoxLine6;
                    }
                    else if (src == textBoxLine6)
                    {
                        upbox = textBoxLine5;
                        downbox = textBoxName;
                    }
                    if (e.KeyCode == Keys.Up)
                    {
                        int ss = upbox.SelectionStart, sL = upbox.SelectionLength;
                        upbox.SelectionLength = 0;
                        upbox.Focus();
                        upbox.SelectionStart = ss;
                        upbox.SelectionLength = sL;
                        e.Handled = true;
                    }
                    if (e.KeyCode == Keys.Down)
                    {
                        int ss = downbox.SelectionStart, sL = downbox.SelectionLength;
                        downbox.SelectionLength = 0;
                        downbox.Focus();
                        downbox.SelectionStart = ss;
                        downbox.SelectionLength = sL;
                        e.Handled = true;
                    }
                    #endregion
                }

                // if alt is held down and key press is left, up, right, or down, special case

                if (((e.KeyCode == Keys.Left) || (e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.Up) || (e.KeyCode == Keys.Down)))
                {
                    #region If Key is Up, Left, Down, or Right, check modifiers
                    // use current selected node as macro
                    // macro should be selected if textboxname is enabled
                    CMacroFile cmf = FindMacroFileByNode(treeView.SelectedNode);
                    CMacro cm = null;
                    if (cmf != null)
                        cm = GetCurrentMacro(cmf);
                    if ((cmf != null) && (cm != null))
                    {
                        #region If it's a Macro Set ONLY
                        if ((e.Alt) && !(e.Control || e.Shift))
                        {
                            #region Alt is being held down, but not Ctrl or Shift
                            // Alt+Left     goes previous Macro
                            // Alt+Right    goes next Macro
                            // Alt+Down     goes to Alt/Ctrl Bar
                            // Alt+Up        "    "    "      "
                            int mn = cm.MacroNumber;
                            if (e.KeyCode == Keys.Left)
                                mn--;
                            else if (e.KeyCode == Keys.Right)
                                mn++;
                            else if ((e.KeyCode == Keys.Up) || (e.KeyCode == Keys.Down))
                            {
                                if ((mn >= 0) && (mn <= 9))
                                    mn += 10;
                                else if ((mn >= 10) && (mn <= 19))
                                    mn -= 10;
                            }
                            if (mn < 0)
                                mn = 19; // skip to last
                            else if (mn > 19)
                                mn = 0;  // skip to first
                            treeView.SelectedNode = cmf.Macros[mn].thisNode;
                            FillForm(cmf.Macros[mn]);
                            e.Handled = true;
                            #endregion
                        }
                        if (!(e.Alt) && (e.Control && e.Shift))
                        {
                            #region if Alt is NOT being held down but both Ctrl+Shift is
                            if ((e.KeyCode == Keys.Up) || (e.KeyCode == Keys.Down))
                            {

                                // Ctrl+Shift+Up or Ctrl+Shift+Down
                                // Cycles between Macro Sets (Next/Previous)
                                TreeNode tN = null;
                                if (e.KeyCode == Keys.Up)
                                {
                                    // If Ctrl+Shift+Up
                                    // if there's no PrevNode in this list (first in list for parents nodes)
                                    if (cmf.thisNode.PrevNode == null)
                                        // Skip to Parent's Last Node
                                        tN = cmf.thisNode.Parent.LastNode;
                                    // Else Go To Previous Node
                                    else tN = cmf.thisNode.PrevNode;
                                    // look to see if it's a valid MacroFile Node
                                    CMacroFile cmfnew = FindMacroFileByNode(tN);
                                    if (cmfnew != null)
                                    {
                                        // if it is, select it
                                        this.treeView.SelectedNode = tN;
                                        FillForm(cmfnew.Macros[cm.MacroNumber]);
                                    }
                                    // if not, don't select it
                                    // avoids problems with FillForm
                                    // in the meantime, no matter what, call this keyset as handled
                                    e.Handled = true;
                                }
                                else if (e.KeyCode == Keys.Down)
                                {
                                    // if Ctrl+Shift+Down
                                    // if there's no NextNode in this list (end of list for parent's)
                                    if (cmf.thisNode.NextNode == null)
                                        // Skip to Parent's First Node
                                        tN = cmf.thisNode.Parent.FirstNode;
                                    else tN = cmf.thisNode.NextNode;
                                    CMacroFile cmfnew = FindMacroFileByNode(tN);
                                    if (cmfnew != null)
                                    {
                                        this.treeView.SelectedNode = tN;
                                        FillForm(cmfnew.Macros[cm.MacroNumber]);
                                    }
                                    e.Handled = true;
                                }
                            }
                            #endregion
                        }
                        #endregion
                    }
                    #endregion
                }

                if (src == textBoxName)
                {
                    if ((e.KeyCode == Keys.V) && (e.Control) && (src == textBoxName))
                    {
                        #region if Pasting
                        HandleSpecialPaste(src); // instead of src.Paste()
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                        #endregion
                    }

                    #region TODO?: Determine if keypressed in textBoxName is not valid and handle
                    // Initialize the flag to true.
                    nonAlphaNumEntered = true;
                    if (
                         (e.KeyCode == Keys.Left) || (e.KeyCode == Keys.Right) ||
                         (e.KeyCode == Keys.Delete) || (e.KeyCode == Keys.Back) ||
                        ((e.KeyCode >= Keys.A) && (e.KeyCode <= Keys.Z)) ||
                        (((e.KeyCode >= Keys.D0) && (e.KeyCode <= Keys.D9)) && (!(e.Shift || e.Alt || e.Control))) ||
                        (((e.KeyCode >= Keys.NumPad0) && (e.KeyCode <= Keys.NumPad9)) && (!(e.Shift || e.Alt || e.Control)))
                        )
                    {
                        nonAlphaNumEntered = false;
                    }
                    //else e.SuppressKeyPress = true;
                    #endregion
                }

                if (src != textBoxName)
                {
                    #region DONE: Handle Tab Key Pressed event in anything but textBoxName
                    if ((e.KeyCode == Keys.Tab) && !(e.Shift || e.Alt || e.Control))
                    {
                        Point p;
                        CMacroFile cmf_ref = FindMacroFileByNode(this.treeView.SelectedNode);
                        CMacro cm_ref = GetCurrentMacro(cmf_ref);
                        bool modified = false;

                        if (cm_ref != null)
                        {
                            start_len my_sel = GetWordOrPhraseFromSelection(src.Text, src.SelectionStart, src.SelectionLength);
                            string correct_selection = src.Text.Substring(my_sel.start, my_sel.length);
                            int ss = src.SelectionStart;
                            int sL = src.SelectionLength;
                            string phrasetoSearch = src.SelectedText;
                            string srcText = src.Text;
                            int srcText_len = src.TextLength;

                            // if we have to modify text to make this work right
                            // b/c apparently Japanese characters do not allow
                            // this to report correct info.
                            this.SuppressNodeUpdates = true;

                            // if string isn't empty and SelectionStart is at the end
                            // add one to get a decent position for char index
                            // due to multi-byte chars in Strings...
                            // if it is empty, GetPositionFromCharIndex() will return
                            // correct coords
                            // Basically, if it's at the end of a non-empty string
                            if ((srcText != String.Empty) && (ss == src.TextLength))
                            {
                                src.Text += ' ';
                                src.Select(ss, sL);
                                modified = true;
                            }

                            if (correct_selection.Contains(src.SelectedText))
                            {
                                phrasetoSearch = GetPhraseWordsFromString(correct_selection);
                            }
                            else
                            {
                                phrasetoSearch = GetPhraseWordsFromString(src.SelectedText);
                                my_sel.start = src.SelectionStart;
                                my_sel.length = src.SelectionLength;
                            }
                            #region DONE: If Selection turns up spaces, or nothing, use default menu with default selection
                            if (phrasetoSearch.Trim() == String.Empty)
                            {
                                p = src.GetPositionFromCharIndex(my_sel.start);
                                if (modified)
                                {
                                    lock (src.Text)
                                    {
                                        src.Text = srcText;
                                    }
                                }
                                src.Select(my_sel.start, my_sel.length);
                                caller = src;

                                this.SuppressNodeUpdates = false;

                                if (this.ATPhraseStrip == null)
                                    this.ATPhraseStrip = BuildATMenu(Preferences.Language);
                                if (!Preferences.Include_Header)
                                {
                                    if (this.ATPhraseStrip.Items.Count > 0)
                                        this.ATPhraseStrip.Items[0].Visible = false;
                                    if (this.ATPhraseStrip.Items.Count > 1)
                                        this.ATPhraseStrip.Items[1].Visible = false;
                                }
                                else
                                {
                                    if (this.ATPhraseStrip.Items.Count > 0)
                                        this.ATPhraseStrip.Items[0].Visible = true;
                                    if (this.ATPhraseStrip.Items.Count > 1)
                                        this.ATPhraseStrip.Items[1].Visible = true;
                                }
                                this.ATPhraseStrip.Show(src, p, ToolStripDropDownDirection.AboveRight);
                                return;
                            }
                            #endregion
                            #region Build Context Menu Strip (done) and show at position
                            #region Initialize variables

                            int MAX_MENUITEMS = Preferences.Max_Menu_Items;
                            if (Preferences.Include_Header)
                                MAX_MENUITEMS++;

                            FFXIATPhrase[] atp = this.ATPhraseLoader.GetPhrases(phrasetoSearch.Trim(" \'\"".ToCharArray()), Preferences.Include_Header); // use selected text

                            #region If 0 choices found, send default menu, ala FFXI
                            if ((atp == null) || (atp.Length <= ((Preferences.Include_Header) ? 1 : 0))) // 0 results
                            {
                                p = src.GetPositionFromCharIndex(my_sel.start);
                                if (modified)
                                {
                                    lock (src.Text)
                                    {
                                        src.Text = srcText;
                                    }
                                }
                                src.Select(my_sel.start, my_sel.length);
                                caller = src;

                                this.SuppressNodeUpdates = false;

                                if (this.ATPhraseStrip == null)
                                    this.ATPhraseStrip = BuildATMenu(Preferences.Language);
                                if (!Preferences.Include_Header)
                                {
                                    if (this.ATPhraseStrip.Items.Count > 0)
                                        this.ATPhraseStrip.Items[0].Visible = false;
                                    if (this.ATPhraseStrip.Items.Count > 1)
                                        this.ATPhraseStrip.Items[1].Visible = false;
                                }
                                else
                                {
                                    if (this.ATPhraseStrip.Items.Count > 0)
                                        this.ATPhraseStrip.Items[0].Visible = true;
                                    if (this.ATPhraseStrip.Items.Count > 1)
                                        this.ATPhraseStrip.Items[1].Visible = true;
                                }
                                this.ATPhraseStrip.Show(src, p, ToolStripDropDownDirection.AboveRight);
                                return;
                            }
                            #endregion
                            IComparer atpComparer = new FFXIATPhraseLoader.ATPhraseCompareByValue();
                            Array.Sort(atp, 1, atp.Length - 1, atpComparer);
                            ContextMenuStrip cms = new ContextMenuStrip();
                            ToolStripMenuItem[] weapons = new ToolStripMenuItem[FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX + 1],
                                armor = new ToolStripMenuItem[FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX + 1],
                                puppet = new ToolStripMenuItem[FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX + 1],
                                objects = new ToolStripMenuItem[FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX + 1];
                            ToolStripMenuItem[] items = new ToolStripMenuItem[FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX + 1],
                                keyitems = new ToolStripMenuItem[FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX + 1];
                            ToolStripMenuItem newitem = null;
                            ToolStripMenuItem[][] tsmi_atp = new ToolStripMenuItem[FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX + 1][];
                            ToolStripItem[][] tsmi = new ToolStripItem[FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX + 1][];
                            ToolStripItem[] header = null;

                            ToolStripMenuItem[] languages = new ToolStripMenuItem[FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX + 1];

                            if (Preferences.Language == FFXIATPhraseLoader.ffxiLanguages.LANG_ALL)
                            {
                                for (int i = FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN; i <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX; i++)
                                {
                                    languages[i] = new ToolStripMenuItem(FFXIATPhraseLoader.Languages[i]);
                                }
                            }

                            #endregion
                            #region For Loop To Build Menu Dynamically
                            for (int i = 0; i < atp.Length; i++)
                            {
                                if (atp[i].value.Trim() == String.Empty)
                                    continue;

                                string ItemName = atp[i].value; // set the itemname

                                byte b1 = atp[i].StringResource,
                                     b2 = atp[i].Language,
                                     b3 = atp[i].GroupID,
                                     b4 = atp[i].MessageID;

                                #region DONE: ContextMenu: Create Header
                                if ((Preferences.Include_Header) && (ItemName.Contains("similar phrase")))
                                {
                                    if (header == null)
                                        header = new ToolStripLabel[1];
                                    header[0] = new ToolStripLabel(ItemName.Trim('.'));
                                    if (Preferences.Language == FFXIATPhraseLoader.ffxiLanguages.LANG_ALL)
                                        header[0].Text += " in all languages.";
                                    Font f = new Font(header[0].Font, FontStyle.Bold);
                                    header[0].Font = f;
                                    if (i != (atp.Length - 1)) // if more than 0 phrases were found
                                    {
                                        Array.Resize(ref header, 2);
                                        header[1] = new ToolStripSeparator();
                                    }
                                }
                                #endregion
                                #region DONE: ContextMenu: Build Items Category if more than MAX_MENUITEMS
                                else if ((atp.Length > MAX_MENUITEMS) && (b1 == 0x07))
                                {
                                    if (items[b2] == null)
                                        items[b2] = new ToolStripMenuItem("Items", Icons.ItemIcon[0]);
                                    newitem = new ToolStripMenuItem(ItemName, null, ContextAT_Click);
                                    ToolStripMenuItem addto = null;
                                    newitem.Name = atp[i].ToString();
                                    if (atp[i].Type == 4) // Weapons
                                    {
                                        if (weapons[b2] == null)
                                            weapons[b2] = new ToolStripMenuItem("Weapons", Icons.WeaponIcon[0]);
                                        addto = weapons[b2];
                                        if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                            (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                            newitem.Image = Icons.WeaponIcon[b2];
                                        else newitem.Image = Icons.WeaponIcon[0];
                                    }
                                    else if (atp[i].Type == 5) // Armors
                                    {
                                        if (armor[b2] == null)
                                            armor[b2] = new ToolStripMenuItem("Armor", Icons.ArmorIcon[0]);
                                        addto = armor[b2];
                                        if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                            (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                            newitem.Image = Icons.ArmorIcon[b2];
                                        else newitem.Image = Icons.ArmorIcon[0];
                                    }
                                    else if (atp[i].Type == 13) // Puppet Items
                                    {
                                        if (puppet[b2] == null)
                                            puppet[b2] = new ToolStripMenuItem("Puppet Items", Icons.PuppetIcon[0]);
                                        addto = puppet[b2];
                                        if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                            (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                            newitem.Image = Icons.PuppetIcon[b2];
                                        else newitem.Image = Icons.PuppetIcon[0];
                                    }
                                    else
                                    {
                                        if (objects[b2] == null)
                                            objects[b2] = new ToolStripMenuItem("Other Items", Icons.ItemIcon[0]);
                                        addto = objects[b2];
                                        if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                            (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                            newitem.Image = Icons.ItemIcon[b2];
                                        else newitem.Image = Icons.ItemIcon[0];
                                    }

                                    addto.DropDownItems.Add(newitem);
                                }
                                #endregion
                                #region DONE: ContextMenu: Build Key Items Category if more than MAX_MENUITEMS
                                else if ((atp.Length > MAX_MENUITEMS) && (b1 == 0x13))
                                {
                                    if (keyitems[b2] == null)
                                        keyitems[b2] = new ToolStripMenuItem("Key Items", Icons.KeyItemIcon[0]);
                                    newitem = new ToolStripMenuItem(ItemName, null, ContextAT_Click);
                                    if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                        (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                        newitem.Image = Icons.KeyItemIcon[b2];
                                    else newitem.Image = Icons.KeyItemIcon[0];
                                    newitem.Name = atp[i].ToString();
                                    keyitems[b2].DropDownItems.Add(newitem);
                                }
                                #endregion
                                #region DONE: ContextMenu: Build Auto-Translate Categories if more than MAX_MENUITEMS
                                else if ((atp.Length > MAX_MENUITEMS) && ((b1 != 0x07) && (b1 != 0x13)))
                                {
                                    int tsmi_cnt = 0;
                                    FFXIATPhrase grp = this.ATPhraseLoader.GetPhraseByID(b1, b2, b3);
                                    string GroupName = "Unknown Group";

                                    if (grp != null)
                                        GroupName = grp.value;

                                    if (tsmi_atp[b2] == null)
                                    {
                                        tsmi_atp[b2] = new ToolStripMenuItem[1];
                                        if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                            (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                            tsmi_atp[b2][tsmi_cnt] = new ToolStripMenuItem(GroupName, Icons.GeneralIcon[b2]);
                                        else tsmi_atp[b2][tsmi_cnt] = new ToolStripMenuItem(GroupName);
                                    }
                                    else
                                    {
                                        for (tsmi_cnt = 0; tsmi_cnt < tsmi_atp[b2].Length; tsmi_cnt++)
                                        {
                                            if (GroupName == tsmi_atp[b2][tsmi_cnt].Text)
                                                break;
                                        }
                                        if (tsmi_cnt == tsmi_atp[b2].Length)
                                        {
                                            Array.Resize(ref tsmi_atp[b2], tsmi_atp[b2].Length + 1);
                                            if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                                (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                                tsmi_atp[b2][tsmi_cnt] = new ToolStripMenuItem(GroupName, Icons.GeneralIcon[b2]);
                                            else tsmi_atp[b2][tsmi_cnt] = new ToolStripMenuItem(GroupName);
                                        }
                                    }
                                    newitem = new ToolStripMenuItem(ItemName, null, ContextAT_Click);
                                    if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                        (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                        newitem.Image = Icons.GeneralIcon[b2];
                                    newitem.Name = atp[i].ToString();

                                    tsmi_atp[b2][tsmi_cnt].DropDownItems.Add(newitem);
                                }
                                #endregion
                                #region DONE: ContextMenu: Build Basic Menu if atp.Length <= MAX_MENUITEMS
                                else
                                {
                                    // Build ContextMenu
                                    if (tsmi[b2] == null)
                                        tsmi[b2] = new ToolStripItem[1];
                                    else Array.Resize(ref tsmi[b2], tsmi[b2].Length + 1);
                                    int Index = tsmi[b2].Length - 1;
                                    tsmi[b2][Index] = new ToolStripMenuItem(ItemName, null, ContextAT_Click);
                                    tsmi[b2][Index].Name = atp[i].ToString();

                                    if (atp[i].StringResource == 0x07)
                                    {
                                        tsmi[b2][Index].ForeColor = Icons.ItemColor;
                                        if (atp[i].Type == 4)
                                        //if ((atp[i].ResourceID >= 10000) && (atp[i].ResourceID <= 20000))
                                        {
                                            if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                                (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                                tsmi[b2][Index].Image = Icons.WeaponIcon[b2];
                                            else tsmi[b2][Index].Image = Icons.WeaponIcon[0];
                                        }
                                        else if (atp[i].Type == 5)
                                        //else if ((atp[i].ResourceID >= 50000) && (atp[i].ResourceID <= 60000))
                                        {
                                            if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                                (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                                tsmi[b2][Index].Image = Icons.ArmorIcon[b2];
                                            else tsmi[b2][Index].Image = Icons.ArmorIcon[0];
                                        }
                                        else if (atp[i].Type == 13)
                                        //else if (atp[i].BaseID >= 0x2000)
                                        {
                                            if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                                (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                                tsmi[b2][Index].Image = Icons.PuppetIcon[b2];
                                            else tsmi[b2][Index].Image = Icons.PuppetIcon[0];
                                        }
                                        //else tsmi[Index].Image = Preferences.ItemIcon;
                                        else
                                        {
                                            if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                                (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                                tsmi[b2][Index].Image = Icons.ItemIcon[b2];
                                            else tsmi[b2][Index].Image = Icons.ItemIcon[0];
                                        }
                                    }
                                    else if (atp[i].StringResource == 0x13)
                                    {
                                        tsmi[b2][Index].ForeColor = Icons.KeyItemColor;
                                        if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                            (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                            tsmi[b2][Index].Image = Icons.KeyItemIcon[b2];
                                        else tsmi[b2][Index].Image = Icons.KeyItemIcon[0];
                                    }
                                    else if ((b2 >= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN) &&
                                            (b2 <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX))
                                    {
                                        tsmi[b2][Index].Image = Icons.GeneralIcon[b2];
                                    }
                                }
                                #endregion
                            }
                            #endregion
                            cms.SuspendLayout();
                            //cms.ShowImageMargin = false;
                            //cms.ShowCheckMargin = false;
                            if (header != null)
                                cms.Items.AddRange(header);
                            if (Preferences.Language == FFXIATPhraseLoader.ffxiLanguages.LANG_ALL)
                            {
                                #region If user opted to load all, we need to show appropriate selections
                                for (int x = FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MIN; x <= FFXIATPhraseLoader.ffxiLanguages.NUM_LANG_MAX; x++)
                                {
                                    if (atp.Length > MAX_MENUITEMS)
                                    {
                                        #region If total in the whole list is greater than MAX_MENUITEMS, group by language
                                        if (tsmi_atp[x] != null)
                                            languages[x].DropDownItems.AddRange(tsmi_atp[x]);
                                        if (items[x] != null)
                                        {
                                            if (weapons[x] != null)
                                                items[x].DropDownItems.Add(weapons[x]);
                                            if (armor != null)
                                                items[x].DropDownItems.Add(armor[x]);
                                            if (puppet != null)
                                                items[x].DropDownItems.Add(puppet[x]);
                                            if (objects != null)
                                                items[x].DropDownItems.Add(objects[x]);
                                            languages[x].DropDownItems.Add(items[x]);
                                        }
                                        if (keyitems[x] != null)
                                            languages[x].DropDownItems.Add(keyitems[x]);
                                        if (tsmi[x] != null)
                                            languages[x].DropDownItems.AddRange(tsmi[x]);
                                        cms.Items.Add(languages[x]);
                                        #endregion
                                    }
                                    else
                                    {
                                        #region Else, total in list is less than MAX_MENUITEMS, DO NOT group by language
                                        if (tsmi_atp[x] != null)
                                            cms.Items.AddRange(tsmi_atp[x]);
                                        if (items[x] != null)
                                        {
                                            if (weapons[x] != null)
                                                items[x].DropDownItems.Add(weapons[x]);
                                            if (armor[x] != null)
                                                items[x].DropDownItems.Add(armor[x]);
                                            if (puppet[x] != null)
                                                items[x].DropDownItems.Add(puppet[x]);
                                            if (objects[x] != null)
                                                items[x].DropDownItems.Add(objects[x]);
                                            cms.Items.Add(items[x]);
                                        }
                                        if (keyitems[x] != null)
                                            cms.Items.Add(keyitems[x]);
                                        if (tsmi[x] != null)
                                            cms.Items.AddRange(tsmi[x]);
                                        #endregion
                                    }
                                }
                                #endregion
                            }
                            else
                            {
                                #region If user only wanted one language loaded, show it's fields
                                if (tsmi_atp[Preferences.Language] != null)
                                    cms.Items.AddRange(tsmi_atp[Preferences.Language]);
                                if (items[Preferences.Language] != null)
                                {
                                    if (weapons[Preferences.Language] != null)
                                        items[Preferences.Language].DropDownItems.Add(weapons[Preferences.Language]);
                                    if (armor[Preferences.Language] != null)
                                        items[Preferences.Language].DropDownItems.Add(armor[Preferences.Language]);
                                    if (puppet[Preferences.Language] != null)
                                        items[Preferences.Language].DropDownItems.Add(puppet[Preferences.Language]);
                                    if (objects[Preferences.Language] != null)
                                        items[Preferences.Language].DropDownItems.Add(objects[Preferences.Language]);
                                    cms.Items.Add(items[Preferences.Language]);
                                }
                                if (keyitems[Preferences.Language] != null)
                                    cms.Items.Add(keyitems[Preferences.Language]);
                                if (tsmi[Preferences.Language] != null)
                                    cms.Items.AddRange(tsmi[Preferences.Language]);
                                #endregion
                            }
                            cms.ResumeLayout();
                            p = src.GetPositionFromCharIndex(my_sel.start);
                            if (modified)
                            {
                                lock (src.Text)
                                {
                                    src.Text = srcText;
                                }
                            }
                            src.Select(my_sel.start, my_sel.length);
                            caller = src;
                            this.SuppressNodeUpdates = false;
                            cms.Show(src, p, ToolStripDropDownDirection.AboveRight);// cms.Show(textBoxLine1.c
                            #endregion
                        }
                    }
                    #endregion
                }
            }
        }

        private void textBoxName_TextChanged(object sender, EventArgs e)
        {
            if (sender is TabTextBox)
            {
                if ((TabTextBox)sender == textBoxName)
                {
                    LogMessage.Log("textBoxName_TextChanged BEGIN: textboxName is sender, selectedNode: {0}", (this.treeView.SelectedNode == null) ? "<NULL>" : this.treeView.SelectedNode.Text);
                    if (!SuppressNodeUpdates)
                    {
                        LogMessage.Log("..textBoxName_TextChanged :SuppressNodeUpdates is false");
                        int i = 0;
                        CMacro cm = FindMacroByNode(this.treeView.SelectedNode);
                        CMacroFile cmf = FindMacroFileByNode(this.treeView.SelectedNode);

                        if ((cm == null) && (cmf == null)) // should NOT happen
                            return;

                        for (i = 0; i < buttons.Length; i++)
                            if (buttons[i].Enabled == false)
                                break;

                        if ((cmf == null) && (cm != null)) // Should NOT happen, but just in case
                        {
                            for (int c = 0; c < MacroFiles.Count; c++)
                            {
                                for (int x = 0; x < 20; x++)
                                {
                                    if (MacroFiles[c].Macros[x] == cm)
                                    {
                                        cmf = MacroFiles[c];
                                        break;
                                    }
                                }
                                if (cmf != null)
                                    break;
                            }
                        }
                        if ((cm == null) && (cmf != null) && (i < buttons.Length))
                        {
                            cm = cmf.Macros[i];
                        }
                        if ((cm != null) && (cmf != null) && (i < buttons.Length))
                        {
                            LogMessage.Log("..textBoxName_TextChanged: cm != null i:{0} Text Changed from '{1}' to '{2}'",
                                i, cm.thisNode.Text, textBoxName.Text);
                            cm.Name = textBoxName.Text;
                            lock (NodeUpdatesToDo)
                            {
                                //cm.thisNode.Text = cm.DisplayName();
                                int nutcnt = 0;
                                for (nutcnt = 0; nutcnt < NodeUpdatesToDo.Length; nutcnt++)
                                {
                                    if (NodeUpdatesToDo[nutcnt].Object1 == cm.thisNode)
                                        break;
                                }
                                if (nutcnt >= NodeUpdatesToDo.Length)
                                {
                                    Array.Resize(ref NodeUpdatesToDo, NodeUpdatesToDo.Length + 1);
                                    nutcnt = NodeUpdatesToDo.Length - 1;
                                    NodeUpdatesToDo[nutcnt] = new TagInfo(String.Empty, cm.thisNode);
                                }
                                NodeUpdatesToDo[nutcnt].Text = cm.DisplayName();
                            }
                            buttons[i].Text = cm.DisplayName();
                            cmf.Changed = true;
                            this.labelCurrentEditing.Text = String.Format("Currently Editing Macro Set ~~ {0}{1} : Name : <{2}>",
                                (cm.MacroNumber < 10) ? "Ctrl-" : "Alt-", (cm.MacroNumber + 1) % 10, (cm.Name.Trim() == String.Empty) ? "<Empty>" : cm.Name);

                        }
                        else
                        {
                            this.labelCurrentEditing.Text = String.Empty;
                            LogMessage.Log("..textBoxName_TextChanged: cm is null? {0} cmf is null? {1}",
                                (cm == null), (cmf == null));
                        }
                    }
                    LogMessage.Log("textBoxName_TextChanged END: textboxName is sender, selectedNode: {0}", (this.treeView.SelectedNode == null) ? "<NULL>" : this.treeView.SelectedNode.Text);
                }
            }
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            if (sender is TabTextBox)
            {
                if ((TabTextBox)sender != textBoxName)
                {
                    if (!SuppressNodeUpdates)
                    {
                        CMacroFile cmf = FindMacroFileByNode(this.treeView.SelectedNode);
                        if (cmf != null)
                        {
                            cmf.Changed = true;
                        }
                    }
                }
            }
        }

        private void textBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (sender is TabTextBox)
            {
                TabTextBox ttb = sender as TabTextBox;
                ttb.Focus();
                if (ttb.ContextMenuStrip != null)
                {
                    #region Handle the Undo menu item
                    if (ttb.CanUndo)
                    {
                        ttb.ContextMenuStrip.Items["undoToolStripMenuItem"].Enabled = true;
                    }
                    else ttb.ContextMenuStrip.Items["undoToolStripMenuItem"].Enabled = false;
                    #endregion

                    #region Handle the Cut, Copy, Paste, and Delete menu items.
                    if (Clipboard.ContainsText())
                    {
                        ttb.ContextMenuStrip.Items["pasteTextToolStripMenuItem"].Enabled = true;
                    }
                    else ttb.ContextMenuStrip.Items["pasteTextToolStripMenuItem"].Enabled = false;

                    if (ttb.SelectedText != String.Empty)
                    {
                        ttb.ContextMenuStrip.Items["cutTextToolStripMenuItem"].Enabled = true;
                        ttb.ContextMenuStrip.Items["copyTextToolStripMenuItem"].Enabled = true;
                        ttb.ContextMenuStrip.Items["deleteTextToolStripMenuItem"].Enabled = true;
                    }
                    else
                    {
                        ttb.ContextMenuStrip.Items["cutTextToolStripMenuItem"].Enabled = false;
                        ttb.ContextMenuStrip.Items["copyTextToolStripMenuItem"].Enabled = false;
                        ttb.ContextMenuStrip.Items["deleteTextToolStripMenuItem"].Enabled = false;
                    }
                    #endregion

                    #region Handle the Save Changes and Reload This Items
                    CMacroFile cmf = FindMacroFileByNode(this.treeView.SelectedNode);
                    ToolStripItem ts = ttb.ContextMenuStrip.Items["saveCurrentToolStripContextMenuItem"];
                    ToolStripItem rs = ttb.ContextMenuStrip.Items["reloadCurrentToolStripMenuItem"];
                    if (cmf != null)
                    {
                        rs.Enabled = true;
                        ts.Enabled = cmf.Changed;

                        String text = String.Format(" \'{0}\'", cmf.thisNode.Text.Trim('*'));

                        ts.Text = "Save" + text;
                        rs.Text = "Reload" + text;
                        ts.Tag = (object)cmf.thisNode;
                        rs.Tag = (object)cmf.thisNode;
                    }
                    else
                    {
                        ts.Tag = null;
                        rs.Tag = null;
                        ts.Text = "Unable to Save Changes";
                        rs.Text = "Unable to Reload This";
                        ts.Enabled = false;
                        rs.Enabled = false;
                    }
                    #endregion

                    #region Handle the final separator, AT Phrase Menu, and Special Menu
                    ToolStripItem[] separator = ttb.ContextMenuStrip.Items.Find("ContextSeparator", true);
                    ToolStripItem[] atpstrip = ttb.ContextMenuStrip.Items.Find("ATPhraseStrip", true);
                    ToolStripItem[] specials = ttb.ContextMenuStrip.Items.Find("Specials", true);

                    caller = ttb;

                    if (ttb != textBoxName)
                    {
                        #region if ttb is a TextBoxLine
                        #region Add Separator or Make it Visible
                        // if both AtphraseStrip and Specials is null, we don't need a separator
                        if (((separator == null) || (separator.Length < 1)) && ((this.ATPhraseStrip != null) || (this.Specials != null)))
                        {
                            // Add separator for LineBoxes
                            // Include a Separator
                            ToolStripItem separatorItem = new ToolStripSeparator();
                            // Name it as well so I don't repeat it
                            separatorItem.Name = "ContextSeparator";
                            ttb.ContextMenuStrip.Items.Add(separatorItem);
                        }
                        else separator[0].Visible = true;
                        #endregion

                        #region Add Special Character Menu or Make it Visible
                        // Handle Special Character Sub-Menu
                        if ((specials == null) || (specials.Length < 1))
                        {
                            if ((this.Specials != null) && (this.Specials.Items.Count > 0))
                            {
                                // specials not found/non-existant, create a new array
                                specials = new ToolStripItem[this.Specials.Items.Count];
                                // copy the items over
                                this.Specials.Items.CopyTo(specials, 0);
                                specials[0].Visible = false; // header
                                if (Specials.Items.Count > 1)
                                    specials[1].Visible = false; // separator
                                // create the drop-down menu tag with the text of the first label from the Specials Menu
                                ToolStripMenuItem specialcharsMenu = new ToolStripMenuItem(specials[0].Text);
                                // Name it so I can Find() it later
                                specialcharsMenu.Name = "Specials";
                                Bitmap bmp = new Bitmap(32, 32);
                                Graphics g = Graphics.FromImage(bmp);
                                g.DrawString("\u221E", new Font(specialcharsMenu.Font.FontFamily, 20.0f, FontStyle.Bold), new SolidBrush(Color.Black), -7.5f, -2.5f);
                                specialcharsMenu.Image = bmp;
                                // Add the array as a drop-down list
                                specialcharsMenu.DropDownItems.AddRange(specials);

                                // Add the specialcharsMenu to the ContextMenuStrip
                                ttb.ContextMenuStrip.Items.Add(specialcharsMenu);
                            }
                        }
                        else
                        {
                            // if found, set to visible
                            specials[0].Visible = true;
                        }
                        #endregion

                        #region Add Auto-Translate Phrase Sub-Menu or make it visible
                        if ((atpstrip == null) || (atpstrip.Length < 1)) // not found
                        {
                            #region Create if and only if the ATPhraseStrip exists
                            if ((this.backupPhraseStrip != null) && (this.backupPhraseStrip.Items.Count > 0)) // copy PhraseStrip context menu
                            {
                                // Create new menu array
                                atpstrip = new ToolStripItem[this.backupPhraseStrip.Items.Count];
                                // Copy all items to array.
                                this.backupPhraseStrip.Items.CopyTo(atpstrip, 0);
                                // Make header Invisible no matter what.
                                atpstrip[0].Visible = false;
                                if (this.backupPhraseStrip.Items.Count > 1)
                                    atpstrip[1].Visible = false;

                                // Sub-Menu title pulled from the first label in the atpstrip
                                ToolStripMenuItem atp = new ToolStripMenuItem(atpstrip[0].Text);
                                if (Preferences.Language == FFXIATPhraseLoader.ffxiLanguages.LANG_ALL)
                                {
                                    Bitmap bmp = new Bitmap(32, 32);
                                    Graphics g = Graphics.FromImage(bmp);
                                    g.DrawString("\u2202", new Font(atp.Font.FontFamily, 20.0f, FontStyle.Bold), new SolidBrush(Color.Black), -7.5f, -2.5f);
                                    atp.Image = bmp;
                                }
                                else atp.Image = Icons.GeneralIcon[Preferences.Language];

                                //.DropDownItems = this.ATPhraseStrip.Items;
                                // make the list (atpstrip) a range under this as a Drop-Down menu
                                //atp.DropDownItems.AddRange(this.ATPhraseStrip.Items.); // Range(atpstrip);
                                atp.DropDownItems.AddRange(atpstrip);
                                // name it so I can make sure I don't add it twice
                                atp.Name = "ATPhraseStrip";
                                ttb.ContextMenuStrip.Items.Add(atp);
                            }
                            #endregion
                        }
                        else
                        {
                            atpstrip[0].Visible = true;
                        }
                        #endregion
                        #endregion
                    }
                    else // TabTextBox (Name only)  block phrase menu, separator, and specials
                    {
                        #region if ttb is a TextBoxName
                        if ((separator != null) && (separator.Length >= 1))
                            separator[0].Visible = false;
                        if ((atpstrip != null) && (atpstrip.Length >= 1)) // found ATPhrase Strip in ContextMenu
                            atpstrip[0].Visible = false;
                        if ((specials != null) && (specials.Length >= 1))
                            specials[0].Visible = false;
                        #endregion
                    }
                    #endregion
                }
            }
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Control ctrl = this.ActiveControl;

            if (ctrl is TabTextBox)
            {

                TabTextBox tx = ctrl as TabTextBox;

                tx.Undo();

            }
        }

        private void cutTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Control ctrl = this.ActiveControl;

            if (ctrl != null)
            {
                if (ctrl is TabTextBox)
                {
                    TabTextBox tx = ctrl as TabTextBox;
                    tx.Cut();
                }
            }
        }

        private void copyTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Control ctrl = this.ActiveControl;

            if (ctrl != null)
            {
                if (ctrl is TabTextBox)
                {
                    TabTextBox tx = ctrl as TabTextBox;
                    tx.Copy();
                }
            }
        }

        private void deleteTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Control ctrl = this.ActiveControl;

            if (ctrl != null)
            {
                if (ctrl is TabTextBox)
                {
                    TabTextBox tx = ctrl as TabTextBox;
                    if (tx.SelectedText != String.Empty)
                    {
                        int ss = tx.SelectionStart;
                        tx.Text = tx.Text.Remove(tx.SelectionStart, tx.SelectionLength);
                        tx.SelectionStart = ss;
                    }
                }
            }
        }

        private void pasteTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Control ctrl = this.ActiveControl;

            if (ctrl != null)
            {
                if (ctrl is TabTextBox)
                {
                    TabTextBox tx = ctrl as TabTextBox;
                    if (tx == textBoxName)
                    {
                        HandleSpecialPaste(tx);
                    } // it's not a special case (one of the 6 lines), do paste as normal.
                    else tx.Paste();
                }
            }
        }

        private void HandleSpecialPaste(TabTextBox tx)
        {
            if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                String s = Clipboard.GetText(TextDataFormat.UnicodeText);
                if ((s != null) && (s != String.Empty))
                {
                    s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-zA-Z0-9]", "");
                    
                    //String copy = tx.Text;
                    int ss = tx.SelectionStart;
                    int len = tx.Text.Length;
                    if (tx.SelectedText != String.Empty)
                    {
                        len -= tx.SelectionLength;
                        //copy = copy.Remove(tx.SelectionStart, tx.SelectionLength);
                        len += s.Length;
                        //copy = copy.Insert(tx.SelectionStart, s);
                    }
                    else
                    {
                        //copy = copy.Insert(tx.SelectionStart, s);
                        len += s.Length;
                    }

                    ss += s.Length;
                    int diff = 0;
                    if (len > tx.MaxLength)
                    {
                        diff = len - tx.MaxLength;
                    }

                    if (diff > s.Length)
                        s = String.Empty;
                    else if (diff > 0)
                        s = s.Substring(0, s.Length - diff);

                    if ((s.Length > 0) && (s != String.Empty))
                    {
                        tx.Paste(s);
                        if (ss > tx.Text.Length)
                            tx.SelectionStart = tx.Text.Length;
                        else tx.SelectionStart = ss;
                    }

                    tx.SelectionLength = 0;
                }
                else tx.Paste();
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Control ctrl = this.ActiveControl;
            if (sender is TabTextBox)
            {
                LogMessage.Log("Sender is still TabTextBox");
            }
            else LogMessage.Log("Sender is NOT TabTextBox");

            if (ctrl != null)
            {
                if (ctrl is TabTextBox)
                {
                    TabTextBox tx = ctrl as TabTextBox;
                    tx.SelectAll();
                }
            }
        }
        #endregion
        #endregion

        #region MainForm Methods (Button Clicked/Enabled Changed Event Handlers)
        private void button_Click(object sender, EventArgs e)
        {
            if (sender is Button)
            {
                Button b = sender as Button;
                CMacroFile cmf = FindMacroFileByNode(this.treeView.SelectedNode);

                if (cmf == null)
                    return;
                int i;
                for (i = 0; i < buttons.Length; i++)
                    if (b == buttons[i])
                        break;
                this.treeView.SelectedNode = cmf.Macros[i].thisNode;
            }
        }
        private void button_EnabledChanged(object sender, EventArgs e)
        {
            if (sender is Button)
            {
                Button b = sender as Button;
            }
        }
        #endregion

        #region MainForm Methods (Button OnClick Event Handlers for the 4 Language Buttons)
        private void usa_Click(object sender, EventArgs e)
        {
            Preferences.Program_Language = FFXIATPhraseLoader.ffxiLanguages.LANG_ENGLISH;
            usa.BackgroundImage = Icons.UsaDisabled;
            usa.Enabled = false;
            japan.BackgroundImage = Icons.JapanEnabled;
            japan.Enabled = true;
            deutsch.BackgroundImage = Icons.DeutschEnabled;
            deutsch.Enabled = true;
            france.BackgroundImage = Icons.FranceEnabled;
            france.Enabled = true;
        }

        private void japan_Click(object sender, EventArgs e)
        {
            Preferences.Program_Language = FFXIATPhraseLoader.ffxiLanguages.LANG_JAPANESE;
            japan.BackgroundImage = Icons.JapanDisabled;
            japan.Enabled = false;
            usa.BackgroundImage = Icons.UsaEnabled;
            usa.Enabled = true;
            deutsch.BackgroundImage = Icons.DeutschEnabled;
            deutsch.Enabled = true;
            france.BackgroundImage = Icons.FranceEnabled;
            france.Enabled = true;
        }

        private void deutsch_Click(object sender, EventArgs e)
        {
            Preferences.Program_Language = FFXIATPhraseLoader.ffxiLanguages.LANG_DEUTSCH;
            deutsch.BackgroundImage = Icons.DeutschDisabled;
            deutsch.Enabled = false;
            japan.BackgroundImage = Icons.JapanEnabled;
            japan.Enabled = true;
            usa.BackgroundImage = Icons.UsaEnabled;
            usa.Enabled = true;
            france.BackgroundImage = Icons.FranceEnabled;
            france.Enabled = true;
        }

        private void france_Click(object sender, EventArgs e)
        {
            Preferences.Program_Language = FFXIATPhraseLoader.ffxiLanguages.LANG_FRENCH;
            france.BackgroundImage = Icons.FranceDisabled;
            france.Enabled = false;
            japan.BackgroundImage = Icons.JapanEnabled;
            japan.Enabled = true;
            usa.BackgroundImage = Icons.UsaEnabled;
            usa.Enabled = true;
            deutsch.BackgroundImage = Icons.DeutschEnabled;
            deutsch.Enabled = true;
        }
        #endregion

        #region MainForm Methods (Label Event Handlers)
        void labelName_DoubleClick(object sender, System.EventArgs e)
        {
            textBoxName.Focus();
            textBoxName.SelectAll();
        }
        void label1_DoubleClick(object sender, System.EventArgs e)
        {
            textBoxLine1.Focus();
            textBoxLine1.SelectAll();
        }
        void label2_DoubleClick(object sender, System.EventArgs e)
        {
            textBoxLine2.Focus();
            textBoxLine2.SelectAll();
        }
        void label3_DoubleClick(object sender, System.EventArgs e)
        {
            textBoxLine3.Focus();
            textBoxLine3.SelectAll();
        }
        void label4_DoubleClick(object sender, System.EventArgs e)
        {
            textBoxLine4.Focus();
            textBoxLine4.SelectAll();
        }
        void label5_DoubleClick(object sender, System.EventArgs e)
        {
            textBoxLine5.Focus();
            textBoxLine5.SelectAll();
        }
        void label6_DoubleClick(object sender, System.EventArgs e)
        {
            textBoxLine6.Focus();
            textBoxLine6.SelectAll();
        }
        #endregion
        #endregion

        #region MainForm Constructor : DONE
        public MainForm()
        {
            InitializeComponent();
            LogMessage.Initialize();
            timer.Interval = 250;
            timer.Tick += new EventHandler(timer_Tick);

            // Get all Product attributes on this assembly
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);

            // If there aren't any Product attributes, return an empty string
            if (attributes.Length == 0)
                this.aboutToolStripMenuItem.Text = "About Yekyaa's FFXI ME! v2";
            // If there is a Product attribute, return its value
            else this.aboutToolStripMenuItem.Text = "About " + ((AssemblyProductAttribute)attributes[0]).Product;

            this.imageListForTreeView.Images.Add("ClosedFolder", Resources.ClosedFolder);
            this.imageListForTreeView.Images.Add("OpenFolder", Resources.OpenFolder);
            this.imageListForTreeView.Images.Add("CharFolderClosed", Resources.CharFolderClosed);
            this.imageListForTreeView.Images.Add("CharFolderOpen", Resources.CharFolderOpen);
            this.imageListForTreeView.Images.Add("ClosedBook", Resources.Book_angleHS);
            this.imageListForTreeView.Images.Add("OpenBook", Resources.Book_openHS);
            this.imageListForTreeView.Images.Add("Macrofile", Resources.Macrofile);
            this.imageListForTreeView.Images.Add("EditMacrofile", Resources.MacrofileEdit);
            this.imageListForTreeView.Images.Add("Bars", Resources.Bars);
            this.imageListForTreeView.Images.Add("Macro", Resources.Macro);
            this.imageListForTreeView.Images.Add("EditMacro", Resources.MacroEdit);
        }
        #endregion

        #region Additional classes used by MainForm (TagInfo, MenuCompare, TabTextBox, start_len)
        /// <summary>
        /// Class used to automatically select a word based on where the Caret is in the TextBox.
        /// </summary>
        private class start_len : Object
        {
            public int start;
            public int length;

            public start_len()
            {
                start = 0;
                length = 0;
            }
            public start_len(int start_pos, int len)
            {
                start = start_pos;
                length = len;
            }
            public override string ToString()
            {
                return String.Format("start: {0}, length: {1}", this.start, this.length);
            }
        }

        public class TabTextBox : TextBox
        {
            protected override bool IsInputKey(Keys key)
            {
                if (key == Keys.Tab)
                    return true;
                return base.IsInputKey(key);
            }
        }

        /// <summary>
        /// TagInfo is designed to hold generic information in a single object.
        /// </summary>
        public class TagInfo : Object
        {
            #region TagInfo Variables
            String _Text;
            String _Type;
            Object _Obj1;
            Object _Obj2;
            #endregion

            #region TagInfo Properties
            public Object Object1
            {
                get { return _Obj1; }
                set { _Obj1 = value; }
            }
            public Object Object2
            {
                get { return _Obj2; }
                set { _Obj2 = value; }
            }
            public String Type
            {
                get { return _Type; }
                set { if (_Type != value) _Type = value; }
            }
            public String Text
            {
                get { return _Text; }
                set { if (_Text != value) _Text = value; }
            }
            #endregion

            #region TagInfo Methods
            public override string ToString()
            {
                if (this._Type == "Overwrite_TTL")
                {
                    CBook cbold = this._Obj1 as CBook;
                    CBook cbnew = this._Obj2 as CBook;
                    return String.Format("Overwrite destination book {0} with source book {1}?",
                        Utilitiies.EllipsifyPath(cbnew.fName, 60), Utilitiies.EllipsifyPath(cbold.fName, 60));
                }
                else if (this._Type == "Copy_TTL")
                {
                    CBook cbold = this._Obj1 as CBook;
                    //CBook cbnew = this._Obj2 as CBook;
                    String path = this._Text;
                    return String.Format("Copy source book {0} to destination directory {1}?",
                        Utilitiies.EllipsifyPath(cbold.fName, 60), Utilitiies.EllipsifyPath(this._Text, 60));
                }
                else if (this._Type == "Save_File")
                {
                    CMacroFile cmf = this._Obj1 as CMacroFile;
                    return String.Format("Save changes to macrofile {0}?",
                        (cmf != null) ? Utilitiies.EllipsifyPath(cmf.fName, 60) : "<<Unknown>>");
                }
                else if (this._Type == "Save_TTL")
                {
                    CBook cb = this._Obj1 as CBook;
                    return String.Format("Save changes to book file {0}?",
                        (cb == null) ? "<<Unknown>>" : Utilitiies.EllipsifyPath(cb.fName, 60));
                }
                else if (this._Type == "Delete_Folder")
                {
                    return String.Format("Delete folder {0} and all subfolders and files?",
                        Utilitiies.EllipsifyPath(this._Text, 60));
                }
                else if (this._Type == "Delete_File")
                {
                    return String.Format("Delete file {0}?", Utilitiies.EllipsifyPath(this._Text, 60));
                }
                return base.ToString();
            }

            static public TagInfo GetTagInfo(Object obj)
            {
                if (obj is TagInfo)
                    return (obj as TagInfo);
                else return (new TagInfo());
            }
            #endregion

            #region TagInfo Constructor
            public TagInfo(String type, String text, Object obj1, Object obj2)
                : this(type, obj1, obj2)
            {
                this._Text = text;
            }
            public TagInfo(String type, Object obj1, Object obj2)
                : this(type, obj1)
            {
                this._Obj2 = obj2;
            }
            public TagInfo(String type, Object obj1)
                : this()
            {
                this._Type = type;
                this._Obj1 = obj1;
            }
            public TagInfo(String type, String text, Object obj1)
                : this(type, obj1)
            {
                this._Text = text;
            }
            public TagInfo(String type, String text)
                : this()
            {
                this._Type = type;
                this._Text = text;
            }
            public TagInfo(String type): this()
            {
                this._Type = type;
            }
            public TagInfo(TagInfo ti)
            {
                this._Type = ti.Type;
                this._Text = ti.Text;
                this._Obj1 = ti.Object1;
                this._Obj2 = ti.Object2;
            }
            public TagInfo()
            {
                this._Text = String.Empty;
                this._Type = String.Empty;
                this._Obj1 = null;
                this._Obj2 = null;
            }
            #endregion
        }

        /// <summary>
        /// IComparer-style interface for alphabetizing the Context Menus.
        /// </summary>
        public class MenuCompare : System.Collections.IComparer
        {
            int System.Collections.IComparer.Compare(Object x, Object y)
            {
                ToolStripMenuItem xs = x as ToolStripMenuItem;
                ToolStripMenuItem ys = y as ToolStripMenuItem;
                return (String.Compare(xs.Text, ys.Text, true, System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        #endregion

        private void SaveAllMacroFilesMenuItem_Click(object sender, EventArgs e)
        {
            SaveAllMacroSets(false, false);
        }
    }

}
