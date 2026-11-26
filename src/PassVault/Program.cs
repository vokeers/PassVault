using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PassVault
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args != null && args.Length > 0 && string.Equals(args[0], "--selftest", StringComparison.OrdinalIgnoreCase))
            {
                string result = SelfTest.Run();
                string resultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "selftest-result.txt");
                File.WriteAllText(resultPath, result, new UTF8Encoding(false));
                Environment.ExitCode = result.IndexOf("PASS ALL", StringComparison.Ordinal) >= 0 ? 0 : 1;
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                MessageBox.Show("程序发生错误。\r\n\r\n" + e.Exception.Message, "PassVault", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            string vaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vault.pvault");
            string mutexName = "PassVault_" + HashText(vaultPath.ToLowerInvariant());
            bool createdNew;
            using (Mutex mutex = new Mutex(true, mutexName, out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("PassVault 已经在运行。", "PassVault", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                try
                {
                    Application.Run(new UnlockForm(new VaultStore(vaultPath)));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("无法启动 PassVault。\r\n\r\n" + ex.Message, "PassVault", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string HashText(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}