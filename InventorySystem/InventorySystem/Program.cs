using System;
using System.Collections.Generic;

namespace InventorySystem
{
    class Program
    {
        static readonly Services service = new Services();

        static void Main(string[] args)
        {
            Console.Title = "Inventory Management System";

            while (true)
            {
                if (!service.IsLoggedIn)
                {
                    if (!LoginScreen()) return;
                }
                MainMenu();
            }
        }

        //  LOGIN
        static bool LoginScreen()
        {
            while (true)
            {
                Console.Clear();
                Header("INVENTORY MANAGEMENT SYSTEM");
                Console.WriteLine("  Default login: admin / Admin@123");
                //Divider();

                string username = Ask("Username");
                string password = AskPassword("Password");

                try
                {
                    var user = service.Login(username, password);
                    Success($"Welcome, {user.FullName}! ({user.Role})");
                    Pause();
                    return true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Error(ex.Message);
                    Console.Write("\n  [R] Retry: ");
                    Console.Write("\n  [0] Exit: ");
                    string option = Console.ReadLine()?.Trim().ToUpper();
                    if (option == "0") return false;
                }
            }
        }
        //  MAIN MENU

        static void MainMenu()
        {
            while (true)
            {
                Console.Clear();
                Header("MAIN MENU");
                Info($"Logged in as: {service.CurrentUser.FullName} [{service.CurrentUser.Role}]");
                //Divider();

                Console.WriteLine("  [1] Products");
                Console.WriteLine("  [2] Categories");
                Console.WriteLine("  [3] Suppliers");
                Console.WriteLine("  [4] Transaction History");
                if (service.CurrentUser.Role == UserRole.Admin)
                    Console.WriteLine("  [5] User Management");
                //Divider();
                Console.WriteLine("  [P] Change My Password");
                Console.WriteLine("  [L] Logout");
                Console.WriteLine("  [0] Exit");

                switch (MenuChoice())
                {
                    case "1": ProductMenu(); break;
                    case "2": CategoryMenu(); break;
                    case "3": SupplierMenu(); break;
                    case "4": TransactionMenu(); break;
                    case "5":
                        if (service.CurrentUser.Role == UserRole.Admin) UserMenu();
                        else Error("Admin access required."); Pause(); break;
                    case "P": ChangeMyPassword(); break;
                    case "L":
                        service.Logout();
                        Success("Logged out.");
                        Pause();
                        return;
                    case "0":
                        Console.Write("\n  Exit? (y/n): ");
                        if (Console.ReadLine()?.Trim().ToLower() == "y")
                        {
                            Console.Clear();
                            Console.WriteLine("\n  Goodbye!\n");
                            Environment.Exit(0);
                        }
                        break;
                }
            }
        }
        //  PRODUCT MENU
        static void ProductMenu()
        {
            while (true)
            {
                Console.Clear();
                Header("PRODUCTS");
                Console.WriteLine("  [1] View All Products");
                Console.WriteLine("  [2] Search Products");
                Console.WriteLine("  [3] Add Product");
                Console.WriteLine("  [4] Update Product");
                Console.WriteLine("  [5] Delete Product");
                Console.WriteLine("  [6] Restock Product");
                Console.WriteLine("  [7] Deduct Stock");
                Console.WriteLine("  [8] Low Stock Alerts");
                Console.WriteLine("  [9] Total Inventory Value");
                Console.WriteLine("  [0] Back");

                switch (MenuChoice())
                {
                    case "1": ViewAllProducts(); break;
                    case "2": SearchProducts(); break;
                    case "3": Try(AddProduct); break;
                    case "4": Try(UpdateProduct); break;
                    case "5": Try(DeleteProduct); break;
                    case "6": Try(RestockProduct); break;
                    case "7": Try(DeductStock); break;
                    case "8": LowStockAlerts(); break;
                    case "9": InventoryValue(); break;
                    case "0": return;
                }
            }
        }

        static void ViewAllProducts()
        {
            Console.Clear();
            Header("ALL PRODUCTS");
            var list = service.GetProducts();
            if (list.Count == 0) { Warn("No products found."); Pause(); return; }
            ProductTable(list);
            Pause();
        }

        static void SearchProducts()
        {
            Console.Clear();
            Header("SEARCH PRODUCTS");
            string keyword = Ask("Keyword");
            var results = service.Search(keyword);
            Console.WriteLine($"\n  Found {results.Count} result(s):\n");
            if (results.Count == 0) Warn("No products matched.");
            else ProductTable(results);
            Pause();
        }

