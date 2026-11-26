using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PassVault
{
    public sealed class PasswordOptions
    {
        public bool UseUppercase { get; private set; }
        public bool UseLowercase { get; private set; }
        public bool UseDigits { get; private set; }
        public bool UseSymbols { get; private set; }
        public int Length { get; private set; }

        public PasswordOptions(bool useUppercase, bool useLowercase, bool useDigits, bool useSymbols, int length)
        {
            UseUppercase = useUppercase;
            UseLowercase = useLowercase;
            UseDigits = useDigits;
            UseSymbols = useSymbols;
            Length = length;
        }

        public static PasswordOptions CreateDefault()
        {
            return new PasswordOptions(true, true, true, false, 10);
        }

        public static PasswordOptions CreateAll(int length)
        {
            return new PasswordOptions(true, true, true, true, length);
        }

        public int SelectedTypeCount
        {
            get
            {
                int count = 0;
                if (UseUppercase) count++;
                if (UseLowercase) count++;
                if (UseDigits) count++;
                if (UseSymbols) count++;
                return count;
            }
        }
    }

    public static class PasswordGenerator
    {
        private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
        private const string Digits = "23456789";
        private const string Symbols = "!@#$%^&*()-_=+[]{}:,.?";

        public static string Generate(PasswordOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            if (options.Length < 6 || options.Length > 128)
            {
                throw new VaultException("密码长度必须在 6 到 128 之间。");
            }

            List<string> selectedSets = new List<string>();
            if (options.UseUppercase) selectedSets.Add(Uppercase);
            if (options.UseLowercase) selectedSets.Add(Lowercase);
            if (options.UseDigits) selectedSets.Add(Digits);
            if (options.UseSymbols) selectedSets.Add(Symbols);

            if (selectedSets.Count == 0)
            {
                throw new VaultException("至少需要勾选一种字符类型。");
            }
            if (options.Length < selectedSets.Count)
            {
                throw new VaultException("密码长度不能小于已勾选的字符类型数量。");
            }

            string all = string.Empty;
            for (int i = 0; i < selectedSets.Count; i++)
            {
                all += selectedSets[i];
            }

            List<char> chars = new List<char>(options.Length);
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                for (int i = 0; i < selectedSets.Count; i++)
                {
                    chars.Add(selectedSets[i][NextIndex(rng, selectedSets[i].Length)]);
                }
                while (chars.Count < options.Length)
                {
                    chars.Add(all[NextIndex(rng, all.Length)]);
                }

                for (int i = chars.Count - 1; i > 0; i--)
                {
                    int j = NextIndex(rng, i + 1);
                    char tmp = chars[i];
                    chars[i] = chars[j];
                    chars[j] = tmp;
                }
            }

            return new string(chars.ToArray());
        }

        public static string Generate(int length)
        {
            return Generate(PasswordOptions.CreateAll(length));
        }

        private static int NextIndex(RandomNumberGenerator rng, int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException("exclusiveMax");
            }

            byte[] bytes = new byte[4];
            uint limit = uint.MaxValue - (uint.MaxValue % (uint)exclusiveMax);
            while (true)
            {
                rng.GetBytes(bytes);
                uint value = BitConverter.ToUInt32(bytes, 0);
                if (value < limit)
                {
                    return (int)(value % (uint)exclusiveMax);
                }
            }
        }
    }
}