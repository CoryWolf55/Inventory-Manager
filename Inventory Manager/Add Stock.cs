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
    public partial class Add_Stock : Form
    {
        public Add_Stock()
        {
            InitializeComponent();
            //Fill the units box
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

            InventoryManager.Instance.UpdateCurrentStockDisplay(this.textBox1);

            //Fill the suggestion Box

            if (textBox2 == null) return;
            textBox2.Clear();
            foreach (var item in InventoryManager.Instance.GrabSuggestions())
            {
                textBox2.AppendText($"{item.Name}{Environment.NewLine}");
            }

            //Fill menu suggestion box
            if (textBox3 == null) return;
            textBox3.Clear();
            var neededItems = NeededMenuItems(DateTime.Now);
            if(neededItems == null) return;
            foreach (var item in neededItems)
            {
                textBox1.AppendText($"{item.Name}: {item.Quantity} {item.Unit}{Environment.NewLine}");
            }

        }

        private void SubmitButton_Click(object sender, EventArgs e)
        {
            var inventoryItems = new List<InventoryItem>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                string name = (row.Cells["Name"].Value?.ToString() ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name)) continue; 

                string quantityText = row.Cells["Quantity"].Value?.ToString() ?? "0";
                string unit = row.Cells["Unit"].Value?.ToString() ?? "";

                double.TryParse(quantityText, out double quantity);

                inventoryItems.Add(new InventoryItem
                {
                    Name = name,
                    Quantity = quantity,
                    Unit = unit
                });
            }

            // Update the program-wide inventory: replace by name or add new
            foreach (var newItem in inventoryItems)
            {
                int idx = Program.inventory.FindIndex(r =>
                    string.Equals(r.Name, newItem.Name, StringComparison.OrdinalIgnoreCase));

                if (idx >= 0)
                {
                    var existing = Program.inventory[idx];
                    if (string.IsNullOrWhiteSpace(existing.Unit) && string.IsNullOrWhiteSpace(newItem.Unit))
                    {
                        Program.inventory.Add(newItem);
                        continue;
                    }
                    else if(string.IsNullOrWhiteSpace(existing.Unit) || string.IsNullOrWhiteSpace(newItem.Unit))
                    {
                        Program.inventory[idx] = newItem;
                        continue;
                    }

                    // Convert newItem to existing item's unit BEFORE adding
                    double convertedAmount = InventoryManager.Instance.ConvertTo(
                        newItem.Quantity,
                        newItem.Unit,
                        existing.Unit
                    );

                    // Add correctly
                    existing.Quantity += convertedAmount;

                    // Optional: convert to best unit
                    var final = InventoryManager.Instance.UnitConversion(existing.Quantity, existing.Unit);
                    existing.Quantity = final.Key;
                    existing.Unit = final.Value;
                }
                else
                {
                    // Item doesn't exist → add it normally
                    Program.inventory.Add(newItem);
                }
            }

            // Persist inventory to disk
            SqliteDataAccess.SaveInventory();

            // Refresh UI view and clear inputs
            UpdateStock();
            dataGridView1.Rows.Clear();
        }

        private void UpdateStock()
        {
            // If this form has a textbox named textBox1 to show current stock (as in Remove_Stock),
            // populate it from Program.inventory so the user sees the updated values.
            if (this.textBox1 == null) return;

            textBox1.Clear();
            foreach (var item in Program.inventory)
            {
                textBox1.AppendText($"{item.Name}: {item.Quantity} {item.Unit}{Environment.NewLine}");
            }
        }

        private List<InventoryItem> NeededMenuItems(DateTime time)
        {
            // Use current time if null
            if (time == null)
                time = DateTime.Now;

            time = MenuManager.Instance.NormalizeDate(time);

            // Try to get menu for selected day
            if (!Program.scheduleMenu.TryGetValue(time, out var selectedMenu))
                return null;

            // SECTION LIST
            Dictionary<int, MenuSection> sections = MenuManager.Instance.GrabSectionList(selectedMenu);

            List<string> recipeNames = new List<string>();
            List<InventoryItem> returnList = new List<InventoryItem>();

            // Inventory indexed by item name
            Dictionary<string, InventoryItem> items =
                Program.inventory.ToDictionary(i => i.Name, i => i);

            // Ingredient requirements indexed by ingredient name
            Dictionary<string, RecipeIngredient> ingredientList = new Dictionary<string, RecipeIngredient>();


            // Gather all recipe names used in this menu
            foreach (var section in sections.Values)
            {
                if (section == null) continue;
                recipeNames.AddRange(section.sectionRecipeNames);
            }


            // Gather all ingredients for those recipes and COMBINE duplicates
            foreach (var recipe in Program.recipes)
            {
                if (recipeNames.Contains(recipe.Name))
                {
                    foreach (var ing in recipe.Ingredients)
                    {
                        // If ingredient already exists, accumulate quantities
                        if (ingredientList.TryGetValue(ing.Name, out var existing))
                        {
                            existing.Quantity += ing.Quantity;
                        }
                        else
                        {
                            ingredientList.Add(ing.Name, new RecipeIngredient
                            {
                                Name = ing.Name,
                                Quantity = ing.Quantity,
                                Unit = ing.Unit
                            });
                        }
                    }
                }
            }

            foreach (var pair in ingredientList)
            {
                string ingredientName = pair.Key;
                RecipeIngredient needed = pair.Value;

                // If item exists in inventory
                if (items.TryGetValue(ingredientName, out var inv))
                {
                    double shortage = needed.Quantity - inv.Quantity;

                    if (shortage > 0)
                    {
                        // Add only what you need to buy
                        returnList.Add(new InventoryItem
                        {
                            Name = ingredientName,
                            Unit = inv.Unit,
                            Quantity = shortage
                        });
                    }
                }
                else
                {
                    // Ingredient not in inventory at all: need the full quantity
                    returnList.Add(new InventoryItem
                    {
                        Name = ingredientName,
                        Unit = needed.Unit,
                        Quantity = needed.Quantity
                    });
                }
            }

            return returnList;
        }


        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
