using System;

namespace InventorySystem
{
    public enum UserRole { Admin, Manager, Staff }

    public class User
    {
        private static int _nextId = 1;
        private string _password; 

        public int Id { get; private set; }
        public string Username { get; private set; }
        public string FullName { get; private set; }
        public UserRole Role { get; private set; }
        public bool IsActive { get; private set; }
        public int FailedAttempts { get; private set; }
        public DateTime? LastLogin { get; private set; }

        public bool IsLocked => FailedAttempts >= 5;

        public User(string username, string password, string fullName, UserRole role)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                throw new ArgumentException("Username must be at least 3 characters.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters.");
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name is required.");

            Id = _nextId++;
            Username = username.Trim().ToLower();
            _password = password; // store directly
            FullName = fullName.Trim();
            Role = role;
            IsActive = true;
        }

        public bool VerifyPassword(string password)
        {
            bool ok = password == _password;
            if (ok)
            {
                FailedAttempts = 0;
                LastLogin = DateTime.Now;
            }
            else
            {
                FailedAttempts++;
            }
            return ok;
        }

        public void ChangePassword(string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters.");
            _password = newPassword;
        }

        public void Unlock() => FailedAttempts = 0;
        public void Deactivate() => IsActive = false;
        public void Activate() => IsActive = true;

        public static void ResetCounter(int val) => _nextId = val;
    }
}