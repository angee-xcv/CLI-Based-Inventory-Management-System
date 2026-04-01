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
            _categories.Add(new Category("School Supplies", "Stationery & materials"));
            _categories.Add(new Category("Food & Beverages", "Consumables"));

            _suppliers.Add(new Supplier("Tech Inc.", "Alice Reyes", "alice@techworld.com", "09171234567"));
            _suppliers.Add(new Supplier("School PH", "Ben Santos", "ben@officemax.ph", "09281234567"));
            _suppliers.Add(new Supplier("FoodHub Corp.", "Carol Lim", "carol@foodhub.ph", "09391234567"));

            AddProduct("Laptop", "High-performance laptop", 55000, 12, 3, 1, 1);
            AddProduct("Wireless Mouse", "Ergonomic wireless mouse", 850, 50, 10, 1, 1);
            AddProduct("Casual Shirt", "Unisex cotton shirt", 1200, 2, 5, 1, 1);
            AddProduct("Bond Paper ", "A4 ream", 230, 100, 20, 2, 2);
            AddProduct("Ballpen Box", "Blue ballpens (12 pieces)", 95, 60, 15, 2, 2);
            AddProduct("Instant Coffee 3-in-1", "Box of 30 sachets", 180, 4, 10, 3, 3);
            AddProduct("Mineral Water Case", "500ml, 24 bottles", 250, 20, 5, 3, 3);

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
                throw new UnauthorizedAccessException("Account locked (5 failed attempts).");

            if (!user.VerifyPassword(password))
            {
                int left = 5 - user.FailedAttempts;
                string message = left > 0
                    ? $"Wrong password. {left} attempt(s) left."
                    : "Account is locked.";
                throw new UnauthorizedAccessException(message);
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

        //USERS 
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
            _users.FirstOrDefault(user => user.Id == id)
            ?? throw new KeyNotFoundException($"User ID {id} not found.");

        // CATEGORIES 
        public Category AddCategory(string name, string description = "")
        {
            RequireRole(UserRole.Manager);
            if (_categories.Any(category => category.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) && category.IsActive))
                throw new InvalidOperationException("Category already exists.");
            var categories = new Category(name, description);
            _categories.Add(categories);
            return categories;
        }

        public List<Category> GetCategories() => _categories.Where(c => c.IsActive).ToList();

        public Category GetCategory(int id) =>
            _categories.FirstOrDefault(category => category.Id == id)
            ?? throw new KeyNotFoundException($"Category ID {id} not found.");

        //  SUPPLIERS 
        public Supplier AddSupplier(string name, string contact, string email, string phone)
        {
            RequireRole(UserRole.Manager);
            if (_suppliers.Any(supplier => supplier.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase) && supplier.IsActive))
                throw new InvalidOperationException("A supplier with that email already exists.");
            var suppliers = new Supplier(name, contact, email, phone);
            _suppliers.Add(suppliers);
            return suppliers;
        }

        public List<Supplier> GetSuppliers() => _suppliers.Where(s => s.IsActive).ToList();

        public Supplier GetSupplier(int id) =>
            _suppliers.FirstOrDefault(supplier => supplier.Id == id)
            ?? throw new KeyNotFoundException($"Supplier ID {id} not found.");

        // PRODUCTS
        public Product AddProduct(string name, string description, decimal price,
            int stock, int threshold, int categoryId, int supplierId)
        {
            var category = GetCategory(categoryId);
            if (!category.IsActive) throw new InvalidOperationException("Category is inactive.");
            var supplier = GetSupplier(supplierId);
            if (!supplier.IsActive) throw new InvalidOperationException("Supplier is inactive.");
            if (_products.Any(products => products.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) && products.IsActive))
                throw new InvalidOperationException("A product with that name already exists.");

            var product = new Product(name, description, price, stock, threshold, categoryId, supplierId);
            _products.Add(product);
            Log(TransactionType.ProductAdded, product, $"Added with stock {stock}");
            return product;
        }

        public List<Product> GetProducts() => _products.Where(products => products.IsActive).ToList();

        public Product GetProduct(int id) =>
            _products.FirstOrDefault(products => products.Id == id)
            ?? throw new KeyNotFoundException($"Product ID {id} not found.");

        public List<Product> Search(string keyword)
        {
            keyword = keyword?.Trim().ToLower() ?? "";
            return _products.Where(products => products.IsActive &&
                (products.Name.ToLower().Contains(keyword) || products.Description.ToLower().Contains(keyword))).ToList();
        }

        public void UpdateProduct(int id, string name, string description, decimal price,
            int threshold, int categoryId, int supplierId)
        {
            RequireRole(UserRole.Manager);
            var product = GetProduct(id);
            if (!product.IsActive) throw new InvalidOperationException("Product is deleted.");
            GetCategory(categoryId); GetSupplier(supplierId);
            if (_products.Any(item => item.Id != id && item.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) && item.IsActive))
                throw new InvalidOperationException("Another product with that name exists.");
            product.Update(name, description, price, threshold, categoryId, supplierId);
            Log(TransactionType.ProductUpdated, product, "Details updated");
        }

        public void DeleteProduct(int id)
        {
            RequireRole(UserRole.Manager);
            var product = GetProduct(id);
            product.Delete();
            Log(TransactionType.ProductDeleted, product, "Soft-deleted");
        }

        public void Restock(int id, int quantity, string notes = "")
        {
            var product = GetProduct(id);
            int before = product.Stock;
            product.AddStock(quantity);
            Log(TransactionType.StockIn, product, notes, quantity, before, product.Stock);
        }

        public void DeductStock(int id, int quantity, string notes = "")
        {
            var product = GetProduct(id);
            int before = product.Stock;
            product.DeductStock(quantity);
            Log(TransactionType.StockOut, product, notes, quantity, before, product.Stock);
        }

        public List<Product> GetLowStock() =>
            _products.Where(product => product.IsActive && product.IsLowStock()).ToList();

        public decimal TotalInventoryValue() =>
            _products.Where(product => product.IsActive).Sum(product => product.TotalValue());

        // TRANSACTIONS
        public List<TransactionRecord> GetTransactions() =>
            _transactions.OrderByDescending(transaction => transaction.Timestamp).ToList();

        public List<TransactionRecord> GetTransactionsByProduct(int productId) =>
            _transactions.Where(transaction => transaction.ProductId == productId)
                         .OrderByDescending(transaction => transaction.Timestamp).ToList();

        private void Log(TransactionType type, Product product, string notes,
            int? quantity = null, int? before = null, int? after = null)
        {
            _transactions.Add(new TransactionRecord(
                type, product.Id, product.Name, _currentUser?.Username ?? "system", notes, quantity, before, after));
        }
    }
}