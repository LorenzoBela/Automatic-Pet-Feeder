using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace Automatic_Pet_Feeder
{
    public partial class Form1 : Form
    {
        private SerialPort _serialPort;
        
        private DateTime? nextFeedingTime = null;
        private string feedingInterval = "Every Day";

        private const int MAX_LOG_LINES = 1000;
        private int logLineCount = 0;

        private double currentWeight = 0.0;
        private double lastWeightReading = 0.0;
        private bool isWeightStable = false;

        public Form1()
        {
            InitializeComponent();
            label3.Text = "Ready";
            
            LoadFeedingSchedule();
            
            timer1.Start();

            InitializeArduinoLog();

            InitializeWeightMonitor();

            buttonClearLog.Click += buttonClearLog_Click;
        }

        private void InitializeWeightMonitor()
        {
            labelWeightValue.Text = "Not Connected";
            labelWeightValue.ForeColor = Color.FromArgb(255, 165, 79);
        }

        private void UpdateWeightDisplay(double weight)
        {
            try
            {
                if (labelWeightValue.InvokeRequired)
                {
                    labelWeightValue.Invoke(new Action<double>(UpdateWeightDisplay), weight);
                    return;
                }

                currentWeight = weight;
                labelWeightValue.Text = $"{weight:F1}g";

                if (weight >= 200)
                {
                    labelWeightValue.ForeColor = Color.FromArgb(46, 204, 113);
                    labelWeightValue.Text += " (Full)";
                }
                else if (weight >= 50)
                {
                    labelWeightValue.ForeColor = Color.FromArgb(243, 156, 18);
                    labelWeightValue.Text += " (Moderate)";
                }
                else if (weight >= 10)
                {
                    labelWeightValue.ForeColor = Color.FromArgb(231, 76, 60);
                    labelWeightValue.Text += " (Low)";
                }
                else
                {
                    labelWeightValue.ForeColor = Color.FromArgb(169, 68, 66);
                    labelWeightValue.Text += " (Empty)";
                }

                CheckWeightStability(weight);

            }
            catch (Exception ex)
            {
                AppendToLog($"Error updating weight display: {ex.Message}", Color.FromArgb(231, 76, 60));
            }
        }

        private void CheckWeightStability(double weight)
        {
            double weightDifference = Math.Abs(weight - lastWeightReading);
            
            if (weightDifference > 5.0)
            {
                if (weight > lastWeightReading)
                {
                    AppendToLog($"Food dispensed detected: +{weightDifference:F1}g (Total: {weight:F1}g)", Color.FromArgb(52, 152, 219));
                }
                else
                {
                    AppendToLog($"Pet feeding detected: -{weightDifference:F1}g (Remaining: {weight:F1}g)", Color.FromArgb(155, 89, 182));
                }
                
                isWeightStable = false;
            }
            else if (weightDifference < 1.0)
            {
                if (!isWeightStable)
                {
                    isWeightStable = true;
                    AppendToLog($"Weight stabilized at {weight:F1}g", Color.FromArgb(149, 165, 166));
                }
            }

            lastWeightReading = weight;
        }

        private void InitializeArduinoLog()
        {
            richTextBoxLog.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxLog.WordWrap = true;
            richTextBoxLog.DetectUrls = false;
            
            AppendToLog("=== Arduino Log Monitor Started ===", Color.FromArgb(46, 204, 113));
            AppendToLog("Waiting for Arduino connection...", Color.FromArgb(149, 165, 166));
        }

        private void AppendToLog(string message, Color color)
        {
            try
            {
                if (richTextBoxLog.InvokeRequired)
                {
                    richTextBoxLog.Invoke(new Action<string, Color>(AppendToLog), message, color);
                    return;
                }

                if (logLineCount >= MAX_LOG_LINES)
                {
                    var lines = richTextBoxLog.Lines;
                    var newLines = new string[lines.Length - 100];
                    Array.Copy(lines, 100, newLines, 0, newLines.Length);
                    richTextBoxLog.Lines = newLines;
                    logLineCount -= 100;
                }

                string timeStamp = DateTime.Now.ToString("HH:mm:ss");
                string formattedMessage = $"[{timeStamp}] {message}";

                richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
                richTextBoxLog.SelectionLength = 0;
                richTextBoxLog.SelectionColor = color;
                richTextBoxLog.AppendText(formattedMessage + Environment.NewLine);
                
                richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
                richTextBoxLog.ScrollToCaret();

                logLineCount++;

                UpdateLogStatus("• Receiving data");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log error: {ex.Message}");
            }
        }

        private void UpdateLogStatus(string status)
        {
            if (labelLogStatus.InvokeRequired)
            {
                labelLogStatus.Invoke(new Action<string>(UpdateLogStatus), status);
                return;
            }
            
            labelLogStatus.Text = status;
            labelLogStatus.ForeColor = Color.FromArgb(46, 204, 113);
        }

        private void buttonClearLog_Click(object sender, EventArgs e)
        {
            richTextBoxLog.Clear();
            logLineCount = 0;
            AppendToLog("Log cleared by user", Color.FromArgb(243, 156, 18));
            UpdateLogStatus("• Log cleared");
        }

        private void LoadFeedingSchedule()
        {
            try
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.NextFeedingTime))
                {
                    DateTime savedTime;
                    if (DateTime.TryParse(Properties.Settings.Default.NextFeedingTime, out savedTime))
                    {
                        nextFeedingTime = savedTime;
                    }
                }
                
                if (!string.IsNullOrEmpty(Properties.Settings.Default.FeedingInterval))
                {
                    feedingInterval = Properties.Settings.Default.FeedingInterval;
                }
            }
            catch (Exception ex)
            {
                nextFeedingTime = null;
                feedingInterval = "Every Day";
            }
        }

        private void SaveFeedingSchedule()
        {
            try
            {
                if (nextFeedingTime.HasValue)
                {
                    Properties.Settings.Default.NextFeedingTime = nextFeedingTime.Value.ToString();
                }
                else
                {
                    Properties.Settings.Default.NextFeedingTime = "";
                }
                
                Properties.Settings.Default.FeedingInterval = feedingInterval;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            labelCurrentTime.Text = DateTime.Now.ToString("h:mm:ss tt");
            
            UpdateCountdown();

            CheckArduinoData();
        }

        private void CheckArduinoData()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen && _serialPort.BytesToRead > 0)
                {
                    string data = _serialPort.ReadExisting();
                    if (!string.IsNullOrEmpty(data))
                    {
                        string[] lines = data.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (string line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                ProcessArduinoData(line.Trim());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendToLog($"Error reading Arduino data: {ex.Message}", Color.FromArgb(231, 76, 60));
            }
        }

        private void ProcessArduinoData(string data)
        {
            Color messageColor = Color.FromArgb(189, 195, 199);

            if (data.Contains("Food available") || data.Contains("✅"))
            {
                UpdateFoodLevelStatus("Food Available", Color.FromArgb(46, 204, 113));
                messageColor = Color.FromArgb(46, 204, 113);
            }
            else if (data.Contains("Storage low") || data.Contains("Storage empty") || data.Contains("⚠️"))
            {
                UpdateFoodLevelStatus("Low/Empty", Color.FromArgb(231, 76, 60));
                messageColor = Color.FromArgb(231, 76, 60);
            }
            else if (data.ToLower().Contains("distance:"))
            {
                try
                {
                    string[] parts = data.Split(':');
                    if (parts.Length >= 2)
                    {
                        string distanceStr = parts[1].Trim().Replace("cm", "").Replace("centimeters", "").Trim();
                        if (double.TryParse(distanceStr, out double distance))
                        {
                            if (distance <= 10 && distance > 0)
                            {
                                UpdateFoodLevelStatus("Food Available", Color.FromArgb(46, 204, 113));
                                messageColor = Color.FromArgb(46, 204, 113);
                            }
                            else if (distance > 10)
                            {
                                UpdateFoodLevelStatus("Low/Empty", Color.FromArgb(231, 76, 60));
                                messageColor = Color.FromArgb(231, 76, 60);
                            }
                            else if (distance == 0)
                            {
                                UpdateFoodLevelStatus("Sensor Error", Color.FromArgb(243, 156, 18));
                                messageColor = Color.FromArgb(243, 156, 18);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendToLog($"Error parsing distance data: {ex.Message}", Color.FromArgb(231, 76, 60));
                }
            }
            else if (data.Contains("Ultrasonic timeout") || data.Contains("no echo"))
            {
                UpdateFoodLevelStatus("Sensor Error", Color.FromArgb(243, 156, 18));
                messageColor = Color.FromArgb(243, 156, 18);
            }
            else if (data.ToLower().Contains("weight:") || data.ToLower().Contains("w:"))
            {
                try
                {
                    string[] parts = data.Split(':');
                    if (parts.Length >= 2)
                    {
                        string weightStr = parts[1].Trim().Replace("g", "").Replace("grams", "");
                        if (double.TryParse(weightStr, out double weight))
                        {
                            UpdateWeightDisplay(weight);
                            messageColor = Color.FromArgb(155, 89, 182);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendToLog($"Error parsing weight data: {ex.Message}", Color.FromArgb(231, 76, 60));
                }
            }
            else if (data.ToLower().Contains("error") || data.ToLower().Contains("fail"))
            {
                messageColor = Color.FromArgb(231, 76, 60);
            }
            else if (data.ToLower().Contains("warning") || data.ToLower().Contains("warn"))
            {
                messageColor = Color.FromArgb(243, 156, 18);
            }
            else if (data.ToLower().Contains("success") || data.ToLower().Contains("ok") || data.ToLower().Contains("complete"))
            {
                messageColor = Color.FromArgb(46, 204, 113);
            }
            else if (data.ToLower().Contains("feed") || data.ToLower().Contains("dispense"))
            {
                messageColor = Color.FromArgb(52, 152, 219);
            }
            else if (data.ToLower().Contains("sensor") || data.ToLower().Contains("level"))
            {
                messageColor = Color.FromArgb(155, 89, 182);
            }

            AppendToLog($"Arduino: {data}", messageColor);
        }

        private void UpdateFoodLevelStatus(string status, Color color)
        {
            try
            {
                if (label7.InvokeRequired)
                {
                    label7.Invoke(new Action<string, Color>(UpdateFoodLevelStatus), status, color);
                    return;
                }

                label7.Text = status;
                label7.ForeColor = color;

                AppendToLog($"Food level status updated: {status}", color);
            }
            catch (Exception ex)
            {
                AppendToLog($"Error updating food level status: {ex.Message}", Color.FromArgb(231, 76, 60));
            }
        }

        private void UpdateCountdown()
        {
            if (nextFeedingTime.HasValue)
            {
                TimeSpan timeLeft = nextFeedingTime.Value - DateTime.Now;
                
                if (timeLeft.TotalSeconds <= 0)
                {
                    CalculateNextFeedingTime();
                    labelCountdown.Text = "FEEDING TIME!";
                    labelCountdown.ForeColor = Color.FromArgb(46, 204, 113);
                    
                    AppendToLog("Scheduled feeding time reached!", Color.FromArgb(46, 204, 113));
                    return;
                }
                
                if (timeLeft.TotalDays >= 1)
                {
                    labelCountdown.Text = string.Format("{0}d {1:D2}h {2:D2}m {3:D2}s", 
                        (int)timeLeft.TotalDays, timeLeft.Hours, timeLeft.Minutes, timeLeft.Seconds);
                }
                else if (timeLeft.TotalHours >= 1)
                {
                    labelCountdown.Text = string.Format("{0:D2}h {1:D2}m {2:D2}s", 
                        timeLeft.Hours, timeLeft.Minutes, timeLeft.Seconds);
                }
                else
                {
                    labelCountdown.Text = string.Format("{0:D2}m {1:D2}s", 
                        timeLeft.Minutes, timeLeft.Seconds);
                }
                
                if (timeLeft.TotalMinutes <= 5)
                    labelCountdown.ForeColor = Color.FromArgb(231, 76, 60);
                else if (timeLeft.TotalMinutes <= 30)
                    labelCountdown.ForeColor = Color.FromArgb(243, 156, 18);
                else
                    labelCountdown.ForeColor = Color.FromArgb(52, 152, 219);
            }
            else
            {
                labelCountdown.Text = "No schedule set yet";
                labelCountdown.ForeColor = Color.FromArgb(149, 165, 166);
            }
        }

        private void CalculateNextFeedingTime()
        {
            if (nextFeedingTime.HasValue)
            {
                DateTime baseTime = nextFeedingTime.Value;
                
                switch (feedingInterval)
                {
                    case "Every Day":
                        nextFeedingTime = baseTime.AddDays(1);
                        break;
                    case "Every 2 Hours":
                        nextFeedingTime = baseTime.AddHours(2);
                        break;
                    case "Every 3 Hours":
                        nextFeedingTime = baseTime.AddHours(3);
                        break;
                    case "Every 4 Hours":
                        nextFeedingTime = baseTime.AddHours(4);
                        break;
                    case "Every 6 Hours":
                        nextFeedingTime = baseTime.AddHours(6);
                        break;
                    case "Every 8 Hours":
                        nextFeedingTime = baseTime.AddHours(8);
                        break;
                    case "Every 12 Hours":
                        nextFeedingTime = baseTime.AddHours(12);
                        break;
                    case "Twice a Day":
                        nextFeedingTime = baseTime.AddHours(12);
                        break;
                    case "Three Times a Day":
                        nextFeedingTime = baseTime.AddHours(8);
                        break;
                    default:
                        nextFeedingTime = baseTime.AddDays(1);
                        break;
                }
            }
        }

        public void UpdateFeedingSchedule(DateTime feedingTime, string interval)
        {
            nextFeedingTime = feedingTime;
            feedingInterval = interval;
            SaveFeedingSchedule();
            UpdateCountdown();
            
            AppendToLog($"Feeding schedule updated: {feedingTime:yyyy-MM-dd HH:mm} ({interval})", Color.FromArgb(52, 152, 219));
        }

        public void GetCurrentFeedingSchedule(out DateTime? currentNextFeedingTime, out string currentInterval)
        {
            currentNextFeedingTime = nextFeedingTime;
            currentInterval = feedingInterval;
        }

        public void ClearFeedingSchedule()
        {
            nextFeedingTime = null;
            feedingInterval = "Every Day";
            SaveFeedingSchedule();
            UpdateCountdown();
            
            AppendToLog("Feeding schedule cleared", Color.FromArgb(243, 156, 18));
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.SetMainForm(this);
            form2.Show();
            
            AppendToLog("Set Feeding Time form opened", Color.FromArgb(149, 165, 166));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form3 form3 = new Form3();
            form3.Show();
            
            AppendToLog("Manual Dispense form opened", Color.FromArgb(149, 165, 166));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var portName = "COM8";
            var baudRate = 9600;

            try
            {
                label3.Text = "Checking available ports...";
                AppendToLog("Attempting to connect to Arduino...", Color.FromArgb(243, 156, 18));
                
                var ports = SerialPort.GetPortNames();

                if (!ports.Contains(portName))
                {
                    label3.Text = "Port " + portName + " not found. Available: " + string.Join(", ", ports);
                    AppendToLog($"Port {portName} not found. Available ports: {(ports.Length == 0 ? "(none)" : string.Join(", ", ports))}", Color.FromArgb(231, 76, 60));
                    MessageBox.Show("Port " + portName + " not found. Available ports: " + (ports.Length == 0 ? "(none)" : string.Join(", ", ports)), "Port Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    UpdateLogStatus("• Connection failed");
                    return;
                }

                label3.Text = "Connecting to " + portName + "...";
                AppendToLog($"Connecting to {portName} at {baudRate} baud...", Color.FromArgb(52, 152, 219));

                if (_serialPort == null)
                {
                    _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                    {
                        Handshake = Handshake.None,
                        ReadTimeout = 1000,
                        WriteTimeout = 1000,
                        NewLine = "\n"
                    };
                    
                    _serialPort.DataReceived += SerialPort_DataReceived;
                }

                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();

                    try
                    {
                        System.Threading.Thread.Sleep(200);

                        _serialPort.DiscardInBuffer();
                        _serialPort.WriteLine("PING");
                        AppendToLog("Sending handshake (PING)...", Color.FromArgb(52, 152, 219));

                        string response = null;
                        try
                        {
                            response = _serialPort.ReadLine();
                        }
                        catch (TimeoutException)
                        {
                        }

                        if (!string.IsNullOrEmpty(response))
                        {
                            label3.Text = "Connected to " + portName + " at " + baudRate + " baud. Reply: " + response;
                            AppendToLog($"Arduino connected successfully! Response: {response.Trim()}", Color.FromArgb(46, 204, 113));
                            UpdateLogStatus("• Connected & listening");
                            
                            labelWeightValue.Text = "Waiting for data...";
                            labelWeightValue.ForeColor = Color.FromArgb(52, 152, 219);
                            
                            UpdateFoodLevelStatus("Waiting for data...", Color.FromArgb(52, 152, 219));
                        }
                        else
                        {
                            try
                            {
                                _serialPort.Close();
                            }
                            catch { }

                            label3.Text = "No device response on " + portName + ". Connection closed.";
                            AppendToLog($"No response from Arduino on {portName}. Connection closed.", Color.FromArgb(231, 76, 60));
                            UpdateLogStatus("• No response");
                            MessageBox.Show("Connected to " + portName + " but no response received. Ensure your Arduino is programmed to reply to a handshake (e.g. respond to 'PING').", "No Device Response", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        try { if (_serialPort.IsOpen) _serialPort.Close(); } catch { }
                        label3.Text = "Handshake error: " + ex.Message;
                        AppendToLog($"Handshake error: {ex.Message}", Color.FromArgb(231, 76, 60));
                        UpdateLogStatus("• Connection error");
                        MessageBox.Show("Error during handshake: " + ex.Message, "Handshake Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    label3.Text = "Already connected to " + _serialPort.PortName + ".";
                    AppendToLog($"Already connected to {_serialPort.PortName}", Color.FromArgb(243, 156, 18));
                }
            }
            catch (UnauthorizedAccessException uaEx)
            {
                label3.Text = "Access denied: " + uaEx.Message;
                AppendToLog($"Access denied: {uaEx.Message}", Color.FromArgb(231, 76, 60));
                UpdateLogStatus("• Access denied");
                MessageBox.Show("Access to " + portName + " is denied: " + uaEx.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (System.IO.IOException ioEx)
            {
                label3.Text = "I/O error: " + ioEx.Message;
                AppendToLog($"I/O error: {ioEx.Message}", Color.FromArgb(231, 76, 60));
                UpdateLogStatus("• I/O error");
                MessageBox.Show("I/O error opening " + portName + ": " + ioEx.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                label3.Text = "Unexpected error: " + ex.Message;
                AppendToLog($"Unexpected error: {ex.Message}", Color.FromArgb(231, 76, 60));
                UpdateLogStatus("• Connection error");
                MessageBox.Show("Unexpected error: " + ex.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        label3.Text = "Disconnected from " + _serialPort.PortName + ".";
                        AppendToLog($"Disconnected from {_serialPort.PortName}", Color.FromArgb(243, 156, 18));
                        UpdateLogStatus("• Disconnected");
                        
                        labelWeightValue.Text = "Not Connected";
                        labelWeightValue.ForeColor = Color.FromArgb(255, 165, 79);
                        
                        UpdateFoodLevelStatus("Not Connected", Color.FromArgb(255, 165, 79));
                    }
                    else
                    {
                        label3.Text = "Port already closed.";
                        AppendToLog("Port already closed", Color.FromArgb(149, 165, 166));
                    }

                    _serialPort.Dispose();
                    _serialPort = null;
                }
                else
                {
                    label3.Text = "No port to disconnect.";
                    AppendToLog("No port to disconnect", Color.FromArgb(149, 165, 166));
                }
            }
            catch (Exception ex)
            {
                label3.Text = "Error during disconnect: " + ex.Message;
                AppendToLog($"Error during disconnect: {ex.Message}", Color.FromArgb(231, 76, 60));
                UpdateLogStatus("• Disconnect error");
                MessageBox.Show("Error disconnecting: " + ex.Message, "Disconnect Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    AppendToLog("Application closing, disconnecting Arduino...", Color.FromArgb(243, 156, 18));
                    _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort = null;
                }

                label3.Text = "Disconnected";
                UpdateFoodLevelStatus("Disconnected", Color.FromArgb(149, 165, 166));
            }
            catch (Exception ex)
            {
                label3.Text = "Error closing port: " + ex.Message;
            }

            base.OnFormClosing(e);
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void labelWeightValue_Click(object sender, EventArgs e)
        {

        }
    }
}
