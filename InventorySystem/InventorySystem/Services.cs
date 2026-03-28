using System;
using System.Collections.Generic;
using System.Linq;

namespace InventorySystem
{

    public class Services
    {
        private readonly List<Product> _products = new List<Product>();
        private readonly List<Category> _categories = new List<Category>();
        private readonly List<Supplier> _suppliers = new List<Supplier>();
        private readonly List<TransactionRecord> _transactions = new List<TransactionRecord>();
        private readonly List<User> _users = new List<User>();

        private User _currentUser;
        public User CurrentUser => _currentUser;
        public bool IsLoggedIn => _currentUser != null;

 
        public Services()
        {
            //admin account
            _users.Add(new User("admin", "Admin@123", "System Administrator", UserRole.Admin));

            // example data
            _categories.Add(new Category("Electronics", "Electronic devices"));
            _categories.Add(new Category("Office Supplies", "Stationery & materials"));
            _categories.Add(new Category("Food & Beverages", "Consumables"));

            _suppliers.Add(new Supplier("TechWorld Inc.", "Alice Reyes", "alice@techworld.com", "09171234567"));
            _suppliers.Add(new Supplier("OfficeMax PH", "Ben Santos", "ben@officemax.ph", "09281234567"));
            _suppliers.Add(new Supplier("FoodHub Corp.", "Carol Lim", "carol@foodhub.ph", "09391234567"));

            AddProduct("Laptop Pro 15", "High-performance laptop", 55000, 12, 3, 1, 1);
            AddProduct("Wireless Mouse", "Ergonomic wireless mouse", 850, 50, 10, 1, 1);
            AddProduct("USB-C Hub", "7-in-1 USB-C hub", 1200, 2, 5, 1, 1);
            AddProduct("Bond Paper (500s)", "A4 ream", 230, 100, 20, 2, 2);
            AddProduct("Ballpen Box", "Blue ballpens (box of 12)", 95, 60, 15, 2, 2);
            AddProduct("Instant Coffee 3-in-1", "Box of 30 sachets", 180, 4, 10, 3, 3);
            AddProduct("Mineral Water Case", "500ml x 24 bottles", 250, 20, 5, 3, 3);

            _transactions.Clear(); 
        }

        public User Login(string username, string password)
        {
            var user = _users.FirstOrDefault(u =>
                u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));

            if (user == null)
                throw new UnauthorizedAccessException("Invalid username or password.");
            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is deactivated.");
            if (user.IsLocked)
                throw new UnauthorizedAccessException("Account locked (5 failed attempts). Ask an Admin.");

            if (!user.VerifyPassword(password))
            {
                int left = 5 - user.FailedAttempts;
                string msg = left > 0
                    ? $"Wrong password. {left} attempt(s) left."
                    : "Account is now locked.";
                throw new UnauthorizedAccessException(msg);
            }

            _currentUser = user;
            return user;
        }

        public void Logout() => _currentUser = null;

        public void RequireRole(UserRole minimum)
        {
            if (_currentUser == null) throw new UnauthorizedAccessException("Not logged in.");
            if ((int)_currentUser.Role > (int)minimum)
                throw new UnauthorizedAccessException($"Requires {minimum} role or higher.");
        }

        // ── USERS ───────────────────────────────────────────────
        public User AddUser(string username, string password, string fullName, UserRole role)
        {
            RequireRole(UserRole.Admin);
            if (_users.Any(u => u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Username already taken.");
            var user = new User(username, password, fullName, role);
            _users.Add(user);
            return user;
        }

        public List<User> GetUsers() => _users.ToList();

        public void UnlockUser(int id)
        {
            RequireRole(UserRole.Admin);
            GetUser(id).Unlock();
        }

        public void ToggleUserActive(int id)
        {
            RequireRole(UserRole.Admin);
            var u = GetUser(id);
            if (u.Id == _currentUser.Id) throw new InvalidOperationException("Cannot deactivate yourself.");
            if (u.IsActive) u.Deactivate(); else u.Activate();
        }

        public void ResetPassword(int id, string newPassword)
        {
            RequireRole(UserRole.Admin);
            GetUser(id).ChangePassword(newPassword);
        }

        public void ChangeMyPassword(string current, string newPassword)
        {
            if (_currentUser == null) throw new UnauthorizedAccessException("Not logged in.");
            if (!_currentUser.VerifyPassword(current)) throw new UnauthorizedAccessException("Current password is wrong.");
            _currentUser.ChangePassword(newPassword);
            _currentUser.Unlock();
        }

        private User GetUser(int id) =>
            _users.FirstOrDefault(u => u.Id == id)
            ?? throw new KeyNotFoundException($"User ID {id} not found.");

        // CATEGORIES 
        public Category AddCategory(string name, string description = "")
        {
            RequireRole(UserRole.Manager);
            if (_categories.Any(c => c.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) && c.IsActive))
                throw new InvalidOperationException("Category already exists.");
            var cat = new Category(name, description);
            _categories.Add(cat);
            return cat;
        }

        public List<Category> GetCategories() => _categories.Where(c => c.IsActive).ToList();

        public Category GetCategory(int id) =>
            _categories.FirstOrDefault(c => c.Id == id)
            ?? throw new KeyNotFoundException($"Category ID {id} not found.");

        //  SUPPLIERS 
        public Supplier AddSupplier(string name, string contact, string email, string phone)
        {
            RequireRole(UserRole.Manager);
            if (_suppliers.Any(s => s.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase) && s.IsActive))
                throw new InvalidOperationException("A supplier with that email already exists.");
            var sup = new Supplier(name, contact, email, phone);
            _suppliers.Add(sup);
            return sup;
        }

