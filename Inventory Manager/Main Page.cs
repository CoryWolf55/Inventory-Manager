using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Inventory_Manager
{
    public partial class Form1 : Form
    {
        public static Form1 instance;
        private int textBoxCushion = 20;
        public Form1()
        {
            InitializeComponent();
            instance = this;
            panel1.Hide();
        }

       

        private void button3_Click(object sender, EventArgs e)
        {
            // Create an instance of the new form
            Remove_Stock newForm = new Remove_Stock();

            // Show the new form
            newForm.Show(); 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Create an instance of the new form
            Add_Stock newForm = new Add_Stock();

            // Show the new form
            newForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Add_Recipe newForm = new Add_Recipe();
            newForm.Show();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox1.Size = new Size(panel1.Width - textBoxCushion, panel1.Height - textBoxCushion);
            textBox1.Location = new Point((panel1.Width - textBox1.Width) / 2,
                (panel1.Height - textBox1.Height) / 2);

            panel1.Show(); 
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            //Menu Button
            Menu_Form newForm = new Menu_Form();
            newForm.Show();
        }
    }
}
