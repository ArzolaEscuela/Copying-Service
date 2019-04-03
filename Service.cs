using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Copying_Solution
{
    public partial class Service : ServiceBase
    {
        //------------------------------------------------------------------------------------//
        /*----------------------------------- FIELDS -----------------------------------------*/
        //------------------------------------------------------------------------------------//

        private const string _DEFAULT_FROM_FOLDER_NAME = "From";
        private const string _DEFAULT_TO_FOLDER_NAME = "To";
        private const string _SETTINGS_FILE_NAME = "Copying_Solution_Settings.txt";
        private const float _DEFAULT_COPY_TIMER = 5f;

        private static System.Timers.Timer _timer;
        private static float _copyTimerInMiliseconds = -1f;
        private static string _fromDirectory = string.Empty;
        private static string _toDirectory = string.Empty;

        //------------------------------------------------------------------------------------//
        /*--------------------------------- PROPERTIES ---------------------------------------*/
        //------------------------------------------------------------------------------------//

        /// <summary>
        /// Returns true whenever there ISN'T a settings file on the same location as where the executable is.
        /// </summary>
        private bool IsFirstRun { get { return !File.Exists(SettingsFileFullPath); } }

        private string SettingsFileFullPath { get { return AppDomain.CurrentDomain.BaseDirectory + _SETTINGS_FILE_NAME; } }
        private string DefaultFromFolderFullPath { get { return AppDomain.CurrentDomain.BaseDirectory + _DEFAULT_FROM_FOLDER_NAME; } }
        private string DefaultToFolderFullPath { get { return AppDomain.CurrentDomain.BaseDirectory + _DEFAULT_TO_FOLDER_NAME; } }

        private string CopiedFiles
        {
            get
            {
                string copiedFiles = "The following files have been copied.\nFROM: \"" + _fromDirectory + "\"\nTO: \"" + _toDirectory + "\":\n\n";

                string[] allFilesInFrom = Directory.GetFiles(_fromDirectory, "*.*", SearchOption.AllDirectories);

                foreach (string filePath in allFilesInFrom)
                {
                    copiedFiles += "•" + filePath + "\n";
                }

                if (allFilesInFrom.Length == 0)
                {
                    copiedFiles += "Actually, there were no files to copy.";
                }

                return copiedFiles;
            }
        }

        //------------------------------------------------------------------------------------//
        /*---------------------------------- METHODS -----------------------------------------*/
        //------------------------------------------------------------------------------------//

        #region Unused

        public Service()
        {
            InitializeComponent();
        }

        // Remnant of tests
        public void DebugStart()
        {

        }

        protected override void OnStop() { }

        #endregion Unused

        private void PrepareLogging()
        {
            EventLog.Log = "Application"; // Set logs to the application category
            AutoLog = false;

            ((ISupportInitialize)this.EventLog).BeginInit();
            if (!EventLog.SourceExists(this.ServiceName))
            {
                EventLog.CreateEventSource(this.ServiceName, "Application");
            }
            ((ISupportInitialize)this.EventLog).EndInit();

            EventLog.Source = this.ServiceName;
        }

        private void CreateDefaults()
        {
            Directory.CreateDirectory(DefaultFromFolderFullPath);
            Directory.CreateDirectory(DefaultToFolderFullPath);
            string defaultFileContents = _DEFAULT_COPY_TIMER.ToString() + "\n" + DefaultFromFolderFullPath + "\n" + DefaultToFolderFullPath;
            File.WriteAllText(SettingsFileFullPath, defaultFileContents);
        }

        private bool IsDirectoryValid(string toCheck) { return Directory.Exists(toCheck); }

        private void GetSettings()
        {
            int lineIndex = 0;
            string line;
            System.IO.StreamReader file = new System.IO.StreamReader(SettingsFileFullPath);
            while ((line = file.ReadLine()) != null)
            {
                System.Console.WriteLine(line);
                switch (lineIndex)
                {
                    case 0:
                        float copyTimeInSeconds;
                        if (float.TryParse(line, out copyTimeInSeconds))
                        {
                            _copyTimerInMiliseconds = (int)(copyTimeInSeconds * 1000);
                        }
                        else { throw new InvalidCastException("Unable to cast the copy time from the settings file."); }
                        break;
                    case 1:
                        if (!IsDirectoryValid(line)) { throw new InvalidCastException("Invalid FROM directory provided."); }
                        _fromDirectory = line;
                        break;
                    case 2:
                        if (!IsDirectoryValid(line)) { throw new InvalidCastException("Invalid TO directory provided."); }
                        _toDirectory = line;
                        break;
                }
                lineIndex++;
            }

            file.Close();
        }

        private void OnCopyNeeded(object source, ElapsedEventArgs e)
        {
            EventLog.WriteEntry(CopiedFiles);

            // Copy Folders If Required
            foreach (string dirPath in Directory.GetDirectories(_fromDirectory, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(_fromDirectory, _toDirectory));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(_fromDirectory, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(_fromDirectory, _toDirectory), true);
            }
        }

        protected override void OnStart(string[] args)
        {
            PrepareLogging();

            if (IsFirstRun) { CreateDefaults(); }

            GetSettings();

            _timer = new System.Timers.Timer(_copyTimerInMiliseconds);
            _timer.Elapsed += new ElapsedEventHandler(OnCopyNeeded);
            _timer.Interval = _copyTimerInMiliseconds;
            _timer.Enabled = true;
        }
    }
}
