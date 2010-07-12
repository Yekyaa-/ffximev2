using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Yekyaa.FFXIEncoding;

namespace FFXI_ME_v2
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Class that contains all information for a single Book.
        /// </summary>
        public class CBook : Object
        {
            #region CBook Variables
            /// <summary>
            /// Filename for this book.
            /// </summary>
            private String _fName;
            private String[] _bookName;
            private bool _changed;
            private bool _deleted;
            #endregion

            #region CBook Properties
            public bool IsDeleted
            {
                get { return _deleted; }
            }

            public bool Changed
            {
                get { return this._changed; }
                set { this._changed = value; }
            }

            public String fName
            {
                get { return this._fName; }
                set { this._fName = value; }
            }
            #endregion

            #region CBook Methods
            public void Delete()
            {
                this._deleted = true;
            }

            public void Restore()
            {
                this._deleted = false;
            }

            public override string ToString()
            {
                return Utilitiies.EllipsifyPath(_fName, 45);
            }

            public void Load(String filename)
            {
                _fName = filename;
                if (File.Exists(filename))
                {
                    FileStream fs = null;
                    try
                    {
                        fs = File.Open(filename, FileMode.Open, FileAccess.Read);
                        this.Load(fs);
                    }
                    catch
                    {
                    }
                    if (fs != null)
                        fs.Close();
                }
                else
                {
                    if (_bookName == null)
                        _bookName = new String[(int)(Preferences.Max_Macro_Sets / 10)];
                    for (int i = 0; i < _bookName.Length; i++)
                    {
                        _bookName[i] = String.Format("Book{0:D2}", i + 1);
                    }
                }
                this._changed = false;
            }

            public void Load(FileStream fs)
            {
                BinaryReader br_fi = null;
                if (this._bookName == null)
                    this._bookName = new String[(int)(Preferences.Max_Macro_Sets / 10)];
                try
                {
                    br_fi = new BinaryReader(fs);
                    byte[] test_string;
                    StringBuilder test_str = new StringBuilder(23);
                    br_fi.BaseStream.Position = 0x18;
                    for (int i = 0; i < _bookName.Length; i++)
                    {
                        if (br_fi.BaseStream.Position < br_fi.BaseStream.Length)
                        {
                            test_str.Remove(0, test_str.Length);

                            test_string = br_fi.ReadBytes(16);
                            foreach (byte b in test_string)
                            {
                                if (b == 0x00)
                                    break;
                                test_str.Append((char)b);
                            }
                            _bookName[i] = test_str.ToString(0, test_str.Length).Trim('\0');
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    if (br_fi != null)
                        br_fi.Close();
                    this._changed = false;
                }
            }

            public bool Save()
            {
                return this.Save(this._fName);
            }

            public bool Save(String filename)
            {
                bool Error = false;
                // if filename is not empty
                if (filename != String.Empty)
                {
                    BinaryWriter br_fi = null;
                    byte[] tbyte = new byte[320];
                    try
                    {
                        // GetDirectoryName throws PathTooLongException to exit from here safely
                        String dirName = Path.GetDirectoryName(filename);

                        if ((dirName != null) && !Directory.Exists(dirName))
                        {
                            Directory.CreateDirectory(dirName);
                        }

                        br_fi = new BinaryWriter(File.Open(filename, FileMode.Create, FileAccess.Write));

                        for (int s = 0; s < _bookName.Length; s++)
                        {
                            String x = _bookName[s];
                            int location = s * 16;

                            if ((x.Length == 0) || (x == String.Empty))
                            {
                                for (int i = 0; i < 16; i++)
                                    tbyte[location + i] = 0x00;
                            }
                            else
                            {
                                int i = 0;
                                for (i = 0; (i < x.Length) && (i < 16); i++)
                                {
                                    tbyte[location + i] = (byte)x[i];
                                }
                                tbyte[location + 15] = 0x00;
                                if (i < 16)
                                {
                                    for (; i < 16; i++)
                                        tbyte[location + i] = 0x00;
                                }
                            }
                        }
                        MD5 md5 = new MD5CryptoServiceProvider();
                        byte[] hash = md5.ComputeHash(tbyte);
                        br_fi.Write((ulong)1);
                        br_fi.Write(hash, 0, 16);
                        br_fi.Write(tbyte, 0, 320);
                        this._changed = false;
                    }
                    catch (PathTooLongException e)
                    {
                        LogMessage.LogF("... BookSave(): Saving {0} encountered an error -- {1}", filename, e.Message);
                        Error = true;
                    }
                    catch (Exception e)
                    {
                        LogMessage.LogF("... BookSave(): Saving {0} encountered an unexpected error -- {1}", filename, e.Message);
                        Error = true;
                    }
                    if (br_fi != null)
                        br_fi.Close();
                }
                else Error = true;
                return Error;
            }

            public void CopyFrom(CBook cb)
            {
                if (this._bookName == null)
                    this._bookName = new String[(int)(Preferences.Max_Macro_Sets / 10)];
                for (int i = 0; i < _bookName.Length; i++)
                    this._bookName[i] = cb.GetBookName(i);
                this._changed = true;
            }

            public String GetBookName(int index)
            {
                if ((index < 0) || (index > _bookName.Length))
                    return String.Empty;
                return this._bookName[index];
            }

            public void SetBookName(int index, String s)
            {
                String tmp = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-zA-Z0-9]", "");
                if ((index < 0) || (index > _bookName.Length) || (tmp == String.Empty))
                    return;
                if (tmp.Length > 15)
                    tmp = tmp.Substring(0, 15);
                this._bookName[index] = tmp;
                this._changed = true;
            }
            #endregion

            #region CBook Constructor
            public CBook()
            {
                this._changed = false;
                this._fName = String.Empty;
                this._deleted = false;
                this._bookName = new String[(int)(Preferences.Max_Macro_Sets / 10)];
            }

            public CBook(CBook cb, String filename)
                : this()
            {
                this._fName = filename;
                this.CopyFrom(cb);
            }

            public CBook(String filename) : this()
            {
                this.Load(filename);
                if (!File.Exists(filename))
                    this._changed = true;
            }
            #endregion
        }

        /// <summary>
        /// Class that contains all information for Macros.
        /// </summary>
        public class CMacro : Object
        {
            #region CMacro Variables
            private int m_MacroNumber;
            private string[] m_Line;
            private string m_Name;
            private TreeNode _thisNode;
            #endregion

            #region CMacro Properties
            public TreeNode thisNode
            {
                get { return _thisNode; }
                set { _thisNode = value; }
            }

            /// <summary>
            /// Zero-based index into the MacroFile array for the location of this Macro.
            /// </summary>
            public int MacroNumber
            {
                get { return m_MacroNumber; }
                set { m_MacroNumber = value; }
            }
            public string Name
            {
                get { return m_Name; }
                set { m_Name = value; }
            }
            public string[] Line
            {
                get { return m_Line; }
                set { m_Line = value; }
            }
            #endregion

            #region CMacro Methods
            public void Clear()
            {
                this.m_Name = "";
                for (int i = 0; i < 6; i++)
                {
                    this.m_Line[i] = String.Empty;
                }
                this._thisNode.Text = this.DisplayName();
            }

            public override string ToString()
            {
                if (this.Name == String.Empty)
                    return CMacro.DisplayName(this.MacroNumber);
                return this.Name; // return base.ToString();
            }

            #region CMacro Methods (DisplayName() overloads)
            /// <summary>
            /// Provides the name or default string for "this" Macro.
            /// </summary>
            /// <returns>Formatted string containing either the name or a default name.</returns>
            public string DisplayName()
            {
                if (this.Name == String.Empty)
                    return CMacro.DisplayName(this.m_MacroNumber);
                return this.Name;
            }

            /// <summary>
            /// Provides the name of a Macro or default string if none found.
            /// </summary>
            /// <param name="cm">Macro file to get the displayable name for.</param>
            /// <returns>Formatted string containing either the name or a default name.</returns>
            public string DisplayName(CMacro cm)
            {
                if (cm == null)
                    return "<ERROR>";

                if (cm.Name == String.Empty)
                    return CMacro.DisplayName(cm.MacroNumber);
                return cm.Name;
            }

            /// <summary>
            /// Provides a default string for Macros given a number.
            /// </summary>
            /// <param name="num">Number to format the string with.</param>
            /// <returns>Formatted string containing a default name.</returns>
            static public string DisplayName(int num)
            {
                if ((num >= 0) && (num < 20))
                    return String.Format("{0}{1}", (num < 10) ? "Ctrl-" : "Alt-",
                                (num % 10) + 1);
                return "<ERROR>";
            }
            #endregion

            #region CMacro Copying Methods
            public bool Swap(ref CMacro src)
            {
                CMacro tmp = new CMacro();
                //this.MacroNumber = src.MacroNumber; // NEVER COPY THIS
                if (this.m_Line == null)
                    this.m_Line = new String[6];

                if (src == null)
                {
                    this.m_Name = String.Empty;
                    this.m_Line[0] = String.Empty;
                    this.m_Line[1] = String.Empty;
                    this.m_Line[2] = String.Empty;
                    this.m_Line[3] = String.Empty;
                    this.m_Line[4] = String.Empty;
                    this.m_Line[5] = String.Empty;
                }
                else
                {
                    tmp.Name = this.m_Name;
                    this.m_Name = src.Name;
                    src.Name = tmp.Name;

                    if (src._thisNode != null)
                    {
                        if (this._thisNode != null)
                        {
                            String text = src._thisNode.Text;
                            src._thisNode.Text = this._thisNode.Text;
                            this._thisNode.Text = text;
                        }
                    }
                    tmp.Line = this.Line;
                    this.Line = src.Line;
                    src.Line = tmp.Line;
                }
                return true;
            }

            public bool CopyFrom(CMacro src)
            {
                //this.MacroNumber = src.MacroNumber; // NEVER COPY THIS
                if (this.m_Line == null)
                    this.m_Line = new String[6];

                if (src == null)
                {
                    this.m_Name = String.Empty;
                    this.m_Line[0] = String.Empty;
                    this.m_Line[1] = String.Empty;
                    this.m_Line[2] = String.Empty;
                    this.m_Line[3] = String.Empty;
                    this.m_Line[4] = String.Empty;
                    this.m_Line[5] = String.Empty;
                }
                else
                {
                    this.m_Name = (src.Name == null) ? String.Empty : src.Name;

                    // this is some of the major processing on file copies
                    // remove once you have added a notification screen....
                    if (src._thisNode != null)
                    {
                        if (this._thisNode == null)
                            this._thisNode = new TreeNode(src._thisNode.Text);
                        else this._thisNode.Text = src._thisNode.Text;
                    }

                    //this._thisNode = src._thisNode; // NEVER COPY THIS

                    if (src.Line != null)
                    {
                        this.m_Line[0] = src.Line[0];
                        this.m_Line[1] = src.Line[1];
                        this.m_Line[2] = src.Line[2];
                        this.m_Line[3] = src.Line[3];
                        this.m_Line[4] = src.Line[4];
                        this.m_Line[5] = src.Line[5];
                    }
                    else
                    {
                        this.m_Line[0] = String.Empty;
                        this.m_Line[1] = String.Empty;
                        this.m_Line[2] = String.Empty;
                        this.m_Line[3] = String.Empty;
                        this.m_Line[4] = String.Empty;
                        this.m_Line[5] = String.Empty;
                    }
                }
                return true;
            }
            #endregion
            #endregion

            #region CMacro Constructors
            public CMacro()
            {
                m_Name = String.Empty;
                m_Line = new string[6];
                m_Line[0] = String.Empty;
                m_Line[1] = String.Empty;
                m_Line[2] = String.Empty;
                m_Line[3] = String.Empty;
                m_Line[4] = String.Empty;
                m_Line[5] = String.Empty;
                m_MacroNumber = 0;
            }
            #endregion
        }

        /// <summary>
        /// Class that contains all information for Macrofiles.
        /// </summary>
        public class CMacroFile : Object
        {
            #region CMacroFile Variables
            public byte[] MD5Digest;
            private UInt32 _version;
            private string _fName;
            private CMacro[] MacroList; // should be 20 of these per Macro File
            private bool _changed;
            private bool _deleted;
            private int _FileNumber;
            private TreeNode _thisNode;
            private TreeNode _ctrlNode;
            private TreeNode _altNode;
            private FFXIATPhraseLoader ATPhraseLoaderReference = null;
            private FFXIEncoding FFXIEncoding = null;
            #endregion

            #region CMacroFile Properties
            public bool IsDeleted
            {
                get { return _deleted; }
            }

            public bool Changed
            {
                get { return this._changed; }
                set
                {
                    if (this._changed != value)
                    {
                        if (value == true)
                        {
                            if (this._thisNode != null)
                            {
                                this._thisNode.ForeColor = Preferences.ShowFileChanged;
                                string s = this._thisNode.Text;
                                if ((s.Length != 0) && (s[s.Length - 1] != '*'))
                                {
                                    lock (this._thisNode.Text)
                                    {
                                        this._thisNode.Text = String.Format("{0}*", s);
                                    }
                                }
                            }
                        }
                        else if (value == false)
                        {
                            if (this._thisNode != null)
                            {
                                this._thisNode.ForeColor = Preferences.FileNotChanged;
                                string s = this._thisNode.Text;
                                if ((s.Length != 0) && (s[s.Length - 1] == '*'))
                                {
                                    lock (this._thisNode.Text)
                                    {
                                        this._thisNode.Text = s.TrimEnd('*');
                                    }
                                }
                            }
                        }
                        this._changed = value;
                    }
                }
            }

            public CMacro[] Macros
            {
                get { return MacroList; }
                set { MacroList = value; }
            }
            public TreeNode ctrlNode
            {
                get { return _ctrlNode; }
                set { _ctrlNode = value; }
            }
            public TreeNode altNode
            {
                get { return _altNode; }
                set { _altNode = value; }
            }
            public int FileNumber
            {
                get { return _FileNumber; }
                set { _FileNumber = value; }
            }
            public UInt32 version
            {
                get { return _version; }
                set { _version = value; }
            }
            public string fName
            {
                get { return _fName; }
                set { _fName = value; }
            }
            public TreeNode thisNode
            {
                get { return _thisNode; }
                set { _thisNode = value; }
            }
            #endregion

            #region CMacroFile Methods
            #region CMacroFile Delete Method
            public void Clear()
            {
                foreach (CMacro c in this.MacroList)
                    c.Clear();
                this.Changed = true;
            }

            public void Delete()
            {
                this._deleted = true;
                // remove node references, they'll be rebuilt later
                this._thisNode = null;
                this._altNode = null;
                this._ctrlNode = null;
            }
            public void Restore()
            {
                this._deleted = false;
            }
            #endregion

            #region CMacroFile Copying Methods
            public bool Swap(ref CMacroFile src)
            {
                CMacro tmp = new CMacro();
                String text = String.Empty;
                if ((this.Macros != null) && (src.Macros != null))
                {
                    for (int i = 0; i < this.Macros.Length; i++)
                    {
                        this.Macros[i].Swap(ref src.Macros[i]);
                    }
                }
                uint version = src.version;
                src.version = this.version;
                this.version = version;
                this.Changed = true;
                src.Changed = true;
                return true;
            }
            
            public bool CopyFrom(CMacroFile src)
            {
                bool reset = false;

                if (this.Macros == null)
                {
                    this.Macros = new CMacro[20];
                    for (int i = 0; i < this.Macros.Length; i++)
                        this.Macros[i] = new CMacro();
                    reset = true;
                }

                if (src != null)
                {
                    this.version = src.version;

                    if (src.Macros == null)
                    {
                        // if we haven't already reassigned for this.Macros == null
                        if (reset == false)
                        {
                            for (int i = 0; i < this.Macros.Length; i++)
                            {
                                if (this.Macros[i] == null)
                                    this.Macros[i] = new CMacro();
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < this.Macros.Length; i++)
                        {
                            // if we haven't already reassigned for this.Macros == null
                            if (reset == false)
                            {
                                if (this.Macros[i] == null)
                                    this.Macros[i] = new CMacro();
                            }
                            this.Macros[i].CopyFrom(src.Macros[i]);
                        }
                    }
                }
                //TreeNode tmpaltNode = src.altNode;
                //TreeNode tmpctrlNode = src.ctrlNode;

                //this.altNode = src.altNode;
                //this.ctrlNode = src.ctrlNode;
                //this.thisNode = src.thisNode;  // do not modify this.

                // NEVER COPY fNames because we are 
                // copying file information, not where it's at.
                // this.fName = src.fName;

                this.Changed = true;

                return true;
            }

            public override string ToString()
            {
                if (this.fName == String.Empty)
                    return "<UnknownFileName>";
                return Utilitiies.EllipsifyPath(this.fName, 50);
            }
            #endregion

            #region CMacroFile Loading Methods
            public bool Load()
            {
                return this.Load(this.fName, this.ATPhraseLoaderReference);
            }

            public bool Load(string fileName, FFXIATPhraseLoader loaderReference)
            {
                try
                {
                    if (loaderReference != null)
                    {
                        if (this.ATPhraseLoaderReference == null)
                            this.ATPhraseLoaderReference = loaderReference;
                        if (this.FFXIEncoding == null)
                            this.FFXIEncoding = loaderReference.FFXIConvert;
                    }

                    FileInfo f = new FileInfo(fileName);
                    if (!f.Exists)
                    {
                        this.Clear();
                        return false;
                    }
                    if (!IsMacroFile(f)) // Can't access IsMacroFile() from here... Switched it to static, Duh...
                        return false;
                    BinaryReader BR = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read));
                    byte[] FillerBytes = new byte[4];
                    // Read the first four bytes, should come out as an int (01 00 00 00)
                    // This is the basics of an FFXI Macro File... ugh.
                    this.version = BR.ReadUInt32();
                    if (this.version != 1)
                    {
                        BR.Close();
                        return false;
                    }
                    int first_index = -1, last_index = -1;
                    first_index = fileName.LastIndexOf('r');
                    last_index = fileName.LastIndexOf('.');
                    this.FileNumber = -1;
                    if (fileName.Contains("\\mcr") && fileName.Contains(".dat"))
                    {
                        if ((first_index != -1) && (last_index != -1))
                        {
                            string number = fileName.Substring(first_index + 1, last_index - (first_index + 1));
                            if (number == String.Empty)
                                this.FileNumber = 0;
                            else
                            {
                                try
                                {
                                    this.FileNumber = Convert.ToInt32(number, 10);
                                }
                                catch (System.FormatException)
                                {
                                    LogMessage.Log(fileName + ": Number Parsing Error (not mcr#.dat, but mcr#xxxxxx.dat");
                                    this.FileNumber = -1;
                                }
                                finally
                                {
                                    LogMessage.Log(fileName + ":" + this.FileNumber);
                                }
                            }

                        }
                    }
                    FillerBytes = BR.ReadBytes(4);
                    if (this.MD5Digest == null)
                        this.MD5Digest = new byte[16];
                    this.MD5Digest = BR.ReadBytes(16);
                    this.Changed = false; // it hasn't been updated, it's fresh.
                    // HEADER HAS BEEN READ AT THIS POINT.
                    /*Begin Reading MacroFormat 
                     * (0 filler bytes,
                     * 6 lines of 61 chars (61st is null),
                     * 9 chars (9th is null), 1 byte for null) repeat 20 times.
                     */
                    // Create CMacros Array
                    if (this.Macros == null)
                        this.Macros = new CMacro[20];
                    for (int i = 0; i < 20; i++)
                    {
                        // Read Lead Null Bytes
                        if (this.Macros[i] == null)
                            this.Macros[i] = new CMacro();
                        FillerBytes = BR.ReadBytes(4);
                        this.Macros[i].MacroNumber = i;
                        for (int x = 0; x < 6; x++)
                        {
                            if (this.ATPhraseLoaderReference == null)
                                this.Macros[i].Line[x] = this.FFXIEncoding.GetString(BR.ReadBytes(61));
                            else
                            {
                                String encoded = this.FFXIEncoding.GetString(BR.ReadBytes(61));
                                String convertedString = String.Empty;

                                for (int c = 0; c < encoded.Length; c++)
                                {
                                    if ((encoded[c] == FFXIEncoding.StartMarker) &&
                                        ((c + 9) < encoded.Length) &&
                                        (encoded[c + 9] == FFXIEncoding.EndMarker))
                                    {
                                        byte one = Convert.ToByte(String.Format("0x{0}{1}", encoded[c + 1], encoded[c + 2]), 16);
                                        byte two = Convert.ToByte(String.Format("0x{0}{1}", encoded[c + 3], encoded[c + 4]), 16);
                                        byte three = Convert.ToByte(String.Format("0x{0}{1}", encoded[c + 5], encoded[c + 6]), 16);
                                        byte four = Convert.ToByte(String.Format("0x{0}{1}", encoded[c + 7], encoded[c + 8]), 16);

                                        if (two == 0x00)
                                            two = (byte)Preferences.Language;
                                        if (Preferences.Language == FFXIATPhraseLoader.ffxiLanguages.LANG_ALL)
                                            two = (byte)Preferences.Program_Language; // default to a non-special settable

                                        FFXIATPhrase atp = ATPhraseLoaderReference.GetPhraseByID(one, two, three, four);

                                        if (atp == null)
                                        {
                                            convertedString += encoded[c];
                                        }
                                        else
                                        {
                                            convertedString += atp.ToString();
                                            c += 9;
                                        }
                                    }
                                    else convertedString += encoded[c];
                                }
                                this.Macros[i].Line[x] = convertedString;
                            }
                        }
                        this.Macros[i].Name = this.FFXIEncoding.GetString(BR.ReadBytes(9));
                        FillerBytes[0] = BR.ReadByte(); // Read last null byte
                        if (this.Macros[i].thisNode != null)
                        {
                            this.Macros[i].thisNode.Text = this.Macros[i].DisplayName();
                        }
                    }
                    BR.Close();
                }
                // If the end of the stream is reached while reading
                // the data_en, ignore the error and use the
                // default settings for the remaining values.
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
                catch (EndOfStreamException)
                {
                    return false;
                }
                this.fName = fileName;
                return true;
            }
            #endregion

            #region CMacroFile Saving Methods
            public bool Save()
            {
                return (this.Save(this.fName));
            }

            public bool Save(string fileName)
            {
                if ((fileName == null) || (fileName[0] == '\0'))
                {
                    // no filename to save it as.
                    return false;
                }

                #region "Create Data Block, Byte By Byte For MD5 Encoding"
                // Create Macro File First before I start writing anything
                byte[] data = new byte[7600];
                int cnt = 0;
                #region Loop Through All The Macros In "this" Macro Set
                for (int mcrnum = 0; mcrnum < 20; mcrnum++)
                {
                    // 4 byte null header
                    data[cnt++] = 0;
                    data[cnt++] = 0;
                    data[cnt++] = 0;
                    data[cnt++] = 0;
                    #region "Loop Through The Series Of Lines... 6 Lines (61 Bytes Each)"
                    for (int mcrline = 0; mcrline < 6; mcrline++)
                    {
                        #region "Loop Through One Line Byte By Byte For 61 Bytes"
                        byte[] s = this.FFXIEncoding.GetBytes(this.Macros[mcrnum].Line[mcrline]); //.ToCharArray());
                        for (int linecnt = 0; linecnt < 61; linecnt++)
                        {
                            if ((s.Length == 0) || ((linecnt + 1) > s.Length) || (linecnt == 60))
                                data[cnt++] = 0;
                            else
                                data[cnt++] = s[linecnt];
                        }
                        #endregion
                    }
                    #endregion
                    byte[] t = this.FFXIEncoding.GetBytes(this.Macros[mcrnum].Name);
                    #region "Loop Through The Name Byte By Byte For 9 Bytes"
                    for (int namecnt = 0; namecnt < 9; namecnt++)
                    {
                        if ((namecnt + 1) > t.Length)
                            data[cnt++] = 0;
                        else
                            data[cnt++] = t[namecnt];
                    }
                    #endregion
                    data[cnt++] = 0; // final null byte
                }
                #endregion

                if (cnt != 7600)
                {
                    LogMessage.Log("CMacroFile.Save(): Data has only " + cnt + " bytes out of 7600. Not Saving!");
                    return false;
                }
                #endregion

                #region "Create MD5 Hash From Data Block"
                // This is one implementation of the abstract class MD5.

                MD5 md5 = new MD5CryptoServiceProvider();
                this.MD5Digest = md5.ComputeHash(data);
                if (this.MD5Digest.Length != 16)
                {
                    MessageBox.Show("MD5 Hash has only " + this.MD5Digest.Length + " bytes instead of 16.");
                    return false;
                }
                #endregion

                #region "Open The Binary File For Creation/Overwriting, Proceed To Write Everything"
                FileStream fs = null;
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(fileName).TrimEnd('\\')))
                        Directory.CreateDirectory(Path.GetDirectoryName(fileName).TrimEnd('\\'));

                    fs = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite);
                }
                //catch (DirectoryNotFoundException e)
                //{
                //    String[] fPath = fileName.Split('\\');
                //    Array.Resize(ref fPath, fPath.Length - 1);
                //    String pathtocreate = String.Empty;
                //    foreach (String x in fPath)
                //        pathtocreate += x + "\\";
                //    pathtocreate = pathtocreate.Trim('\\');
                //    LogMessage.Log("{0}: {1} not found, attempting to create.", e.Message, pathtocreate);
                //    try
                //    {
                //        Directory.CreateDirectory(pathtocreate);
                //        fs = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite);
                //        LogMessage.Log("..Success");
                //    }
                //    catch (Exception ex)
                //    {
                //        fs = null;
                //        LogMessage.Log("..Failed, {0} & {1}", e.Message, ex.Message);
                //        return false;
                //    }
                //}
                catch (Exception e)
                {
                    LogMessage.Log("Error: {0}", e.Message);
                    return false;
                }
                BinaryWriter BR = null;
                if (fs != null)
                    BR = new BinaryWriter(fs);
                if ((fs != null) && (BR != null)) // Write the header to the new file
                {
                    BR.Write((ulong)1); // Write the first of eight bytes with 0x01 then 7 0x00's
                    BR.Write(this.MD5Digest, 0, 16); // Write MD5 hash
                    BR.Write(data, 0, 7600);
                    BR.Close();
                    fs.Close();
                }
                else return false;
                #endregion
                this.Changed = false;
                return true;
            }
            #endregion
            #endregion

            #region CMacroFile Constructors
            public CMacroFile(FFXIATPhraseLoader loadreference)
            {
                this.version = 0;
                this._changed = false;
                MD5Digest = new byte[16];
                if (this.Macros == null)
                    this.Macros = new CMacro[20];
                this.fName = null;
                for (int i = 0; i < 20; i++)
                {
                    this.Macros[i] = new CMacro();
                    this.Macros[i].MacroNumber = i;
                }
                this._FileNumber = 0;

                this.ATPhraseLoaderReference = loadreference;
                if (loadreference != null)
                    this.FFXIEncoding = loadreference.FFXIConvert;

                _thisNode = null;
                _ctrlNode = null;
                _altNode = null;
            }

            public CMacroFile(string fileName, FFXIATPhraseLoader loaderReference)
            {
                this.Load(fileName, loaderReference);
            }
            #endregion
        }
    }
}