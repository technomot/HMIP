namespace PasswordProtectionApp.Services
{
    public static class PasswordValidator
    {
        public static (bool IsValid, string Message) Validate(string secret, int minLength, int sampleSize, bool restrictionEnabled)
        {
            secret ??= string.Empty;

            if (secret.Length < minLength)
                return (false, $"The password/secret must be at least {minLength} character(s) long.");

            if (restrictionEnabled && secret.Length < sampleSize)
            {
                return (false,
                    $"With character sampling enabled, the secret must be at least {sampleSize} " +
                    "character(s) long, so that this many distinct positions can be requested at login.");
            }

            return (true, string.Empty);
        }
    }
}