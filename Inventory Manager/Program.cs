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
        public static List<InventoryItem> inventory { get; set; } = new List<InventoryItem>();
        public static Dictionary<int, string> eatingTimes { get; set; } = new Dictionary<int, string>
        {
            {1,"Breakfast"},
            {2,"Lunch"},
            {3,"Dinner"},
            {4,"Dessert"},
        };
        public readonly static string[] units = new[] { "g", "kg", "oz", "lb", "ml", "l", "pcs" };

        [STAThread]
        static void Main()
        {
            // Load main lists BEFORE creating forms so all saves use the same in-memory lists
            recipes = SqliteDataAccess.LoadRecipes();
            inventory = SqliteDataAccess.LoadInventory();
            SqliteDataAccess.LoadScheduleMenu(); //Updates list through function

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    public class Recipe
    {
        public int ID { get; set; }             
        public string Name { get; set; }
        public List<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>(); // initialize list to avoid nulls
    }


    public class InventoryItem
    {
        public int ID { get; set; }             
        public string Name { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
    }

    public class RecipeIngredient
    {
        public int ID { get; set; }               
        public int RecipeId { get; set; }         
        public string Name { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
    }


    public class Menu
    {
        public int DateAsInt { get; set; }  
        public Dictionary<int, MenuSection> sections { get; set; } = new Dictionary<int, MenuSection>();

        public Menu() { } 

        public Menu(DateTime time)
        {
            DateAsInt = int.Parse(time.ToString("yyyyMMdd"));
        }
    }

    public class MenuSection
    {
        public List<string> sectionRecipeNames { get; set; } = new List<string>();
    }

    //Schedule has key for dates and the menu that corresponds.
    //Menu will contain the eating times if applied

    public class MenuManager
    {
        public static readonly MenuManager Instance = new MenuManager();
        public Menu selectedMenu = null;

        private MenuManager() { }

        // Normalize DateTime to only year/month/day
        private DateTime NormalizeDate(DateTime dt) => dt.Date;

        // Select a date and ensure it exists in schedule
        public void SelectedDate(DateTime date)
        {
            DateTime normDate = NormalizeDate(date);

            if (!Program.scheduleMenu.TryGetValue(normDate, out selectedMenu))
            {
                selectedMenu = new Menu(normDate);
                Program.scheduleMenu[normDate] = selectedMenu;
            }
        }

        // Add a recipe to a section
        public void AddToSection(int sectionNum, string recipeName)
        {
            if (selectedMenu == null)
                throw new InvalidOperationException("No menu selected. Call SelectedDate() first.");

            if (!Program.eatingTimes.ContainsKey(sectionNum))
                return;

            if (!selectedMenu.sections.TryGetValue(sectionNum, out var section))
            {
                section = new MenuSection();
                selectedMenu.sections[sectionNum] = section;
            }

            if (!section.sectionRecipeNames.Contains(recipeName))
                section.sectionRecipeNames.Add(recipeName);

            // Update dictionary with normalized key
            Program.scheduleMenu[NormalizeDate(selectedMenu.DateAsIntAsDate())] = selectedMenu;

            SqliteDataAccess.SaveScheduleMenu();
        }

        // Remove a recipe from a section
        public void RemoveFromSection(int sectionNum, string recipeName)
        {
            if (selectedMenu == null)
                throw new InvalidOperationException("No menu selected. Call SelectedDate() first.");

            if (selectedMenu.sections.TryGetValue(sectionNum, out var section))
            {
                section.sectionRecipeNames.Remove(recipeName);
            }

            Program.scheduleMenu[NormalizeDate(selectedMenu.DateAsIntAsDate())] = selectedMenu;

            SqliteDataAccess.SaveScheduleMenu();
        }

        // Get all sections for selected menu
        public Dictionary<int, MenuSection> GrabSectionList()
        {
            return selectedMenu?.sections;
        }
    }

    // Helper extension
    public static class MenuExtensions
    {
        public static DateTime DateAsIntAsDate(this Menu menu)
        {
            return new DateTime(menu.DateAsInt / 10000, (menu.DateAsInt / 100) % 100, menu.DateAsInt % 100);
        }
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

        List<string> weightOrder = new List<string> { "g", "kg", "lb" };
        List<string> volumeOrder = new List<string> { "ml", "l" };
        Dictionary<string, float> toBase = new Dictionary<string, float>
        {       
            { "g", 1f },
            { "kg", 1000f },
            { "oz", 28.3495f },
            { "lb", 453.592f },

            { "ml", 1f },
            { "l", 1000f },

            { "pcs", 1f }
        };
        public KeyValuePair<double, string> UnitConversion(double amount, string unit)
        {
            if (unit == "pcs" || amount == 0)
                return new KeyValuePair<double, string>(amount, unit);

            // Determine category
            bool isWeight = unit == "g" || unit == "kg" || unit == "oz" || unit == "lb";
            bool isVolume = unit == "ml" || unit == "l";

            // Pick order list
            var order = isWeight ? weightOrder : volumeOrder;

            // Convert to base unit
            double baseValue = amount * toBase[unit];

            // Find best unit to display
            string bestUnit = unit;
            double bestValue = amount;

            foreach (string u in order)
            {
                double value = baseValue / toBase[u];
                if (value >= 1 && value < 1000)
                {
                    bestUnit = u;
                    bestValue = value;
                }
            }

            return new KeyValuePair<double, string>(bestValue, bestUnit);
        }

        public double ConvertTo(double amount, string fromUnit, string toUnit)
        {

            if (fromUnit == "pcs" || toUnit == "pcs")
                return amount;

            double baseValue = amount * toBase[fromUnit];
            return baseValue / toBase[toUnit];
        }



    }



}
