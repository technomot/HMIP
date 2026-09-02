using System.Linq;

namespace PasswordProtectionApp.Services
{
    public static class PasswordValidator
    {
        private const string ArithmeticSigns = "+-*/=";

        public static bool HasLower(string s) => s.Any(char.IsLower);
        public static bool HasUpper(string s) => s.Any(char.IsUpper);
        public static bool HasDigit(string s) => s.Any(char.IsDigit);
        public static bool HasPunctuation(string s) => s.Any(c => char.IsPunctuation(c) && !ArithmeticSigns.Contains(c));
        public static bool HasArithmetic(string s) => s.Any(c => ArithmeticSigns.Contains(c));
        public static bool HasLetter(string s) => s.Any(char.IsLetter);
        public static (bool IsValid, string Message) Validate(string password, int minLength, bool restrictionEnabled)
        {
            password ??= string.Empty;

            if (password.Length < minLength)
                return (false, $"Password must be at least {minLength} character(s) long.");

            if (!restrictionEnabled)
                return (true, string.Empty);

            int classCount = 0;
            if (HasLower(password)) classCount++;
            if (HasUpper(password)) classCount++;
            if (HasDigit(password)) classCount++;
            if (HasPunctuation(password)) classCount++;
            if (HasArithmetic(password)) classCount++;

            if (classCount < 2)
            {
                return (false,
                    "Password does not satisfy the \"character sampling\" rule: it must combine " +
                    "characters from at least two categories (lowercase letters, uppercase letters, " +
                    "digits, punctuation marks, or arithmetic signs +-*/=).");
            }

            return (true, string.Empty);
        }
    }
}