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
using System.Collections.Concurrent;
using System.Threading;
using Timer = System.Threading.Timer;

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

        // Performance optimization for logging
        private readonly ConcurrentQueue<LogEntry> _logQueue = new ConcurrentQueue<LogEntry>();
        private readonly Timer _logProcessingTimer;
        private readonly object _logLock = new object();
        private bool _isProcessingLogs = false;
        private DateTime _lastLogUpdate = DateTime.MinValue;
        private readonly StringBuilder _logBuffer = new StringBuilder(10000);
        
        // Rate limiting for frequent messages
        private readonly Dictionary<string, DateTime> _lastMessageTimes = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, int> _messageCountSinceLastLog = new Dictionary<string, int>();
        private const int MESSAGE_THROTTLE_MS = 100; // Minimum time between similar messages
        private const int MAX_DUPLICATE_SUPPRESS = 10; // Max duplicates to suppress before showing summary

        private DateTime _lastLogStatusUpdate = DateTime.MinValue;

        private struct LogEntry
        {
            public string Message;
            public Color Color;
            public DateTime Timestamp;
            public bool IsThrottled;
            public int SuppressedCount;
        }

        public Form1()
        {
            InitializeComponent();
            label3.Text = "Ready";
            
            LoadFeedingSchedule();
            
            timer1.Start();

            InitializeArduinoLog();

            InitializeWeightMonitor();

            buttonClearLog.Click += buttonClearLog_Click;

            // Initialize optimized logging timer
            _logProcessingTimer = new Timer(ProcessLogQueue, null, 50, 50); // Process logs every 50ms
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
            // Add to queue for batch processing instead of immediate UI update
            _logQueue.Enqueue(new LogEntry
            {
                Message = message,
                Color = color,
                Timestamp = DateTime.Now,
                IsThrottled = false,
                SuppressedCount = 0
            });
        }

        private void AppendToLogThrottled(string message, Color color, string messageKey)
        {
            DateTime now = DateTime.Now;
            
            // Check if we should throttle this message
            if (_lastMessageTimes.ContainsKey(messageKey))
            {
                var timeSinceLastMessage = (now - _lastMessageTimes[messageKey]).TotalMilliseconds;
                
                if (timeSinceLastMessage < MESSAGE_THROTTLE_MS)
                {
                    // Increment suppressed count
                    if (_messageCountSinceLastLog.ContainsKey(messageKey))
                        _messageCountSinceLastLog[messageKey]++;
                    else
                        _messageCountSinceLastLog[messageKey] = 1;
                    
                    // Don't log if we haven't reached the threshold yet
                    if (_messageCountSinceLastLog[messageKey] < MAX_DUPLICATE_SUPPRESS)
                        return;
                    
                    // Log a summary message
                    var suppressedCount = _messageCountSinceLastLog[messageKey];
                    _logQueue.Enqueue(new LogEntry
                    {
                        Message = $"{message} (+{suppressedCount} similar messages suppressed)",
                        Color = Color.FromArgb(149, 165, 166),
                        Timestamp = now,
                        IsThrottled = true,
                        SuppressedCount = suppressedCount
                    });
                    
                    _messageCountSinceLastLog[messageKey] = 0;
                    _lastMessageTimes[messageKey] = now;
                    return;
                }
            }
            
            // Normal message - update tracking and log
            _lastMessageTimes[messageKey] = now;
            _messageCountSinceLastLog[messageKey] = 0;
            
            _logQueue.Enqueue(new LogEntry
            {
                Message = message,
                Color = color,
                Timestamp = now,
                IsThrottled = false,
                SuppressedCount = 0
            });
        }

        private void ProcessLogQueue(object state)
        {
            // Prevent multiple timer executions from overlapping
            if (_isProcessingLogs) return;
            
            lock (_logLock)
            {
                if (_isProcessingLogs) return;
                _isProcessingLogs = true;
            }

            try
            {
                // Only update UI if we're not overwhelmed and some time has passed
                var now = DateTime.Now;
                if ((now - _lastLogUpdate).TotalMilliseconds < 50 && _logQueue.Count < 50)
                    return;

                var entriesToProcess = new List<LogEntry>();
                var maxEntries = Math.Min(20, _logQueue.Count); // Process max 20 entries at once
                
                for (int i = 0; i < maxEntries; i++)
                {
                    if (_logQueue.TryDequeue(out LogEntry entry))
                        entriesToProcess.Add(entry);
                    else
                        break;
                }

                if (entriesToProcess.Count > 0)
                {
                    // Update UI in one batch operation
                    if (richTextBoxLog.InvokeRequired)
                    {
                        richTextBoxLog.Invoke(new Action<List<LogEntry>>(UpdateLogUIBatch), entriesToProcess);
                    }
                    else
                    {
                        UpdateLogUIBatch(entriesToProcess);
                    }
                    
                    _lastLogUpdate = now;
                }
            }
            finally
            {
                _isProcessingLogs = false;
            }
        }

        private void UpdateLogUIBatch(List<LogEntry> entries)
        {
            try
            {
                // Suspend layout to improve performance
                richTextBoxLog.SuspendLayout();
                
                // Check if we need to trim the log
                if (logLineCount + entries.Count >= MAX_LOG_LINES)
                {
                    var lines = richTextBoxLog.Lines;
                    var removeCount = Math.Min(200, lines.Length / 2); // Remove more lines at once
                    var newLines = new string[lines.Length - removeCount];
                    Array.Copy(lines, removeCount, newLines, 0, newLines.Length);
                    richTextBoxLog.Lines = newLines;
                    logLineCount -= removeCount;
                }

                // Build all messages in a buffer first
                _logBuffer.Clear();
                foreach (var entry in entries)
                {
                    string timeStamp = entry.Timestamp.ToString("HH:mm:ss");
                    string formattedMessage = $"[{timeStamp}] {entry.Message}";
                    _logBuffer.AppendLine(formattedMessage);
                }

                // Append all text at once
                int startPosition = richTextBoxLog.TextLength;
                richTextBoxLog.AppendText(_logBuffer.ToString());

                // Apply colors in batches
                int currentPosition = startPosition;
                foreach (var entry in entries)
                {
                    string timeStamp = entry.Timestamp.ToString("HH:mm:ss");
                    string formattedMessage = $"[{timeStamp}] {entry.Message}";
                    int messageLength = formattedMessage.Length + Environment.NewLine.Length;
                    
                    richTextBoxLog.Select(currentPosition, messageLength - Environment.NewLine.Length);
                    richTextBoxLog.SelectionColor = entry.Color;
                    currentPosition += messageLength;
                }

                // Reset selection and scroll to bottom
                richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
                richTextBoxLog.SelectionLength = 0;
                richTextBoxLog.ScrollToCaret();

                logLineCount += entries.Count;

                // Update status less frequently
                if (entries.Count > 0)
                {
                    UpdateLogStatus("• Receiving data");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log batch update error: {ex.Message}");
            }
            finally
            {
                richTextBoxLog.ResumeLayout();
            }
        }

        private void UpdateLogStatus(string status)
        {
            // Only update status every 500ms to reduce UI updates
            var now = DateTime.Now;
            if ((now - _lastLogStatusUpdate).TotalMilliseconds < 500)
                return;
                
            _lastLogStatusUpdate = now;
            
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
                    // Read larger chunks of data at once
                    int bytesToRead = _serialPort.BytesToRead;
                    
                    // Prevent reading too much data at once to avoid UI freezing
                    if (bytesToRead > 4096)
                    {
                        // If there's too much data, read in chunks and process
                        bytesToRead = 2048;
                    }
                    
                    string data = _serialPort.ReadExisting();
                    if (!string.IsNullOrEmpty(data))
                    {
                        // Process data more efficiently
                        ProcessArduinoDataBatch(data);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendToLog($"Error reading Arduino data: {ex.Message}", Color.FromArgb(231, 76, 60));
            }
        }

        private void ProcessArduinoDataBatch(string data)
        {
            // Split data into lines more efficiently
            string[] lines = data.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            // If too many lines, process only the most recent ones to prevent UI lag
            if (lines.Length > 20)
            {
                // Log a summary of skipped data
                int skippedLines = lines.Length - 15;
                AppendToLog($"High data rate detected: Processing latest 15 lines, skipped {skippedLines} older entries", 
                           Color.FromArgb(243, 156, 18));
                
                // Process only the last 15 lines
                lines = lines.Skip(lines.Length - 15).ToArray();
            }
            
            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    ProcessArduinoData(line.Trim());
                }
            }
        }

        private void ProcessArduinoData(string data)
        {
            Color messageColor = Color.FromArgb(189, 195, 199);
            string messageKey = "general"; // Default key for throttling

            if (data.Contains("Food available") || data.Contains("✅"))
            {
                UpdateFoodLevelStatus("Food Available", Color.FromArgb(46, 204, 113));
                messageColor = Color.FromArgb(46, 204, 113);
                messageKey = "food_available";
            }
            else if (data.Contains("Storage low") || data.Contains("Storage empty") || data.Contains("⚠️"))
            {
                UpdateFoodLevelStatus("Low/Empty", Color.FromArgb(231, 76, 60));
                messageColor = Color.FromArgb(231, 76, 60);
                messageKey = "storage_low";
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
                    messageKey = "distance_reading";
                }
                catch (Exception ex)
                {
                    AppendToLog($"Error parsing distance data: {ex.Message}", Color.FromArgb(231, 76, 60));
                    return;
                }
            }
            else if (data.Contains("Ultrasonic timeout") || data.Contains("no echo"))
            {
                UpdateFoodLevelStatus("Sensor Error", Color.FromArgb(243, 156, 18));
                messageColor = Color.FromArgb(243, 156, 18);
                messageKey = "sensor_error";
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
                    messageKey = "weight_reading";
                }
                catch (Exception ex)
                {
                    AppendToLog($"Error parsing weight data: {ex.Message}", Color.FromArgb(231, 76, 60));
                    return;
                }
            }
            else if (data.ToLower().Contains("error") || data.ToLower().Contains("fail"))
            {
                messageColor = Color.FromArgb(231, 76, 60);
                messageKey = "error";
            }
            else if (data.ToLower().Contains("warning") || data.ToLower().Contains("warn"))
            {
                messageColor = Color.FromArgb(243, 156, 18);
                messageKey = "warning";
            }
            else if (data.ToLower().Contains("success") || data.ToLower().Contains("ok") || data.ToLower().Contains("complete"))
            {
                messageColor = Color.FromArgb(46, 204, 113);
                messageKey = "success";
            }
            else if (data.ToLower().Contains("feed") || data.ToLower().Contains("dispense"))
            {
                messageColor = Color.FromArgb(52, 152, 219);
                messageKey = "feed_dispense";
            }
            else if (data.ToLower().Contains("sensor") || data.ToLower().Contains("level"))
            {
                messageColor = Color.FromArgb(155, 89, 182);
                messageKey = "sensor";
            }

            // Use throttled logging for frequent sensor readings
            if (messageKey == "distance_reading" || messageKey == "weight_reading" || messageKey == "sensor")
            {
                AppendToLogThrottled($"Arduino: {data}", messageColor, messageKey);
            }
            else
            {
                // Use normal logging for important messages
                AppendToLog($"Arduino: {data}", messageColor);
            }
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

                // Only update and log if the status actually changed
                if (label7.Text != status)
                {
                    label7.Text = status;
                    label7.ForeColor = color;

                    AppendToLog($"Food level status updated: {status}", color);
                }
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
                    // Skip all past feeding times and jump to the next future feeding
                    bool wasRecentMiss = timeLeft.TotalMinutes >= -5; // Within last 5 minutes
                    
                    // Only show "FEEDING TIME!" and trigger feeding if the missed time was recent
                    if (wasRecentMiss)
                    {
                        labelCountdown.Text = "FEEDING TIME!";
                        labelCountdown.ForeColor = Color.FromArgb(46, 204, 113);
                        AppendToLog("Scheduled feeding time reached!", Color.FromArgb(46, 204, 113));
                        
                        // Trigger automatic feeding using the configured duration
                        TriggerAutomaticFeeding();
                    }
                    
                    SkipToNextFutureFeeding();
                    
                    if (!wasRecentMiss)
                    {
                        AppendToLog($"Skipped missed feeding times. Next feeding scheduled for {nextFeedingTime.Value:MMM dd, h:mm tt}", Color.FromArgb(243, 156, 18));
                        // Recalculate timeLeft for display
                        timeLeft = nextFeedingTime.Value - DateTime.Now;
                    }
                }
                
                // Display countdown if we have a future feeding time
                if (timeLeft.TotalSeconds > 0)
                {
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
            }
            else
            {
                labelCountdown.Text = "No schedule set yet";
                labelCountdown.ForeColor = Color.FromArgb(149, 165, 166);
            }
        }

        private void TriggerAutomaticFeeding(int? customSeconds = null)
        {
            try
            {
                // Check if Arduino is connected
                if (!IsArduinoConnected())
                {
                    AppendToLog("Cannot trigger automatic feeding: Arduino not connected", Color.FromArgb(231, 76, 60));
                    return;
                }

                // Use custom seconds if provided, otherwise use setting
                int seconds = customSeconds ?? Properties.Settings.Default.AutoFeedDuration;
                
                // Ensure reasonable bounds (1-30 seconds)
                seconds = Math.Max(1, Math.Min(30, seconds));

                // Send dispense command to Arduino
                string command = $"FEED_{seconds}";
                bool commandSent = SendArduinoCommand(command);
                
                if (commandSent)
                {
                    AppendToLog($"Automatic feeding triggered: {seconds} seconds", Color.FromArgb(46, 204, 113));
                }
                else
                {
                    AppendToLog("Failed to send automatic feeding command to Arduino", Color.FromArgb(231, 76, 60));
                }
            }
            catch (Exception ex)
            {
                AppendToLog($"Error during automatic feeding: {ex.Message}", Color.FromArgb(231, 76, 60));
            }
        }

        private void SkipToNextFutureFeeding()
        {
            if (!nextFeedingTime.HasValue) return;
            
            DateTime now = DateTime.Now;
            DateTime originalTime = nextFeedingTime.Value;
            
            // If the scheduled time is in the future, no need to skip
            if (nextFeedingTime.Value > now) return;
            
            // Calculate how many intervals have passed and skip to next future feeding
            switch (feedingInterval)
            {
                case "Every Day":
                    int daysToAdd = (int)Math.Ceiling((now - originalTime).TotalDays);
                    nextFeedingTime = originalTime.AddDays(daysToAdd);
                    break;
                    
                case "Every 2 Hours":
                    int intervals2h = (int)Math.Ceiling((now - originalTime).TotalHours / 2);
                    nextFeedingTime = originalTime.AddHours(intervals2h * 2);
                    break;
                    
                case "Every 3 Hours":
                    int intervals3h = (int)Math.Ceiling((now - originalTime).TotalHours / 3);
                    nextFeedingTime = originalTime.AddHours(intervals3h * 3);
                    break;
                    
                case "Every 4 Hours":
                    int intervals4h = (int)Math.Ceiling((now - originalTime).TotalHours / 4);
                    nextFeedingTime = originalTime.AddHours(intervals4h * 4);
                    break;
                    
                case "Every 6 Hours":
                    int intervals6h = (int)Math.Ceiling((now - originalTime).TotalHours / 6);
                    nextFeedingTime = originalTime.AddHours(intervals6h * 6);
                    break;
                    
                case "Every 8 Hours":
                    int intervals8h = (int)Math.Ceiling((now - originalTime).TotalHours / 8);
                    nextFeedingTime = originalTime.AddHours(intervals8h * 8);
                    break;
                    
                case "Every 12 Hours":
                    int intervals12h = (int)Math.Ceiling((now - originalTime).TotalHours / 12);
                    nextFeedingTime = originalTime.AddHours(intervals12h * 12);
                    break;
                    
                case "Twice a Day":
                    // Find next occurrence of the feeding time (12 hours apart)
                    DateTime baseFeedingTime = new DateTime(now.Year, now.Month, now.Day, originalTime.Hour, originalTime.Minute, 0);
                    DateTime secondFeeding = baseFeedingTime.AddHours(12);
                    
                    if (baseFeedingTime > now)
                        nextFeedingTime = baseFeedingTime;
                    else if (secondFeeding > now)
                        nextFeedingTime = secondFeeding;
                    else
                        nextFeedingTime = baseFeedingTime.AddDays(1);
                    break;
                    
                case "Three Times a Day":
                    // Find next occurrence of the feeding time (8 hours apart)
                    DateTime baseFeeding = new DateTime(now.Year, now.Month, now.Day, originalTime.Hour, originalTime.Minute, 0);
                    DateTime secondFeed = baseFeeding.AddHours(8);
                    DateTime thirdFeed = baseFeeding.AddHours(16);
                    
                    if (baseFeeding > now)
                        nextFeedingTime = baseFeeding;
                    else if (secondFeed > now)
                        nextFeedingTime = secondFeed;
                    else if (thirdFeed > now)
                        nextFeedingTime = thirdFeed;
                    else
                        nextFeedingTime = baseFeeding.AddDays(1);
                    break;
                    
                default:
                    // Default to daily if unknown interval
                    int defaultDaysToAdd = (int)Math.Ceiling((now - originalTime).TotalDays);
                    nextFeedingTime = originalTime.AddDays(defaultDaysToAdd);
                    break;
            }
            
            // Save the updated schedule
            SaveFeedingSchedule();
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
            form3.SetMainForm(this);
            form3.Show();
            
            AppendToLog("Manual Dispense form opened", Color.FromArgb(149, 165, 166));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Try to auto-detect Arduino port with COM8 as first priority
            string[] possiblePorts = { "COM8", "COM3", "COM4", "COM5", "COM6", "COM7", "COM9" };
            string portName = null;
            var baudRate = 9600;

            try
            {
                label3.Text = "Checking available ports...";
                AppendToLog("Attempting to connect to Arduino...", Color.FromArgb(243, 156, 18));
                
                var ports = SerialPort.GetPortNames();
                AppendToLog($"Available ports: {string.Join(", ", ports)}", Color.FromArgb(149, 165, 166));

                // First try to find a port that's in our possible ports list (COM8 first)
                foreach (string testPort in possiblePorts)
                {
                    if (ports.Contains(testPort))
                    {
                        portName = testPort;
                        AppendToLog($"Found preferred port: {portName}", Color.FromArgb(52, 152, 219));
                        break;
                    }
                }

                // If no common port found, use the first available port
                if (portName == null && ports.Length > 0)
                {
                    portName = ports[0];
                    AppendToLog($"Using first available port: {portName}", Color.FromArgb(243, 156, 18));
                }

                if (portName == null)
                {
                    label3.Text = "No COM ports found";
                    AppendToLog("No COM ports available", Color.FromArgb(231, 76, 60));
                    MessageBox.Show("No COM ports found. Please check if Arduino is connected.", "No Ports Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    UpdateLogStatus("• No ports found");
                    return;
                }

                label3.Text = "Connecting to " + portName + "...";
                AppendToLog($"Trying to connect to {portName} at {baudRate} baud...", Color.FromArgb(52, 152, 219));

                if (_serialPort == null)
                {
                    _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                    {
                        Handshake = Handshake.None,
                        ReadTimeout = 2000,  // Increased timeout
                        WriteTimeout = 2000,
                        NewLine = "\n"
                    };
                    
                    _serialPort.DataReceived += SerialPort_DataReceived;
                }

                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();

                    try
                    {
                        System.Threading.Thread.Sleep(1000);  // Give Arduino more time to boot

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
                            AppendToLog("No response to PING - trying again...", Color.FromArgb(243, 156, 18));
                            System.Threading.Thread.Sleep(500);
                            _serialPort.WriteLine("PING");
                            try
                            {
                                response = _serialPort.ReadLine();
                            }
                            catch (TimeoutException)
                            {
                                // Still no response, but continue anyway
                                AppendToLog("No PING response, but connection established", Color.FromArgb(243, 156, 18));
                            }
                        }

                        if (!string.IsNullOrEmpty(response) && response.Trim() == "PONG")
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
                            // Connection established but no proper handshake - still usable
                            label3.Text = "Connected to " + portName + " (no handshake response)";
                            AppendToLog($"Connected to {portName} but no PONG response received", Color.FromArgb(243, 156, 18));
                            AppendToLog("Connection may still work for sending commands", Color.FromArgb(243, 156, 18));
                            UpdateLogStatus("• Connected (no handshake)");
                            
                            labelWeightValue.Text = "Connected (no handshake)";
                            labelWeightValue.ForeColor = Color.FromArgb(243, 156, 18);
                            
                            UpdateFoodLevelStatus("Connected (no handshake)", Color.FromArgb(243, 156, 18));
                        }
                    }
                    catch (Exception ex)
                    {
                        try { if (_serialPort.IsOpen) _serialPort.Close(); } catch { }
                        label3.Text = "Handshake error: " + ex.Message;
                        AppendToLog($"Handshake error: {ex.Message}", Color.FromArgb(231, 76, 60));
                        UpdateLogStatus("• Connection error");
                        MessageBox.Show("Error during handshake: " + ex.Message + "\n\nMake sure your Arduino is running the correct sketch.", "Handshake Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("Access to " + (portName ?? "port") + " is denied. Port may be in use by another application.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (System.IO.IOException ioEx)
            {
                label3.Text = "I/O error: " + ioEx.Message;
                AppendToLog($"I/O error: {ioEx.Message}", Color.FromArgb(231, 76, 60));
                UpdateLogStatus("• I/O error");
                MessageBox.Show("I/O error opening " + (portName ?? "port") + ": " + ioEx.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                // Dispose of the log processing timer
                _logProcessingTimer?.Dispose();
                
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

        // Public methods for Arduino communication (used by Form3)
        public bool IsArduinoConnected()
        {
            return _serialPort != null && _serialPort.IsOpen;
        }

        public bool SendArduinoCommand(string command)
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.WriteLine(command);
                    AppendToLog($"Sent command to Arduino: {command}", Color.FromArgb(52, 152, 219));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                AppendToLog($"Error sending command to Arduino: {ex.Message}", Color.FromArgb(231, 76, 60));
                return false;
            }
        }

        public void LogMessage(string message, Color color)
        {
            AppendToLog(message, color);
        }
    }
}
