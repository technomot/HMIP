using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PasswordProtectionApp.Models;

namespace PasswordProtectionApp.Services
{
    public class UserStore
    {
        public const string AdminUserName = "ADMIN";

        private readonly string _filePath;
        public List<UserAccount> Users { get; private set; } = new List<UserAccount>();

        public UserStore(string filePath = null)
        {
            _filePath = filePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PasswordProtectionApp", "users.json");

            Load();
        }

        public void Load()
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(_filePath))
            {
                Users = new List<UserAccount>
                {
                    new UserAccount(AdminUserName, string.Empty, false, false, 1)
                };
                Save();
                return;
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                Users = JsonSerializer.Deserialize<List<UserAccount>>(json) ?? new List<UserAccount>();

                if (!Users.Any(u => u.UserName.Equals(AdminUserName, StringComparison.OrdinalIgnoreCase)))
                {
                    Users.Insert(0, new UserAccount(AdminUserName, string.Empty, false, false, 1));
                    Save();
                }
            }
            catch
            {
                Users = new List<UserAccount>
                {
                    new UserAccount(AdminUserName, string.Empty, false, false, 1)
                };
                Save();
            }
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(Users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public UserAccount FindUser(string userName)
        {
            return Users.FirstOrDefault(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
        }

        public bool UserExists(string userName) => FindUser(userName) != null;

        public UserAccount AddUser(string userName, int minPasswordLength = 1)
        {
            if (UserExists(userName))
                throw new InvalidOperationException("A user with this name already exists.");

            var user = new UserAccount(userName, string.Empty, false, false, minPasswordLength);
            Users.Add(user);
            Save();
            return user;
        }

        public void RemoveUser(string userName)
        {
            var user = FindUser(userName);
            if (user != null)
            {
                Users.Remove(user);
                Save();
            }
        }
    }
}