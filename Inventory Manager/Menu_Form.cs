using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

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
                box.Items.Clear();

            // Fill recipes box
            listBox5.Items.Clear();
            foreach (var ing in Program.recipes)
                listBox5.Items.Add($"   - {ing.Name}");

            // Fill the comboBox
            comboBox1.Items.AddRange(Program.eatingTimes.Values.ToArray());

            // **NEW: select today's menu at startup**
            MenuManager.Instance.SelectedDate(DateTime.Today);
            UpdateListBoxes();
        }

        private void Menu_Form_Load(object sender, EventArgs e)
        {
        }

        private void monthCalendar1_DateChanged(object sender, DateRangeEventArgs e)
        {
            DateTime selectedDate = e.Start.Date;
            MenuManager.Instance.SelectedDate(selectedDate);
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

            // Use the calendar's selected date
            DateTime menuDate = monthCalendar1.SelectionStart.Date;
            MenuManager.Instance.SelectedDate(menuDate);

            var recipe = Program.recipes[recipeIndex];
            if (recipe == null) return;

            MenuManager.Instance.AddToSection(selectedSection, recipe.Name);
            UpdateListBoxes();

            listBox5.ClearSelected();
            comboBox1.DroppedDown = false;
        }

        private void RemoveFromSection(int sectionNumber, ListBox box)
        {
            if (box.SelectedIndex < 0)
                return;

            string recipeName = box.SelectedItem.ToString();

            // Use the calendar's selected date
            DateTime menuDate = monthCalendar1.SelectionStart.Date;
            MenuManager.Instance.SelectedDate(menuDate);

            MenuManager.Instance.RemoveFromSection(sectionNumber, recipeName);
            UpdateListBoxes();
        }

        // Edit buttons
        private void button1_Click(object sender, EventArgs e) => RemoveFromSection(1, listBox1);
        private void button2_Click(object sender, EventArgs e) => RemoveFromSection(2, listBox2);
        private void button3_Click(object sender, EventArgs e) => RemoveFromSection(3, listBox3);
        private void button4_Click(object sender, EventArgs e) => RemoveFromSection(4, listBox4);

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}