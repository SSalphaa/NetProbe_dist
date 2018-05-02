using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetProbe
{
    public partial class Create_prjForm : Form
    {

        public Create_prjForm()
        {
            InitializeComponent();
        }

        private void btn_Return_Click(object sender, EventArgs e)
        {
           //If user wants to abort project creation, close the form
                this.Close();
               
        }
        
       private void Create_prjForm_FormClosing(object sender, CancelEventArgs e)
        {
          //Message box to ask if you want to close
            if (MessageBox.Show("You are about to abort new project creation...Do you confirm?", "NetProbe",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                //Validate Project Creation Closure
                e.Cancel = false;
                //Search for active form (Parent Form : Mainview)
                Form m = Form.ActiveForm;
                //Set Parent Form's controls visible and enabled
                foreach (Control c in m.Controls)
                {
                    c.Visible = true;
                    c.Enabled = true;
                }
            }
            else
                // Abort Project creation Closure if user clicks the "No" user dialog button 
                e.Cancel = true;
         
        }
        //Selecting the location folder of the project file
        private void btn_Location_Click(object sender, EventArgs e)
        {
            //Create a new instance of the FolderBrowserDialog class

            FolderBrowserDialog LocFolder = new FolderBrowserDialog();

            //Allow the user to have the possibility to create a new Folder 
            LocFolder.ShowNewFolderButton = true;

            // Show the FolderBrowserDialog (Method that allow to select a folder)
            // When the user is done selecting the folder and hit "OK" button.
            if (LocFolder.ShowDialog() == DialogResult.OK)
            {

                textBoxLoc.Text = LocFolder.SelectedPath;

                Environment.SpecialFolder root = LocFolder.RootFolder;

            }
        }

        private void btn_File_Click(object sender, EventArgs e)
        {
            OpenFileDialog PrjFileDialog = new OpenFileDialog();


            PrjFileDialog.InitialDirectory = @"C:\Users";

            PrjFileDialog.Title = "Browse Project File";

            if (PrjFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBoxFile.Text = PrjFileDialog.FileName;
            }
        }
    }
}
