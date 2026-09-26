using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;

namespace ProxyTG_HTTP.Parser
{
    public static class ProxyLineParser
    {
        private static readonly char[] LineSeparators = { '\n', '\r' };

        public static List<ProxyData> ParseIpPort(ReadOnlyMemory<byte> readOnlyMemory, ProxyType proxyType, ILogger logger)
        {
            try
            {
                List<ProxyData> list = new List<ProxyData>();
                var text = System.Text.Encoding.UTF8.GetString(readOnlyMemory.Span);
                var lines = text.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);

                if (lines == null)
                    return new List<ProxyData>();

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0)
                        continue;

                    ReadOnlySpan<char> span = trimmed.AsSpan();

                    var separator = span.LastIndexOf(':');

                    if (separator <= 0 || separator == span.Length - 1)
                        continue;

                    var ip = span.Slice(0, separator);
                    var port = span.Slice(separator + 1);

                    if (ip.IsEmpty || port.IsEmpty || port.Contains(' '))
                        continue;

                    list.Add(new ProxyData
                    {
                        Type = proxyType,
                        Server = ip.ToString(),
                        Port = port.ToString(),
                    });
                }
                return list;
            }
            catch (InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, logger);
                return new List<ProxyData>();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, logger);
                return new List<ProxyData>();
            }
        }
    }
}
