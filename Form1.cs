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

        public Form1()
        {
            InitializeComponent();
            // Default status: Ready
            label3.Text = "Ready";
            
            // Load saved feeding schedule
            LoadFeedingSchedule();
            
            // Start the timer for real-time updates
            timer1.Start();
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
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Buksan ang Form3 para sa manual dispense
            Form3 form3 = new Form3();
            form3.Show(); // use ShowDialog() if you want it modal
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Default COM3 at 9600
            var portName = "COM3";
            var baudRate = 9600;

            try
            {
                label3.Text = "Checking available ports...";
                var ports = SerialPort.GetPortNames();

                if (!ports.Contains(portName))
                {
                    label3.Text = "Port " + portName + " not found. Available: " + string.Join(", ", ports);
                    MessageBox.Show("Port " + portName + " not found. Available ports: " + (ports.Length == 0 ? "(none)" : string.Join(", ", ports)), "Port Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                label3.Text = "Connecting to " + portName + "...";

                if (_serialPort == null)
                {
                    _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                    {
                        Handshake = Handshake.None,
                        ReadTimeout = 1000,
                        WriteTimeout = 1000,
                        NewLine = "\n"
                    };
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
                            MessageBox.Show("Connected to " + portName + " but no response received. Ensure your Arduino is programmed to reply to a handshake (e.g. respond to 'PING').", "No Device Response", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Kung may error sa handshake, i-close ang port
                        try { if (_serialPort.IsOpen) _serialPort.Close(); } catch { }
                        label3.Text = "Handshake error: " + ex.Message;
                        MessageBox.Show("Error during handshake: " + ex.Message, "Handshake Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    label3.Text = "Already connected to " + _serialPort.PortName + ".";
                }
            }
            catch (UnauthorizedAccessException uaEx)
            {
                label3.Text = "Access denied: " + uaEx.Message;
                MessageBox.Show("Access to " + portName + " is denied: " + uaEx.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (System.IO.IOException ioEx)
            {
                label3.Text = "I/O error: " + ioEx.Message;
                MessageBox.Show("I/O error opening " + portName + ": " + ioEx.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                label3.Text = "Unexpected error: " + ex.Message;
                MessageBox.Show("Unexpected error: " + ex.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                    }
                    else
                    {
                        label3.Text = "Port already closed.";
                    }

                    _serialPort.Dispose();
                    _serialPort = null;
                }
                else
                {
                    label3.Text = "No port to disconnect.";
                }
            }
            catch (Exception ex)
            {
                label3.Text = "Error during disconnect: " + ex.Message;
                MessageBox.Show("Error disconnecting: " + ex.Message, "Disconnect Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Siguraduhing nakasara ang port kapag nagsara ang form
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // Save feeding schedule before closing
                SaveFeedingSchedule();
                
                if (_serialPort != null && _serialPort.IsOpen)
                {
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
    }
}
