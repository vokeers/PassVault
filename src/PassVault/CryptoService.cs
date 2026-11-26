using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PassVault
{
    internal sealed class DerivedKeys : IDisposable
    {
        public byte[] EncryptionKey { get; private set; }
        public byte[] MacKey { get; private set; }
        public byte[] Salt { get; private set; }
        public int Iterations { get; private set; }

        public DerivedKeys(byte[] encryptionKey, byte[] macKey, byte[] salt, int iterations)
        {
            EncryptionKey = encryptionKey;
            MacKey = macKey;
            Salt = salt;
            Iterations = iterations;
        }

        public void Dispose()
        {
            Zero(EncryptionKey);
            Zero(MacKey);
            EncryptionKey = null;
            MacKey = null;
        }

        internal static void Zero(byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
            {
                Array.Clear(bytes, 0, bytes.Length);
            }
        }
    }

    internal static class CryptoService
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("PVLT0001");
        private const int Version = 1;
        private const int SaltLength = 32;
        private const int IvLength = 16;
        private const int MacLength = 32;
        private const int DefaultIterations = 300000;
        private const int MaxCipherLength = 100 * 1024 * 1024;

        public static byte[] CreateSalt()
        {
            byte[] salt = new byte[SaltLength];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        public static DerivedKeys DeriveKeys(string masterPassword, byte[] salt, int iterations)
        {
            if (masterPassword == null)
            {
                throw new ArgumentNullException("masterPassword");
            }
            if (salt == null || salt.Length != SaltLength)
            {
                throw new ArgumentException("Salt 长度不正确。", "salt");
            }
            if (iterations < 100000 || iterations > 5000000)
            {
                throw new VaultException("密码库的密钥派生参数无效。");
            }

            byte[] material = null;
            try
            {
                using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(
                    masterPassword, salt, iterations, HashAlgorithmName.SHA256))
                {
                    material = kdf.GetBytes(64);
                }

                byte[] encryptionKey = new byte[32];
                byte[] macKey = new byte[32];
                Buffer.BlockCopy(material, 0, encryptionKey, 0, 32);
                Buffer.BlockCopy(material, 32, macKey, 0, 32);

                byte[] saltCopy = new byte[salt.Length];
                Buffer.BlockCopy(salt, 0, saltCopy, 0, salt.Length);
                return new DerivedKeys(encryptionKey, macKey, saltCopy, iterations);
            }
            finally
            {
                DerivedKeys.Zero(material);
            }
        }

        public static DerivedKeys DeriveNewKeys(string masterPassword)
        {
            byte[] salt = CreateSalt();
            return DeriveKeys(masterPassword, salt, DefaultIterations);
        }

        public static byte[] Encrypt(byte[] plaintext, DerivedKeys keys)
        {
            if (plaintext == null)
            {
                throw new ArgumentNullException("plaintext");
            }
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }

            byte[] iv = new byte[IvLength];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            byte[] ciphertext = null;
            try
            {
                using (AesManaged aes = new AesManaged())
                {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = keys.EncryptionKey;
                    aes.IV = iv;

                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
                    }
                }

                byte[] authenticated;
                using (MemoryStream headerStream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(headerStream, Encoding.UTF8, true))
                    {
                        writer.Write(Magic);
                        writer.Write(Version);
                        writer.Write(keys.Iterations);
                        writer.Write(keys.Salt.Length);
                        writer.Write(iv.Length);
                        writer.Write(ciphertext.Length);
                        writer.Write(keys.Salt);
                        writer.Write(iv);
                        writer.Write(ciphertext);
                    }
                    authenticated = headerStream.ToArray();
                }

                byte[] mac;
                using (HMACSHA256 hmac = new HMACSHA256(keys.MacKey))
                {
                    mac = hmac.ComputeHash(authenticated);
                }

                using (MemoryStream result = new MemoryStream(authenticated.Length + mac.Length))
                {
                    result.Write(authenticated, 0, authenticated.Length);
                    result.Write(mac, 0, mac.Length);
                    return result.ToArray();
                }
            }
            finally
            {
                DerivedKeys.Zero(ciphertext);
            }
        }

        public static byte[] Decrypt(byte[] blob, string masterPassword, out DerivedKeys keys)
        {
            keys = null;
            if (blob == null || blob.Length < Magic.Length + 4 + 4 + 4 + 4 + 4 + SaltLength + IvLength + MacLength)
            {
                throw new VaultException("密码库文件已损坏。");
            }

            byte[] salt = null;
            byte[] iv = null;
            byte[] ciphertext = null;
            byte[] mac = null;
            int iterations;
            byte[] authenticated = null;
            byte[] plaintext = null;

            try
            {
                using (MemoryStream stream = new MemoryStream(blob, false))
                using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    byte[] magic = reader.ReadBytes(Magic.Length);
                    if (!FixedTimeEquals(magic, Magic))
                    {
                        throw new VaultException("不是有效的 PassVault 密码库文件。");
                    }

                    int version = reader.ReadInt32();
                    if (version != Version)
                    {
                        throw new VaultException("密码库版本不受支持。");
                    }

                    iterations = reader.ReadInt32();
                    int saltLength = reader.ReadInt32();
                    int ivLength = reader.ReadInt32();
                    int cipherLength = reader.ReadInt32();

                    if (saltLength != SaltLength || ivLength != IvLength ||
                        cipherLength < 1 || cipherLength > MaxCipherLength)
                    {
                        throw new VaultException("密码库文件头无效。");
                    }

                    long expected = Magic.Length + 4 + 4 + 4 + 4 + 4 +
                                    saltLength + ivLength + cipherLength + MacLength;
                    if (blob.LongLength != expected)
                    {
                        throw new VaultException("密码库文件长度不正确。");
                    }

                    salt = reader.ReadBytes(saltLength);
                    iv = reader.ReadBytes(ivLength);
                    ciphertext = reader.ReadBytes(cipherLength);
                    mac = reader.ReadBytes(MacLength);
                }

                int authenticatedLength = blob.Length - MacLength;
                authenticated = new byte[authenticatedLength];
                Buffer.BlockCopy(blob, 0, authenticated, 0, authenticatedLength);
                byte[] expectedMac;
                keys = DeriveKeys(masterPassword, salt, iterations);
                using (HMACSHA256 hmac = new HMACSHA256(keys.MacKey))
                {
                    expectedMac = hmac.ComputeHash(authenticated);
                }

                if (!FixedTimeEquals(mac, expectedMac))
                {
                    throw new VaultException("主密码错误，或密码库文件已损坏。");
                }

                using (AesManaged aes = new AesManaged())
                {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = keys.EncryptionKey;
                    aes.IV = iv;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                    }
                }

                return plaintext;
            }
            catch (VaultException)
            {
                if (keys != null)
                {
                    keys.Dispose();
                    keys = null;
                }
                throw;
            }
            catch (CryptographicException ex)
            {
                if (keys != null)
                {
                    keys.Dispose();
                    keys = null;
                }
                throw new VaultException("主密码错误，或密码库文件已损坏。", ex);
            }
            finally
            {
                DerivedKeys.Zero(authenticated);
                DerivedKeys.Zero(ciphertext);
                if (plaintext != null && keys == null)
                {
                    DerivedKeys.Zero(plaintext);
                }
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }
            return diff == 0;
        }
    }
}