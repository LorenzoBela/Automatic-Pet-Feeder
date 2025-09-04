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
        // SerialPort para sa Arduino connection
        private SerialPort _serialPort;
        
        // Feeding schedule variables
        private DateTime? nextFeedingTime = null;
        private string feedingInterval = "Every Day";

        // Arduino log variables
        private const int MAX_LOG_LINES = 1000; // Maximum lines to keep in log
        private int logLineCount = 0;

        // Weight sensor variables
        private double currentWeight = 0.0;
        private double lastWeightReading = 0.0;
        private bool isWeightStable = false;

        public Form1()
        {
            InitializeComponent();
            // Default status: Ready
            label3.Text = "Ready";
            
            // Load saved feeding schedule
            LoadFeedingSchedule();
            
            // Start the timer for real-time updates
            timer1.Start();

            // Initialize Arduino log
            InitializeArduinoLog();

            // Initialize weight monitor
            InitializeWeightMonitor();

            // Wire up event handlers
            buttonClearLog.Click += buttonClearLog_Click;
        }

        private void InitializeWeightMonitor()
        {
            // Set initial weight status
            labelWeightValue.Text = "Not Connected";
            labelWeightValue.ForeColor = Color.FromArgb(255, 165, 79); // Orange
        }

        private void UpdateWeightDisplay(double weight)
        {
            try
            {
                // Ensure we're on the UI thread
                if (labelWeightValue.InvokeRequired)
                {
                    labelWeightValue.Invoke(new Action<double>(UpdateWeightDisplay), weight);
                    return;
                }

                currentWeight = weight;
                labelWeightValue.Text = $"{weight:F1}g";

                // Update color based on weight level
                if (weight >= 200) // Full bowl
                {
                    labelWeightValue.ForeColor = Color.FromArgb(46, 204, 113); // Green
                    labelWeightValue.Text += " (Full)";
                }
                else if (weight >= 50) // Moderate amount
                {
                    labelWeightValue.ForeColor = Color.FromArgb(243, 156, 18); // Orange
                    labelWeightValue.Text += " (Moderate)";
                }
                else if (weight >= 10) // Low amount
                {
                    labelWeightValue.ForeColor = Color.FromArgb(231, 76, 60); // Red
                    labelWeightValue.Text += " (Low)";
                }
                else // Empty or nearly empty
                {
                    labelWeightValue.ForeColor = Color.FromArgb(169, 68, 66); // Dark red
                    labelWeightValue.Text += " (Empty)";
                }

                // Check for weight stability (for feeding detection)
                CheckWeightStability(weight);

            }
            catch (Exception ex)
            {
                AppendToLog($"Error updating weight display: {ex.Message}", Color.FromArgb(231, 76, 60));
            }
        }

        private void CheckWeightStability(double weight)
        {
            // Check if weight has changed significantly (indicating feeding or dispensing)
            double weightDifference = Math.Abs(weight - lastWeightReading);
            
            if (weightDifference > 5.0) // Significant change (more than 5g)
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
            else if (weightDifference < 1.0) // Stable weight
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
            // Configure RichTextBox for smooth scrolling
            richTextBoxLog.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxLog.WordWrap = true;
            richTextBoxLog.DetectUrls = false;
            
            // Add welcome message
            AppendToLog("=== Arduino Log Monitor Started ===", Color.FromArgb(46, 204, 113));
            AppendToLog("Waiting for Arduino connection...", Color.FromArgb(149, 165, 166));
        }

        private void AppendToLog(string message, Color color)
        {
            try
            {
                // Ensure we're on the UI thread
                if (richTextBoxLog.InvokeRequired)
                {
                    richTextBoxLog.Invoke(new Action<string, Color>(AppendToLog), message, color);
                    return;
                }

                // Manage log size - remove old lines if too many
                if (logLineCount >= MAX_LOG_LINES)
                {
                    var lines = richTextBoxLog.Lines;
                    var newLines = new string[lines.Length - 100]; // Remove 100 old lines
                    Array.Copy(lines, 100, newLines, 0, newLines.Length);
                    richTextBoxLog.Lines = newLines;
                    logLineCount -= 100;
                }

                // Add timestamp and message
                string timeStamp = DateTime.Now.ToString("HH:mm:ss");
                string formattedMessage = $"[{timeStamp}] {message}";

                // Append text with color
                richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
                richTextBoxLog.SelectionLength = 0;
                richTextBoxLog.SelectionColor = color;
                richTextBoxLog.AppendText(formattedMessage + Environment.NewLine);
                
                // Auto-scroll to bottom
                richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
                richTextBoxLog.ScrollToCaret();

                logLineCount++;

                // Update status
                UpdateLogStatus("• Receiving data");
            }
            catch (Exception ex)
            {
                // Handle any logging errors silently
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
            labelLogStatus.ForeColor = Color.FromArgb(46, 204, 113); // Green
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
                // Load from application settings
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
                // If loading fails, use defaults
                nextFeedingTime = null;
                feedingInterval = "Every Day";
            }
        }

        private void SaveFeedingSchedule()
        {
            try
            {
                // Save to application settings
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
                // Ignore save errors for now
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Update current time display
            labelCurrentTime.Text = DateTime.Now.ToString("h:mm:ss tt");
            
            // Update countdown if feeding time is set
            UpdateCountdown();

            // Check for Arduino data if connected
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
                        // Split data by lines and process each
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
            Color messageColor = Color.FromArgb(189, 195, 199); // Default gray

            // Check for weight sensor data
            if (data.ToLower().Contains("weight:") || data.ToLower().Contains("w:"))
            {
                // Extract weight value from Arduino data
                // Expected format: "Weight: 123.4" or "W: 123.4"
                try
                {
                    string[] parts = data.Split(':');
                    if (parts.Length >= 2)
                    {
                        string weightStr = parts[1].Trim().Replace("g", "").Replace("grams", "");
                        if (double.TryParse(weightStr, out double weight))
                        {
                            UpdateWeightDisplay(weight);
                            messageColor = Color.FromArgb(155, 89, 182); // Purple for weight data
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendToLog($"Error parsing weight data: {ex.Message}", Color.FromArgb(231, 76, 60));
                }
            }
            // Determine color based on message content
            else if (data.ToLower().Contains("error") || data.ToLower().Contains("fail"))
            {
                messageColor = Color.FromArgb(231, 76, 60); // Red
            }
            else if (data.ToLower().Contains("warning") || data.ToLower().Contains("warn"))
            {
                messageColor = Color.FromArgb(243, 156, 18); // Orange
            }
            else if (data.ToLower().Contains("success") || data.ToLower().Contains("ok") || data.ToLower().Contains("complete"))
            {
                messageColor = Color.FromArgb(46, 204, 113); // Green
            }
            else if (data.ToLower().Contains("feed") || data.ToLower().Contains("dispense"))
            {
                messageColor = Color.FromArgb(52, 152, 219); // Blue
            }
            else if (data.ToLower().Contains("sensor") || data.ToLower().Contains("level"))
            {
                messageColor = Color.FromArgb(155, 89, 182); // Purple
            }

            AppendToLog($"Arduino: {data}", messageColor);
        }

        private void UpdateCountdown()
        {
            if (nextFeedingTime.HasValue)
            {
                TimeSpan timeLeft = nextFeedingTime.Value - DateTime.Now;
                
                if (timeLeft.TotalSeconds <= 0)
                {
                    // Time to feed! Calculate next feeding time
                    CalculateNextFeedingTime();
                    labelCountdown.Text = "FEEDING TIME!";
                    labelCountdown.ForeColor = Color.FromArgb(46, 204, 113); // Green
                    
                    // Log feeding event
                    AppendToLog("Scheduled feeding time reached!", Color.FromArgb(46, 204, 113));
                    return;
                }
                
                // Display countdown
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
                
                // Change color based on time left
                if (timeLeft.TotalMinutes <= 5)
                    labelCountdown.ForeColor = Color.FromArgb(231, 76, 60); // Red
                else if (timeLeft.TotalMinutes <= 30)
                    labelCountdown.ForeColor = Color.FromArgb(243, 156, 18); // Orange
                else
                    labelCountdown.ForeColor = Color.FromArgb(52, 152, 219); // Blue
            }
            else
            {
                labelCountdown.Text = "No schedule set yet";
                labelCountdown.ForeColor = Color.FromArgb(149, 165, 166); // Gray
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

        // Method to be called from Form2 when feeding schedule is saved
        public void UpdateFeedingSchedule(DateTime feedingTime, string interval)
        {
            nextFeedingTime = feedingTime;
            feedingInterval = interval;
            SaveFeedingSchedule(); // Save the new schedule
            UpdateCountdown();
            
            // Log schedule update
            AppendToLog($"Feeding schedule updated: {feedingTime:yyyy-MM-dd HH:mm} ({interval})", Color.FromArgb(52, 152, 219));
        }

        // Method to get current feeding schedule for Form2
        public void GetCurrentFeedingSchedule(out DateTime? currentNextFeedingTime, out string currentInterval)
        {
            currentNextFeedingTime = nextFeedingTime;
            currentInterval = feedingInterval;
        }

        // Method to clear the feeding schedule
        public void ClearFeedingSchedule()
        {
            nextFeedingTime = null;
            feedingInterval = "Every Day";
            SaveFeedingSchedule();
            UpdateCountdown();
            
            // Log schedule clear
            AppendToLog("Feeding schedule cleared", Color.FromArgb(243, 156, 18));
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Buksan ang bagong form (Form2)
            Form2 form2 = new Form2();
            form2.SetMainForm(this); // Pass reference to this form
            form2.Show(); // or form2.ShowDialog() para modal
            
            // Log form opening
            AppendToLog("Set Feeding Time form opened", Color.FromArgb(149, 165, 166));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Buksan ang Form3 para sa manual dispense
            Form3 form3 = new Form3();
            form3.Show(); // use ShowDialog() if you want it modal
            
            // Log form opening
            AppendToLog("Manual Dispense form opened", Color.FromArgb(149, 165, 166));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Default COM3 at 9600
            var portName = "COM3";
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
                    
                    // Add event handler for data received
                    _serialPort.DataReceived += SerialPort_DataReceived;
                }

                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();

                    try
                    {
                        // Bigyan ng konting oras ang device to reset/prepare
                        System.Threading.Thread.Sleep(200);

                        // Send 'PING' — Arduino should reply (e.g. 'PONG')
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
                            // Walang reply sa loob ng timeout
                        }

                        if (!string.IsNullOrEmpty(response))
                        {
                            label3.Text = "Connected to " + portName + " at " + baudRate + " baud. Reply: " + response;
                            AppendToLog($"Arduino connected successfully! Response: {response.Trim()}", Color.FromArgb(46, 204, 113));
                            UpdateLogStatus("• Connected & listening");
                            
                            // Update weight monitor status
                            labelWeightValue.Text = "Waiting for data...";
                            labelWeightValue.ForeColor = Color.FromArgb(52, 152, 219); // Blue
                        }
                        else
                        {
                            // Walang reply -> isara ang port, walang device na sumagot
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
                        // Kung may error sa handshake, i-close ang port
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
            // This event will be called when data is received from Arduino
            // The actual data reading is handled in CheckArduinoData() called by timer
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Disconnect button — disconnect sa port
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
                        
                        // Reset weight monitor
                        labelWeightValue.Text = "Not Connected";
                        labelWeightValue.ForeColor = Color.FromArgb(255, 165, 79); // Orange
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

        // Siguraduhing nakasara ang port kapag nagsara ang form
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
            }
            catch (Exception ex)
            {
                // Show error kung may problema sa closing
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