        static void AddProduct()
        {
            service.RequireRole(UserRole.Manager);
            Console.Clear();
            Header("ADD PRODUCT");

            if (service.GetCategories().Count == 0) { Error("No categories. Add one first."); Pause(); return; }
            if (service.GetSuppliers().Count == 0) { Error("No suppliers. Add one first."); Pause(); return; }

            string name = Ask("Product Name");
            string description = Ask("Description (optional)", false);
            decimal price = AskDecimal("Price");
            int stock = AskInt("Initial Stock", 0);
            int threshold = AskInt("Low Stock Threshold", 0);

            CategoryList();
            int categoryId = AskInt("Category ID", 1);

            SupplierList();
            int supplierId = AskInt("Supplier ID", 1);

            var product = service.AddProduct(name, description, price, stock, threshold, categoryId, supplierId);
            Success($"Product '{product.Name}' added (ID: {product.Id}).");
            Pause();
        }

        static void UpdateProduct()
        {
            service.RequireRole(UserRole.Manager);
            Console.Clear();
            Header("UPDATE PRODUCT");
            var list = service.GetProducts();
            if (list.Count == 0) { Warn("No products."); Pause(); return; }
            ProductTable(list);

            int id = AskInt("Product ID to update", 1);
            var product = service.GetProduct(id);

            Console.WriteLine($"\n  Editing: [{product.Id}] {product.Name}  (press Enter to keep current value)\n");

            string name = AskOrKeep($"Name [{product.Name}]", product.Name);
            string description = AskOrKeep($"Description [{product.Description}]", product.Description);
            decimal price = ReadDecimalOrKeepCurrent($"Price [{product.Price:F2}]", product.Price);
            int thr = AskIntOrKeep($"Low Stock Threshold [{product.LowStockThreshold}]", product.LowStockThreshold);

            CategoryList();
            int catId = AskIntOrKeep($"Category ID [{product.CategoryId}]", product.CategoryId);

            SupplierList();
            int supId = AskIntOrKeep($"Supplier ID [{product.SupplierId}]", product.SupplierId);

            service.UpdateProduct(id, name, description, price, thr, catId, supId);
            Success("Product updated.");
            Pause();
        }

        static void DeleteProduct()
        {
            service.RequireRole(UserRole.Manager);
            Console.Clear();
            Header("DELETE PRODUCT");
            var list = service.GetProducts();
            if (list.Count == 0) { Warn("No products."); Pause(); return; }
            ProductTable(list);

            int id = AskInt("Product ID to delete", 1);
            var product = service.GetProduct(id);
            Console.Write($"\n  Delete [{product.Id}] {product.Name}? (y/n): ");
            if (Console.ReadLine()?.Trim().ToLower() != "y") { Info("Cancelled."); Pause(); return; }

            service.DeleteProduct(id);
            Success($"'{product.Name}' deleted.");
            Pause();
        }

        static void RestockProduct()
        {
            Console.Clear();
            Header("RESTOCK PRODUCT");
            var list = service.GetProducts();
            if (list.Count == 0) { Warn("No products."); Pause(); return; }
            ProductTable(list);

            int id = AskInt("Product ID", 1);
            int quantity = AskInt("Quantity to add", 1);
            string notes = Ask("Notes (optional)", false);

            service.Restock(id, quantity, notes);
            Success($"Added {quantity} unit(s).");
            Pause();
        }

        static void DeductStock()
        {
            Console.Clear();
            Header("DEDUCT STOCK");
            var list = service.GetProducts();
            if (list.Count == 0) { Warn("No products."); Pause(); return; }
            ProductTable(list);

            int id = AskInt("Product ID", 1);
            int quantity = AskInt("Quantity to deduct", 1);
            string notes = Ask("Reason/Notes (optional)", false);

            service.DeductStock(id, quantity, notes);
            Success($"Deducted {quantity} unit(s).");
            Pause();
        }

        static void LowStockAlerts()
        {
            Console.Clear();
            Header("LOW STOCK ALERTS");
            var list = service.GetLowStock();
            if (list.Count == 0) { Success("All products are stocked."); Pause(); return; }

            Warn($"{list.Count} product(s) are low on stock:\n");
            Console.WriteLine($"  {"ID",-5} {"Name",-25} {"Stock",-8} {"Threshold",-12} {"Value",12}");
            Console.WriteLine("  " + new string('-', 65));
            foreach (var p in list)
                Console.WriteLine($"  {p.Id,-5} {p.Name,-25} {p.Stock,-8} {p.LowStockThreshold,-12} {p.TotalValue(),12:C}");
            Pause();
        }

