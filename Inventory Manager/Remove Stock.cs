using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Manager
{
    public partial class Remove_Stock : Form
    {
        // Guard to avoid re-entrant selection handling
        private bool suppressSelectionEvent = false;

        // Mapping of recipe -> listbox start index and item count
        private readonly List<int> recipeStartIndices = new List<int>();
        private readonly List<int> recipeItemCounts = new List<int>();

        public static Recipe selectedRecipe { get; set; } = null; 
        public static InventoryItem selectedIngredient { get; set; } = null;

        public Remove_Stock()
        {
            InitializeComponent();

            // Ensure we handle selection changes
            this.listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;

            // Allow selecting multiple contiguous lines (we will programmatically control selection)
            this.listBox1.SelectionMode = SelectionMode.MultiExtended;

            // small UI niceties
            this.listBox1.HorizontalScrollbar = true;
            this.listBox1.IntegralHeight = false;

            // Handle Enter key on textBox
            this.textBox2.KeyDown += textBox2_KeyDown;

            this.textBox3.KeyDown += textBox3_KeyDown;



        }

        private void Remove_Stock_Load(object sender, EventArgs e)
        {
            // Fill current stock textbox
            textBox1.Clear();
            foreach (var item in Program.inventory)
            {
                textBox1.AppendText($"{item.Name}: {item.Quantity} {item.Unit}{Environment.NewLine}");
            }

            // Fill the recipes listbox
            RebuildListBoxAllExpanded();
        }

        private void RebuildListBoxAllExpanded()
        {
            recipeStartIndices.Clear();
            recipeItemCounts.Clear();

            listBox1.BeginUpdate();
            listBox1.Items.Clear();

            int pos = 0;
            for (int i = 0; i < Program.recipes.Count; i++)
            {
                var recipe = Program.recipes[i];

                // header
                recipeStartIndices.Add(pos);

                listBox1.Items.Add(recipe.Name);
                pos++;

                int childCount = 0;
                var ingredients = recipe.Ingredients ?? Enumerable.Empty<RecipeIngredient>();
                foreach (var ing in ingredients)
                {
                    listBox1.Items.Add($"   - {ing.Name}, {ing.Quantity} {ing.Unit}");
                    pos++;
                    childCount++;
                }

                // total items for this recipe (header + ingredients)
                recipeItemCounts.Add(1 + childCount);
            }

            listBox1.EndUpdate();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressSelectionEvent) return;
            if (listBox1.SelectedIndex < 0) return;

            int clickedIndex = listBox1.SelectedIndex;

            // find which recipe the clicked index belongs to
            int recipeIndex = -1;
            for (int i = 0; i < recipeStartIndices.Count; i++)
            {
                int start = recipeStartIndices[i];
                int count = recipeItemCounts[i];
                if (clickedIndex >= start && clickedIndex < start + count)
                {
                    recipeIndex = i;
                    break;
                }
            }

            if (recipeIndex < 0) return;
            //The selected Recipe
            selectedRecipe = Program.recipes[recipeIndex];

            // select the full contiguous range for that recipe (header + ingredients)
            int selectStart = recipeStartIndices[recipeIndex];
            int selectCount = recipeItemCounts[recipeIndex];

            suppressSelectionEvent = true;

            // Clear any previous selections
            for (int i = 0; i < listBox1.Items.Count; i++)
                listBox1.SetSelected(i, false);

            // Select the contiguous block representing the full recipe
            for (int i = selectStart; i < selectStart + selectCount; i++)
                listBox1.SetSelected(i, true);

            // Scroll the header into view for clarity
            if (selectStart >= 0 && selectStart < listBox1.Items.Count)
                listBox1.TopIndex = selectStart;

            suppressSelectionEvent = false;

            //focus the textbox for input of removal amount
            textBox2.Focus();
        }

        // KeyDown handler: runs when a key is pressed while textBox2 has focus.
        // Check for Enter and then read/process the text.
        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //stop going to a new line and change stock
                e.SuppressKeyPress = true;
                
                ChangeStock();
            }
        }


        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // prevent the ding and prevent a newline from being inserted
                e.SuppressKeyPress = true;
                string input = textBox3.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(input))
                    return;

                //Turn input to ingredient
                foreach(InventoryItem item in Program.inventory)
                {
                    if(item.Name == input)
                    {
                        selectedIngredient = item;
                        break;
                    }
                }

                //Take it to find the amount then clear
               
                textBox2.Focus();
                
                textBox3.Clear();
            }
        }

        private void ChangeStock()
        {
            
            

            string input = textBox2.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(input))
                return;

            int numberToRemove = 0;
            if (!int.TryParse(input, out numberToRemove) || numberToRemove <= 0)
            {
                MessageBox.Show("Please enter a number", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //Clear now that we have the number
            textBox2.Clear();

            if (selectedIngredient != null)
            {
                foreach(InventoryItem item in Program.inventory)
                {
                    if (item.Name == selectedIngredient.Name)
                    {
                        if(item.Quantity >= numberToRemove)
                        {
                            item.Quantity -= numberToRemove;
                        }
                        break;
                    }
                }

                SqliteDataAccess.SaveInventory();
                UpdateText();

                return;
            }
            

                // Remove stock from selected ingredients
               List<RecipeIngredient> ingredientsFound = new List<RecipeIngredient>();
            foreach (RecipeIngredient ingredient in selectedRecipe.Ingredients)
            {   
                bool foundItemInInventory = false;
                foreach (InventoryItem item in Program.inventory)
                {
                    if (item.Name == ingredient.Name)
                    {
                        foundItemInInventory = true;
                        ingredientsFound.Add(ingredient);
                        if (item.Quantity >= numberToRemove)
                        {
                            item.Quantity -= numberToRemove;
                        }
                        if(item.Quantity <= 0)
                        {
                            item.Quantity = 0;
                            MessageBox.Show($"Ingredient {item.Name} has run out of stock!", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        break;
                    }
                }
                if(foundItemInInventory == false)
                {
                    //Didnt find this item in the inventory
                    MessageBox.Show($"Ingredient {ingredient.Name} not found in inventory. No stock removed for this item.", "Ingredient Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            }
            //Check for ingredients not found
            foreach (RecipeIngredient ing in ingredientsFound)
            {
                foreach (RecipeIngredient ingredient in selectedRecipe.Ingredients)
                {
                    if(ing.Name != ingredient.Name)
                    {
                        //Didnt find this item in the list
                        MessageBox.Show($"Ingredient {ingredient.Name} not found in inventory. No stock removed for this item.", "Ingredient Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }


            SqliteDataAccess.SaveInventory();
            UpdateText();


        }

        private void UpdateText()
        {
            // Refresh current stock textbox
            textBox1.Clear();
            foreach (var item in Program.inventory)
            {
                textBox1.AppendText($"{item.Name}: {item.Quantity} {item.Unit}{Environment.NewLine}");
            }
        }


    }
    
}
