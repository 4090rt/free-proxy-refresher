using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.Cache;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ExceptionBase.LogInfoANDLogWarn;
using ProxyTG_HTTP.ModelData.ParseData;
using SimpleW;

namespace ProxyTG_HTTP.Controller
{
    [Route("/proxy")]
    public class ControllerGetAllProxy: SimpleW.Controller 
    {
        public static IServiceProvider Services = default!;

        private readonly ILogger<ControllerGetAllProxy> _logger;
        private readonly MemoryCacheHttpList _memoryCacheHttpList;
        private readonly MemoryCacheMTProtoList _memoryMTProtoList;
        private readonly MemoryCacheSocks5List _memorySocks5List;

        public ControllerGetAllProxy()
        {
            _logger = Services.GetRequiredService<ILogger<ControllerGetAllProxy>>();
            _memoryCacheHttpList = Services.GetRequiredService<MemoryCacheHttpList>();
            _memoryMTProtoList = Services.GetRequiredService<MemoryCacheMTProtoList>();
            _memorySocks5List = Services.GetRequiredService<MemoryCacheSocks5List>();
        }

        [Route("GET", "/MTPROTO")]
        public Task<List<MtProtoParse>> GetMTProto()
        {
            try
            {
                List<MtProtoParse> list = _memoryMTProtoList.Get();

                if (list == null || list.Count == 0)
                {
                    WarningAndInfoLog.LogWarning("Запрос /MTPROTO: кэш пуст", _logger);
                    return Task.FromResult(new List<MtProtoParse>());
                }

                WarningAndInfoLog.LogInfo($"Запрос /MTPROTO: отдано {list.Count} прокси", _logger);
                return Task.FromResult(list);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult(new List<MtProtoParse>());
            }
        }

        [Route("GET", "/HTTP")]
        public Task<List<HttpParse>> GetHTTP()
        {
            try
            {
                List<HttpParse> list = _memoryCacheHttpList.Get();

                if (list == null || list.Count == 0)
                {
                    WarningAndInfoLog.LogWarning("Запрос /HTTP: кэш пуст", _logger);
                    return Task.FromResult(new List<HttpParse>());
                }

                WarningAndInfoLog.LogInfo($"Запрос /HTTP: отдано {list.Count} прокси", _logger);
                return Task.FromResult(list);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult(new List<HttpParse>());
            }
        }

        [Route("GET", "/SOCKS5")]
        public Task<List<HttpParse>> GetSocks5()
        {
            try
            {
                List<HttpParse> list = _memorySocks5List.Get();

                if (list == null || list.Count == 0)
                {
                    WarningAndInfoLog.LogWarning("Запрос /SOCKS5: кэш пуст", _logger);
                    return Task.FromResult(new List<HttpParse>());
                }

                WarningAndInfoLog.LogInfo($"Запрос /SOCKS5: отдано {list.Count} прокси", _logger);
                return Task.FromResult(list);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult(new List<HttpParse>());
            }
        }

        [Route("GET", "/ALL")]
        public Task<AllStrcut> AllLists()
        {
            try
            {
                List<HttpParse> httpList = _memoryCacheHttpList.Get();
                List<MtProtoParse> mtProtoList = _memoryMTProtoList.Get();
                List<HttpParse> socks5List = _memorySocks5List.Get();

                if ((httpList == null || httpList.Count == 0) &&
                    (mtProtoList == null || mtProtoList.Count == 0) &&
                    (socks5List == null || socks5List.Count == 0))
                {
                    WarningAndInfoLog.LogWarning("Запрос /ALL: все кэши пусты", _logger);
                    return Task.FromResult(new AllStrcut());
                }

                var strcut = new AllStrcut
                {
                    listHTTP = httpList ?? new List<HttpParse>(),
                    listMTPRoto = mtProtoList ?? new List<MtProtoParse>(),
                    listSocks5 = socks5List ?? new List<HttpParse>()
                };

                WarningAndInfoLog.LogInfo($"Запрос /ALL: отдано HTTP {httpList?.Count ?? 0}, MTProto {mtProtoList?.Count ?? 0}, SOCKS5 {socks5List?.Count ?? 0}", _logger);
                return Task.FromResult(strcut);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult(new AllStrcut());
            }
        }

        [Route("GET", "/HTTPandMTProto")]
        public Task<AllStrcut> All()
        {
            try
            {
                List<HttpParse> httpList = _memoryCacheHttpList.Get();
                List<MtProtoParse> mtProtoList = _memoryMTProtoList.Get();

                if (httpList == null || httpList.Count == 0 || mtProtoList == null || mtProtoList.Count == 0)
                {
                    WarningAndInfoLog.LogWarning($"Запрос /HTTPandMTProto: HTTP {httpList?.Count ?? 0}, MTProto {mtProtoList?.Count ?? 0}", _logger);
                    return Task.FromResult(new AllStrcut());
                }

                var strcut = new AllStrcut
                {
                    listHTTP = httpList,
                    listMTPRoto = mtProtoList
                };

                WarningAndInfoLog.LogInfo($"Запрос /HTTPandMTProto: отдано HTTP {httpList.Count}, MTProto {mtProtoList.Count}", _logger);
                return Task.FromResult(strcut);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return Task.FromResult(new AllStrcut());
            }
        }
    }
}
