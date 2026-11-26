using System;

namespace PassVault
{
    public static class PasswordPolicy
    {
        public const int MinimumLength = 6;
        private static readonly string[] CommonPasswords = new string[]
        {
            "123456", "1234567", "12345678", "123456789", "1234567890",
            "111111", "000000", "654321", "abcdef", "abcdefg", "abcdefgh",
            "password", "password1", "password123", "passw0rd", "qwerty",
            "qwerty123", "abc123", "123abc", "abc123456", "a123456",
            "admin", "admin123", "admin888", "letmein", "welcome",
            "welcome1", "iloveyou", "qazwsx", "qazwsx123", "1qaz2wsx",
            "1q2w3e4r", "asdfgh", "asdfghjkl", "zxcvbn", "zxcvbnm",
            "root123", "test123", "login123", "changeme"
        };

        public static void Validate(string password, string fieldName)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new VaultException(fieldName + "不能为空。");
            }
            if (password.Length < MinimumLength)
            {
                throw new VaultException(fieldName + "至少需要 " + MinimumLength + " 个字符。");
            }
            if (!HasLetter(password) || !HasDigit(password))
            {
                throw new VaultException(fieldName + "必须同时包含字母和数字。");
            }
            if (IsCommonOrSequential(password))
            {
                throw new VaultException(fieldName + "不能使用连续字符、重复模式或常见简单密码。");
            }
        }

        public static bool IsValid(string password)
        {
            try
            {
                Validate(password, "密码");
                return true;
            }
            catch (VaultException)
            {
                return false;
            }
        }

        public static bool AreOptionsCompliant(PasswordOptions options)
        {
            if (options == null)
            {
                return false;
            }
            bool hasLetter = options.UseUppercase || options.UseLowercase;
            return hasLetter && options.UseDigits;
        }

        private static bool HasLetter(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetter(value[i])) return true;
            }
            return false;
        }

        private static bool HasDigit(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsDigit(value[i])) return true;
            }
            return false;
        }

        private static bool IsCommonOrSequential(string password)
        {
            string lower = password.ToLowerInvariant();
            for (int i = 0; i < CommonPasswords.Length; i++)
            {
                if (string.Equals(lower, CommonPasswords[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            if (HasRepeatedRun(lower, 4))
            {
                return true;
            }
            if (HasSequentialRun(lower, 3))
            {
                return true;
            }
            if (HasRepeatingPattern(lower))
            {
                return true;
            }
            return false;
        }

        private static bool HasRepeatedRun(string value, int minimumRun)
        {
            if (value.Length < minimumRun) return false;
            int run = 1;
            for (int i = 1; i < value.Length; i++)
            {
                if (value[i] == value[i - 1])
                {
                    run++;
                    if (run >= minimumRun) return true;
                }
                else
                {
                    run = 1;
                }
            }
            return false;
        }

        private static bool HasSequentialRun(string value, int minimumRun)
        {
            if (value.Length < minimumRun) return false;
            int run = 1;
            int direction = 0;
            for (int i = 1; i < value.Length; i++)
            {
                char previous = value[i - 1];
                char current = value[i];
                if (!char.IsLetterOrDigit(previous) || !char.IsLetterOrDigit(current))
                {
                    run = 1;
                    direction = 0;
                    continue;
                }

                int diff = current - previous;
                if (diff == 1 || diff == -1)
                {
                    if (direction == 0 || direction == diff)
                    {
                        direction = diff;
                        run++;
                    }
                    else
                    {
                        direction = diff;
                        run = 2;
                    }
                    if (run >= minimumRun) return true;
                }
                else
                {
                    run = 1;
                    direction = 0;
                }
            }
            return false;
        }

        private static bool HasRepeatingPattern(string value)
        {
            for (int block = 1; block <= value.Length / 2; block++)
            {
                if (value.Length % block != 0) continue;
                bool same = true;
                for (int i = block; i < value.Length; i++)
                {
                    if (value[i] != value[i % block])
                    {
                        same = false;
                        break;
                    }
                }
                if (same) return true;
            }
            return false;
        }
    }
}