using System;

namespace InventorySystem
{
    public class Category
    {
        private static int _nextId = 1;

        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public bool IsActive { get; private set; }

        public Category(string name, string description = "")
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.");
            Id = _nextId++;
            Name = name.Trim();
            Description = description?.Trim() ?? "";
            IsActive = true;
        }

        public void Update(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.");
            Name = name.Trim();
            Description = description?.Trim() ?? "";
        }

        public void Delete() => IsActive = false;

        public static void ResetCounter(int value) => _nextId = value;
    }
}   