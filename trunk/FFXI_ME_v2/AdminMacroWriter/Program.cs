using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Xml;

namespace AdminMacroWriter
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
        public static bool ShowDebugInfo = false;
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
        static public void Initialize()
        {
            lock (locker)
            {
                try
                {
                    DateTime dt = DateTime.Now;
                    logName = String.Format("AdminMacroWriter-{0:D4}.{1:D2}.{2:D2}.log",
                        dt.Year, dt.Month, dt.Day);
                    logfile = File.CreateText(logName);
                }
                catch
                {
                    initialized = false;
                    logfile = null;
                }
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
                            if (!LogMessage.ShowDebugInfo)
                                File.Delete(LogMessage.logName);
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
        /// <param name="force_show">True: Log the following message to file regardless of ShowDebugInfo status.
        /// False: Print to the logfile only if ShowDebugInfo is set to true (for those not so important messages).</param>
        /// <param name="logMessage">A string containing the message you want printed to the logfile.</param>
        static public void Log(String logMessage)
        {
            // force_show overrides ShowDebugInfo
            // so if ShowDebugInfo is false, it suppresses Logs
            // if force_show is true however it will not (for those REALLY important ones)

            if (!LogMessage.ShowDebugInfo)
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
                    logfile.WriteLine("{0} {1}:{2}", DateTime.Now.ToShortDateString(),
                        DateTime.Now.ToLongTimeString(), logMessage);
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
            Log(String.Format(format, args));
        }
        #endregion
    }

    class Program
    {
        // Base GetSetting() overload
        private static String GetValue(XmlDocument xmlDocument, String xPath, String defaultValue)
        {
            XmlNode xmlNode = xmlDocument.SelectSingleNode("filelist/" + xPath);
            if (xmlNode != null) { return xmlNode.InnerText; }
            else { return defaultValue; }
        }

        /// <summary>
        /// Gets a list of nodes that are below the given path.
        /// </summary>
        /// <param name="xPath">Path to search under for child nodes.</param>
        /// <returns>Array of strings with the name of each node under xmlNode xPath.</returns>
        private static List<String> GetNodeList(XmlDocument xmlDocument, String xPath)
        {
            List<String> return_list = new List<String>();
            if (xmlDocument != null)
            {
                XmlNode xmlNode;
                if (xPath == String.Empty)
                    xmlNode = xmlDocument.SelectSingleNode("filelist");
                else xmlNode = xmlDocument.SelectSingleNode("filelist/" + xPath);

                if ((xmlNode != null) && xmlNode.HasChildNodes)
                {
                    foreach (XmlNode testNode in xmlNode.ChildNodes)
                    {
                        return_list.Add(testNode.Name.ToString());
                    }
                }
            }
            return return_list;
        }

        public class StringVars
        {
            public String source;
            public String dest;
            public StringVars()
            {
                source = String.Empty;
                dest = String.Empty;
            }
            public StringVars(String s, String d)
            {
                source = s;
                dest = d;
            }
        }

        static void Main(string[] args)
        {
            XmlDocument xmlDocument = new XmlDocument();
            String pathName = String.Empty;
            bool CleanDoc = false;

            LogMessage.Initialize();
            if (!LogMessage.Initialized)
            {
                Console.WriteLine("FATAL ERROR: Unable to initialize LogMessage, Press Enter to exit");
                Console.ReadLine();
                return;
            }

            // Verify running as administrator
            if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)))
            {
                LogMessage.Log("Not running as administrator, exiting!");
                Console.WriteLine("You must be running this task as administrator, exiting.");
                LogMessage.Close();
                return;
            }

            if (args.Length < 1)
            {
                LogMessage.Log("No arguments given, exiting...");
                Console.WriteLine("No arguments given, exiting.");
                LogMessage.Close();
                return;
            }

            if (args.Length > 1)
            {
                if ((args[0] == "/debug") || (args[1] == "/debug"))
                    LogMessage.ShowDebugInfo = true;
            }

            FileInfo fi = new FileInfo(args[0]);
            if (!fi.Exists)
            {
                LogMessage.Log("{0} doesn't exist...", args[0]);
                Console.WriteLine("Specified XML file doesn't exist, exiting.");
                LogMessage.Close();
                return;
            }

            pathName = fi.FullName;

            try 
            { 
                xmlDocument.Load(pathName);
                StringWriter sw = new StringWriter();
                XmlTextWriter xmltw = new XmlTextWriter(sw);
                xmltw.Formatting = Formatting.Indented;
                xmlDocument.WriteTo(xmltw);
                xmltw.Close();
                LogMessage.Log("...Loaded XML successfully.");
                LogMessage.Log("\r\n" + sw.ToString());
            }  // file should already exist, if we fail on the Load, exit
            catch
            {
                LogMessage.Log("Unable to XML.Load() XML File");
                Console.WriteLine("Unable to load XML file, exiting.");
                LogMessage.Close();
                return;
            }

            LogMessage.Log("Processing file: {0}", pathName);

            XmlNode mainNode = xmlDocument.SelectSingleNode("filelist");
            if (mainNode != null)
            {
                try
                {
                    CleanDoc = Boolean.Parse(mainNode.Attributes["deletexml"].Value);
                    LogMessage.Log("...Delete XML is true");
                }
                catch
                {
                    CleanDoc = false;
                }
            }
            
            if (!mainNode.HasChildNodes)
            {
                LogMessage.Log("main has No Child Nodes...");
                if (CleanDoc)
                {
                    try     { File.Delete(pathName); LogMessage.Log("...deleting {0}.", pathName); }
                    catch   { LogMessage.Log("...unable to delete {0}.", pathName); }
                }
                LogMessage.Log("...exiting");
                Console.WriteLine("No Child Nodes found after main node, exiting.");
                LogMessage.Close();
                return;
            }

            XmlNodeList xnl = mainNode.ChildNodes;

            LogMessage.Log("Processing Nodes...");
            foreach (XmlNode node in xnl)
            {
                XmlAttributeCollection xac = node.Attributes;

                if (node.Name == "deletefolder")
                {
                    DirectoryInfo deletedi = new DirectoryInfo(node.InnerXml);

                    if (Directory.Exists(deletedi.FullName))
                    {
                        try     { Directory.Delete(deletedi.FullName, true); LogMessage.Log("...Delete Folder: {0}", deletedi.FullName); }
                        catch   { LogMessage.Log("...Unable to delete directory: {0}", deletedi.FullName); continue; }
                    }
                    else LogMessage.Log("...{0}: Directory doesn't exist, ignoring.", deletedi.FullName);
                }
                else if (node.Name == "deletefile")
                {
                    FileInfo deletefi = new FileInfo(node.InnerXml);
                    if (File.Exists(deletefi.FullName))
                    {
                        try     { File.Delete(deletefi.FullName); LogMessage.Log("...Delete File: {0}", deletefi.FullName); }
                        catch   { LogMessage.Log("...Unable to delete: {0}", deletefi.FullName); continue; }
                    }
                    else LogMessage.Log("...{0}: File doesn't exist, ignoring.", deletefi.FullName);
                }
                else if (node.Name == "copyfile")
                {
                    #region Initialize file-specific variables
                    String source, dest;
                    bool CleanSource = false;

                    try { source = xac["source"].Value; }
                    catch { source = String.Empty; }

                    try { dest = xac["dest"].Value; }
                    catch { dest = String.Empty; }

                    try { CleanSource = Boolean.Parse(xac["deletesource"].Value); }
                    catch { CleanSource = false; }
                    #endregion

                    #region Verify filenames and existence of source
                    if (source == String.Empty || dest == String.Empty)
                    {
                        LogMessage.Log("... Copyfile: Source '{0}' Dest '{1}': One of the files has no name.", source, dest);
                        continue;
                    }
                    else if (!File.Exists(source))
                    {
                        LogMessage.Log("... Copyfile: Source '{0}' does not exist, skipping.", source);
                        continue;
                    }
                    #endregion

                    #region Copy a file to another file, possibly delete the source file
                    try
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(dest).TrimEnd('\\')))
                            Directory.CreateDirectory(Path.GetDirectoryName(dest).TrimEnd('\\'));

                        File.Copy(source, dest, true);
                        LogMessage.Log("... Copyfile: '{0}' -> '{1}' Success", source, dest);
                    }
                    //catch (System.IO.DirectoryNotFoundException)
                    //{
                    //    try
                    //    {
                    //        Directory.CreateDirectory(Path.GetDirectoryName(dest).TrimEnd('\\'));
                    //        File.Copy(source, dest, true);
                    //        LogMessage.Log("... Copyfile: Created path and copied {0} to {1}.", source, dest);
                    //    }
                    //    catch (Exception e)
                    //    {
                    //        LogMessage.Log("... Copyfile: {0}", e.Message);
                    //        continue;
                    //    }
                    //}
                    catch (UnauthorizedAccessException uae)
                    {
                        LogMessage.Log("... Copyfile: {0}", uae.Message);
                        continue;
                    }
                    catch (Exception exception)
                    {
                        LogMessage.Log("... Copyfile: {0}", exception.Message);
                        continue;
                    }
                    if (CleanSource)
                    {
                        File.Delete(source);
                        LogMessage.Log("... Deleted source file {0}.", source);
                    }
                    #endregion
                }
            }
            LogMessage.Log("Done processing nodes.");
            if (CleanDoc)
            {
                try
                {
                    File.Delete(pathName);
                    LogMessage.Log("..Deleted file {0}", pathName);
                }
                catch (Exception e)
                {
                    LogMessage.Log("..{0}: {1}", pathName, e.Message);
                }
            }
            else    { LogMessage.Log("..{0}: Not deleting file.", pathName); }
#if (DEBUG)
            Console.WriteLine("Program completed successfully, see logfile for specific information.");
            Console.ReadLine();
#endif
            LogMessage.Log("Done processing file.");
            LogMessage.Close();
        }
    }
}
