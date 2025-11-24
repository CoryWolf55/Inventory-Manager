using System;
using System.Data;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace Inventory_Manager
{
    public class SqliteDataAccess
    {
        #region Recipes + Ingredients
        public static List<Recipe> LoadRecipes()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                // Load recipes
                var recipes = cnn.Query<Recipe>("SELECT ID, Name FROM Recipes").ToList();

                // Load ingredients
                var ingredients = cnn.Query<RecipeIngredient>(
                    "SELECT RecipeId, Name, Quantity, Unit FROM RecipeIngredient").ToList();

                // Attach ingredients to recipes
                foreach (var recipe in recipes)
                {
                    recipe.Ingredients = ingredients.Where(i => i.RecipeId == recipe.ID).ToList();
                }

                return recipes;
            }
        }

        public static void SaveRecipes()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                // Clear old data
                ClearRecipes();

                foreach (Recipe recipe in Program.recipes)
                {
                    // Insert recipe and get generated ID
                    long recipeId = cnn.ExecuteScalar<long>(
                        "INSERT INTO Recipes (Name) VALUES (@Name); SELECT last_insert_rowid();",
                        recipe);

                    // Insert ingredients
                    foreach (var ing in recipe.Ingredients)
                    {
                        cnn.Execute(
                            "INSERT INTO RecipeIngredient (RecipeId, Name, Quantity, Unit) VALUES (@RecipeId, @Name, @Quantity, @Unit)",
                            new { RecipeId = recipeId, ing.Name, ing.Quantity, ing.Unit });
                    }
                }
            }
        }

        public static void ClearRecipes()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("DELETE FROM RecipeIngredient"); // clear ingredients first
                cnn.Execute("DELETE FROM Recipes"); // then recipes
            }
        }
        #endregion

        #region Inventory
        public static List<InventoryItem> LoadInventory()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                return cnn.Query<InventoryItem>("SELECT Name, Quantity, Unit FROM InventoryItem").ToList();
            }
        }

        public static void SaveInventory()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("DELETE FROM InventoryItem"); // clear old data
                foreach (InventoryItem inv in Program.inventory)
                {
                    cnn.Execute(
                        "INSERT INTO InventoryItem (Name, Quantity, Unit) VALUES (@Name, @Quantity, @Unit)",
                        inv);
                }
            }
        }
        #endregion

        #region ScheduleMenu
        public static void LoadScheduleMenu()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                Program.scheduleMenu.Clear(); // clear old in-memory data

                var menuDates = cnn.Query<int>("SELECT DateAsInt FROM Menus").ToList();
                foreach (var dateAsInt in menuDates)
                {
                    DateTime dt = DateTime.ParseExact(dateAsInt.ToString(), "yyyyMMdd", null);
                    var menu = new Menu(dt);

                    var sections = cnn.Query<(int SectionNum, string RecipeName)>(
                        "SELECT SectionNum, RecipeName FROM MenuSections WHERE MenuDate=@DateAsInt",
                        new { DateAsInt = dateAsInt }).ToList();

                    foreach (var s in sections)
                    {
                        if (!menu.sections.ContainsKey(s.SectionNum))
                            menu.sections[s.SectionNum] = new MenuSection();

                        menu.sections[s.SectionNum].sectionRecipeNames.Add(s.RecipeName);
                    }

                    Program.scheduleMenu[dt] = menu;
                }
            }
        }

        public static void SaveScheduleMenu()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                // Clear old data
                cnn.Execute("DELETE FROM Menus");
                cnn.Execute("DELETE FROM MenuSections");

                foreach (var kvp in Program.scheduleMenu)
                {
                    int dateAsInt = int.Parse(kvp.Key.ToString("yyyyMMdd"));
                    cnn.Execute("INSERT INTO Menus (DateAsInt) VALUES (@DateAsInt)", new { DateAsInt = dateAsInt });

                    foreach (var section in kvp.Value.sections)
                    {
                        int sectionNum = section.Key;
                        foreach (var recipeName in section.Value.sectionRecipeNames)
                        {
                            cnn.Execute(
                                "INSERT INTO MenuSections (MenuDate, SectionNum, RecipeName) VALUES (@MenuDate, @SectionNum, @RecipeName)",
                                new { MenuDate = dateAsInt, SectionNum = sectionNum, RecipeName = recipeName });
                        }
                    }
                }
            }
        }
        #endregion

        private static string LoadConnectionString(string id = "Default")
        {
            return System.Configuration.ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}
