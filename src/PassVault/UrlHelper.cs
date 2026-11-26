using System;

namespace PassVault
{
    public static class UrlHelper
    {
        public static string NormalizeWebsite(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new VaultException("网站不能为空。");
            }

            string value = input.Trim();
            if (value.IndexOf("://", StringComparison.Ordinal) < 0)
            {
                value = "https://" + value;
            }

            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri))
            {
                throw new VaultException("网站地址格式不正确。");
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new VaultException("只允许打开 http:// 或 https:// 网站。");
            }

            if (string.IsNullOrWhiteSpace(uri.Host))
            {
                throw new VaultException("网站地址缺少域名。");
            }

            return uri.AbsoluteUri;
        }
    }
}