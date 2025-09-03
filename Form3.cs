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
        public Form3()
        {
            InitializeComponent();
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
                    return;
                }

                buttonDispense.Enabled = false;
                progressBar1.Value = 0;
                label5.Text = "0%";

                int totalSteps = Math.Max(1, seconds * 10); // update every 100ms
                for (int step = 1; step <= totalSteps; step++)
                {
                    await Task.Delay(100);
                    int percent = (int)Math.Min(100, (step * 100.0) / totalSteps);
                    progressBar1.Value = percent;
                    label5.Text = percent + "%";
                }

                MessageBox.Show("Dispense complete.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                buttonDispense.Enabled = true;
            }
        }
    }
}