        public List<Supplier> GetSuppliers() => _suppliers.Where(s => s.IsActive).ToList();

        public Supplier GetSupplier(int id) =>
            _suppliers.FirstOrDefault(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Supplier ID {id} not found.");

        // PRODUCTS
        public Product AddProduct(string name, string description, decimal price,
            int stock, int threshold, int categoryId, int supplierId)
        {
            var category = GetCategory(categoryId);
            if (!category.IsActive) throw new InvalidOperationException("Category is inactive.");
            var sup = GetSupplier(supplierId);
            if (!sup.IsActive) throw new InvalidOperationException("Supplier is inactive.");
            if (_products.Any(p => p.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) && p.IsActive))
                throw new InvalidOperationException("A product with that name already exists.");

            var product = new Product(name, description, price, stock, threshold, categoryId, supplierId);
            _products.Add(product);
            Log(TransactionType.ProductAdded, product, $"Added with stock {stock}");
            return product;
        }

        public List<Product> GetProducts() => _products.Where(p => p.IsActive).ToList();

        public Product GetProduct(int id) =>
            _products.FirstOrDefault(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Product ID {id} not found.");

        public List<Product> Search(string keyword)
        {
            keyword = keyword?.Trim().ToLower() ?? "";
            return _products.Where(p => p.IsActive &&
                (p.Name.ToLower().Contains(keyword) || p.Description.ToLower().Contains(keyword))).ToList();
        }

        public void UpdateProduct(int id, string name, string description, decimal price,
            int threshold, int categoryId, int supplierId)
        {
            RequireRole(UserRole.Manager);
            var p = GetProduct(id);
            if (!p.IsActive) throw new InvalidOperationException("Product is deleted.");
            GetCategory(categoryId); GetSupplier(supplierId);
            if (_products.Any(x => x.Id != id && x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) && x.IsActive))
                throw new InvalidOperationException("Another product with that name exists.");
            p.Update(name, description, price, threshold, categoryId, supplierId);
            Log(TransactionType.ProductUpdated, p, "Details updated");
        }

        public void DeleteProduct(int id)
        {
            RequireRole(UserRole.Manager);
            var p = GetProduct(id);
            p.Delete();
            Log(TransactionType.ProductDeleted, p, "Soft-deleted");
        }

        public void Restock(int id, int qty, string notes = "")
        {
            var p = GetProduct(id);
            int before = p.Stock;
            p.AddStock(qty);
            Log(TransactionType.StockIn, p, notes, qty, before, p.Stock);
        }

        public void DeductStock(int id, int qty, string notes = "")
        {
            var p = GetProduct(id);
            int before = p.Stock;
            p.DeductStock(qty);
            Log(TransactionType.StockOut, p, notes, qty, before, p.Stock);
        }

        public List<Product> GetLowStock() =>
            _products.Where(p => p.IsActive && p.IsLowStock()).ToList();

        public decimal TotalInventoryValue() =>
            _products.Where(p => p.IsActive).Sum(p => p.TotalValue());

        // TRANSACTIONS
        public List<TransactionRecord> GetTransactions() =>
            _transactions.OrderByDescending(t => t.Timestamp).ToList();

        public List<TransactionRecord> GetTransactionsByProduct(int productId) =>
            _transactions.Where(t => t.ProductId == productId)
                         .OrderByDescending(t => t.Timestamp).ToList();

        private void Log(TransactionType type, Product p, string notes,
            int? qty = null, int? before = null, int? after = null)
        {
            _transactions.Add(new TransactionRecord(
                type, p.Id, p.Name, _currentUser?.Username ?? "system", notes, qty, before, after));
        }
    }
}