using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PassVault
{
    public sealed class VaultStore
    {
        public string VaultPath { get; private set; }
        public string BackupPath { get { return VaultPath + ".bak"; } }
        public bool Exists { get { return File.Exists(VaultPath); } }

        public VaultStore(string vaultPath)
        {
            if (string.IsNullOrWhiteSpace(vaultPath))
            {
                throw new ArgumentException("密码库路径不能为空。", "vaultPath");
            }
            VaultPath = Path.GetFullPath(vaultPath);
        }

        public VaultSession Create(string masterPassword)
        {
            MasterPasswordPolicy.Validate(masterPassword);
            if (Exists)
            {
                throw new VaultException("密码库已经存在。");
            }

            DerivedKeys keys = null;
            try
            {
                keys = CryptoService.DeriveNewKeys(masterPassword);
                VaultData data = new VaultData();
                VaultSession session = new VaultSession(this, data, keys);
                Save(data, keys);
                return session;
            }
            catch
            {
                if (keys != null)
                {
                    keys.Dispose();
                }
                throw;
            }
        }

        public VaultSession Unlock(string masterPassword)
        {
            if (string.IsNullOrEmpty(masterPassword))
            {
                throw new VaultException("请输入主密码。");
            }
            if (!Exists)
            {
                throw new VaultException("密码库文件不存在。");
            }

            byte[] blob = null;
            byte[] plaintext = null;
            DerivedKeys keys = null;
            try
            {
                blob = File.ReadAllBytes(VaultPath);
                plaintext = CryptoService.Decrypt(blob, masterPassword, out keys);

                VaultData data;
                using (MemoryStream stream = new MemoryStream(plaintext, false))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(VaultData));
                    data = serializer.ReadObject(stream) as VaultData;
                }

                if (data == null)
                {
                    throw new VaultException("密码库内容无效。");
                }
                if (data.Entries == null)
                {
                    data.Entries = new List<PasswordEntry>();
                }

                RepairMissingFields(data);
                return new VaultSession(this, data, keys);
            }
            catch (VaultException)
            {
                if (keys != null)
                {
                    keys.Dispose();
                }
                throw;
            }
            catch (Exception ex)
            {
                if (keys != null)
                {
                    keys.Dispose();
                }
                throw new VaultException("无法读取密码库。文件可能已损坏。", ex);
            }
            finally
            {
                if (plaintext != null)
                {
                    Array.Clear(plaintext, 0, plaintext.Length);
                }
                if (blob != null)
                {
                    Array.Clear(blob, 0, blob.Length);
                }
            }
        }

        internal void Save(VaultData data, DerivedKeys keys)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }

            byte[] json = null;
            byte[] blob = null;
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(VaultData));
                    serializer.WriteObject(stream, data);
                    json = stream.ToArray();
                }

                blob = CryptoService.Encrypt(json, keys);
                WriteAtomic(blob);
            }
            catch (VaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new VaultException("保存密码库失败。请确认程序目录可写，并检查磁盘空间。", ex);
            }
            finally
            {
                if (json != null)
                {
                    Array.Clear(json, 0, json.Length);
                }
                if (blob != null)
                {
                    Array.Clear(blob, 0, blob.Length);
                }
            }
        }

        private void WriteAtomic(byte[] blob)
        {
            string tempPath = VaultPath + ".tmp";
            try
            {
                using (FileStream stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(blob, 0, blob.Length);
                    stream.Flush(true);
                }

                if (File.Exists(VaultPath))
                {
                    try
                    {
                        File.Replace(tempPath, VaultPath, BackupPath, true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        ReplaceFallback(tempPath);
                    }
                    catch (IOException)
                    {
                        ReplaceFallback(tempPath);
                    }
                }
                else
                {
                    File.Move(tempPath, VaultPath);
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); }
                    catch { }
                }
            }
        }

        private void ReplaceFallback(string tempPath)
        {
            if (File.Exists(VaultPath))
            {
                File.Copy(VaultPath, BackupPath, true);
                File.Delete(VaultPath);
            }
            File.Move(tempPath, VaultPath);
        }

        private static void RepairMissingFields(VaultData data)
        {
            for (int i = 0; i < data.Entries.Count; i++)
            {
                PasswordEntry entry = data.Entries[i];
                if (entry == null)
                {
                    data.Entries.RemoveAt(i);
                    i--;
                    continue;
                }
                if (string.IsNullOrEmpty(entry.Id))
                {
                    entry.Id = Guid.NewGuid().ToString("N");
                }
                if (entry.SiteName == null) entry.SiteName = string.Empty;
                if (entry.Website == null) entry.Website = string.Empty;
                if (entry.Account == null) entry.Account = string.Empty;
                if (entry.Password == null) entry.Password = string.Empty;
                if (entry.Note == null) entry.Note = string.Empty;
                if (string.IsNullOrEmpty(entry.CreatedAt)) entry.CreatedAt = DateTime.UtcNow.ToString("o");
                if (string.IsNullOrEmpty(entry.UpdatedAt)) entry.UpdatedAt = entry.CreatedAt;
            }
        }
    }

    public sealed class VaultSession : IDisposable
    {
        private VaultStore store;
        private VaultData data;
        private DerivedKeys keys;
        private bool disposed;

        internal VaultSession(VaultStore store, VaultData data, DerivedKeys keys)
        {
            this.store = store;
            this.data = data;
            this.keys = keys;
        }

        public IReadOnlyList<PasswordEntry> Entries
        {
            get
            {
                EnsureNotDisposed();
                return data.Entries;
            }
        }

        public PasswordEntry FindEntry(string id)
        {
            EnsureNotDisposed();
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            for (int i = 0; i < data.Entries.Count; i++)
            {
                if (string.Equals(data.Entries[i].Id, id, StringComparison.Ordinal))
                {
                    return data.Entries[i];
                }
            }
            return null;
        }

        public string GenerateUniquePassword(PasswordOptions options, string excludeEntryId)
        {
            EnsureNotDisposed();
            for (int attempt = 0; attempt < 100; attempt++)
            {
                string candidate = PasswordGenerator.Generate(options);
                if (!IsPasswordUsed(candidate, excludeEntryId))
                {
                    return candidate;
                }
            }
            throw new VaultException("无法生成唯一密码，请重试。");
        }

        public string GenerateUniquePassword(int length, string excludeEntryId)
        {
            return GenerateUniquePassword(PasswordOptions.CreateAll(length), excludeEntryId);
        }

        public PasswordEntry AddEntry(string siteName, string website, string account, string password, string note)
        {
            EnsureNotDisposed();
            string cleanSiteName = CleanOptional(siteName, 120, "网站名称");
            string normalizedWebsite = UrlHelper.NormalizeWebsite(website);
            string cleanAccount = CleanOptional(account, 256, "账号");
            if (cleanAccount.Length == 0)
            {
                throw new VaultException("账号不能为空。");
            }
            string cleanNote = CleanOptional(note, 2000, "备注");
            ValidatePasswordValue(password);
            EnsureUniquePassword(password, null);

            PasswordEntry entry = new PasswordEntry();
            entry.Id = Guid.NewGuid().ToString("N");
            entry.SiteName = cleanSiteName;
            entry.Website = normalizedWebsite;
            entry.Account = cleanAccount;
            entry.Password = password;
            entry.Note = cleanNote;
            entry.CreatedAt = DateTime.UtcNow.ToString("o");
            entry.UpdatedAt = entry.CreatedAt;

            data.Entries.Add(entry);
            try
            {
                store.Save(data, keys);
                return entry;
            }
            catch
            {
                data.Entries.Remove(entry);
                throw;
            }
        }

        public void UpdateEntry(string id, string siteName, string website, string account, string password, string note)
        {
            EnsureNotDisposed();
            PasswordEntry entry = FindEntry(id);
            if (entry == null)
            {
                throw new VaultException("记录不存在。");
            }

            string cleanSiteName = CleanOptional(siteName, 120, "网站名称");
            string normalizedWebsite = UrlHelper.NormalizeWebsite(website);
            string cleanAccount = CleanOptional(account, 256, "账号");
            if (cleanAccount.Length == 0)
            {
                throw new VaultException("账号不能为空。");
            }
            string cleanNote = CleanOptional(note, 2000, "备注");
            ValidatePasswordValue(password);
            EnsureUniquePassword(password, id);

            string oldSiteName = entry.SiteName;
            string oldWebsite = entry.Website;
            string oldAccount = entry.Account;
            string oldPassword = entry.Password;
            string oldNote = entry.Note;
            string oldUpdatedAt = entry.UpdatedAt;

            entry.SiteName = cleanSiteName;
            entry.Website = normalizedWebsite;
            entry.Account = cleanAccount;
            entry.Password = password;
            entry.Note = cleanNote;
            entry.UpdatedAt = DateTime.UtcNow.ToString("o");

            try
            {
                store.Save(data, keys);
            }
            catch
            {
                entry.SiteName = oldSiteName;
                entry.Website = oldWebsite;
                entry.Account = oldAccount;
                entry.Password = oldPassword;
                entry.Note = oldNote;
                entry.UpdatedAt = oldUpdatedAt;
                throw;
            }
        }

        public void ResetPassword(string id, string newPassword)
        {
            EnsureNotDisposed();
            PasswordEntry entry = FindEntry(id);
            if (entry == null)
            {
                throw new VaultException("记录不存在。");
            }
            ValidatePasswordValue(newPassword);
            if (string.Equals(entry.Password, newPassword, StringComparison.Ordinal))
            {
                throw new VaultException("新密码不能与旧密码相同。");
            }
            EnsureUniquePassword(newPassword, id);

            string oldPassword = entry.Password;
            string oldUpdatedAt = entry.UpdatedAt;
            entry.Password = newPassword;
            entry.UpdatedAt = DateTime.UtcNow.ToString("o");
            try
            {
                store.Save(data, keys);
            }
            catch
            {
                entry.Password = oldPassword;
                entry.UpdatedAt = oldUpdatedAt;
                throw;
            }
        }

        public void DeleteEntry(string id)
        {
            EnsureNotDisposed();
            PasswordEntry entry = FindEntry(id);
            if (entry == null)
            {
                throw new VaultException("记录不存在。");
            }

            int index = data.Entries.IndexOf(entry);
            data.Entries.RemoveAt(index);
            try
            {
                store.Save(data, keys);
            }
            catch
            {
                data.Entries.Insert(index, entry);
                throw;
            }
        }

        private void EnsureUniquePassword(string password, string excludeEntryId)
        {
            for (int i = 0; i < data.Entries.Count; i++)
            {
                PasswordEntry entry = data.Entries[i];
                if (excludeEntryId != null &&
                    string.Equals(entry.Id, excludeEntryId, StringComparison.Ordinal))
                {
                    continue;
                }
                if (string.Equals(entry.Password, password, StringComparison.Ordinal))
                {
                    throw new DuplicatePasswordException(
                        "该密码已被“" + EntryDisplayName(entry) +
                        "”使用。每个账号必须使用不同密码，请重新生成或修改密码。");
                }
            }
        }

        private bool IsPasswordUsed(string password, string excludeEntryId)
        {
            for (int i = 0; i < data.Entries.Count; i++)
            {
                PasswordEntry entry = data.Entries[i];
                if (excludeEntryId != null &&
                    string.Equals(entry.Id, excludeEntryId, StringComparison.Ordinal))
                {
                    continue;
                }
                if (string.Equals(entry.Password, password, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static string EntryDisplayName(PasswordEntry entry)
        {
            string name = entry.SiteName;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = entry.Website;
            }
            return name + " / " + entry.Account;
        }

        private static string CleanOptional(string value, int maxLength, string fieldName)
        {
            string clean = (value ?? string.Empty).Trim();
            if (clean.Length > maxLength)
            {
                throw new VaultException(fieldName + "过长，最多 " + maxLength + " 个字符。");
            }
            return clean;
        }

        private static void ValidatePasswordValue(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new VaultException("密码不能为空。");
            }
            if (password.Length > 1024)
            {
                throw new VaultException("密码长度过长。");
            }
        }

        private void EnsureNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("VaultSession");
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            if (data != null && data.Entries != null)
            {
                for (int i = 0; i < data.Entries.Count; i++)
                {
                    if (data.Entries[i] != null)
                    {
                        data.Entries[i].Password = null;
                        data.Entries[i].Note = null;
                    }
                }
                data.Entries.Clear();
            }
            if (keys != null)
            {
                keys.Dispose();
                keys = null;
            }
            data = null;
            store = null;
        }
    }
}