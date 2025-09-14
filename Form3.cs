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

                // Send dispense command to Arduino
                string command = $"DISPENSE_{seconds}";
                bool commandSent = mainForm.SendArduinoCommand(command);
                
                if (!commandSent)
                {
                    MessageBox.Show("Failed to send command to Arduino. Please check the connection.", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

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
                MessageBox.Show($"Dispense complete! Food dispensed for {seconds} seconds.", "Dispense Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
    }
}
