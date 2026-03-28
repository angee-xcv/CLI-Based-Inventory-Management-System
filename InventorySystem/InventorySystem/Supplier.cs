using System;

namespace InventorySystem
{
    public class Supplier
    {
        private static int _nextId = 1;

        public int Id { get; private set; }
        public string Name { get; private set; }
        public string ContactPerson { get; private set; }
        public string Email { get; private set; }
        public string Phone { get; private set; }
        public bool IsActive { get; private set; }

        public Supplier(string name, string contactPerson, string email, string phone)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Supplier name is required.");
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@")) throw new ArgumentException("Valid email is required.");
            if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("PhoneNumber is required.");

            Id = _nextId++;
            Name = name.Trim();
            ContactPerson = contactPerson?.Trim() ?? "";
            Email = email.Trim().ToLower();
            Phone = phone.Trim();
            IsActive = true;
        }

        public void Update(string name, string contactPerson, string email, string phone)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Supplier name is required.");
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@")) throw new ArgumentException("Valid email is required.");
            Name = name.Trim();
            ContactPerson = contactPerson?.Trim() ?? "";
            Email = email.Trim().ToLower();
            Phone = phone.Trim();
        }

        public void Delete() => IsActive = false;

        public static void ResetCounter(int val) => _nextId = val;
    }
}