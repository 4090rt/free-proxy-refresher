using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ExceptionBase.LogInfoANDLogWarn;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.Cache
{
    public class MemoryCacheHttpList
    {
        private readonly ILogger<MemoryCacheHttpList> _logger;
        private readonly IMemoryCache _memorycache;

        public string cache_key = "HTTPCache_key";
        public string staleCachekey = "HTTPCache_key_stale";
        public MemoryCacheHttpList(ILogger<MemoryCacheHttpList> logger, IMemoryCache memorycache)
        {
            _logger = logger;
            _memorycache = memorycache;
        }

        public void Cache(List<HttpParse> parseHttps)
        {
            try
            {
                    if (parseHttps != null && parseHttps.Count != 0)
                    {
                        var options = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromHours(13))
                        .SetSlidingExpiration(TimeSpan.FromHours(13));

                        _memorycache.Set(cache_key, parseHttps, options);

                        var staleoptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromHours(13))
                        .SetSlidingExpiration(TimeSpan.FromHours(14));

                        _memorycache.Set(staleCachekey, parseHttps, staleoptions);

                        WarningAndInfoLog.LogInfo($"✅ HTTP-кэш обновлён: {parseHttps.Count} прокси", _logger);
                    }
                    else
                    {
                        WarningAndInfoLog.LogWarning("Результат запроса HTTP пуст, кэш не обновлён", _logger);
                    }
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
            }
        }

        public List<HttpParse> Get()
        {
            try
            {
                if (_memorycache.TryGetValue(cache_key, out List<HttpParse>? fresh) && fresh != null)
                {
                    WarningAndInfoLog.LogInfo($"Отдаю fresh HTTP-кэш: {fresh.Count} прокси", _logger);
                    return fresh;
                }

                if (_memorycache.TryGetValue(staleCachekey, out List<HttpParse>? stale) && stale != null)
                {
                    WarningAndInfoLog.LogWarning($"Fresh HTTP пуст, отдаю stale: {stale.Count} прокси", _logger);
                    return stale;
                }
                WarningAndInfoLog.LogWarning("🆘 Нет ни fresh, ни stale HTTP данных", _logger);
                return new List<HttpParse>();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<HttpParse>();
            }
        }
    }
}
