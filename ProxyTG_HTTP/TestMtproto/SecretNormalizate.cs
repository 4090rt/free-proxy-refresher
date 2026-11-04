using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestMtproto
{
    public static class SecretNormalizate
    {
        public static (byte[] key, string? domain) Normalizate(string secretkey)
        {
            var lowered = secretkey.Trim().ToLowerInvariant();

            if (lowered.StartsWith("0x"))
                lowered = lowered[2..];

            if (lowered.StartsWith("ee") && lowered.Length == 34)
                return (Convert.FromHexString(lowered.Substring(2, 32)), null);

            if (lowered.StartsWith("dd") && lowered.Length == 66)
            {
                byte[] key = Convert.FromHexString(lowered.Substring(34, 32));
                string domain = Encoding.ASCII
                    .GetString(Convert.FromHexString(lowered.Substring(2, 32)))
                    .TrimEnd('\0');
                return (key, domain);
            }

            if (lowered.Length == 32)
                return (Convert.FromHexString(lowered), null);

            throw new FormatException($"Неизвестный формат secret: {lowered}");
        }
    }
}
