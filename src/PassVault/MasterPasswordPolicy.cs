using System;

namespace PassVault
{
    public static class MasterPasswordPolicy
    {
        public const int MinimumLength = PasswordPolicy.MinimumLength;
        public const int MaximumLength = 256;

        public static void Validate(string masterPassword)
        {
            PasswordPolicy.Validate(masterPassword, "主密码");
            if (masterPassword.Length > MaximumLength)
            {
                throw new VaultException("主密码过长。");
            }
        }
    }
}