using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.Cache;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.ControllerStatsModel;
using ProxyTG_HTTP.ModelData.ParseData;
using SimpleW;

namespace ProxyTG_HTTP.Controller
{
    [Route("/proxy")]
    public class ControllerGetAllProxy : SimpleW.Controller
    {
        public static IServiceProvider Services = default!;

        private readonly ILogger<ControllerGetAllProxy> _logger;
        private readonly MemoryCacheMTProtoList _memoryMTProtoList;
        private readonly MemoryCacheSocks5List _memorySocks5List;

        public ControllerGetAllProxy()
        {
            _logger = Services.GetRequiredService<ILogger<ControllerGetAllProxy>>();
            _memoryMTProtoList = Services.GetRequiredService<MemoryCacheMTProtoList>();
            _memorySocks5List = Services.GetRequiredService<MemoryCacheSocks5List>();
        }

        [Route("GET", "/MTPROTO")]
        public Task<IReadOnlyList<MtProtoParse>> GetMTProto()
        {
            try
            {
                return Task.FromResult(_memoryMTProtoList.Get());
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult<IReadOnlyList<MtProtoParse>>(Array.Empty<MtProtoParse>());
            }
        }

        [Route("GET", "/SOCKS5")]
        public Task<IReadOnlyList<HttpParse>> GetSocks5()
        {
            try
            {
                CheckedSocks5? checkedSocks = _memorySocks5List.GetTestedEntry();

                if (checkedSocks != null)
                {
                    if (checkedSocks.Proxies.Count == 0)
                        _logger.LogWarning("Отдаю пустой список");

                    return Task.FromResult(checkedSocks.Proxies);
                }

                return Task.FromResult(_memorySocks5List.Get());
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult<IReadOnlyList<HttpParse>>(Array.Empty<HttpParse>());
            }
        }

        [Route("GET", "/ALL")]
        public Task<AllStrcut> AllLists()
        {
            try
            {
                IReadOnlyList<MtProtoParse> mtProtoList = _memoryMTProtoList.Get();

                CheckedSocks5? checkedSocks5 = _memorySocks5List.GetTestedEntry();

                IReadOnlyList<HttpParse> socks5List = checkedSocks5 != null
                    ? checkedSocks5.Proxies
                    : _memorySocks5List.Get();

                var strcut = new AllStrcut
                {
                    listMTPRoto = mtProtoList,
                    listSocks5 = socks5List,
                };

                return Task.FromResult(strcut);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult(new AllStrcut());
            }
        }

        [Route("GET", "/STATS")]
        public Task<ControllerStats> StatsProxy()
        {
            try
            {
                IReadOnlyList<HttpParse> proxiall = _memorySocks5List.Get();
                CheckedSocks5 proxytested = _memorySocks5List.GetTestedEntry() ?? new CheckedSocks5();

                var resultStats = new ControllerStats()
                {
                    ProxiesAll = proxiall.Count,
                    ProxiesTested = proxytested.Proxies.Count,
                    DateTime = proxytested.CheckedAtUtc
                };

                return Task.FromResult(resultStats);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult(new ControllerStats());
            }
        }
    }
}
