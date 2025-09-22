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

                mainForm.LogMessage($"?? Command sent in {responseTime:F0}ms", Color.FromArgb(52, 152, 219));
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

        // Test LED toggle response times
        private async void buttonTestLED_Click(object sender, EventArgs e)
        {
            if (mainForm == null || !mainForm.IsArduinoConnected())
            {
                MessageBox.Show("Arduino is not connected.", "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            mainForm.LogMessage("?? Testing LED toggle response times...", Color.FromArgb(52, 152, 219));

            try
            {
                var responses = new List<double>();

                // Test LED ON
                var startTime = DateTime.Now;
                if (mainForm.SendArduinoCommand("LED_ON"))
                {
                    var ledOnTime = (DateTime.Now - startTime).TotalMilliseconds;
                    responses.Add(ledOnTime);
                    mainForm.LogMessage($"?? LED ON response: {ledOnTime:F0}ms", Color.FromArgb(46, 204, 113));
                }
                else
                {
                    mainForm.LogMessage($"?? LED ON failed to send", Color.FromArgb(231, 76, 60));
                }

                await Task.Delay(1000);

                // Test LED OFF
                startTime = DateTime.Now;
                if (mainForm.SendArduinoCommand("LED_OFF"))
                {
                    var ledOffTime = (DateTime.Now - startTime).TotalMilliseconds;
                    responses.Add(ledOffTime);
                    mainForm.LogMessage($"?? LED OFF response: {ledOffTime:F0}ms", Color.FromArgb(46, 204, 113));
                }
                else
                {
                    mainForm.LogMessage($"?? LED OFF failed to send", Color.FromArgb(231, 76, 60));
                }

                // Calculate average response time
                if (responses.Any())
                {
                    double avgResponseTime = responses.Average();
                    mainForm.LogMessage($"?? Average LED response time: {avgResponseTime:F1}ms", Color.FromArgb(155, 89, 182));
                    
                    MessageBox.Show($"LED toggle test completed!\n\nLED ON: {responses[0]:F0}ms\nLED OFF: {responses[1]:F0}ms\nAverage: {avgResponseTime:F1}ms", 
                                  "Test Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("LED test failed - no responses received.", "Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"Error during LED test: {ex.Message}", Color.FromArgb(231, 76, 60));
                MessageBox.Show($"Error during LED test: {ex.Message}", "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Show basic response time statistics
        private void buttonShowStats_Click(object sender, EventArgs e)
        {
            if (mainForm == null)
            {
                MessageBox.Show("Main form not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            StringBuilder statsMessage = new StringBuilder();
            statsMessage.AppendLine("=== Basic Arduino Communication Stats ===\n");
            
            statsMessage.AppendLine($"Arduino Connected: {(mainForm.IsArduinoConnected() ? "YES" : "NO")}");
            
            if (mainForm.IsArduinoConnected())
            {
                statsMessage.AppendLine("Connection Status: ACTIVE");
                statsMessage.AppendLine("\nTo see detailed response time statistics:");
                statsMessage.AppendLine("1. Use 'Test LED Response' for quick timing test");
                statsMessage.AppendLine("2. Use 'Stress Test' for comprehensive analysis");
                statsMessage.AppendLine("3. Check the main form log for detailed timing data");
            }
            else
            {
                statsMessage.AppendLine("Connection Status: DISCONNECTED");
                statsMessage.AppendLine("\nPlease connect to Arduino first to collect statistics.");
            }

            MessageBox.Show(statsMessage.ToString(), "Communication Statistics", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Stress test for comprehensive performance analysis
        private async void buttonStressTest_Click(object sender, EventArgs e)
        {
            if (mainForm == null || !mainForm.IsArduinoConnected())
            {
                MessageBox.Show("Arduino is not connected.", "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("This will send multiple commands to test Arduino response performance.\n\nContinue?", 
                                       "Stress Test", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result != DialogResult.Yes) return;

            mainForm.LogMessage("?? Starting Arduino stress test...", Color.FromArgb(52, 152, 219));

            try
            {
                var testCommands = new[]
                {
                    "PING",
                    "LED_ON",
                    "LED_OFF", 
                    "STATUS"
                };

                var results = new List<double>();
                int successCount = 0;
                int totalCommands = 0;
                
                for (int round = 1; round <= 3; round++)
                {
                    mainForm.LogMessage($"?? Round {round}/3", Color.FromArgb(52, 152, 219));
                    
                    foreach (var cmd in testCommands)
                    {
                        totalCommands++;
                        var startTime = DateTime.Now;
                        
                        if (mainForm.SendArduinoCommand(cmd))
                        {
                            var responseTime = (DateTime.Now - startTime).TotalMilliseconds;
                            results.Add(responseTime);
                            successCount++;
                            mainForm.LogMessage($"? {cmd}: {responseTime:F0}ms", Color.FromArgb(46, 204, 113));
                        }
                        else
                        {
                            mainForm.LogMessage($"? {cmd}: Failed", Color.FromArgb(231, 76, 60));
                        }
                        
                        await Task.Delay(200); // Small delay between commands
                    }
                    
                    await Task.Delay(1000); // Delay between rounds
                }

                // Analyze results
                if (results.Any())
                {
                    var avgTime = results.Average();
                    var minTime = results.Min();
                    var maxTime = results.Max();
                    var successRate = (successCount * 100.0) / totalCommands;

                    mainForm.LogMessage("?? === Stress Test Results ===", Color.FromArgb(155, 89, 182));
                    mainForm.LogMessage($"?? Total Commands: {totalCommands}", Color.FromArgb(155, 89, 182));
                    mainForm.LogMessage($"?? Successful: {successCount} ({successRate:F1}%)", Color.FromArgb(155, 89, 182));
                    mainForm.LogMessage($"?? Average Response: {avgTime:F1}ms", Color.FromArgb(155, 89, 182));
                    mainForm.LogMessage($"?? Range: {minTime:F1}ms - {maxTime:F1}ms", Color.FromArgb(155, 89, 182));

                    MessageBox.Show($"Stress test completed!\n\nTotal Commands: {totalCommands}\nSuccessful: {successCount}\nSuccess Rate: {successRate:F1}%\n\nResponse Times:\nAverage: {avgTime:F1}ms\nFastest: {minTime:F1}ms\nSlowest: {maxTime:F1}ms", 
                                  "Stress Test Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    mainForm.LogMessage("?? Stress test failed - no successful responses", Color.FromArgb(231, 76, 60));
                    MessageBox.Show("Stress test failed - no successful responses received.", "Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"Error during stress test: {ex.Message}", Color.FromArgb(231, 76, 60));
                MessageBox.Show($"Error during stress test: {ex.Message}", "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
