using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Automatic_Pet_Feeder
{
    public partial class Form3 : Form
    {
        private Form1 mainForm;

        public Form3()
        {
            InitializeComponent();
        }

        public void SetMainForm(Form1 form)
        {
            mainForm = form;
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void buttonDispense_Click(object sender, EventArgs e)
        {
            try
            {
                int seconds = (int)numericUpDown1.Value;
                if (seconds <= 0)
                {
                    MessageBox.Show("Please enter a valid dispense time (1-30 seconds).", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Check if Arduino is connected
                if (mainForm == null || !mainForm.IsArduinoConnected())
                {
                    MessageBox.Show("Arduino is not connected. Please connect to Arduino first from the main form.", "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                buttonDispense.Enabled = false;
                progressBar1.Value = 0;
                label5.Text = "0%";

                // Measure response time
                var startTime = DateTime.Now;
                
                // Send dispense command to Arduino
                string command = $"DISPENSE_{seconds}";
                bool commandSent = mainForm.SendArduinoCommand(command);
                
                var responseTime = (DateTime.Now - startTime).TotalMilliseconds;
                
                if (!commandSent)
                {
                    MessageBox.Show("Failed to send command to Arduino. Please check the connection.", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                mainForm.LogMessage($"⚡ Command sent in {responseTime:F0}ms", Color.FromArgb(52, 152, 219));
                mainForm.LogMessage($"Manual dispense started: {seconds} seconds", Color.FromArgb(52, 152, 219));

                // Show progress for the duration of the dispense
                int totalSteps = Math.Max(1, seconds * 10); // update every 100ms
                for (int step = 1; step <= totalSteps; step++)
                {
                    await Task.Delay(100);
                    int percent = (int)Math.Min(100, (step * 100.0) / totalSteps);
                    progressBar1.Value = percent;
                    label5.Text = percent + "%";
                }

                mainForm.LogMessage($"Manual dispense completed: {seconds} seconds", Color.FromArgb(46, 204, 113));
                
                string completionMessage = $"Dispense complete! Food dispensed for {seconds} seconds.\n\nCommand Response Time: {responseTime:F0}ms";
                MessageBox.Show(completionMessage, "Dispense Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                mainForm?.LogMessage($"Error during manual dispense: {ex.Message}", Color.FromArgb(231, 76, 60));
                MessageBox.Show($"Error during dispense: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                buttonDispense.Enabled = true;
                progressBar1.Value = 0;
                label5.Text = "0%";
            }
        }

        // Get current weight reading from Arduino
        private void buttonGetWeight_Click(object sender, EventArgs e)
        {
            if (mainForm == null || !mainForm.IsArduinoConnected())
            {
                MessageBox.Show("Arduino is not connected.", "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            mainForm.LogMessage("🔍 Requesting current weight...", Color.FromArgb(52, 152, 219));
            
            var startTime = DateTime.Now;
            bool commandSent = mainForm.SendArduinoCommand("WEIGHT");
            var responseTime = (DateTime.Now - startTime).TotalMilliseconds;
            
            if (commandSent)
            {
                mainForm.LogMessage($"⚡ Weight request sent in {responseTime:F0}ms", Color.FromArgb(46, 204, 113));
                MessageBox.Show($"Weight reading requested successfully!\n\nResponse Time: {responseTime:F0}ms\n\nCheck the main form log for the weight value.", 
                              "Weight Request Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                mainForm.LogMessage("❌ Failed to request weight", Color.FromArgb(231, 76, 60));
                MessageBox.Show("Failed to send weight request to Arduino.", "Request Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Tare the weight scale
        private void buttonTare_Click(object sender, EventArgs e)
        {
            if (mainForm == null || !mainForm.IsArduinoConnected())
            {
                MessageBox.Show("Arduino is not connected.", "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("This will reset the scale to zero with the current load.\n\nRemove any weight from the scale before proceeding.\n\nContinue?", 
                                       "Tare Scale", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result != DialogResult.Yes) return;

            mainForm.LogMessage("⚖️ Taring weight scale...", Color.FromArgb(52, 152, 219));
            
            var startTime = DateTime.Now;
            bool commandSent = mainForm.SendArduinoCommand("TARE");
            var responseTime = (DateTime.Now - startTime).TotalMilliseconds;
            
            if (commandSent)
            {
                mainForm.LogMessage($"⚡ Tare command sent in {responseTime:F0}ms", Color.FromArgb(46, 204, 113));
                MessageBox.Show($"Scale tare initiated successfully!\n\nResponse Time: {responseTime:F0}ms\n\nThe scale will reset to zero.", 
                              "Tare Started", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                mainForm.LogMessage("❌ Failed to send tare command", Color.FromArgb(231, 76, 60));
                MessageBox.Show("Failed to send tare command to Arduino.", "Tare Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Start calibration process
        private void buttonCalibrate_Click(object sender, EventArgs e)
        {
            if (mainForm == null || !mainForm.IsArduinoConnected())
            {
                MessageBox.Show("Arduino is not connected.", "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("This will start the scale calibration process.\n\nYou will need a known weight (like a 100g item) for calibration.\n\nIMPORTANT: Follow the instructions in the main form log carefully.\n\nContinue?", 
                                       "Start Calibration", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result != DialogResult.Yes) return;

            mainForm.LogMessage("🎯 Starting scale calibration process...", Color.FromArgb(155, 89, 182));
            mainForm.LogMessage("📋 Follow the calibration instructions below:", Color.FromArgb(155, 89, 182));
            
            var startTime = DateTime.Now;
            bool commandSent = mainForm.SendArduinoCommand("CAL");
            var responseTime = (DateTime.Now - startTime).TotalMilliseconds;
            
            if (commandSent)
            {
                mainForm.LogMessage($"⚡ Calibration command sent in {responseTime:F0}ms", Color.FromArgb(46, 204, 113));
                MessageBox.Show($"Calibration process started!\n\nResponse Time: {responseTime:F0}ms\n\nIMPORTANT: Check the main form log and follow the calibration instructions step by step.", 
                              "Calibration Started", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                mainForm.LogMessage("❌ Failed to start calibration", Color.FromArgb(231, 76, 60));
                MessageBox.Show("Failed to send calibration command to Arduino.", "Calibration Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Get comprehensive system status
        private void buttonSystemStatus_Click(object sender, EventArgs e)
        {
            if (mainForm == null || !mainForm.IsArduinoConnected())
            {
                MessageBox.Show("Arduino is not connected.", "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            mainForm.LogMessage("📊 Requesting system status...", Color.FromArgb(155, 89, 182));
            
            var startTime = DateTime.Now;
            bool commandSent = mainForm.SendArduinoCommand("STATUS");
            var responseTime = (DateTime.Now - startTime).TotalMilliseconds;
            
            if (commandSent)
            {
                mainForm.LogMessage($"⚡ Status request sent in {responseTime:F0}ms", Color.FromArgb(46, 204, 113));
                MessageBox.Show($"System status requested successfully!\n\nResponse Time: {responseTime:F0}ms\n\nCheck the main form log for detailed status information.", 
                              "Status Request Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                mainForm.LogMessage("❌ Failed to request system status", Color.FromArgb(231, 76, 60));
                MessageBox.Show("Failed to send status request to Arduino.", "Status Request Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Get immediate distance reading from Arduino
        private void buttonGetDistance_Click(object sender, EventArgs e)
        {
            if (mainForm == null || !mainForm.IsArduinoConnected())
            {
                MessageBox.Show("Arduino is not connected.", "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            mainForm.LogMessage("📏 Requesting detailed distance reading with diagnostics...", Color.FromArgb(52, 152, 219));
            
            var startTime = DateTime.Now;
            bool commandSent = mainForm.SendArduinoCommand("DISTANCE");
            var responseTime = (DateTime.Now - startTime).TotalMilliseconds;
            
            if (commandSent)
            {
                mainForm.LogMessage($"⚡ Distance request sent in {responseTime:F0}ms", Color.FromArgb(46, 204, 113));
                MessageBox.Show($"Distance reading with diagnostics requested!\n\nResponse Time: {responseTime:F0}ms\n\nCheck the main form log for detailed sensor diagnostics.", 
                              "Distance Diagnostic Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                mainForm.LogMessage("❌ Failed to request distance", Color.FromArgb(231, 76, 60));
                MessageBox.Show("Failed to send distance request to Arduino.", "Request Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Add new button for ultrasonic sensor diagnostics
        private void buttonUltrasonicTest_Click(object sender, EventArgs e)
        {
            if (mainForm == null || !mainForm.IsArduinoConnected())
            {
                MessageBox.Show("Arduino is not connected.", "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("This will perform a comprehensive test of the ultrasonic sensor.\n\nIt will run multiple readings and show detailed diagnostics.\n\nContinue?", 
                                       "Ultrasonic Sensor Test", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result != DialogResult.Yes) return;

            mainForm.LogMessage("🔍 Starting ultrasonic sensor comprehensive test...", Color.FromArgb(155, 89, 182));
            
            var startTime = DateTime.Now;
            bool commandSent = mainForm.SendArduinoCommand("ULTRA_TEST");
            var responseTime = (DateTime.Now - startTime).TotalMilliseconds;
            
            if (commandSent)
            {
                mainForm.LogMessage($"⚡ Ultrasonic test command sent in {responseTime:F0}ms", Color.FromArgb(46, 204, 113));
                MessageBox.Show($"Ultrasonic sensor test started!\n\nResponse Time: {responseTime:F0}ms\n\nMonitor the main form log for detailed test results and diagnostics.", 
                              "Sensor Test Started", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                mainForm.LogMessage("❌ Failed to start ultrasonic sensor test", Color.FromArgb(231, 76, 60));
                MessageBox.Show("Failed to send ultrasonic test command to Arduino.", "Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Add new button for ultrasonic sensor statistics
        private void buttonUltrasonicStats_Click(object sender, EventArgs e)
        {
            if (mainForm == null || !mainForm.IsArduinoConnected())
            {
                MessageBox.Show("Arduino is not connected.", "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            mainForm.LogMessage("📊 Requesting ultrasonic sensor statistics...", Color.FromArgb(155, 89, 182));
            
            var startTime = DateTime.Now;
            bool commandSent = mainForm.SendArduinoCommand("ULTRA_STATS");
            var responseTime = (DateTime.Now - startTime).TotalMilliseconds;
            
            if (commandSent)
            {
                mainForm.LogMessage($"⚡ Statistics request sent in {responseTime:F0}ms", Color.FromArgb(46, 204, 113));
                MessageBox.Show($"Ultrasonic sensor statistics requested!\n\nResponse Time: {responseTime:F0}ms\n\nCheck the main form log for comprehensive sensor performance data.", 
                              "Statistics Requested", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                mainForm.LogMessage("❌ Failed to request sensor statistics", Color.FromArgb(231, 76, 60));
                MessageBox.Show("Failed to send statistics request to Arduino.", "Request Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Add new button to reset ultrasonic sensor statistics
        private void buttonResetUltrasonicStats_Click(object sender, EventArgs e)
        {
            if (mainForm == null || !mainForm.IsArduinoConnected())
            {
                MessageBox.Show("Arduino is not connected.", "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("This will reset all ultrasonic sensor statistics to zero.\n\nThis is useful for starting fresh diagnostics.\n\nContinue?", 
                                       "Reset Sensor Statistics", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result != DialogResult.Yes) return;

            mainForm.LogMessage("🔄 Resetting ultrasonic sensor statistics...", Color.FromArgb(52, 152, 219));
            
            var startTime = DateTime.Now;
            bool commandSent = mainForm.SendArduinoCommand("ULTRA_RESET");
            var responseTime = (DateTime.Now - startTime).TotalMilliseconds;
            
            if (commandSent)
            {
                mainForm.LogMessage($"⚡ Reset command sent in {responseTime:F0}ms", Color.FromArgb(46, 204, 113));
                MessageBox.Show($"Ultrasonic sensor statistics reset!\n\nResponse Time: {responseTime:F0}ms\n\nStatistics tracking restarted from zero.", 
                              "Statistics Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                mainForm.LogMessage("❌ Failed to reset sensor statistics", Color.FromArgb(231, 76, 60));
                MessageBox.Show("Failed to send reset command to Arduino.", "Reset Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
