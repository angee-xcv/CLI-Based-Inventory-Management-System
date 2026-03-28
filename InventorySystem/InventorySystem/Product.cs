using System;

namespace InventorySystem
{
    public class Product
    {
        private static int _nextId = 1;

        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public decimal Price { get; private set; }
        public int Stock { get; private set; }
        public int LowStockThreshold { get; private set; }
        public int CategoryId { get; private set; }
        public int SupplierId { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public Product(string name, string description, decimal price, int stock, int lowStockThreshold, int categoryId, int supplierId)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Product name is required.");
            if (price < 0) throw new ArgumentException("Price cannot be negative.");
            if (stock < 0) throw new ArgumentException("Stock cannot be negative.");
            if (lowStockThreshold < 0) throw new ArgumentException("Threshold cannot be negative.");

            Id = _nextId++;
            Name = name.Trim();
            Description = description?.Trim() ?? "";
            Price = price;
            Stock = stock;
            LowStockThreshold = lowStockThreshold;
            CategoryId = categoryId;
            SupplierId = supplierId;
            IsActive = true;
            CreatedAt = DateTime.Now;
        }

        public void Update(string name, string description, decimal price, int lowStockThreshold, int categoryId, int supplierId)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Product name is required.");
            if (price < 0) throw new ArgumentException("Price cannot be negative.");

            Name = name.Trim();
            Description = description?.Trim() ?? "";
            Price = price;
            LowStockThreshold = lowStockThreshold;
            CategoryId = categoryId;
            SupplierId = supplierId;
        }

        public void AddStock(int qty)
        {
            if (qty <= 0) throw new ArgumentException("Quantity must be greater than zero.");
            Stock += qty;
        }

        public void DeductStock(int qty)
        {
            if (qty <= 0) throw new ArgumentException("Quantity must be greater than zero.");
            if (qty > Stock) throw new InvalidOperationException($"Not enough stock. Available: {Stock}");
            Stock -= qty;
        }

        public void Delete() => IsActive = false;
        public bool IsLowStock() => Stock <= LowStockThreshold;
        public decimal TotalValue() => Price * Stock;

        public static void ResetCounter(int val) => _nextId = val;
    }
}