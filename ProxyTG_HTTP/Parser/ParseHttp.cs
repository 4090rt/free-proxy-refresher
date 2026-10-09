using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;

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
            => ProxyLineParser.ParseIpPort(readOnlyMemory, ProxyType.Http, _logger);
    }
}
