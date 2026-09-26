using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.Parser
{
    public class ParseMTProto
    {
        private static readonly char[] LineSeparators = { '\n', '\r' };

        public readonly ILogger<ParseMTProto> _logger;

        public ParseMTProto(ILogger<ParseMTProto> logger)
        {
            _logger = logger;
        }

        public List<ProxyData> ParseMethod(ReadOnlyMemory<byte> readOnlyMemory)
        {
            try
            { 
                var list = new List<ProxyData>();
                var text = System.Text.Encoding.UTF8.GetString(readOnlyMemory.Span);
                var lines = text.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);

                if (lines == null)
                    return new List<ProxyData>();

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    ReadOnlySpan<char> span = trimmed.AsSpan();

                    var startServer = span.IndexOf("server=", StringComparison.OrdinalIgnoreCase);
                    var startPort = span.IndexOf("port=", StringComparison.OrdinalIgnoreCase);
                    var startSecret = span.IndexOf("secret=", StringComparison.OrdinalIgnoreCase);

                    if (startServer == -1 || startSecret == -1 || startPort == -1)
                        continue;

                    startServer += "server=".Length;
                    startPort += "port=".Length;
                    startSecret += "secret=".Length;

                    var EndServer = span.IndexOf("&port", StringComparison.OrdinalIgnoreCase);
                    var EndPort = span.IndexOf("&secret", StringComparison.OrdinalIgnoreCase);

                    if (EndServer == -1 || EndPort == -1)
                        continue;

                    var server = span.Slice(startServer, EndServer - startServer);
                    var port = span.Slice(startPort, EndPort - startPort);
                    var secret = span.Slice(startSecret);

                    var data = new ProxyData
                    {
                        Type = ProxyType.MTProto,
                        Server = server.ToString(),
                        Port = port.ToString(),
                        Secret = secret.TrimEnd().ToString(),
                    };

                    list.Add(data);
                }
                return list;
            }
            catch(InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, _logger);
                return new List<ProxyData>();
            }
            catch(Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<ProxyData>();
            }
        }
    }
}
