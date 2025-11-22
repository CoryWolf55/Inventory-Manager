using Inventory_Manager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Collections.Specialized.BitVector32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Inventory_Manager
{
    internal static class Program
    {
        //Main lists used throughout the program
        public static List<Recipe> recipes { get; set; } = new List<Recipe>();
        public static Dictionary<DateTime, Menu> scheduleMenu { get; set; } = 
            new Dictionary<DateTime, Menu>();
        public static Dictionary<int, string> eatingTimes { get; set; } = new Dictionary<int, string>
        {
            {1,"Breakfast"},
            {2,"Lunch"},
            {3,"Dinner"},
            {4,"Dessert"},
        };


        public static List<InventoryItem> inventory { get; set; } = new List<InventoryItem>();
        public readonly static string[] units = new[] { "g", "kg", "oz", "lb", "ml", "l", "pcs" };

        [STAThread]
        static void Main()
        {
            // Load main lists BEFORE creating forms so all saves use the same in-memory lists
            recipes = SaveData.Instance.LoadFromFile();
            inventory = SaveData.Instance.LoadInventoryFromFile();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    public class InventoryItem
    {
        public string Name { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
    }
    public class RecipeIngredient
    {
        public string Name { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
    }

    public class Menu
    {
        public Dictionary<int, MenuSection> sections = new Dictionary<int, MenuSection>();
        public int dateAsInt;

        public Menu(DateTime time)
        {
            dateAsInt = int.Parse(time.ToString("yyyyMMdd"));
        }
    }


    public class MenuSection
    {
        public List<string> sectionRecipeNames = new List<string>();

    }

    //Schedule has key for dates and the menu that corresponds.
    //Menu will contain the eating times if applied

    public class MenuManager
    {
        public static readonly MenuManager Instance = new MenuManager();
        public static Menu selectedMenu = null;

        public void SelectedDate(DateTime date)
        {
            if (!Program.scheduleMenu.TryGetValue(date, out selectedMenu))
            {
                selectedMenu = new Menu(date);
                Program.scheduleMenu[date] = selectedMenu;
            }
        }

        public void AddToSection(int sectionNum, string recipeName)
        {
            if (selectedMenu == null)
                throw new InvalidOperationException("No menu selected. Call SelectedDate() first.");

            if (!Program.eatingTimes.ContainsKey(sectionNum)) return;

            if (!selectedMenu.sections.TryGetValue(sectionNum, out var section))
            {
                section = new MenuSection();
                selectedMenu.sections[sectionNum] = section;
            }

            if (!section.sectionRecipeNames.Contains(recipeName))
                section.sectionRecipeNames.Add(recipeName);
        }


        public Dictionary<int, MenuSection> GrabSectionList()
        {
            return selectedMenu?.sections;
        }
    }








    public class Recipe
    {
        public string Name { get; set; }
        public List<RecipeIngredient> Ingredients { get; set; }
    }

    public class  InventoryManager
    {
        public static readonly InventoryManager Instance = new InventoryManager();
        private InventoryManager() { }

        public List<RecipeIngredient> GrabSuggestions()
        {
            // Implementation for grabbing suggestions for Add_Stock form
            List<RecipeIngredient> suggestions = new List<RecipeIngredient>();
            foreach (Recipe recipe in Program.recipes)
            {
                foreach (RecipeIngredient ingredient in recipe.Ingredients)
                {
                    suggestions.Add(ingredient);
                }
            }
            if(suggestions != null && suggestions.Count > 0)
                return suggestions;
            MessageBox.Show("No recipes found to suggest ingredients from.", "No Suggestions", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return new List<RecipeIngredient>();
        }

        public void UpdateCurrentStockDisplay(System.Windows.Forms.TextBox stockTextBox)
        {
            if (stockTextBox == null) return;
            stockTextBox.Clear();
            foreach (var item in Program.inventory)
            {
                stockTextBox.AppendText($"{item.Name}: {item.Quantity} {item.Unit}{Environment.NewLine}");
            }
        }

        private readonly List<int> recipeStartIndices = new List<int>();
        private readonly List<int> recipeItemCounts = new List<int>();
        public Recipe FindRecipe(int index)
        {
            if (index < 0) return null;

            int clickedIndex = index;

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

            if (recipeIndex < 0) return null;
            //The selected Recipe
            return Program.recipes[recipeIndex];
        }

        
    }

    public sealed class SaveData
    {
        // Singleton instance (eager initialization)
        public static readonly SaveData Instance = new SaveData();

        // Prevent external construction
        private SaveData() { }

        /// <summary>
        /// Save the current recipes list to %AppData%\RecipeSave.txt
        /// Format:
        /// Recipe: &lt;Name&gt;
        ///  - &lt;IngredientName&gt;, &lt;Quantity&gt; &lt;Unit&gt;
        /// (blank line between recipes)
        /// </summary>
        public void SaveFile()
        {
            try
            {
                string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                Directory.CreateDirectory(folderPath);
                string filePath = Path.Combine(folderPath, "RecipeSave.txt");

                var lines = new List<string>();

                // use the up-to-date program-wide list
                var listToSave = Program.recipes ?? new List<Recipe>();

                foreach (var recipe in listToSave)
                {
                    lines.Add($"Recipe: {recipe?.Name ?? string.Empty}");

                    if (recipe?.Ingredients != null)
                    {
                        foreach (var ing in recipe.Ingredients)
                        {
                            string name = ing?.Name ?? string.Empty;
                            string qty = (ing != null) ? ing.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
                            string unit = (ing != null && !string.IsNullOrWhiteSpace(ing.Unit)) ? ing.Unit : string.Empty;
                            lines.Add($" - {name}, {qty} {unit}".TrimEnd());
                        }
                    }

                    lines.Add(string.Empty); // blank separator
                }

                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save recipes: " + ex.Message, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Load the save file and populate the provided target list.
        /// Existing contents of target will be cleared.
        /// </summary>
        public void LoadInto(List<Recipe> target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(folderPath, "RecipeSave.txt");

            target.Clear();

            if (!File.Exists(filePath))
                return;

            try
            {
                var lines = File.ReadAllLines(filePath);
                Recipe current = null;

                foreach (var raw in lines)
                {
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        current = null;
                        continue;
                    }

                    var line = raw.Trim();

                    if (line.StartsWith("Recipe:", StringComparison.OrdinalIgnoreCase))
                    {
                        string name = line.Substring("Recipe:".Length).Trim();
                        current = new Recipe { Name = name, Ingredients = new List<RecipeIngredient>() };
                        target.Add(current);
                    }
                    else if (line.StartsWith("-") || line.StartsWith(" -"))
                    {
                        // normalize and remove leading dash
                        string content = line.StartsWith("-") ? line.Substring(1).Trim() : line.Substring(2).Trim();

                        int commaIndex = content.IndexOf(',');
                        if (commaIndex >= 0)
                        {
                            string ingName = content.Substring(0, commaIndex).Trim();
                            string rest = content.Substring(commaIndex + 1).Trim(); // e.g. "100 g"

                            double qty = 0;
                            string unit = string.Empty;

                            if (!string.IsNullOrEmpty(rest))
                            {
                                var tokens = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (tokens.Length >= 1)
                                {
                                    // try invariant first, then current culture
                                    if (!double.TryParse(tokens[0], NumberStyles.Float, CultureInfo.InvariantCulture, out qty))
                                    {
                                        double.TryParse(tokens[0], NumberStyles.Float, CultureInfo.CurrentCulture, out qty);
                                    }
                                }

                                if (tokens.Length > 1)
                                    unit = string.Join(" ", tokens, 1, tokens.Length - 1);
                            }

                            if (current != null)
                            {
                                current.Ingredients.Add(new RecipeIngredient
                                {
                                    Name = ingName,
                                    Quantity = qty,
                                    Unit = unit
                                });
                            }
                        }
                    }
                    else
                    {
                        // unknown line format - ignore
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load recipes: " + ex.Message, "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Convenience: returns a new list loaded from the save file.
        /// </summary>
        public List<Recipe> LoadFromFile()
        {
            var result = new List<Recipe>();
            LoadInto(result);
            return result;
        }

        /// <summary>
        /// Save the current inventory list to %AppData%\InventorySave.txt
        /// Format:
        /// Item: &lt;Name&gt;
        ///  - &lt;Quantity&gt; &lt;Unit&gt;
        /// (blank line between items)
        /// </summary>
        public void SaveInventory()
        {
            try
            {
                string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                Directory.CreateDirectory(folderPath);
                string filePath = Path.Combine(folderPath, "InventorySave.txt");

                var lines = new List<string>();
                var listToSave = Program.inventory ?? new List<InventoryItem>();

                foreach (var item in listToSave)
                {
                    lines.Add($"Item: {item?.Name ?? string.Empty}");
                    string qty = (item != null) ? item.Quantity.ToString(CultureInfo.InvariantCulture) : "0";
                    string unit = (item != null && !string.IsNullOrWhiteSpace(item.Unit)) ? item.Unit : string.Empty;
                    lines.Add($" - {qty} {unit}".TrimEnd());
                    lines.Add(string.Empty); // separator
                }

                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save inventory: " + ex.Message, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Load inventory from file into the provided target list.
        /// </summary>
        public void LoadInventoryInto(List<InventoryItem> target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(folderPath, "InventorySave.txt");

            target.Clear();

            if (!File.Exists(filePath))
                return;

            try
            {
                var lines = File.ReadAllLines(filePath);
                InventoryItem current = null;

                foreach (var raw in lines)
                {
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        current = null;
                        continue;
                    }

                    var line = raw.Trim();

                    if (line.StartsWith("Item:", StringComparison.OrdinalIgnoreCase))
                    {
                        string name = line.Substring("Item:".Length).Trim();
                        current = new InventoryItem { Name = name, Quantity = 0, Unit = string.Empty };
                        target.Add(current);
                    }
                    else if (line.StartsWith("-") || line.StartsWith(" -"))
                    {
                        string content = line.StartsWith("-") ? line.Substring(1).Trim() : line.Substring(2).Trim();
                        // content expected like "100 g" or "2 pcs"
                        if (!string.IsNullOrEmpty(content))
                        {
                            var tokens = content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            double qty = 0;
                            string unit = string.Empty;
                            if (tokens.Length >= 1)
                            {
                                if (!double.TryParse(tokens[0], NumberStyles.Float, CultureInfo.InvariantCulture, out qty))
                                {
                                    double.TryParse(tokens[0], NumberStyles.Float, CultureInfo.CurrentCulture, out qty);
                                }
                            }
                            if (tokens.Length > 1)
                                unit = string.Join(" ", tokens, 1, tokens.Length - 1);

                            if (current != null)
                            {
                                current.Quantity = qty;
                                current.Unit = unit;
                            }
                        }
                    }
                    else
                    {
                        // unknown line - ignore
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load inventory: " + ex.Message, "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Convenience: returns a new list loaded from the inventory save file.
        /// </summary>
        public List<InventoryItem> LoadInventoryFromFile()
        {
            var result = new List<InventoryItem>();
            LoadInventoryInto(result);
            return result;
        }

        /// <summary>
        /// Clear the in-memory recipes and remove the save file.
        /// Use the instance method to ensure the in-memory list is updated.
        /// </summary>
        public void ClearAll()
        {
            // clear in-memory list used by the app
            if (Program.recipes != null)
                Program.recipes.Clear();

            // delete the on-disk file
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(folderPath, "RecipeSave.txt");
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete save file: " + ex.Message, "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Static wrapper kept for compatibility with existing callers
        public static void ClearSavedRecipes()
        {
            Instance.ClearAll();
        }
    }
}
