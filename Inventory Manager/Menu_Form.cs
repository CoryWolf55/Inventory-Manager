using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Inventory_Manager
{
    public partial class Menu_Form : Form
    {

        private ListBox[] listBoxes;

        public Menu_Form()
        {
            InitializeComponent();
            //Add listBoxes to a list
            listBoxes = new ListBox[] { listBox1, listBox2, listBox3, listBox4 };
            foreach (ListBox box in listBoxes)
            {
                box.Text = null;
            }

            //Fill recipes box
            listBox5.Text = null;
            foreach (var ing in Program.recipes)
            {
                listBox5.Items.Add($"   - {ing.Name}");
            }

            //Fill the comboBox
            comboBox1.Items.AddRange(Program.eatingTimes.Values.ToArray());
        }

        private void Menu_Form_Load(object sender, EventArgs e)
        {
            
        }

        private void monthCalendar1_DateChanged(object sender, DateRangeEventArgs e)
        {
            //Where the change happens
            DateTime selectedDate = e.Start;

            MenuManager.Instance.SelectedDate(selectedDate);
            //Update text boxes
            UpdateListBoxes();
        }

        private void UpdateListBoxes()
        {
            var sections = MenuManager.Instance.GrabSectionList();
            if (sections == null) return;

            // Clear all listboxes before refilling
            foreach (var box in listBoxes)
                box.Items.Clear();

            foreach (var entry in sections)
            {
                int index = entry.Key - 1;

                if (index < 0 || index >= listBoxes.Length)
                    continue;

                foreach (string name in entry.Value.sectionRecipeNames)
                {
                    if (!listBoxes[index].Items.Contains(name))
                        listBoxes[index].Items.Add(name);
                }
            }
        }


        private void button5_Click(object sender, EventArgs e)
        {
            int selectedSection = comboBox1.SelectedIndex + 1;
            int recipeIndex = listBox5.SelectedIndex;

            if (selectedSection <= 0 || recipeIndex < 0)
                return;

            // Ensure a menu exists
            if (MenuManager.selectedMenu == null)
            {
                MenuManager.Instance.SelectedDate(DateTime.Today);
            }

            var recipe = Program.recipes[recipeIndex];
            if (recipe == null)
                return;

            MenuManager.Instance.AddToSection(selectedSection, recipe.Name);
            UpdateListBoxes();
            listBox5.ClearSelected();
            comboBox1.DroppedDown = false;
        }



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listBox4_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listBox5_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        //Edit buttons
        private void button1_Click(object sender, EventArgs e)
        {
            //edit button for section 1
            RemoveFromSection(1, listBox1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //edit button for section 2
            RemoveFromSection(2, listBox2);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //edit button for section 3
            RemoveFromSection(3, listBox3);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //edit button for section 4
            RemoveFromSection(4, listBox4);
        }

        private void RemoveFromSection(int sectionNumber, ListBox box)
        {
            if (box.SelectedIndex < 0)
                return;

            string recipeName = box.SelectedItem.ToString();

            MenuManager.Instance.RemoveFromSection(sectionNumber, recipeName);
            UpdateListBoxes();
        }
    }

}
