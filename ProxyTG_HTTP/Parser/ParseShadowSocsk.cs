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

                List<ShadowSocksParse> shadowSocksParses = new List<ShadowSocksParse>();

                foreach (var line in list)
                {
                    var trimmed = line?.SS?.Trim();
                    ReadOnlySpan<char> span = trimmed.AsSpan();

                    int passstart = 0;

                    if (span.StartsWith("ss://", StringComparison.OrdinalIgnoreCase))
                    {
                        passstart = 5;
                    }
                    else if (span.StartsWith("://", StringComparison.OrdinalIgnoreCase))
                    {
                        passstart = 3;
                    }
                    else
                    {
                        passstart = 0;
                    }

                    int passend = span.IndexOf("@");

                    if (passend < 0)
                        continue;

                    int hostostart = passend + 1;
                    int hosttoendindex = span.Slice(hostostart).IndexOf(":");
                    int hosttoend = hostostart + hosttoendindex;

                    if (hosttoend < 0)
                        continue;

                    int portostart = hosttoend + 1;
                    int porttonedindex = span.Slice(portostart).IndexOf("#");
                    int porttoned = porttonedindex < 0 ? span.Length : portostart + porttonedindex;

                    if (porttoned < 0)
                        continue;

                    var password = span.Slice(passstart, passend - passstart);
                    var host = span.Slice(hostostart, hosttoend - hostostart);
                    var port = span.Slice(portostart, porttoned - portostart);

                    try
                    {
                        var passfrombase64 = Convert.FromBase64String(password.ToString());
                        ReadOnlySpan<char> chars = Encoding.UTF8.GetString(passfrombase64);

                        int colon = chars.IndexOf(":");

                        if (colon < 0)
                            continue;

                        var codding = chars.Slice(0, colon);
                        var pass = chars.Slice(colon+1);

                        var data = new ShadowSocksParse
                        {
                            MethodServer = codding.ToString(),
                            ServerHost = host.ToString(),
                            ServerPort = port.ToString(),
                            ServerPassword = pass.ToString()
                        };

                        shadowSocksParses.Add(data);

                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }

                return shadowSocksParses.AsReadOnly();
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
