using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ExceptionBase.LogInfoANDLogWarn;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;

namespace ProxyTG_HTTP.Cache
{
    public class MemoryCacheSocks5List
    {
        private readonly ILogger<MemoryCacheSocks5List> _logger;
        private readonly IMemoryCache _memorycache;

        public string cache_key = "Socks5Cache_key";
        public string staleCachekey = "Socks5Cache_key_stale";

        public MemoryCacheSocks5List(ILogger<MemoryCacheSocks5List> logger, IMemoryCache memorycache)
        {
            _logger = logger;
            _memorycache = memorycache;
        }

        public void Cache(List<HttpParse> parseSocks5)
        {
            try
            {
                if (parseSocks5 != null && parseSocks5.Count != 0)
                {
                    var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(13))
                    .SetSlidingExpiration(TimeSpan.FromHours(13));

                    _memorycache.Set(cache_key, parseSocks5, options);

                    var staleoptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(13))
                    .SetSlidingExpiration(TimeSpan.FromHours(14));

                    _memorycache.Set(staleCachekey, parseSocks5, staleoptions);

                    WarningAndInfoLog.LogInfo($"✅ SOCKS5-кэш обновлён: {parseSocks5.Count} прокси", _logger);
                }
                else
                {
                    WarningAndInfoLog.LogWarning("Результат запроса SOCKS5 пуст, кэш не обновлён", _logger);
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
                    WarningAndInfoLog.LogInfo($"Отдаю fresh SOCKS5-кэш: {fresh.Count} прокси", _logger);
                    return fresh;
                }

                if (_memorycache.TryGetValue(staleCachekey, out List<HttpParse>? stale) && stale != null)
                {
                    WarningAndInfoLog.LogWarning($"Fresh SOCKS5 пуст, отдаю stale: {stale.Count} прокси", _logger);
                    return stale;
                }
                WarningAndInfoLog.LogWarning("🆘 Нет ни fresh, ни stale SOCKS5 данных", _logger);
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