        static void InventoryValue()
        {
            Console.Clear();
            Header("TOTAL INVENTORY VALUE");
            var list = service.GetProducts();
            if (list.Count == 0) { Warn("No products."); Pause(); return; }

            Console.WriteLine($"  {"ID",-5} {"Name",-25} {"Price",10} {"Stock",8} {"Total Value",14}");
            Console.WriteLine("  " + new string('-', 65));

            foreach (var p in list)
                Console.WriteLine($"  {p.Id,-5} {p.Name,-25} {p.Price,10:C} {p.Stock,8} {p.TotalValue(),14:C}");

            Console.WriteLine("  " + new string('-', 65));
            Console.WriteLine($"\n  Overall Total: {service.TotalInventoryValue():C}");
            Console.WriteLine($"  Products: {list.Count}  |  Total Units: {TotalUnits(list)}");
            Pause();
        }

        static int TotalUnits(List<Product> list) { int t = 0; foreach (var p in list) t += p.Stock; return t; }

        //  CATEGORY MENU
        static void CategoryMenu()
        {
            while (true)
            {
                Console.Clear();
                Header("CATEGORIES");
                Console.WriteLine("  [1] View All Categories");
                Console.WriteLine("  [2] Add Category");
                Console.WriteLine("  [0] Back");

                switch (MenuChoice())
                {
                    case "1":
                        Console.Clear(); Header("ALL CATEGORIES");
                        var cats = service.GetCategories();
                        if (cats.Count == 0) { Warn("No categories."); Pause(); break; }
                        Console.WriteLine($"  {"ID",-5} {"Name",-25} {"Description",-35}");
                        Console.WriteLine("  " + new string('-', 65));
                        foreach (var c in cats)
                            Console.WriteLine($"  {c.Id,-5} {c.Name,-25} {c.Description,-35}");
                        Pause();
                        break;
                    case "2":
                        Try(() => {
                            Console.Clear(); Header("ADD CATEGORY");
                            string name = Ask("Category Name");
                            string desc = Ask("Description (optional)", false);
                            var cat = service.AddCategory(name, desc);
                            Success($"Category '{cat.Name}' added (ID: {cat.Id}).");
                            Pause();
                        });
                        break;
                    case "0": return;
                }
            }
        }

        //  SUPPLIER MENU
        static void SupplierMenu()
        {
            while (true)
            {
                Console.Clear();
                Header("SUPPLIERS");
                Console.WriteLine("  [1] View All Suppliers");
                Console.WriteLine("  [2] Add Supplier");
                Console.WriteLine("  [0] Back");

                switch (MenuChoice())
                {
                    case "1":
                        Console.Clear(); Header("ALL SUPPLIERS");
                        var sups = service.GetSuppliers();
                        if (sups.Count == 0) { Warn("No suppliers."); Pause(); break; }
                        Console.WriteLine($"  {"ID",-5} {"Name",-22} {"Contact",-18} {"Email",-28} {"Phone",-14}");
                        Console.WriteLine("  " + new string('-', 90));
                        foreach (var s in sups)
                            Console.WriteLine($"  {s.Id,-5} {s.Name,-22} {s.ContactPerson,-18} {s.Email,-28} {s.Phone,-14}");
                        Pause();
                        break;
                    case "2":
                        Try(() => {
                            Console.Clear(); Header("ADD SUPPLIER");
                            string name = Ask("Supplier Name");
                            string contact = Ask("Contact Person");
                            string email = Ask("Email");
                            string phone = Ask("Phone");
                            var sup = service.AddSupplier(name, contact, email, phone);
                            Success($"Supplier '{sup.Name}' added (ID: {sup.Id}).");
                            Pause();
                        });
                        break;
                    case "0": return;
                }
            }
        }

        //  TRANSACTION MENU
        static void TransactionMenu()
        {
            while (true)
            {
                Console.Clear();
                Header("TRANSACTION HISTORY");
                Console.WriteLine("  [1] All Transactions");
                Console.WriteLine("  [2] By Product ID");
                Console.WriteLine("  [0] Back");

                switch (MenuChoice())
                {
                    case "1":
                        Console.Clear(); Header("ALL TRANSACTIONS");
                        PrintTransactions(service.GetTransactions());
                        Pause();
                        break;
                    case "2":
                        Console.Clear(); Header("TRANSACTIONS BY PRODUCT");
                        int pid = AskInt("Product ID", 1);
                        PrintTransactions(service.GetTransactionsByProduct(pid));
                        Pause();
                        break;
                    case "0": return;
                }
            }
        }

