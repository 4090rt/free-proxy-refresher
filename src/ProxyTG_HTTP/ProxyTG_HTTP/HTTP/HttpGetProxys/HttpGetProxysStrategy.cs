using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.LogSaveClass;
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
        public readonly LogSave _logSave;

        public RequestMTProto(GetProxys getProxys, ParseMTProto parseMTProto, ILogger<RequestMTProto> logger, LogSave logSave)
        {
            _getProxys = getProxys;
            _parseMTProto = parseMTProto;
            _logger = logger;
            _logSave = logSave;
        }

        public async Task<List<ProxyData>> RequestInProxys(string client)
        {
            try
            {
                ReadOnlyMemory<byte> readOnlyMemory = await _getProxys.GetMethod(client).ConfigureAwait(false);

                if (readOnlyMemory.Length == 0)
                    return new List<ProxyData>();

                List<ProxyData> mtProtoParses = _parseMTProto.ParseMethod(readOnlyMemory); 
            
                if (mtProtoParses.Count == 0)
                    return new List<ProxyData>();

                _logger.LogInformation($"Распознано MTProto прокси: {mtProtoParses.Count}");
                await _logSave.SaveLog($"Распознано MTProto прокси: {mtProtoParses.Count}", DateTime.UtcNow.ToString());

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
        public readonly LogSave _logSave;

        public RequestHttp(GetProxys getProxys, ParseHttp parseHttp, ILogger<RequestHttp> logger, LogSave logSave)
        {
            _getProxys = getProxys;
            _parseHttp = parseHttp;
            _logger = logger;
            _logSave = logSave;
        }

        public async Task<List<ProxyData>> RequestInProxys(string client)
        {
            try
            {
                ReadOnlyMemory<byte> readOnlyMemory = await _getProxys.GetMethod(client).ConfigureAwait(false);

                if (readOnlyMemory.Length == 0)
                    return new List<ProxyData>();

                List<ProxyData> parseHttps = _parseHttp.ParseMethod(readOnlyMemory);

                if (parseHttps.Count == 0)
                    return new List<ProxyData>();

                _logger.LogInformation($"Распознано HTTP прокси: {parseHttps.Count}");
                await _logSave.SaveLog($"Распознано HTTP прокси: {parseHttps.Count}", DateTime.UtcNow.ToString());

                return parseHttps;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex,_logger);
                return new List<ProxyData>();
            }
        }
    }

    public class RequestSocks5 : HttpGetProxysStrategy
    {
        public readonly GetProxys _getProxys;
        public readonly ParseSocks5 _parseSocks5;
        public readonly ILogger<RequestSocks5> _logger;
        public readonly LogSave _logSave;

        public RequestSocks5(GetProxys getProxys, ParseSocks5 parseSocks5, ILogger<RequestSocks5> logger, LogSave logSave)
        {
            _getProxys = getProxys;
            _parseSocks5 = parseSocks5;
            _logger = logger;
            _logSave = logSave;
        }

        public async Task<List<ProxyData>> RequestInProxys(string client)
        {
            try
            {
                ReadOnlyMemory<byte> readOnlyMemory = await _getProxys.GetMethod(client).ConfigureAwait(false);

                if (readOnlyMemory.Length == 0)
                    return new List<ProxyData>();

                List<ProxyData> socks5 = _parseSocks5.ParseMethod(readOnlyMemory);

                if (socks5.Count == 0)
                    return new List<ProxyData>();

                _logger.LogInformation($"Распознано SOCKS5 прокси: {socks5.Count}");
                await _logSave.SaveLog($"Распознано SOCKS5 прокси: {socks5.Count}", DateTime.UtcNow.ToString());

                return socks5;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
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
