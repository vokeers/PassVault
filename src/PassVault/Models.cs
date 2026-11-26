using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PassVault
{
    [DataContract]
    public sealed class PasswordEntry
    {
        [DataMember(Name = "id", Order = 1)]
        public string Id { get; set; }

        [DataMember(Name = "siteName", Order = 2)]
        public string SiteName { get; set; }

        [DataMember(Name = "website", Order = 3)]
        public string Website { get; set; }

        [DataMember(Name = "account", Order = 4)]
        public string Account { get; set; }

        [DataMember(Name = "password", Order = 5)]
        public string Password { get; set; }

        [DataMember(Name = "note", Order = 6)]
        public string Note { get; set; }

        [DataMember(Name = "createdAt", Order = 7)]
        public string CreatedAt { get; set; }

        [DataMember(Name = "updatedAt", Order = 8)]
        public string UpdatedAt { get; set; }
    }

    [DataContract]
    public sealed class VaultData
    {
        [DataMember(Name = "formatVersion", Order = 1)]
        public int FormatVersion { get; set; }

        [DataMember(Name = "createdAt", Order = 2)]
        public string CreatedAt { get; set; }

        [DataMember(Name = "entries", Order = 3)]
        public List<PasswordEntry> Entries { get; set; }

        public VaultData()
        {
            FormatVersion = 1;
            CreatedAt = DateTime.UtcNow.ToString("o");
            Entries = new List<PasswordEntry>();
        }
    }

    public class VaultException : Exception
    {
        public VaultException(string message) : base(message) { }
        public VaultException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class DuplicatePasswordException : VaultException
    {
        public DuplicatePasswordException(string message) : base(message) { }
    }
}