        static void PrintTransactions(List<TransactionRecord> list)
        {
            if (list.Count == 0) { Warn("No transactions found."); return; }
            Console.WriteLine($"  {"#",-6} {"Date & Time",-20} {"Type",-12} {"Product",-22} {"Qty",5} {"Before",7} {"After",7} {"By",-12}");
            Console.WriteLine("  " + new string('-', 95));
            foreach (var t in list)
            {
                string qty = t.QuantityChanged.HasValue ? t.QuantityChanged.ToString() : "-";
                string before = t.StockBefore.HasValue ? t.StockBefore.ToString() : "-";
                string after = t.StockAfter.HasValue ? t.StockAfter.ToString() : "-";
                Console.WriteLine($"  {t.Id,-6} {t.Timestamp:MM/dd/yy HH:mm:ss,-20} {t.TypeLabel(),-12} {t.ProductName,-22} {qty,5} {before,7} {after,7} {t.PerformedBy,-12}");
            }
        }

        //  USER MANAGEMENT
        static void UserMenu()
        {
            while (true)
            {
                Console.Clear();
                Header("USER MANAGEMENT");
                Console.WriteLine("  [1] View All Users");
                Console.WriteLine("  [2] Add User");
                Console.WriteLine("  [3] Unlock User");
                Console.WriteLine("  [4] Toggle Active/Inactive");
                Console.WriteLine("  [5] Reset User Password");
                Console.WriteLine("  [0] Back");

                switch (MenuChoice())
                {
                    case "1":
                        Console.Clear(); Header("ALL USERS");
                        var users = service.GetUsers();
                        Console.WriteLine($"  {"ID",-5} {"Username",-15} {"Full Name",-25} {"Role",-10} {"Status",-10} {"Last Login",-20}");
                        Console.WriteLine("  " + new string('-', 90));
                        foreach (var u in users)
                        {
                            string status = u.IsLocked ? "LOCKED" : (u.IsActive ? "Active" : "Inactive");
                            string last = u.LastLogin.HasValue ? u.LastLogin.Value.ToString("MM/dd/yy HH:mm") : "Never";
                            Console.WriteLine($"  {u.Id,-5} {u.Username,-15} {u.FullName,-25} {u.Role,-10} {status,-10} {last,-20}");
                        }
                        Pause();
                        break;
                    case "2":
                        Try(() => {
                            Console.Clear(); Header("ADD USER");
                            string uname = Ask("Username (min 3 chars)");
                            string pass = AskPassword("Password (min 6 chars)");
                            string fname = Ask("Full Name");
                            Console.WriteLine("  Roles: 0=Admin  1=Manager  2=Staff");
                            int r = AskInt("Role", 0, 2);
                            var u = service.AddUser(uname, pass, fname, (UserRole)r);
                            Success($"User '{u.Username}' created as {u.Role}.");
                            Pause();
                        }); break;
                    case "3": Try(() => { int id = AskInt("User ID to unlock", 1); service.UnlockUser(id); Success("User unlocked."); Pause(); }); break;
                    case "4": Try(() => { int id = AskInt("User ID to toggle", 1); service.ToggleUserActive(id); Success("Status toggled."); Pause(); }); break;
                    case "5":
                        Try(() => {
                            int id = AskInt("User ID", 1);
                            string np = AskPassword("New Password (min 6 chars)");
                            service.ResetPassword(id, np);
                            Success("Password reset.");
                            Pause();
                        }); break;
                    case "0": return;
                }
            }
        }

        static void ChangeMyPassword()
        {
            Console.Clear(); Header("CHANGE MY PASSWORD");
            try
            {
                string cur = AskPassword("Current Password");
                string np = AskPassword("New Password (min 6 chars)");
                string conf = AskPassword("Confirm New Password");
                if (np != conf) { Error("Passwords do not match."); Pause(); return; }
                service.ChangeMyPassword(cur, np);
                Success("Password changed.");
            }
            catch (Exception ex) { Error(ex.Message); }
            Pause();
        }

 

        static void ProductTable(List<Product> list)
        {
            Console.WriteLine($"  {"ID",-5} {"Name",-25} {"Price",10} {"Stock",7} {"Thresh",8} {"Cat",5} {"Sup",5} {"Status",8}");
            Console.WriteLine("  " + new string('-', 76));
            foreach (var p in list)
            {
                string status = p.IsLowStock() ? "LOW" : "OK";
                Console.WriteLine($"  {p.Id,-5} {p.Name,-25} {p.Price,10:C} {p.Stock,7} {p.LowStockThreshold,8} {p.CategoryId,5} {p.SupplierId,5} {status,8}");
            }
        }

        static void CategoryList()
        {
            Console.WriteLine("\n  -- Categories --");
            foreach (var c in service.GetCategories())
                Console.WriteLine($"  [{c.Id}] {c.Name}");
        }

        static void SupplierList()
        {
            Console.WriteLine("\n  -- Suppliers --");
            foreach (var s in service.GetSuppliers())
                Console.WriteLine($"  [{s.Id}] {s.Name}");
        }

        static void Header(string title)
        {
            string bar = new string('=', 28);
            Console.WriteLine($"\n  {bar}");
            Console.WriteLine($"  {title}");
           Console.WriteLine($"  {bar}\n");
        }

        
        static void Success(string msg) => Console.WriteLine($"\n  [OK] {msg}");
        static void Error(string msg) => Console.WriteLine($"\n  [ERROR] {msg}");
        static void Warn(string msg) => Console.WriteLine($"\n  [!] {msg}");
        static void Info(string msg) => Console.WriteLine($"  {msg}");
        static void Pause() { Console.Write("\n  Press any key to continue..."); Console.ReadKey(true); }

        static string MenuChoice()
        {
            Console.Write("\n  Choice: ");
            return Console.ReadLine()?.Trim().ToUpper() ?? "";
        }

        static string Ask(string prompt, bool required = true)
        {
            while (true)
            {
                Console.Write($"  {prompt}: ");
                string val = Console.ReadLine()?.Trim() ?? "";
                if (!required || val.Length > 0) return val;
                Error("This field is required.");
            }
        }

        static string AskOrKeep(string prompt, string current)
        {
            Console.Write($"  {prompt}: ");
            string val = Console.ReadLine()?.Trim() ?? "";
            return string.IsNullOrEmpty(val) ? current : val;
        }

        static string AskPassword(string prompt = "Password")
        {
            Console.Write($"  {prompt}: ");
            string pass = "";
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Backspace && pass.Length > 0) { pass = pass.Substring(0, pass.Length - 1); Console.Write("\b \b"); }
                else if (key.Key != ConsoleKey.Enter && !char.IsControl(key.KeyChar)) { pass += key.KeyChar; Console.Write("*"); }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return pass;
        }

