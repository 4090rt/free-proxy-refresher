using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;

namespace ProxyTG_HTTP.Parser
{
    public class ParseSocks5
    {
        private readonly ILogger<ParseSocks5> _logger;

        public ParseSocks5(ILogger<ParseSocks5> logger)
        {
            _logger = logger;
        }

        public List<ProxyData> ParseMethod(ReadOnlyMemory<byte> readOnlyMemory)
            => ProxyLineParser.ParseIpPort(readOnlyMemory, ProxyType.Socks5, _logger);
    }
}
