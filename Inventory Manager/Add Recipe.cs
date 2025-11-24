using Inventory_Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Manager
{
    public partial class Add_Recipe : Form
    {
        public static Add_Recipe instance;
        
        public Add_Recipe()
        {
            
            InitializeComponent();
            instance = this;

            // 2a. Prefer setting items on the column if it's a ComboBox column
            if (dataGridView1.Columns.Contains("Unit"))
            {
                var col = dataGridView1.Columns["Unit"];
                if (col is DataGridViewComboBoxColumn comboCol)
                {
                    comboCol.Items.Clear();
                    comboCol.Items.AddRange(Program.units);
                }
                else
                {
                    // 2b. Fallback: set items per cell if column is not a ComboBox column
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (row.IsNewRow) continue;

                        if (row.Cells["Unit"] is DataGridViewComboBoxCell comboCell)
                        {
                            comboCell.Items.Clear();
                            comboCell.Items.AddRange(Program.units);
                        }
                    }
                }
            }
            //Write saved recipes to textbox
            WriteSavedRecipes();

        }

        

        private void SubmitButton_Click(object sender, EventArgs e)
        {
            var ingredients = new List<RecipeIngredient>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                string name = row.Cells["Name"].Value?.ToString() ?? "";
                string quantityText = row.Cells["Quantity"].Value?.ToString() ?? "0";
                string unit = row.Cells["Unit"].Value?.ToString() ?? "";

                double.TryParse(quantityText, out double quantity);

                ingredients.Add(new RecipeIngredient
                {
                    Name = name,
                    Quantity = quantity,
                    Unit = unit
                });
            }

            // Build the recipe from UI
            var recipe = new Recipe
            {
                Name = textBox1.Text,
                Ingredients = ingredients
            };

            // Update the program-wide list (replace existing by name or add)
            int idx = Program.recipes.FindIndex(r => string.Equals(r.Name, recipe.Name, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
                Program.recipes[idx] = recipe;
            else
                Program.recipes.Add(recipe);

            // Save once (writes the full list)
            SqliteDataAccess.SaveRecipes();

            // Refresh UI view and clear inputs
            WriteSavedRecipes();
            textBox1.Clear();
            dataGridView1.Rows.Clear();
        }

        private void WriteSavedRecipes()
        {
            if (Program.recipes.Count == 0)
            {
                textBox2.Text = "No saved recipes yet.";
                return;
            }

            // Build a string representation from the in-memory list
            var sb = new System.Text.StringBuilder();
            foreach (var recipe in Program.recipes)
            {
                sb.AppendLine($"Recipe: {recipe.Name}");
                if (recipe.Ingredients != null)
                {
                    foreach (var ing in recipe.Ingredients)
                    {
                        sb.AppendLine($" - {ing.Name}, {ing.Quantity} {ing.Unit}");
                    }
                }
                sb.AppendLine();
            }

            textBox2.Text = sb.ToString();
        }




        private string LoadText(string file)
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(folderPath, file);

            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            else
            {
                return "No saved recipes yet.";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ChangeRecipe();
        }


        private void ChangeRecipe()
        {
            string recipeName = textBox3.Text;
            if (recipeName == "")
            {
                MessageBox.Show("Please enter a recipe name to change.");
                return;
            }
            Recipe recipeData = new Recipe();
            // Find Recipe
            recipeData = FindRecipe(recipeName);
            if (recipeData == null)
            {
                MessageBox.Show("Recipe not found");
                return;
            }

            // Populate fields
            textBox1.Text = recipeData.Name;
            dataGridView1.Rows.Clear();
            foreach (var ingredient in recipeData.Ingredients)
            {
                dataGridView1.Rows.Add(ingredient.Name, ingredient.Quantity, ingredient.Unit);
            }
        }

        private Recipe FindRecipe(string name)
        {
            foreach (Recipe recipe in Program.recipes)
            {
                if (recipe.Name == name)
                {
                    return recipe;
                }
            }
            return null;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you Sure? \n This will delete all of your recipes", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                // Clear all input fields
                textBox1.Clear();
                dataGridView1.Rows.Clear();
                textBox3.Clear();
                SqliteDataAccess.ClearRecipes();
                textBox2.Text = "No saved recipes yet.";
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }

}