        static int AskInt(string prompt, int min = int.MinValue, int max = int.MaxValue)
        {
            while (true)
            {
                string raw = Ask(prompt);
                if (int.TryParse(raw, out int value) && value >= min && value <= max) return value;
                Error($"Enter a whole number ({min}–{max}).");
            }
        }

        static int AskIntOrKeep(string prompt, int current)
        {
            Console.Write($"  {prompt}: ");
            string raw = Console.ReadLine()?.Trim() ?? "";
            return (string.IsNullOrEmpty(raw) || !int.TryParse(raw, out int value)) ? current : value;
        }

        static decimal AskDecimal(string prompt, decimal min = 0)
        {
            while (true)
            {
                string raw = Ask(prompt);
                if (decimal.TryParse(raw, out decimal value) && value >= min) return value;
                Error($"Enter a valid number (min {min}).");
            }
        }

        static decimal ReadDecimalOrKeepCurrent(string prompt, decimal current)
        {
            Console.Write($"  {prompt}: ");
            string raw = Console.ReadLine()?.Trim() ?? "";
            return (string.IsNullOrEmpty(raw) || !decimal.TryParse(raw, out decimal value)) ? current : value;
        }

        static void Try(Action action)
        {
            try { action(); }
            catch (UnauthorizedAccessException ex) { Error(ex.Message); Pause(); }
            catch (InvalidOperationException ex) { Error(ex.Message); Pause(); }
            catch (KeyNotFoundException ex) { Error(ex.Message); Pause(); }
            catch (ArgumentException ex) { Error(ex.Message); Pause(); }
            catch (Exception ex) { Error($"Unexpected: {ex.Message}"); Pause(); }
        }
    }
}