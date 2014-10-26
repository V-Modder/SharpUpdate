using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Collections.Generic;

namespace SharpUpdate
{
    /// <summary>
    /// Form that download the update
    /// </summary>
    internal partial class SharpUpdateDownloadForm : Form
    {
        /// <summary>
        /// The web client to download the update
        /// </summary>
        private WebClient webClient;

        /// <summary>
        /// The thread to hash the file on
        /// </summary>
        private BackgroundWorker bgWorker;

        /// <summary>
        /// Iterating variable in the events
        /// </summary>
        private int count = 0;

        /// <summary>
        /// Gets the list of alltemp files path for the downloaded files
        /// </summary>
        internal List<SharpUpdateFileInfo> TempFilesPath
        {
            get { return this.files; }
        }

        /// <summary>
        /// Holds all paths to the tempfiles
        /// </summary>
        private List<SharpUpdateFileInfo> files;

        /// <summary>
        /// Creates a new SharpUpdateDownloadForm
        /// </summary>
        internal SharpUpdateDownloadForm(List<SharpUpdateFileInfo> files, Icon programIcon)
        {
            InitializeComponent();

            if (programIcon != null)
                this.Icon = programIcon;

            this.files = files;

            this.progressBarAll.Maximum = files.Count;

            // Set the temp file name and create new 0-byte file
            files[count].TempFile = Path.GetTempFileName();

            // Set up WebClient to download file
            webClient = new WebClient();
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(webClient_DownloadProgressChanged);
            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(webClient_DownloadFileCompleted);

            // Set up backgroundworker to hash file
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

            // Download file
            try { webClient.DownloadFileAsync(new Uri(files[count].Url), files[count++].TempFile); }
            catch { this.DialogResult = DialogResult.No; this.Close(); }
        }

        /// <summary>
        /// Downloads file from server
        /// </summary>
        private void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            // Update progressbar on download
            this.lblProgress.Text = String.Format("Downloaded {0} of {1}", FormatBytes(e.BytesReceived, 1, true), FormatBytes(e.TotalBytesToReceive, 1, true));
            this.progressBar.Value = e.ProgressPercentage;
        }

        private void webClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                this.DialogResult = DialogResult.No;
                this.Close();
            }
            else if (e.Cancelled)
            {
                this.DialogResult = DialogResult.Abort;
                this.Close();
            }
            else
            {
                this.progressBar.Value = 0;
                this.progressBarAll.Value++;
                if (this.count < files.Count)
                {
                    // Set the temp file name and create new 0-byte file
                    files[count].TempFile = Path.GetTempFileName();

                    // Set up WebClient to download file
                    webClient = new WebClient();
                    webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(webClient_DownloadProgressChanged);
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(webClient_DownloadFileCompleted);

                    // Set up backgroundworker to hash file
                    bgWorker = new BackgroundWorker();
                    bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
                    bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

                    // Download file
                    try { webClient.DownloadFileAsync(new Uri(files[count].Url), files[count++].TempFile); }
                    catch { this.DialogResult = DialogResult.No; this.Close(); }
                }
                else
                {
                    // Show the "Hashing" label and set the progressbar to marquee
                    this.lblProgress.Text = "Verifying Download...";
                    this.progressBar.Style = ProgressBarStyle.Marquee;
                    this.count = 0;

                    // Start the hashing
                    bgWorker.RunWorkerAsync(new string[] { this.files[count].TempFile, this.files[count++].Md5 });
                }
            }
        }

        /// <summary>
        /// Formats the byte count to closest byte type
        /// </summary>
        /// <param name="bytes">The amount of bytes</param>
        /// <param name="decimalPlaces">How many decimal places to show</param>
        /// <param name="showByteType">Add the byte type on the end of the string</param>
        /// <returns>The bytes formatted as specified</returns>
        private string FormatBytes(long bytes, int decimalPlaces, bool showByteType)
        {
            double newBytes = bytes;
            string formatString = "{0";
            string byteType = "B";

            // Check if best size in KB
            if (newBytes > 1024 && newBytes < 1048576)
            {
                newBytes /= 1024;
                byteType = "KB";
            }
            else if (newBytes > 1048576 && newBytes < 1073741824)
            {
                // Check if best size in MB
                newBytes /= 1048576;
                byteType = "MB";
            }
            else
            {
                // Best size in GB
                newBytes /= 1073741824;
                byteType = "GB";
            }

            // Show decimals
            if (decimalPlaces > 0)
                formatString += ":0.";

            // Add decimals
            for (int i = 0; i < decimalPlaces; i++)
                formatString += "0";

            // Close placeholder
            formatString += "}";

            // Add byte type
            if (showByteType)
                formatString += byteType;

            return String.Format(formatString, newBytes);
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string file = ((string[])e.Argument)[0];
            string updateMD5 = ((string[])e.Argument)[1];

            // Hash the file and compare to the hash in the update xml
            if (Hasher.HashFile(file, HashType.MD5) != updateMD5)
                e.Result = DialogResult.No;
            else
                e.Result = DialogResult.OK;
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((DialogResult)e.Result != DialogResult.OK)
            {
                this.DialogResult = (DialogResult)e.Result;
                this.Close();
            }
            else
            {
                if (count < files.Count)
                    bgWorker.RunWorkerAsync(new string[] { this.files[count].TempFile, this.files[count++].Md5 });
                else
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void SharpUpdateDownloadForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (webClient.IsBusy)
            {
                webClient.CancelAsync();
                this.DialogResult = DialogResult.Abort;
            }

            if (bgWorker.IsBusy)
            {
                bgWorker.CancelAsync();
                this.DialogResult = DialogResult.Abort;
            }
        }
    }
}