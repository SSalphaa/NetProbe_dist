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
            MdiClient chld;
            closeToolStripMenuItem.Enabled = false;
            foreach (Control ctrl in this.Controls)
            {
               try
                {
                    chld = (MdiClient)ctrl;

                    chld.BackColor = this.BackColor;
               }
                catch (InvalidCastException exe)
                {
                    //Type conversion of Label, ImageBox into MdiClient is impossible, it raises exceptions
                    //Thus, we do nothing when its raised
                }
            }
        }
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //Create a new Create_prjForm object instance
            Create_prjForm f = new Create_prjForm();
            //Define Mainview as Parent Form for Create_prjForm
            f.MdiParent = this;
            f.Text = "NetProbe :: New Project Creation";
            //Hide picture box and item controls and disable the menustrip control to user interactions
            this.label1.Visible=false;
            this.pictureBox1.Visible=false;
            this.menuStrip1.Enabled = false;
            //Display Form
            f.Show();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dashboard d = new Dashboard();
            d.MdiParent = this;
            //Hide picture box and item controls and disable the menustrip control to user interactions
            this.label1.Visible = false;
            this.pictureBox1.Visible = false;
            this.fichierToolStripMenuItem.Enabled = false;
            this.loadToolStripMenuItem.Enabled = false;
            this.closeToolStripMenuItem.Enabled = true;
            //Display Form
            d.Show();
            
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form f = ActiveMdiChild;
            f.Close();
            
        }

        private void Mainview_childrenClosed(object sender, EventArgs e)
        {
            Form f = this.ActiveMdiChild;

            if (f == null)
            {
                //the last child form was just closed
                this.fichierToolStripMenuItem.Enabled = true;
                this.loadToolStripMenuItem.Enabled = true;
                this.closeToolStripMenuItem.Enabled = false;
            }
        }
    }
}
