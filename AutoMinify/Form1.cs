using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Configuration;

namespace AutoMinify
{
    public partial class Form1 : Form
    {
        public delegate void DelegateHideForm();

        private Dictionary<string, FileSystemWatcher> _FileSystemWatcher { get; set; }

        public Form1()
        {
            InitializeComponent();
            _FileSystemWatcher = new Dictionary<string, FileSystemWatcher>();
            notifyIcon1.Visible = false;
            


            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                LoadSaveFile(args[1]);
                this.WindowState = FormWindowState.Minimized;
                var dhf = new DelegateHideForm(HideForm);
                var bw = new BackgroundWorker();
                bw.DoWork += (s, e) =>
                {
                    this.Invoke(dhf);
                };
                bw.RunWorkerAsync();
            }



        }

        #region Add
        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "原始js檔(*.src.js)|*.src.js";
            openFileDialog1.Multiselect = true;
            openFileDialog1.ShowDialog();
            if (openFileDialog1.FileNames.Length > 0 && openFileDialog1.FileName != "openFileDialog1")
            {
                var list = new Dictionary<string, string>();

                #region 取得清單
                var max = listBox1.Items.Count;
                for (var i = 0; i < max; i++)
                {
                    var a = listBox1.Items[i].ToString();
                    if (!list.ContainsKey(a))
                    {
                        list.Add(a, a);
                    }
                }
                #endregion

                #region 加入清單
                foreach (var a in openFileDialog1.FileNames)
                {
                    if (!list.ContainsKey(a))
                    {
                        list.Add(a, a);
                        AddFileSystemWatcher(a);
                    }
                }
                #endregion

                #region 顯示清單
                listBox1.Items.Clear();
                foreach (var a in list.OrderBy(x => x.Key))
                {
                    listBox1.Items.Add(a.Value);
                }
                #endregion
            }
        }
        #endregion

        #region Remove

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count > 0)
            {
                var path = listBox1.SelectedItems[0].ToString();

                RemoveFileSystemWatcher(path);
            }
        }

        #endregion

        #region Save
        private void button3_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "文字檔(*.txt)|*.txt";
            saveFileDialog1.ShowDialog();
            if (!string.IsNullOrEmpty(saveFileDialog1.FileName))
            {
                var path = saveFileDialog1.FileName;
                var sb = new StringBuilder();
                var max = listBox1.Items.Count;
                for (var i = 0; i < max; i++)
                {
                    sb.Append(listBox1.Items[i].ToString() + "\r\n");
                }
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.AppendAllText(path, sb.ToString());
            }
        }
        #endregion

        #region Load
        private void button4_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "openFileDialog1";
            openFileDialog1.Filter = "文字檔(*.txt)|*.txt";
            openFileDialog1.Multiselect = false;
            openFileDialog1.ShowDialog();
            if (openFileDialog1.FileNames.Length > 0 && openFileDialog1.FileName != "openFileDialog1")
            {
                LoadSaveFile(openFileDialog1.FileName);
            }
        }
        #endregion

        #region AddFileSystemWatcher
        private void AddFileSystemWatcher(string path)
        {
            if (!_FileSystemWatcher.ContainsKey(path))
            {
                _FileSystemWatcher.Add(path, new FileSystemWatcher());
            }
            _FileSystemWatcher[path].Path = Path.GetDirectoryName(path);
            _FileSystemWatcher[path].NotifyFilter = NotifyFilters.LastWrite;
            _FileSystemWatcher[path].Filter = Path.GetFileName(path);
            _FileSystemWatcher[path].Changed += (s, e) =>
            {
                _FileSystemWatcher[path].EnableRaisingEvents = false;
                MinifyFile(e.FullPath);
                _FileSystemWatcher[path].EnableRaisingEvents = true;

            };
            _FileSystemWatcher[path].EnableRaisingEvents = true;
        }
        #endregion

        #region RemoveFileSystemWatcher
        private void RemoveFileSystemWatcher(string path)
        {
            if (_FileSystemWatcher.ContainsKey(path))
            {
                _FileSystemWatcher[path].Dispose();

                _FileSystemWatcher.Remove(path);

                var index = listBox1.Items.IndexOf(path);
                if (index < 0)
                {
                    index = 0;
                }

                listBox1.Items.Remove(path);

                if (index >= listBox1.Items.Count)
                {
                    listBox1.SelectedIndex = listBox1.Items.Count - 1;
                }
                else
                {
                    listBox1.SelectedIndex = index;
                }

            }
        }
        #endregion

        #region MinifyFile
        private void MinifyFile(string path, bool hasLog = true)
        {
            if (!IsDebugger.Checked)
            {
                var filemap = new ExeConfigurationFileMap();
                filemap.ExeConfigFilename = string.Format("{0}{1}", Application.ExecutablePath, ".config");
                var config = ConfigurationManager.OpenMappedExeConfiguration(filemap, ConfigurationUserLevel.None);
                var app = config.AppSettings;

                var cmd = app.Settings["MinifierFilePath"].Value;
                var arg = string.Format(app.Settings["MinifierCommand"].Value, path, Path.GetDirectoryName(path) + "\\" + Path.GetFileName(path).Replace(".src.", "."));
                var si = new ProcessStartInfo(cmd, arg);
                si.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(si);
                if (hasLog)
                {
                    MinifyLog(string.Format("[{0}] {1} {2}\r\n", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), cmd, arg));
                }
            }
        }
        #endregion

        #region LoadSaveFile
        private void LoadSaveFile(string path)
        {
            if (File.Exists(path))
            {
                var list = new Dictionary<string, string>();

                #region 清除現有
                foreach (var a in _FileSystemWatcher.ToArray())
                {
                    RemoveFileSystemWatcher(a.Key);
                }
                #endregion

                #region 載入檔案
                using (var sr = new StreamReader(path))
                {
                    while (sr.Peek() > 0)
                    {
                        var a = sr.ReadLine();
                        if (!string.IsNullOrEmpty(a))
                        {
                            if (!list.ContainsKey(a))
                            {
                                list.Add(a, a);
                                AddFileSystemWatcher(a);
                            }
                        }
                    }
                }

                #endregion

                #region 顯示清單
                listBox1.Items.Clear();
                foreach (var a in list.OrderBy(x => x.Key))
                {
                    listBox1.Items.Add(a.Value);
                }
                #endregion
            }
        }
        #endregion

        #region MinifyLog
        private void MinifyLog(string log)
        {
            textBox1.Text += log;
        }
        #endregion

        #region DebugMode
        private void IsDebugger_CheckedChanged(object sender, EventArgs e)
        {
            var checkBox = (CheckBox)sender;
            if (checkBox.Checked)
            {
                #region Debug Mode

                foreach (var a in _FileSystemWatcher)
                {
                    var source = a.Key;
                    var destination = a.Key.Replace(".src.", ".");

                    if (File.Exists(destination))
                    {
                        File.Delete(destination);
                    }
                    File.Copy(source, destination);
                }

                #endregion
            }
            else
            {
                #region General Mode

                foreach (var a in _FileSystemWatcher)
                {
                    var source = a.Key;
                    MinifyFile(source, false);
                }

                #endregion
            }
        }
        #endregion

        #region UI

        #region FormSizeChange
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.Visible = true;
            }
        }
        #endregion

        #region NotifyIconDoubleClick
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }
        #endregion

        #region FormClose
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var mbs = MessageBox.Show("確定要關閉嗎?", this.Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (mbs == System.Windows.Forms.DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        }
        #endregion

        #region HideForm
        public void HideForm()
        {
            this.Hide();
        }
        #endregion

        #region TextBoxTipe
        private ToolTip tt;

        private void MinifierCommand_MouseEnter(object sender, EventArgs e)
        {
            tt = new ToolTip();
            tt.InitialDelay = 0;
            tt.IsBalloon = false;
            tt.Show("參數說明：\n　輸入{0}為來源\n　輸入{1}為目的", MinifierCommand, 0);
        }

        private void MinifierCommand_MouseLeave(object sender, EventArgs e)
        {
            tt.Dispose();
        } 
        #endregion
        
        #endregion

        private void button5_Click(object sender, EventArgs e)
        {
            var filemap = new ExeConfigurationFileMap();
            filemap.ExeConfigFilename = string.Format("{0}{1}", Application.ExecutablePath, ".config");
            var config = ConfigurationManager.OpenMappedExeConfiguration(filemap, ConfigurationUserLevel.None);
            var app = config.AppSettings;
            app.Settings["MinifierFilePath"].Value = MinifierFilePath.Text;
            app.Settings["MinifierCommand"].Value = MinifierCommand.Text;
            app.Settings["MinifierHelp"].Value = MinifierHelp.Text;
            config.Save(ConfigurationSaveMode.Modified);
            MessageBox.Show("儲存完畢");
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tc = (TabControl)sender;
            if (tc.SelectedIndex == 2)
            {
                #region Setting

                var filemap = new ExeConfigurationFileMap();
                filemap.ExeConfigFilename = string.Format("{0}{1}", Application.ExecutablePath, ".config");
                var config = ConfigurationManager.OpenMappedExeConfiguration(filemap, ConfigurationUserLevel.None);
                var app = config.AppSettings;

                MinifierFilePath.Text = app.Settings["MinifierFilePath"].Value;
                MinifierCommand.Text = app.Settings["MinifierCommand"].Value; 
                MinifierHelp.Text = app.Settings["MinifierHelp"].Value;

                #endregion
            }
        }
    }
}