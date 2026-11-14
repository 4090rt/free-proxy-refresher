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
    public interface ProxyRecord
    { 
        string Host {  get; set; }
        string Port { get; set; }
        string Date { get; set; }
    }

    public class ParseHttpandHttps
    {
        private readonly ILogger<ParseHttpandHttps> _logger;
        private static readonly char[] LineSeparators = { '\n', '\r' };

        public ParseHttpandHttps(ILogger<ParseHttpandHttps> logger)
        {
            _logger = logger;
        }

        public async Task<IReadOnlyList<T>> ParseMthod<T>(ReadOnlyMemory<byte> bytes,
            CancellationToken cancellationToken) where T: ProxyRecord, new()
        {   
            try
            {
                List<T> values = new List<T>();

                var lines = Encoding.UTF8.GetString(bytes.Span);
                var trimmedLine = lines.Trim();
                var lienssep = trimmedLine.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);

                if (lienssep == null)
                    return new List<T>().AsReadOnly();

                foreach (var line in lienssep)
                {
                    int hostend = line.LastIndexOf(':');

                    if (hostend <= 0) 
                        continue;

                    int portstart = hostend + 1;

                    ReadOnlySpan<char> span = line.AsSpan();

                    var SliecedHost = span.Slice(0, hostend);
                    var SliecedPort = span.Slice(portstart);
                    if (!int.TryParse(SliecedPort.ToString(), out int port) || port < 1 || port > 65535)
                        continue;

                    values.Add(new T
                    {
                        Host = SliecedHost.ToString(),
                        Port = SliecedPort.ToString(),
                        Date = DateTime.UtcNow.ToString()
                    });
                }
                if (values.Count != 0)
                    return values.AsReadOnly();
                else
                    return new List<T>().AsReadOnly();
            }
            catch (OperationCanceledException ex)
            {
                throw; 
            }
            catch (InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, _logger);
                return new List<T>().AsReadOnly();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<T>().AsReadOnly();
            }
        }
    }
}
