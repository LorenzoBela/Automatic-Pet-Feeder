using ReaLTaiizor.Forms;
using ReaLTaiizor.Controls;

namespace Automatic_Pet_Feeder
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new ReaLTaiizor.Controls.BigLabel();
            this.buttonExit = new ReaLTaiizor.Controls.MaterialButton();
            this.panel1 = new ReaLTaiizor.Controls.CyberGroupBox();
            this.label6 = new ReaLTaiizor.Controls.DungeonLabel();
            this.comboBoxInterval = new System.Windows.Forms.ComboBox();
            this.label5 = new ReaLTaiizor.Controls.DungeonLabel();
            this.label4 = new ReaLTaiizor.Controls.DungeonLabel();
            this.label3 = new ReaLTaiizor.Controls.DungeonLabel();
            this.buttonSave = new ReaLTaiizor.Controls.MaterialButton();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.label2 = new ReaLTaiizor.Controls.DungeonLabel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.label1.Location = new System.Drawing.Point(200, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(216, 41);
            this.label1.TabIndex = 0;
            this.label1.Text = "Feeding Settings";
            // 
            // buttonExit
            // 
            this.buttonExit.AutoSize = false;
            this.buttonExit.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonExit.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.buttonExit.Depth = 0;
            this.buttonExit.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.buttonExit.HighEmphasis = true;
            this.buttonExit.Icon = null;
            this.buttonExit.Location = new System.Drawing.Point(520, 390);
            this.buttonExit.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.buttonExit.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.NoAccentTextColor = System.Drawing.Color.Empty;
            this.buttonExit.Size = new System.Drawing.Size(120, 45);
            this.buttonExit.TabIndex = 1;
            this.buttonExit.Text = "Exit";
            this.buttonExit.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Text;
            this.buttonExit.UseAccentColor = false;
            this.buttonExit.UseVisualStyleBackColor = true;
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // panel1
            // 
            this.panel1.Alpha = 20;
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.panel1.Background = true;
            this.panel1.Background_WidthPen = 3F;
            this.panel1.BackgroundPen = true;
            this.panel1.ColorBackground = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(52)))), ((int)(((byte)(68)))));
            this.panel1.ColorBackground_1 = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(52)))), ((int)(((byte)(68)))));
            this.panel1.ColorBackground_2 = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(63)))), ((int)(((byte)(86)))));
            this.panel1.ColorBackground_Pen = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(200)))), ((int)(((byte)(238)))));
            this.panel1.ColorLighting = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(200)))), ((int)(((byte)(238)))));
            this.panel1.ColorPen_1 = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(52)))), ((int)(((byte)(68)))));
            this.panel1.ColorPen_2 = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(63)))), ((int)(((byte)(86)))));
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.comboBoxInterval);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.buttonSave);
            this.panel1.Controls.Add(this.dateTimePicker1);
            this.panel1.Controls.Add(this.label2);
            this.panel1.CyberGroupBoxStyle = ReaLTaiizor.Enum.Cyber.StateStyle.Custom;
            this.panel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.panel1.Lighting = false;
            this.panel1.LinearGradient_Background = false;
            this.panel1.LinearGradientPen = false;
            this.panel1.Location = new System.Drawing.Point(75, 120);
            this.panel1.Name = "panel1";
            this.panel1.PenWidth = 15;
            this.panel1.RGB = false;
            this.panel1.Rounding = true;
            this.panel1.RoundingInt = 20;
            this.panel1.Size = new System.Drawing.Size(500, 250);
            this.panel1.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            this.panel1.TabIndex = 2;
            this.panel1.Tag = "Cyber";
            this.panel1.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            this.panel1.Timer_RGB = 300;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.label6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(88)))), ((int)(((byte)(100)))));
            this.label6.Location = new System.Drawing.Point(35, 160);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(178, 15);
            this.label6.TabIndex = 7;
            this.label6.Text = "Set feeding time and repeat frequency";
            // 
            // comboBoxInterval
            // 
            this.comboBoxInterval.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.comboBoxInterval.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxInterval.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxInterval.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.comboBoxInterval.ForeColor = System.Drawing.Color.White;
            this.comboBoxInterval.FormattingEnabled = true;
            this.comboBoxInterval.Items.AddRange(new object[] {
            "Every Day",
            "Every 2 Hours",
            "Every 3 Hours",
            "Every 4 Hours",
            "Every 6 Hours",
            "Every 8 Hours",
            "Every 12 Hours",
            "Twice a Day",
            "Three Times a Day"});
            this.comboBoxInterval.Location = new System.Drawing.Point(200, 85);
            this.comboBoxInterval.Name = "comboBoxInterval";
            this.comboBoxInterval.Size = new System.Drawing.Size(200, 28);
            this.comboBoxInterval.TabIndex = 6;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.label5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(88)))), ((int)(((byte)(100)))));
            this.label5.Location = new System.Drawing.Point(35, 90);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(58, 20);
            this.label5.TabIndex = 5;
            this.label5.Text = "Interval:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(88)))), ((int)(((byte)(100)))));
            this.label4.Location = new System.Drawing.Point(330, 40);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(61, 15);
            this.label4.TabIndex = 4;
            this.label4.Text = "(12-hr time)";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.label3.Location = new System.Drawing.Point(35, 135);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(99, 20);
            this.label3.TabIndex = 3;
            this.label3.Text = "Next Feeding:";
            // 
            // buttonSave
            // 
            this.buttonSave.AutoSize = false;
            this.buttonSave.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonSave.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.buttonSave.Depth = 0;
            this.buttonSave.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.buttonSave.HighEmphasis = true;
            this.buttonSave.Icon = null;
            this.buttonSave.Location = new System.Drawing.Point(190, 185);
            this.buttonSave.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.buttonSave.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.NoAccentTextColor = System.Drawing.Color.Empty;
            this.buttonSave.Size = new System.Drawing.Size(130, 45);
            this.buttonSave.TabIndex = 2;
            this.buttonSave.Text = "Save Settings";
            this.buttonSave.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Contained;
            this.buttonSave.UseAccentColor = true;
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.CalendarForeColor = System.Drawing.Color.White;
            this.dateTimePicker1.CalendarMonthBackground = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.dateTimePicker1.CustomFormat = "h:mm tt";
            this.dateTimePicker1.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePicker1.Location = new System.Drawing.Point(200, 35);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.ShowUpDown = true;
            this.dateTimePicker1.Size = new System.Drawing.Size(120, 27);
            this.dateTimePicker1.TabIndex = 1;
            this.dateTimePicker1.ValueChanged += new System.EventHandler(this.dateTimePicker1_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(88)))), ((int)(((byte)(100)))));
            this.label2.Location = new System.Drawing.Point(35, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(94, 20);
            this.label2.TabIndex = 0;
            this.label2.Text = "Feeding Time:";
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(33)))), ((int)(((byte)(36)))));
            this.ClientSize = new System.Drawing.Size(650, 450);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form2";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Feeding Settings - Premium Edition";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ReaLTaiizor.Controls.BigLabel label1;
        private ReaLTaiizor.Controls.MaterialButton buttonExit;
        private ReaLTaiizor.Controls.CyberGroupBox panel1;
        private ReaLTaiizor.Controls.DungeonLabel label6;
        private System.Windows.Forms.ComboBox comboBoxInterval;
        private ReaLTaiizor.Controls.DungeonLabel label5;
        private ReaLTaiizor.Controls.DungeonLabel label4;
        private ReaLTaiizor.Controls.DungeonLabel label3;
        private ReaLTaiizor.Controls.MaterialButton buttonSave;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private ReaLTaiizor.Controls.DungeonLabel label2;
    }
}