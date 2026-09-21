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
    public class ParseHttp
    {
        private readonly ILogger<ParseHttp> _logger;
        public ParseHttp(ILogger<ParseHttp> logger)
        {
            _logger = logger;
        }

        public List<ProxyData> ParseMethod(ReadOnlyMemory<byte> readOnlyMemory)
        {
            try
            { 
                List<ProxyData> list = new List<ProxyData>();
                var text = System.Text.Encoding.UTF8.GetString(readOnlyMemory.Span);
                var lines = text.Split('\n', '\r', StringSplitOptions.RemoveEmptyEntries);

                if (lines == null)
                    return new List<ProxyData>();

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    ReadOnlySpan<char> span = trimmed.AsSpan();

                    var separator = span.IndexOf(':');

                    if (separator == -1)
                        continue;

                    var Ip = span.Slice(0, separator);
                    var Port = span.Slice(separator + 1);

                    if (Ip.IsEmpty || Port.IsEmpty) continue;

                    var data = new ProxyData
                    {
                        Type = ProxyType.Http,
                        Server = Ip.ToString(),
                        Port = Port.ToString(),
                    };
                    list.Add(data);
                }
                return list;
            }
            catch (InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, _logger);
                return new List<ProxyData>();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<ProxyData>();
            }
        }
    }
}
