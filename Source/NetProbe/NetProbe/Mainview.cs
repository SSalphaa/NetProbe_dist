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
    public partial class Mainview : Form
    {
        public Mainview()
        {
            
            InitializeComponent();
        }

        private void Mainview_Load(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //Create a new Create_prjForm object instance
            Create_prjForm f = new Create_prjForm();
            //Define Mainview as Parent Form for Create_prjForm
            f.MdiParent = this;
            f.Text = "New Project Creation";
            //Display Form
            f.Show();
            //Hide picture box and item controls and disable the menustrip control to user interactions
            this.label1.Visible=false;
            this.pictureBox1.Visible=false;
            this.menuStrip1.Enabled = false;
        }
    }
}
