using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace ODSPatcher
{
    public partial class Form1 : Form
    {
        private static string PATCH_LINK = "https://raw.githubusercontent.com/ODSOperations/releases/master/";
        private static HashSet<string> redownload;

        public Form1()
        {
            InitializeComponent();
        }

        private string CalculateSHA1(string path)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                using (FileStream fileStream = File.OpenRead(path))
                {
                    return BitConverter.ToString(sha1.ComputeHash(fileStream)).Replace("-", "").ToLower();
                }
            }
        }

        private void PatchGame()
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    Dictionary<string, object> manifest = JsonConvert.DeserializeObject<Dictionary<string, object>>(client.DownloadString(PATCH_LINK + "manifest.json"));
                    redownload = new HashSet<string>();

                    foreach (KeyValuePair<string, object> filePair in manifest)
                    {
                        string filename = filePair.Key;
                        FileInfo info = new FileInfo(filename);

                        if (filename.Equals("DessertStorm.exe") || filename.Equals("ods_setup.exe"))
                        {
                            continue;
                        }

                        if (!info.Exists)
                        {
                            redownload.Add(filename);
                        }
                        else if (filePair.Value is string)
                        {
                            string sha1 = (string)filePair.Value;

                            if (CalculateSHA1(filename) != sha1)
                            {
                                redownload.Add(filename);
                            }
                        }
                        else if (info.Length != (long)filePair.Value)
                        {
                            redownload.Add(filename);
                        }
                    }

                    client.DownloadProgressChanged += Client_DownloadProgressChanged;
                    client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    int allFiles = redownload.Count;
                    int currentFile = 0;

                    foreach (string filename in redownload)
                    {
                        currentFile++;
                        this.InvokeEx(f => f.SetFile(currentFile, allFiles));
                        string folder = Path.GetDirectoryName(filename);

                        if (!String.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }

                        client.DownloadFileAsync(new Uri(PATCH_LINK + filename), filename);

                        while (client.IsBusy) { }
                    }

                    this.InvokeEx(f => f.PatchDone());
                } catch (Exception)
                {
                    this.InvokeEx(f => f.PatchFailed());
                }
            }
        }

        private void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.InvokeEx(f => f.UpdateBar(100));
        }

        private void SetFile(int file, int all)
        {
            label4.Text = "Updating file " + file + " of " + all + "...";
        }

        private void PatchDone()
        {
            Process process = new Process();
            process.StartInfo.EnvironmentVariables["ODS_LAUNCHER"] = "1";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = "ods.exe";
            process.Start();
            Application.Exit();
        }
        
        private void PatchFailed()
        {
            MessageBox.Show("Oops! Looks like the download servers are down! Try reinstalling the launcher.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.InvokeEx(f => f.UpdateBar(e.ProgressPercentage));
        }

        private void UpdateBar(int value) {
            progressBar1.Value = value;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Thread thread = new Thread(PatchGame);
            thread.IsBackground = true;
            thread.Start();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://opdessertstorm.com");
        }
    }
}
