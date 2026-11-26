using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PassVault
{
    internal static class SelfTest
    {
        private const string MasterPassword = "Correct-Horse-Battery-Staple-2026";

        public static string Run()
        {
            StringBuilder log = new StringBuilder();
            string tempDir = Path.Combine(Path.GetTempPath(), "PassVaultSelfTest-" + Guid.NewGuid().ToString("N"));
            VaultSession session = null;
            try
            {
                Directory.CreateDirectory(tempDir);
                string vaultPath = Path.Combine(tempDir, "vault.pvault");
                VaultStore store = new VaultStore(vaultPath);

                session = store.Create(MasterPassword);
                Check(File.Exists(vaultPath), "创建密码库文件");
                byte[] initialBytes = File.ReadAllBytes(vaultPath);
                Check(initialBytes.Length > 64, "加密文件非空");
                Check(!Encoding.UTF8.GetString(initialBytes).Contains("example.com"), "文件不包含明文网站");
                log.AppendLine("PASS 创建加密密码库");
                byte[] saltA = CryptoService.CreateSalt();
                byte[] saltB = CryptoService.CreateSalt();
                Check(saltA.Length == 32, "盐长度 32 字节");
                Check(!BytesEqual(saltA, saltB), "随机盐不相同");
                log.AppendLine("PASS 随机盐生成");
                string saltVault1 = Path.Combine(tempDir, "salt1.pvault");
                string saltVault2 = Path.Combine(tempDir, "salt2.pvault");
                VaultStore saltStore1 = new VaultStore(saltVault1);
                VaultStore saltStore2 = new VaultStore(saltVault2);
                VaultSession saltSession1 = saltStore1.Create(MasterPassword);
                saltSession1.Dispose();
                VaultSession saltSession2 = saltStore2.Create(MasterPassword);
                saltSession2.Dispose();
                byte[] saltVaultBytes1 = File.ReadAllBytes(saltVault1);
                byte[] saltVaultBytes2 = File.ReadAllBytes(saltVault2);
                Check(BitConverter.ToInt32(saltVaultBytes1, 12) == 300000, "密码库迭代次数写入文件头");
                byte[] fileSalt1 = new byte[32];
                byte[] fileSalt2 = new byte[32];
                Buffer.BlockCopy(saltVaultBytes1, 28, fileSalt1, 0, 32);
                Buffer.BlockCopy(saltVaultBytes2, 28, fileSalt2, 0, 32);
                Check(!BytesEqual(fileSalt1, fileSalt2), "不同密码库使用不同随机盐");
                log.AppendLine("PASS 密码库随机盐写入文件头");
                bool shortMasterBlocked = false;
                try { MasterPasswordPolicy.Validate("Aa1"); } catch (VaultException) { shortMasterBlocked = true; }
                Check(shortMasterBlocked, "主密码少于 6 位被拒绝");

                bool noDigitMasterBlocked = false;
                try { MasterPasswordPolicy.Validate("abcdef"); } catch (VaultException) { noDigitMasterBlocked = true; }
                Check(noDigitMasterBlocked, "主密码缺少数字被拒绝");

                bool noLetterMasterBlocked = false;
                try { MasterPasswordPolicy.Validate("123456"); } catch (VaultException) { noLetterMasterBlocked = true; }
                Check(noLetterMasterBlocked, "主密码缺少字母被拒绝");

                bool simpleMasterBlocked = false;
                try { MasterPasswordPolicy.Validate("abc123"); } catch (VaultException) { simpleMasterBlocked = true; }
                Check(simpleMasterBlocked, "主密码连续/常见密码被拒绝");

                MasterPasswordPolicy.Validate("Zx9!kL");
                log.AppendLine("PASS 主密码规则");

                PasswordOptions defaultOptions = PasswordOptions.CreateDefault();
                string firstPassword = session.GenerateUniquePassword(defaultOptions, null);
                string secondPassword = session.GenerateUniquePassword(PasswordOptions.CreateAll(20), null);
                Check(firstPassword.Length == 10, "默认密码长度 10");
                Check(HasUpper(firstPassword) && HasLower(firstPassword) && HasDigit(firstPassword), "默认密码包含大小写和数字");
                Check(!HasSymbol(firstPassword), "默认密码不包含符号");
                Check(secondPassword.Length == 20, "自定义长度 20");
                Check(firstPassword != secondPassword, "随机密码不相同");
                Check(HasUpper(secondPassword) && HasLower(secondPassword) && HasDigit(secondPassword) && HasSymbol(secondPassword), "全字符类型生成");

                string digitsOnly = PasswordGenerator.Generate(new PasswordOptions(false, false, true, false, 12));
                Check(digitsOnly.Length == 12 && AllDigits(digitsOnly), "仅数字选项");

                bool noOptionsBlocked = false;
                try
                {
                    PasswordGenerator.Generate(new PasswordOptions(false, false, false, false, 10));
                }
                catch (VaultException)
                {
                    noOptionsBlocked = true;
                }
                Check(noOptionsBlocked, "未勾选字符类型被拒绝");
                log.AppendLine("PASS 密码生成选项");

                PasswordEntry first = session.AddEntry(
                    "示例网站",
                    "example.com",
                    "alice@example.com",
                    firstPassword,
                    "测试备注");
                PasswordEntry second = session.AddEntry(
                    "GitHub",
                    "https://github.com/login",
                    "alice",
                    secondPassword,
                    "工作账号");
                Check(session.Entries.Count == 2, "新增两条记录");
                Check(first.SiteName == "示例网站" && first.Note == "测试备注", "网站名称和备注已保存");
                Check(File.Exists(vaultPath + ".bak"), "保存时生成 .bak 备份");
                log.AppendLine("PASS 新增记录、网站名称和备注");

                bool duplicateBlocked = false;
                try
                {
                    session.AddEntry("重复站点", "example.org", "other", firstPassword, "重复备注");
                }
                catch (DuplicatePasswordException)
                {
                    duplicateBlocked = true;
                }
                Check(duplicateBlocked, "重复密码被阻止");
                log.AppendLine("PASS 重复密码阻止");

                string normalized = UrlHelper.NormalizeWebsite("example.com/path");
                Check(normalized.StartsWith("https://example.com/", StringComparison.Ordinal), "网站自动补全 https");
                bool fileUrlBlocked = false;
                try
                {
                    UrlHelper.NormalizeWebsite("file:///C:/Windows/win.ini");
                }
                catch (VaultException)
                {
                    fileUrlBlocked = true;
                }
                Check(fileUrlBlocked, "拒绝非 http/https 链接");
                log.AppendLine("PASS 网站地址校验");

                string resetPassword = session.GenerateUniquePassword(PasswordOptions.CreateDefault(), first.Id);
                session.ResetPassword(first.Id, resetPassword);
                Check(first.Password == resetPassword, "重置随机密码");
                Check(resetPassword != firstPassword, "重置后密码改变");
                Check(resetPassword.Length == 10, "重置后密码长度 10");
                log.AppendLine("PASS 重置随机密码");

                session.Dispose();
                session = null;

                VaultSession unlocked = store.Unlock(MasterPassword);
                try
                {
                    Check(unlocked.Entries.Count == 2, "重新解锁记录数量");
                    PasswordEntry restored = unlocked.FindEntry(first.Id);
                    Check(restored != null && restored.Password == resetPassword, "重新解锁后密码正确");
                    Check(restored.SiteName == "示例网站", "重新解锁后网站名称正确");
                    Check(restored.Note == "测试备注", "重新解锁后备注正确");
                }
                finally
                {
                    unlocked.Dispose();
                }
                log.AppendLine("PASS 正确主密码解锁");

                bool wrongPasswordBlocked = false;
                try
                {
                    store.Unlock("wrong-master-password-12345");
                }
                catch (VaultException)
                {
                    wrongPasswordBlocked = true;
                }
                Check(wrongPasswordBlocked, "错误主密码被拒绝");
                log.AppendLine("PASS 错误主密码拒绝");

                byte[] original = File.ReadAllBytes(vaultPath);
                byte[] tampered = (byte[])original.Clone();
                tampered[tampered.Length / 2] ^= 0x5A;
                File.WriteAllBytes(vaultPath, tampered);
                bool tamperBlocked = false;
                try
                {
                    store.Unlock(MasterPassword);
                }
                catch (VaultException)
                {
                    tamperBlocked = true;
                }
                Check(tamperBlocked, "篡改文件被拒绝");
                File.WriteAllBytes(vaultPath, original);
                log.AppendLine("PASS 篡改检测");

                Check(File.Exists(vaultPath + ".bak"), "备份文件仍然存在");
                log.AppendLine("PASS 备份文件存在");
                log.AppendLine("PASS ALL");
                return log.ToString();
            }
            catch (Exception ex)
            {
                log.AppendLine("FAIL " + ex.GetType().Name + ": " + ex.Message);
                return log.ToString();
            }
            finally
            {
                if (session != null)
                {
                    session.Dispose();
                }
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch { }
            }
        }

        private static void Check(bool condition, string name)
        {
            if (!condition)
            {
                throw new Exception("检查失败：" + name);
            }
        }

        private static bool HasUpper(string value)
        {
            for (int i = 0; i < value.Length; i++) if (char.IsUpper(value[i])) return true;
            return false;
        }

        private static bool HasLower(string value)
        {
            for (int i = 0; i < value.Length; i++) if (char.IsLower(value[i])) return true;
            return false;
        }

        private static bool HasDigit(string value)
        {
            for (int i = 0; i < value.Length; i++) if (char.IsDigit(value[i])) return true;
            return false;
        }

        private static bool HasSymbol(string value)
        {
            for (int i = 0; i < value.Length; i++) if (!char.IsLetterOrDigit(value[i])) return true;
            return false;
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }
            return true;
        }
        private static bool AllDigits(string value)
        {
            for (int i = 0; i < value.Length; i++) if (!char.IsDigit(value[i])) return false;
            return true;
        }
    }
}