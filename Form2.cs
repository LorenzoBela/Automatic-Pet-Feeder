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
    public partial class Form2 : Form
    {
        private Form1 mainForm;

        public Form2()
        {
            InitializeComponent();
            InitializeDefaultValues(); // Set defaults first
        }

        public void SetMainForm(Form1 form)
        {
            mainForm = form;
            LoadCurrentSchedule(); // Load current schedule after main form is set
        }

        private void LoadCurrentSchedule()
        {
            if (mainForm != null)
            {
                DateTime? currentNextFeedingTime;
                string currentInterval;
                
                // Get current schedule from main form
                mainForm.GetCurrentFeedingSchedule(out currentNextFeedingTime, out currentInterval);
                
                if (currentNextFeedingTime.HasValue && !string.IsNullOrEmpty(currentInterval))
                {
                    // Calculate the base feeding time from the current schedule
                    DateTime baseTime = CalculateBaseTimeFromSchedule(currentNextFeedingTime.Value, currentInterval);
                    
                    // Set the date time picker to the base feeding time
                    dateTimePicker1.Value = baseTime;
                    
                    // Set the interval combo box
                    for (int i = 0; i < comboBoxInterval.Items.Count; i++)
                    {
                        if (comboBoxInterval.Items[i].ToString() == currentInterval)
                        {
                            comboBoxInterval.SelectedIndex = i;
                            break;
                        }
                    }
                    
                    UpdateNextFeedingDisplay();
                }
            }
        }

        private void InitializeDefaultValues()
        {
            // Set default values
            dateTimePicker1.Value = DateTime.Today.AddHours(8); // Default to 8:00 AM
            if (comboBoxInterval.Items.Count > 0)
            {
                comboBoxInterval.SelectedIndex = 0; // Default to first item
            }
            UpdateNextFeedingDisplay();
        }

        private DateTime CalculateBaseTimeFromSchedule(DateTime nextFeedingTime, string interval)
        {
            // This method tries to determine what the original feeding time was
            // based on the next feeding time and interval
            
            DateTime now = DateTime.Now;
            
            switch (interval)
            {
                case "Every Day":
                    if (nextFeedingTime > now)
                    {
                        return nextFeedingTime; // Use the next feeding time
                    }
                    else
                    {
                        return nextFeedingTime.AddDays(-1); // Go back one day
                    }
                
                case "Every 2 Hours":
                case "Every 3 Hours":
                case "Every 4 Hours":
                case "Every 6 Hours":
                case "Every 8 Hours":
                case "Every 12 Hours":
                    // For hourly intervals, just use the time component
                    return new DateTime(now.Year, now.Month, now.Day, 
                                      nextFeedingTime.Hour, nextFeedingTime.Minute, 0);
                
                case "Twice a Day":
                case "Three Times a Day":
                    // Use the time component from the next feeding
                    return new DateTime(now.Year, now.Month, now.Day, 
                                      nextFeedingTime.Hour, nextFeedingTime.Minute, 0);
                
                default:
                    return nextFeedingTime;
            }
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            try
            {
                string feedingTime = dateTimePicker1.Value.ToString("h:mm tt");
                string interval = comboBoxInterval.SelectedItem?.ToString() ?? "Every Day";
                
                // Calculate the actual next feeding time
                DateTime nextFeeding = CalculateNextFeeding();
                
                // Update the main form with the feeding schedule
                if (mainForm != null)
                {
                    mainForm.UpdateFeedingSchedule(nextFeeding, interval);
                }
                
                MessageBox.Show("Feeding schedule saved!\n\nTime: " + feedingTime + "\nInterval: " + interval + "\n\nNext feeding: " + nextFeeding.ToString("MMM dd, h:mm tt"), 
                              "Settings Saved", 
                              MessageBoxButtons.OK, 
                              MessageBoxIcon.Information);
                
                UpdateNextFeedingDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving settings: " + ex.Message, 
                              "Error", 
                              MessageBoxButtons.OK, 
                              MessageBoxIcon.Error);
            }
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            UpdateNextFeedingDisplay();
        }

        private void UpdateNextFeedingDisplay()
        {
            try
            {
                DateTime nextFeeding = CalculateNextFeeding();
                label3.Text = "Next Feeding: " + nextFeeding.ToString("MMM dd, h:mm tt");
            }
            catch
            {
                label3.Text = "Next Feeding: Not set";
            }
        }

        private DateTime CalculateNextFeeding()
        {
            DateTime selectedTime = dateTimePicker1.Value;
            DateTime now = DateTime.Now;
            string interval = comboBoxInterval.SelectedItem?.ToString() ?? "Every Day";

            // Create today's feeding time
            DateTime todayFeeding = new DateTime(now.Year, now.Month, now.Day, 
                                               selectedTime.Hour, selectedTime.Minute, 0);

            switch (interval)
            {
                case "Every Day":
                    if (todayFeeding > now)
                        return todayFeeding;
                    else
                        return todayFeeding.AddDays(1);

                case "Every 2 Hours":
                    return GetNextHourlyFeeding(now, selectedTime, 2);

                case "Every 3 Hours":
                    return GetNextHourlyFeeding(now, selectedTime, 3);

                case "Every 4 Hours":
                    return GetNextHourlyFeeding(now, selectedTime, 4);

                case "Every 6 Hours":
                    return GetNextHourlyFeeding(now, selectedTime, 6);

                case "Every 8 Hours":
                    return GetNextHourlyFeeding(now, selectedTime, 8);

                case "Every 12 Hours":
                    return GetNextHourlyFeeding(now, selectedTime, 12);

                case "Twice a Day":
                    // Feed at selected time and 12 hours later
                    DateTime firstFeeding = todayFeeding;
                    DateTime secondFeeding = todayFeeding.AddHours(12);
                    
                    if (firstFeeding > now)
                        return firstFeeding;
                    else if (secondFeeding > now)
                        return secondFeeding;
                    else
                        return firstFeeding.AddDays(1);

                case "Three Times a Day":
                    // Feed at selected time, +8 hours, +16 hours
                    DateTime first = todayFeeding;
                    DateTime second = todayFeeding.AddHours(8);
                    DateTime third = todayFeeding.AddHours(16);
                    
                    if (first > now)
                        return first;
                    else if (second > now)
                        return second;
                    else if (third > now)
                        return third;
                    else
                        return first.AddDays(1);

                default:
                    return todayFeeding > now ? todayFeeding : todayFeeding.AddDays(1);
            }
        }

        private DateTime GetNextHourlyFeeding(DateTime now, DateTime selectedTime, int hourInterval)
        {
            DateTime baseTime = new DateTime(now.Year, now.Month, now.Day, 
                                           selectedTime.Hour, selectedTime.Minute, 0);
            
            while (baseTime <= now)
            {
                baseTime = baseTime.AddHours(hourInterval);
            }
            
            return baseTime;
        }
    }
}