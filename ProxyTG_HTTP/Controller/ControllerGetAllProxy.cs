using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.Cache;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.ControllerStatsModel;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.ModelData.UpdateData;
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
        private readonly MemoryCacheShadowSoscks _memoryCacheShadowSoscks;
        private readonly MemoryCacheHttp _memoryCacheHttp;
        private readonly MemoryCacheHTTPS _memoryCacheHTTPS;

        public ControllerGetAllProxy()
        {
            _logger = Services.GetRequiredService<ILogger<ControllerGetAllProxy>>();
            _memoryMTProtoList = Services.GetRequiredService<MemoryCacheMTProtoList>();
            _memorySocks5List = Services.GetRequiredService<MemoryCacheSocks5List>();
            _memoryCacheShadowSoscks = Services.GetRequiredService<MemoryCacheShadowSoscks>();
            _memoryCacheHttp = Services.GetRequiredService<MemoryCacheHttp>();
            _memoryCacheHTTPS = Services.GetRequiredService<MemoryCacheHTTPS>();
        }

        [Route("GET", "/MTPROTO")]
        public Task<IReadOnlyList<MtProtoParse>> GetMTProto()
        {
            try
            {
                ChekedMtproto mtprotolist = _memoryMTProtoList.GetUpdated();

                if (mtprotolist != null)
                {
                    if (mtprotolist.Proxies.Count == 0)
                    {
                        _logger.LogWarning("Отдаю  cырой  список");
                        return Task.FromResult(_memoryMTProtoList.Get());
                    }

                    return Task.FromResult(mtprotolist.Proxies);
                }

                return Task.FromResult(_memoryMTProtoList.Get());
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult<IReadOnlyList<MtProtoParse>>(Array.Empty<MtProtoParse>());
            }
        }

        [Route("GET", "/SOCKS5")]
        public Task<IReadOnlyList<MtpParse>> GetSocks5()
        {
            try
            {
                CheckedSocks5? checkedSocks = _memorySocks5List.GetTestedEntry();

                if (checkedSocks != null)
                {
                    if (checkedSocks.Proxies.Count == 0)
                    {
                        _logger.LogWarning("Отдаю cырой список");
                        return Task.FromResult(_memorySocks5List.Get());
                    }

                    return Task.FromResult(checkedSocks.Proxies);
                }

                return Task.FromResult(_memorySocks5List.Get());
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult<IReadOnlyList<MtpParse>>(Array.Empty<MtpParse>());
            }
        }

        [Route("GET", "/SHADOWSOCKS")]
        public Task<IReadOnlyList<ShadowSocksParse>> GetShadowSocks()
        {
            try
            {
                 ChekedShadow? chekedShadow = _memoryCacheShadowSoscks.GetUpdate();

                if (chekedShadow != null)
                {
                    if (chekedShadow.Proxies.Count == 0)
                    {
                        _logger.LogWarning("Отдаю cырой список");
                        return Task.FromResult(_memoryCacheShadowSoscks.Get());
                    }

                    return Task.FromResult(chekedShadow.Proxies);
                }
                return Task.FromResult(_memoryCacheShadowSoscks.Get());
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult<IReadOnlyList<ShadowSocksParse>>(Array.Empty<ShadowSocksParse>());
            }
        }
        [Route("GET", "/HTTP")]
        public Task<IReadOnlyList<HttpParse>> GetHttp()
        {
            try
            {
                ChekedHTTP? chekedHTTP = _memoryCacheHttp.GetUpdated();

                if (chekedHTTP != null)
                {
                    if (chekedHTTP.Proxies.Count == 0)
                    {
                        _logger.LogWarning("Отдаю cырой список"); 
                        return Task.FromResult(_memoryCacheHttp.Get());
                    }

                    return Task.FromResult(chekedHTTP.Proxies);
                }
                else
                {
                    return Task.FromResult(_memoryCacheHttp.Get());
                }
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult<IReadOnlyList<HttpParse>>(Array.Empty<HttpParse>());
            }
        }

        [Route("GET", "/HTTPS")]
        public Task<IReadOnlyList<HttpsParse>> GetHttps()
        {
            try
            {
                CheckHTTPS? checkHTTPS = _memoryCacheHTTPS.GetUodate();

                if (checkHTTPS != null)
                {
                    if (checkHTTPS.Proxies.Count == 0)
                    {
                        _logger.LogWarning("Отдаю cырой список");
                        return Task.FromResult(_memoryCacheHTTPS.Get());
                    }
                    return Task.FromResult(checkHTTPS.Proxies);
                }
                else
                {
                    return Task.FromResult(_memoryCacheHTTPS.Get());
                }
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult<IReadOnlyList<HttpsParse>>(Array.Empty<HttpsParse>());
            }
        }

        [Route("GET", "/ALL")]
        public Task<AllStrcut> AllLists()
        {
            try
            {
                ChekedMtproto mtProtoListcheck = _memoryMTProtoList.GetUpdated();

                IReadOnlyList<MtProtoParse> mtProtoList =
                    mtProtoListcheck != null && mtProtoListcheck.Proxies.Count > 0
                    ? mtProtoListcheck.Proxies
                    : _memoryMTProtoList.Get();

                CheckedSocks5 ? checkedSocks5 = _memorySocks5List.GetTestedEntry();

                IReadOnlyList<MtpParse> socks5List = checkedSocks5 != null && checkedSocks5.Proxies.Count > 0
                    ? checkedSocks5.Proxies
                    : _memorySocks5List.Get();

                ChekedShadow? chekedShadow = _memoryCacheShadowSoscks.GetUpdate();

                IReadOnlyList<ShadowSocksParse>shadowSocksParses = chekedShadow != null && chekedShadow.Proxies.Count > 0
                    ?chekedShadow.Proxies
                    : _memoryCacheShadowSoscks.Get();

                ChekedHTTP? chekedHTTP = _memoryCacheHttp.GetUpdated();

                IReadOnlyList<HttpParse> httpParses = chekedHTTP != null && chekedHTTP.Proxies.Count > 0
                    ? chekedHTTP.Proxies
                    : _memoryCacheHttp.Get();

                CheckHTTPS? chekedHTTPS = _memoryCacheHTTPS.GetUodate();

                IReadOnlyList<HttpsParse> httpsParses = chekedHTTPS != null && chekedHTTPS.Proxies.Count > 0
                    ? chekedHTTPS.Proxies
                    : _memoryCacheHTTPS.Get();

                var strcut = new AllStrcut
                {
                    listMTPRoto = mtProtoList,
                    listSocks5 = socks5List,
                    listShadowSocks = shadowSocksParses,
                    listHTTP = httpParses,
                    listHTTPS = httpsParses
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
        public Task<List<ControllerStats>> StatsProxy()
        {
            List<ControllerStats> controllerStats = new List<ControllerStats>();
            try
            {
                IReadOnlyList<MtpParse> proxiall = _memorySocks5List.Get();
                CheckedSocks5 proxytested = _memorySocks5List.GetTestedEntry() ?? new CheckedSocks5();

                IReadOnlyList<MtProtoParse> mtprotoall = _memoryMTProtoList.Get();
                ChekedMtproto mtprototested = _memoryMTProtoList.GetUpdated() ?? new ChekedMtproto();

                IReadOnlyList<ShadowSocksParse> shadowSocksParses = _memoryCacheShadowSoscks.Get();
                ChekedShadow chekedShadow = _memoryCacheShadowSoscks.GetUpdate() ?? new ChekedShadow();

                IReadOnlyList<HttpParse> httpParses = _memoryCacheHttp.Get();
                ChekedHTTP chekedHTTP = _memoryCacheHttp.GetUpdated() ?? new ChekedHTTP();

                IReadOnlyList<HttpsParse> httpsParses = _memoryCacheHTTPS.Get();
                CheckHTTPS checkHTTPS = _memoryCacheHTTPS.GetUodate() ?? new CheckHTTPS();

                var resultStatsSocks5 = new ControllerStats()
                {
                    ProxiesAll = proxiall.Count,
                    ProxiesTested = proxytested.Proxies.Count,
                    DateTime = proxytested.CheckedAtUtc
                };
                controllerStats.Add(resultStatsSocks5);

                var resultStatsMTproto = new ControllerStats()
                {
                    ProxiesAll = mtprotoall.Count,
                    ProxiesTested = mtprototested.Proxies.Count,
                    DateTime = mtprototested.CheckedAtUtc
                };
                controllerStats.Add(resultStatsMTproto);

                var resultStatsShadow = new ControllerStats()
                {
                    ProxiesAll = shadowSocksParses.Count,
                    ProxiesTested = chekedShadow.Proxies.Count,
                    DateTime = chekedShadow.CheckedAtUtc
                };
                controllerStats.Add(resultStatsShadow);

                var resultHTTP = new ControllerStats()
                {
                    ProxiesAll = httpParses.Count,
                    ProxiesTested = chekedHTTP.Proxies.Count,
                    DateTime = chekedHTTP.CheckedAtUtc
                };
                controllerStats.Add(resultHTTP);

                var resultHTTPS = new ControllerStats()
                {
                    ProxiesAll = httpsParses.Count,
                    ProxiesTested = checkHTTPS.Proxies.Count,
                    DateTime = checkHTTPS.CheckedAtUtc
                };
                controllerStats.Add(resultHTTPS);

                return Task.FromResult(controllerStats);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult(controllerStats);
            }
        }
    }
}
