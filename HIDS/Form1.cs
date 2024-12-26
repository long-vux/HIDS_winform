using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;

namespace HIDS
{
    public partial class Form1 : Form
    {

        private FileSystemWatcher watcher;
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private Timer systemMetricsTimer;
        public Form1()
        {
            InitializeComponent();
            InitializePerformanceCounters();
            InitializeTimer();
            btnStop.Enabled = false;
        }

        private void InitializePerformanceCounters()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        }

        private void InitializeTimer()
        {
            systemMetricsTimer = new Timer();
            systemMetricsTimer.Interval = 1000; // 1 giây
            systemMetricsTimer.Tick += SystemMetricsTimer_Tick;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string path = txtDirectory.Text;

            if (Directory.Exists(path))
            {
                watcher = new FileSystemWatcher();
                watcher.Path = path;
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                watcher.Filter = "*.*";

                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;

                watcher.EnableRaisingEvents = true;
                lblStatus.Text = "Đang giám sát: " + path;

                // Bắt đầu Timer
                systemMetricsTimer.Start();
            }
            else
            {
                MessageBox.Show("Thư mục không tồn tại!");
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                lblStatus.Text = "Đã dừng giám sát.";
                lblStatus.Text = "Đã dừng giám sát.";
            }

            // Dừng Timer
            systemMetricsTimer.Stop();

            btnStart.Enabled = true;
            btnStop.Enabled = false;

            // nếu có dữ liệu trong listBoxLogs thì mới hiện thông báo
            if (listBoxLogs.Items.Count > 0)
            {
                // Alert người dùng có muốn xuất ra file ko?
                DialogResult dialogResult = MessageBox.Show(
                    "Bạn có muốn xuất ra file log.txt không?",
                    "Xuất file log",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dialogResult == DialogResult.Yes)
                {
                    // Hộp thoại chọn đường dẫn lưu file
                    using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                    {
                        saveFileDialog.Filter = "Text Files (*.txt)|*.txt";
                        saveFileDialog.Title = "Chọn nơi lưu file log";
                        saveFileDialog.FileName = "log.txt";

                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                // Tạo nội dung log từ listBoxLogs
                                string logContent = string.Join(Environment.NewLine, listBoxLogs.Items.Cast<string>());

                                // Ghi dữ liệu vào file
                                File.WriteAllText(saveFileDialog.FileName, logContent);

                                MessageBox.Show(
                                    "File log đã được xuất thành công!",
                                    "Thông báo",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(
                                    $"Đã xảy ra lỗi khi xuất file log:\n{ex.Message}",
                                    "Lỗi",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }

            // Xóa dữ liệu trong listBoxLogs
            listBoxLogs.Items.Clear();
        }

        // on open file dialog
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Chọn thư mục cần giám sát";

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    txtDirectory.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        // làm việc với file
        private bool IsCriticalFile(string filePath)
        {
            // Danh sách các tệp quan trọng cần giám sát
            string[] criticalFiles = { "C:\\path\\to\\important_file.txt" };
            return criticalFiles.Contains(filePath);
        }

        // gửi cảnh báo qua email của quản lý
        private void SendAlert(string message)
        {
            // Gửi cảnh báo qua email
            MessageBox.Show(message, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            string logMessage = $"Tệp {e.ChangeType}: {e.FullPath}";
            Invoke((MethodInvoker)delegate
            {
                listBoxLogs.Items.Add(logMessage);
            });
            WriteLog(logMessage);

            // Kiểm tra hành vi bất thường
            if (e.ChangeType == WatcherChangeTypes.Deleted && IsCriticalFile(e.FullPath))
            {
                SendAlert($"Cảnh báo: Tệp quan trọng đã bị xóa: {e.FullPath}");
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            string logMessage = $"Tệp đổi tên: {e.OldFullPath} thành {e.FullPath}";
            Invoke((MethodInvoker)delegate
            {
                listBoxLogs.Items.Add(logMessage);
            });
            WriteLog(logMessage);
        }

        private void WriteLog(string message)
        {
            string logFilePath = "log.txt"; // Đường dẫn tệp nhật ký
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }

        private void SystemMetricsTimer_Tick(object sender, EventArgs e)
        {
            UpdateSystemMetrics();
        }

        private void UpdateSystemMetrics()
        {
            float cpuUsage = cpuCounter.NextValue();
            float availableRam = ramCounter.NextValue();

            Invoke((MethodInvoker)delegate
            {
                lblCpuUsage.Text = $"CPU Usage: {cpuUsage}%";
                lblAvailableRam.Text = $"Available RAM: {availableRam} MB";
            });
        }
        

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
