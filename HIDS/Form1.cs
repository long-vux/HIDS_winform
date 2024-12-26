using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Net;

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
            btnBrowse.Enabled = false;
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
        private bool IsCriticalFile(string fileName)
        {
            try
            {
                // Đường dẫn tệp important_file.txt
                string importantFilePath = @"C:\Users\lenovo\Desktop\important_file.txt";

                // Đọc tất cả các dòng từ tệp
                if (File.Exists(importantFilePath))
                {
                    string[] criticalFiles = File.ReadAllLines(importantFilePath);

                    // Kiểm tra fileName có trong danh sách không (không phân biệt hoa thường)
                    return criticalFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi đọc tệp quan trọng:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }


        // gửi cảnh báo qua email của quản lý
        private void SendEmailNotification(string filePath)
        {
            string to = "hoanglongvu233@gmail.com"; // Recipient email
            string subject = "Important File Deletion Alert";

            // Construct the HTML body
            string body = $@"
        <html>
        <head>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    line-height: 1.6;
                    color: #333;
                }}
                .alert {{
                    color: #ff0000;
                    font-weight: bold;
                }}
                .metrics {{
                    margin-top: 20px;
                    font-size: 14px;
                }}
                .footer {{
                    margin-top: 30px;
                    font-size: 12px;
                    color: #777;
                }}
            </style>
        </head>
        <body>
            <h2>Important File Deletion Alert</h2>
            <p class='alert'>The critical file <strong>{filePath}</strong> has been deleted or modified.</p>
            
            <div class='metrics'>
                <h3>System Metrics</h3>
                <p>CPU Usage: {cpuCounter.NextValue()}%</p>
                <p>Available RAM: {ramCounter.NextValue()} MB</p>
            </div>
            
            <div class='footer'>
                <p>This is an automated alert from your Host-based Intrusion Detection System (HIDS).</p>
            </div>
        </body>
        </html>";

            // Send the email
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress("henrylong.work@gmail.com");
                mail.To.Add(to);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true; // Enable HTML content

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential("henrylong.work@gmail.com", "qqmj cexm vbju jbvl");
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            string logMessage = $"Tệp {e.ChangeType}: {e.FullPath}";
            Invoke((MethodInvoker)delegate
            {
                listBoxLogs.Items.Add(logMessage);
            });
            WriteLog(logMessage);

            string fileName = Path.GetFileName(e.FullPath); // Lấy tên tệp

            // Kiểm tra hành vi bất thường
            if ((e.ChangeType == WatcherChangeTypes.Deleted || e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Renamed) && IsCriticalFile(fileName))
            {
                // show thông báo
                MessageBox.Show(
                    "Tệp quan trọng đã bị xóa hoặc chỉnh sửa!",
                    "Cảnh báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                // Gửi cảnh báo qua email
                SendEmailNotification(e.FullPath);
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

                // Kiểm tra ngưỡng CPU
                if (cpuUsage > 80) // Ngưỡng 80%
                {
                    SendAlert($"Cảnh báo: Sử dụng CPU cao: {cpuUsage}%");
                }

                // Kiểm tra ngưỡng RAM
                if (availableRam < 100) // Ngưỡng 100 MB
                {
                    SendAlert($"Cảnh báo: RAM khả dụng thấp: {availableRam} MB");
                }
            });
        }

        private void SendAlert(string message)
        {
            // Gửi cảnh báo qua email hoặc hiển thị thông báo
            MessageBox.Show(message, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            // thoát chương trình
            Application.Exit();
        }
    }
}
