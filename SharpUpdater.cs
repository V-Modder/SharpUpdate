using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

namespace SharpUpdate
{
    /// <summary>
    /// Provides application update support in C#
    /// </summary>
    public class SharpUpdater : IDisposable
    {
        /// <summary>
        /// Holds the program-to-update's info
        /// </summary>
        private ISharpUpdatable applicationInfo;

        /// <summary>
        /// Thread to find update
        /// </summary>
        private BackgroundWorker bgWorker;

        /// <summary>
        /// Creates a new SharpUpdater object
        /// </summary>
        /// <param name="applicationInfo">The info about the application so it can be displayed on dialog boxes to user</param>
        public SharpUpdater(ISharpUpdatable applicationInfo)
        {
            this.applicationInfo = applicationInfo;

            // Set up backgroundworker
            this.bgWorker = new BackgroundWorker();
            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
        }

        /// <summary>
        /// Checks for an update for the program passed.
        /// If there is an update, a dialog asking to download will appear
        /// </summary>
        public void DoUpdate()
        {
            if (!this.bgWorker.IsBusy)
                this.bgWorker.RunWorkerAsync(this.applicationInfo);
        }

        /// <summary>
        /// Checks for/parses update.xml on server
        /// </summary>
        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ISharpUpdatable application = (ISharpUpdatable)e.Argument;

            // Check for update on server
            if (!SharpUpdateXml.ExistsOnServer(application.UpdateXmlLocation))
                e.Cancel = true;
            else // Parse update xml
                e.Result = SharpUpdateXml.Parse(application.UpdateXmlLocation, application.ApplicationID);
        }

        /// <summary>
        /// Get the drirectory of the application
        /// </summary>
        /// <returns>Full path to the application to update</returns>
        private string AssemblyDirectory()
        {
            string codeBase = this.applicationInfo.ApplicationAssembly.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// After the background worker is done, prompt to update if there is one
        /// </summary>
        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // If there is a file on the server
            if (!e.Cancelled)
            {
                SharpUpdateXml update = (SharpUpdateXml)e.Result;

                // Check if the update is not null and is a newer version than the current application
                if (update != null)
                {
                    //Check for missing files
                    if(!update.HasAllFiles(AssemblyDirectory()))
                        this.DownloadUpdate(update);
                    else if (update.IsNewerThan(this.applicationInfo.ApplicationAssembly.GetName().Version))
                    {
                        // Ask to accept the update
                        if (new SharpUpdateAcceptForm(this.applicationInfo, update).ShowDialog(this.applicationInfo.Context) == DialogResult.Yes)
                            this.DownloadUpdate(update); // Do the update
                    }
                }
            }
        }

        /// <summary>
        /// Downloads update and installs the update
        /// </summary>
        /// <param name="update">The update xml info</param>
        private void DownloadUpdate(SharpUpdateXml update)
        {
            SharpUpdateDownloadForm form = new SharpUpdateDownloadForm(update.UpdateFiles, this.applicationInfo.ApplicationIcon);
            DialogResult result = form.ShowDialog(this.applicationInfo.Context);

            // Download update
            if (result == DialogResult.OK)
            {
                string currentPath = this.applicationInfo.ApplicationAssembly.Location;
                // "Install" it
                UpdateApplication(form.TempFilesPath, currentPath, update.LaunchArgs);

                Application.Exit();
            }
            else if (result == DialogResult.Abort)
            {
                MessageBox.Show("The update download was cancelled.\nThis program has not been modified.", "Update Download Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("There was a problem downloading the update.\nPlease try again later.", "Update Download Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Hack to close program, delete original, move the new one to that location
        /// </summary>
        /// <param name="tempFilePath">The temporary file's path</param>
        /// <param name="currentPath">The path of the current application</param>
        /// <param name="launchArgs">The launch arguments</param>
        private void UpdateApplication(List<SharpUpdateFileInfo> tempFilePath, string currentPath, string launchArgs)
        {
            //Wait 4 seceonds to close the application
            string argument = "/C choice /C Y /N /D Y /T 4 & ";

            //Delete all file that will be updated
            foreach (SharpUpdateFileInfo fi in tempFilePath)
            {
                argument += string.Format("Del /F /Q \"{0}\" & ", Path.GetDirectoryName(currentPath) + "\\" + fi.FileName);
            }

            //Wait 2 seconds
            argument += "choice /C Y /N /D Y /T 2 & ";

            //Move all downloaded files from temporarily path to installed path 
            foreach (SharpUpdateFileInfo fi in tempFilePath)
            {
                argument += string.Format("Move /Y \"{0}\" \"{1}\" & ", fi.TempFile, Path.GetDirectoryName(currentPath) + "\\" + fi.FileName);
            }

            //Start the application after updateing all files
            argument += "Start \"\" /D \"{0}\" \"{1}\" {2}";

            //Start the process to do above tasks
            ProcessStartInfo Info = new ProcessStartInfo();
            Info.Arguments = String.Format(argument, Path.GetDirectoryName(currentPath), Path.GetFileName(tempFilePath[0].FileName), launchArgs);
            Info.WindowStyle = ProcessWindowStyle.Hidden;
            Info.CreateNoWindow = true;
            Info.FileName = "cmd.exe";
            Process.Start(Info);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (bgWorker.IsBusy)
                bgWorker.CancelAsync();
        }
    }
}