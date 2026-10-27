using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.Parser
{
    public class ParseShadowSocsk
    {
        private readonly ILogger<ParseShadowSocsk> _logger; 
        private static readonly char[] LineSeparators = { '\n', '\r' };

        public ParseShadowSocsk(ILogger<ParseShadowSocsk> logger)
        {
            _logger = logger;
        }
        public async Task<IReadOnlyList<ShadowSocksParse>> ParsedMetod(IReadOnlyList<ShadowSocksParseSS> list, CancellationToken cancellation)
        {
            try
            {
                if (list.Count == 0)
                    return new List<ShadowSocksParse>().AsReadOnly();

                foreach (var line in list)
                {
                    var trimmed = line?.SS?.Trim();
                    ReadOnlySpan<char> span = trimmed.AsSpan();

                }
            }
            catch (OperationCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, _logger);
                return new List<ShadowSocksParse>().AsReadOnly();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<ShadowSocksParse>().AsReadOnly();
            }
        }
    }
}
