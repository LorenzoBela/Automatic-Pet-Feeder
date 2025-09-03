using ReaLTaiizor.Forms;
using ReaLTaiizor.Controls;

namespace Automatic_Pet_Feeder
{
    partial class Form3
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
            this.label5 = new ReaLTaiizor.Controls.DungeonLabel();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label4 = new ReaLTaiizor.Controls.DungeonLabel();
            this.label3 = new ReaLTaiizor.Controls.DungeonLabel();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.buttonDispense = new ReaLTaiizor.Controls.MaterialButton();
            this.label2 = new ReaLTaiizor.Controls.DungeonLabel();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Garamond", 26F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(203)))), ((int)(((byte)(92)))));

            this.label1.Location = new System.Drawing.Point(180, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(290, 39);
            this.label1.TabIndex = 0;
            this.label1.Text = "Manual Dispense";
            // 
            // buttonExit
            // 
            this.buttonExit.AutoSize = false;
            this.buttonExit.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonExit.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.buttonExit.Depth = 0;
            this.buttonExit.Font = new System.Drawing.Font("Playfair Display", 12F, System.Drawing.FontStyle.Bold);
            this.buttonExit.HighEmphasis = true;
            this.buttonExit.Icon = null;
            this.buttonExit.Location = new System.Drawing.Point(500, 390);
            this.buttonExit.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.buttonExit.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.NoAccentTextColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(68)))), ((int)(((byte)(66)))));
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
            this.panel1.ColorBackground = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(71)))), ((int)(((byte)(94)))));
            this.panel1.ColorBackground_1 = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(71)))), ((int)(((byte)(94)))));
            this.panel1.ColorBackground_2 = System.Drawing.Color.FromArgb(((int)(((byte)(58)))), ((int)(((byte)(81)))), ((int)(((byte)(104)))));
            this.panel1.ColorBackground_Pen = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(203)))), ((int)(((byte)(92)))));
            this.panel1.ColorLighting = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(203)))), ((int)(((byte)(92)))));
            this.panel1.ColorPen_1 = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(71)))), ((int)(((byte)(94)))));
            this.panel1.ColorPen_2 = System.Drawing.Color.FromArgb(((int)(((byte)(58)))), ((int)(((byte)(81)))), ((int)(((byte)(104)))));
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.progressBar1);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.numericUpDown1);
            this.panel1.Controls.Add(this.buttonDispense);
            this.panel1.Controls.Add(this.label2);
            this.panel1.CyberGroupBoxStyle = ReaLTaiizor.Enum.Cyber.StateStyle.Custom;
            this.panel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.panel1.Lighting = false;
            this.panel1.LinearGradient_Background = false;
            this.panel1.LinearGradientPen = false;
            this.panel1.Location = new System.Drawing.Point(100, 120);
            this.panel1.Name = "panel1";
            this.panel1.PenWidth = 15;
            this.panel1.RGB = false;
            this.panel1.Rounding = true;
            this.panel1.RoundingInt = 20;
            this.panel1.Size = new System.Drawing.Size(450, 250);
            this.panel1.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            this.panel1.TabIndex = 2;
            this.panel1.Tag = "Cyber";
            this.panel1.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            this.panel1.Timer_RGB = 300;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Font = new System.Drawing.Font("Cormorant Garamond", 12F, System.Drawing.FontStyle.Bold);
            this.label5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(203)))), ((int)(((byte)(92)))));
            this.label5.Location = new System.Drawing.Point(390, 170);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(24, 19);
            this.label5.TabIndex = 7;
            this.label5.Text = "0%";
            // 
            // progressBar1
            // 
            this.progressBar1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(58)))), ((int)(((byte)(81)))), ((int)(((byte)(104)))));
            this.progressBar1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(203)))), ((int)(((byte)(92)))));
            this.progressBar1.Location = new System.Drawing.Point(120, 170);
            this.progressBar1.Maximum = 100;
            this.progressBar1.Minimum = 0;
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(260, 20);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 6;
            this.progressBar1.Value = 0;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Cormorant Garamond", 11F, System.Drawing.FontStyle.Italic);
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(183)))), ((int)(((byte)(198)))));
            this.label4.Location = new System.Drawing.Point(35, 210);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(144, 18);
            this.label4.TabIndex = 5;
            this.label4.Text = "Manually dispense food for pet";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Cormorant Garamond", 13F, System.Drawing.FontStyle.Regular);
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(209)))), ((int)(((byte)(216)))), ((int)(((byte)(224)))));
            this.label3.Location = new System.Drawing.Point(35, 35);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(122, 21);
            this.label3.TabIndex = 4;
            this.label3.Text = "Dispense Time (sec):";
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(58)))), ((int)(((byte)(81)))), ((int)(((byte)(104)))));
            this.numericUpDown1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numericUpDown1.Font = new System.Drawing.Font("Cormorant Garamond", 13F);
            this.numericUpDown1.ForeColor = System.Drawing.Color.White;
            this.numericUpDown1.Location = new System.Drawing.Point(220, 30);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.numericUpDown1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(120, 27);
            this.numericUpDown1.TabIndex = 3;
            this.numericUpDown1.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // buttonDispense
            // 
            this.buttonDispense.AutoSize = false;
            this.buttonDispense.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonDispense.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.buttonDispense.Depth = 0;
            this.buttonDispense.Font = new System.Drawing.Font("Playfair Display", 13F, System.Drawing.FontStyle.Bold);
            this.buttonDispense.HighEmphasis = true;
            this.buttonDispense.Icon = null;
            this.buttonDispense.Location = new System.Drawing.Point(155, 85);
            this.buttonDispense.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.buttonDispense.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            this.buttonDispense.Name = "buttonDispense";
            this.buttonDispense.NoAccentTextColor = System.Drawing.Color.Empty;
            this.buttonDispense.Size = new System.Drawing.Size(150, 55);
            this.buttonDispense.TabIndex = 2;
            this.buttonDispense.Text = "Dispense Now";
            this.buttonDispense.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Contained;
            this.buttonDispense.UseAccentColor = true;
            this.buttonDispense.UseVisualStyleBackColor = true;
            this.buttonDispense.Click += new System.EventHandler(this.buttonDispense_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Cormorant Garamond", 13F, System.Drawing.FontStyle.Regular);
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(209)))), ((int)(((byte)(216)))), ((int)(((byte)(224)))));
            this.label2.Location = new System.Drawing.Point(35, 170);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 21);
            this.label2.TabIndex = 0;
            this.label2.Text = "Progress:";
            // 
            // Form3
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(39)))), ((int)(((byte)(52)))));
            this.ClientSize = new System.Drawing.Size(650, 450);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form3";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Manual Dispense - Luxury Edition";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ReaLTaiizor.Controls.BigLabel label1;
        private ReaLTaiizor.Controls.MaterialButton buttonExit;
        private ReaLTaiizor.Controls.CyberGroupBox panel1;
        private ReaLTaiizor.Controls.DungeonLabel label5;
        private System.Windows.Forms.ProgressBar progressBar1;
        private ReaLTaiizor.Controls.DungeonLabel label4;
        private ReaLTaiizor.Controls.DungeonLabel label3;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private ReaLTaiizor.Controls.MaterialButton buttonDispense;
        private ReaLTaiizor.Controls.DungeonLabel label2;
    }
}
