using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ExceptionBase.LogInfoANDLogWarn;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.ModelData.UpdateData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.Cache
{
    public class MemoryCacheHTTPS
    {
        private readonly ILogger<MemoryCacheHTTPS> _logger;
        private readonly IMemoryCache _memorycache;

        public const string cache_key = "HttpsCache_key";
        public const string staleCachekey = "HttpsCache_key_stale";
        public const string cache_keyUpdate = "HttpsCache_keyUpdate";
        public const string staleCachekeyUpdate = "HttpsCache_key_staleUpdate";

        public MemoryCacheHTTPS(ILogger<MemoryCacheHTTPS> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memorycache = memoryCache;
        }

        public void Cache(IReadOnlyList<HttpsParse> httpsParses)
        {
            try
            {
                if (httpsParses == null || httpsParses.Count == 0)
                    return;

                var Options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(13))
                    .SetSlidingExpiration(TimeSpan.FromHours(13));

                var StaleOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(13))
                    .SetSlidingExpiration(TimeSpan.FromHours(14));

                _memorycache.Set(cache_key, httpsParses, Options);
                _memorycache.Set(staleCachekey, httpsParses, StaleOptions);

                WarningAndInfoLog.LogInfo($"✅ HTTPS-кэш обновлён: {httpsParses.Count} прокси", _logger);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
            }
        }

        public IReadOnlyList<HttpsParse> Get()
        {
            try
            {
                if (_memorycache.TryGetValue(cache_key, out IReadOnlyList<HttpsParse>? fresh) && fresh != null)
                {
                    WarningAndInfoLog.LogWarning($"Fresh HTTPS пуст, отдаю stale: {fresh.Count} прокси", _logger);
                    return fresh;
                }
                if (_memorycache.TryGetValue(staleCachekey, out IReadOnlyList<HttpsParse>? stale) && stale != null)
                {
                    WarningAndInfoLog.LogWarning($"Stale HTTPS пуст, отдаю stale: {stale.Count} прокси", _logger);
                    return stale;
                }
                WarningAndInfoLog.LogWarning("🆘 Нет ни fresh, ни stale Http данных", _logger);
                return new List<HttpsParse>().AsReadOnly();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<HttpsParse>().AsReadOnly();
            }
        }

        public void CacheUpdate(IReadOnlyList<HttpsParse> httpsParses)
        {
            try
            {
                if (httpsParses == null || httpsParses.Count == 0)
                    return;

                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(13));

                var cheked = new CheckHTTPS
                {
                    Proxies = httpsParses,
                    CheckedAtUtc = DateTime.UtcNow
                };

                WarningAndInfoLog.LogInfo($"✅ HTTPS-кэш обновлён: {httpsParses.Count} прокси", _logger);

                _memorycache.Set(cache_keyUpdate, cheked, options);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
            }
        }

        public CheckHTTPS GetUodate()
        {
            try
            {   
                if (_memorycache.TryGetValue(cache_keyUpdate, out CheckHTTPS? checkHTTPS) && checkHTTPS != null)
                {
                    WarningAndInfoLog.LogInfo($"Отдаю updated HTTPS-кэш: {checkHTTPS.Proxies.Count} прокси", _logger);
                    return checkHTTPS;
                }
                WarningAndInfoLog.LogWarning("🆘 Нет updated HTTPS данных", _logger);
                return new CheckHTTPS();
            }
            catch(Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new CheckHTTPS();
            }
        }
    }
}
