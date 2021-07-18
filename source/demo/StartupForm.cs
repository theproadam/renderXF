using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace demo
{
    public partial class StartupForm : Form
    {
        public StartupForm(string[] args)
        {
            startArgs = args;
            InitializeComponent();
        }

        public static int W;
        public static int H;

        public static string FilePath;
        public static bool FullscreenSelected;
        public static bool NormalsInterpolated;
        public static bool ApplicationTerminated = true;
        string[] startArgs;

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textBoxWIDTH.Text, out W) | !int.TryParse(textBoxHEIGHT.Text, out H))
            {
                MessageBox.Show("Please Input Valid Resolution!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (FilePath != null)
            {
                FullscreenSelected = checkBoxFULLSCREEN.Checked;
                NormalsInterpolated = checkBoxSHADOW.Checked;
                ApplicationTerminated = false;
                this.Close();
            }
            else
                MessageBox.Show("Please Select A File To Load", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void buttonEXIT_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonLOAD_Click(object sender, EventArgs e)
        {
            OpenFileDialog oDialog = new OpenFileDialog();
            oDialog.Filter = "Stereolithography format|*.stl|All Files|*.*";

            if (oDialog.ShowDialog() == DialogResult.OK)
            {
                buttonLOAD.Text = oDialog.SafeFileName;
                FilePath = oDialog.FileName;
            }

        }

        private void StartupForm_Load(object sender, EventArgs e)
        {
            if (startArgs.Length > 1)
            {
                if (System.IO.File.Exists(startArgs[1]))
                {
                    if (System.IO.Path.GetExtension(startArgs[1]) != ".stl")
                        MessageBox.Show("Invalid File Type Submitted", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                    {
                        FilePath = startArgs[1];
                        buttonLOAD.Text = System.IO.Path.GetFileName(startArgs[1]);
                    }
                }
            }
        }

        private void checkBoxFULLSCREEN_CheckedChanged(object sender, EventArgs e)
        {
            textBoxHEIGHT.Enabled = !checkBoxFULLSCREEN.Checked;
            textBoxWIDTH.Enabled = !checkBoxFULLSCREEN.Checked;
        }
    }
}
