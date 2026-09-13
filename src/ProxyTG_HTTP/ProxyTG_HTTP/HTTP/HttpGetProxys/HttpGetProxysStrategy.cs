using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.HTTP.HttpGet;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HttpGetProxys
{
    public interface HttpGetProxysStrategy
    {
        public Task<List<ProxyData>> RequestInProxys(string client);
    }

    public class RequestMTProto: HttpGetProxysStrategy
    {
        public readonly GetProxys _getProxys;
        public readonly ParseMTProto _parseMTProto;
        public readonly ILogger<RequestMTProto> _logger;
        public RequestMTProto(GetProxys getProxys, ParseMTProto parseMTProto, ILogger<RequestMTProto> logger) { _getProxys = getProxys; 
            _parseMTProto = parseMTProto; _logger = logger;}
        public async Task<List<ProxyData>> RequestInProxys(string client)
        {
            try
            {
                ReadOnlyMemory<byte> readOnlyMemory = await _getProxys.GetMethod(client).ConfigureAwait(false);

                if (readOnlyMemory.Length == 0)
                    return new List<ProxyData>();

                List<ProxyData> mtProtoParses = await _parseMTProto.ParseMethod(readOnlyMemory).ConfigureAwait(false); 
            
                if (mtProtoParses.Count == 0)
                    return new List<ProxyData>();

                return mtProtoParses;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<ProxyData>();
            }
        }
    }

    public class RequestHttp : HttpGetProxysStrategy
    {
        public readonly GetProxys _getProxys;
        public readonly ParseHttp _parseHttp;
        public readonly ILogger<RequestHttp> _logger;
        public RequestHttp(GetProxys getProxys, ParseHttp parseHttp, ILogger<RequestHttp> logger) { _getProxys = getProxys;
            _parseHttp = parseHttp; _logger = logger;}
        public async Task<List<ProxyData>> RequestInProxys(string client)
        {
            try
            {
                ReadOnlyMemory<byte> readOnlyMemory = await _getProxys.GetMethod(client).ConfigureAwait(false);

                if (readOnlyMemory.Length == 0)
                    return new List<ProxyData>();

                List<ProxyData> parseHttps = await _parseHttp.ParseMethod(readOnlyMemory).ConfigureAwait(false);

                if (parseHttps.Count == 0)
                    return new List<ProxyData>();

                return parseHttps;

            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex,_logger);
                return new List<ProxyData>();
            }
        }
    }

    public class StrategyClass
    {
        public HttpGetProxysStrategy _strategy;

        public StrategyClass(HttpGetProxysStrategy strategy) => _strategy = strategy;

        public void SetStrategy(HttpGetProxysStrategy httpGetProxysStrategy) => _strategy = httpGetProxysStrategy;

        public async Task<List<ProxyData>> MainMethod(string client) => await _strategy.RequestInProxys(client).ConfigureAwait(false);
    }
}
