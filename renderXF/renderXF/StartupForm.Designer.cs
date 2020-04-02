namespace renderXF
{
    partial class StartupForm
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
            this.buttonEXIT = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.checkBoxSHADOW = new System.Windows.Forms.CheckBox();
            this.buttonLOAD = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.checkBoxFULLSCREEN = new System.Windows.Forms.CheckBox();
            this.textBoxHEIGHT = new System.Windows.Forms.TextBox();
            this.textBoxWIDTH = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonEXIT
            // 
            this.buttonEXIT.Location = new System.Drawing.Point(172, 204);
            this.buttonEXIT.Name = "buttonEXIT";
            this.buttonEXIT.Size = new System.Drawing.Size(75, 23);
            this.buttonEXIT.TabIndex = 12;
            this.buttonEXIT.Text = "Exit";
            this.buttonEXIT.UseVisualStyleBackColor = true;
            this.buttonEXIT.Click += new System.EventHandler(this.buttonEXIT_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(91, 204);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 11;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(261, 70);
            this.panel1.TabIndex = 9;
            // 
            // checkBoxSHADOW
            // 
            this.checkBoxSHADOW.AutoSize = true;
            this.checkBoxSHADOW.Location = new System.Drawing.Point(19, 99);
            this.checkBoxSHADOW.Name = "checkBoxSHADOW";
            this.checkBoxSHADOW.Size = new System.Drawing.Size(97, 17);
            this.checkBoxSHADOW.TabIndex = 10;
            this.checkBoxSHADOW.Text = "Frame Caching";
            this.checkBoxSHADOW.UseVisualStyleBackColor = true;
            // 
            // buttonLOAD
            // 
            this.buttonLOAD.Location = new System.Drawing.Point(125, 16);
            this.buttonLOAD.Name = "buttonLOAD";
            this.buttonLOAD.Size = new System.Drawing.Size(100, 23);
            this.buttonLOAD.TabIndex = 9;
            this.buttonLOAD.Text = "Load";
            this.buttonLOAD.UseVisualStyleBackColor = true;
            this.buttonLOAD.Click += new System.EventHandler(this.buttonLOAD_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 21);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(44, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Source:";
            // 
            // checkBoxFULLSCREEN
            // 
            this.checkBoxFULLSCREEN.AutoSize = true;
            this.checkBoxFULLSCREEN.Location = new System.Drawing.Point(125, 99);
            this.checkBoxFULLSCREEN.Name = "checkBoxFULLSCREEN";
            this.checkBoxFULLSCREEN.Size = new System.Drawing.Size(74, 17);
            this.checkBoxFULLSCREEN.TabIndex = 6;
            this.checkBoxFULLSCREEN.Text = "Fullscreen";
            this.checkBoxFULLSCREEN.UseVisualStyleBackColor = true;
            this.checkBoxFULLSCREEN.CheckedChanged += new System.EventHandler(this.checkBoxFULLSCREEN_CheckedChanged);
            // 
            // textBoxHEIGHT
            // 
            this.textBoxHEIGHT.Location = new System.Drawing.Point(125, 73);
            this.textBoxHEIGHT.Name = "textBoxHEIGHT";
            this.textBoxHEIGHT.Size = new System.Drawing.Size(100, 20);
            this.textBoxHEIGHT.TabIndex = 4;
            this.textBoxHEIGHT.Text = "768";
            // 
            // textBoxWIDTH
            // 
            this.textBoxWIDTH.Location = new System.Drawing.Point(125, 47);
            this.textBoxWIDTH.Name = "textBoxWIDTH";
            this.textBoxWIDTH.Size = new System.Drawing.Size(100, 20);
            this.textBoxWIDTH.TabIndex = 3;
            this.textBoxWIDTH.Text = "1024";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 47);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Width:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Height:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBoxSHADOW);
            this.groupBox1.Controls.Add(this.buttonLOAD);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.checkBoxFULLSCREEN);
            this.groupBox1.Controls.Add(this.textBoxHEIGHT);
            this.groupBox1.Controls.Add(this.textBoxWIDTH);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Location = new System.Drawing.Point(12, 76);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(235, 122);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Startup Settings";
            // 
            // StartupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(261, 239);
            this.Controls.Add(this.buttonEXIT);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StartupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "renderX Startup";
            this.Load += new System.EventHandler(this.StartupForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonEXIT;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox checkBoxSHADOW;
        private System.Windows.Forms.Button buttonLOAD;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBoxFULLSCREEN;
        private System.Windows.Forms.TextBox textBoxHEIGHT;
        private System.Windows.Forms.TextBox textBoxWIDTH;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}