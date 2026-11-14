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
    public class MemoryCacheHttp
    {
        private readonly ILogger<MemoryCacheHttp> _logger;
        private readonly IMemoryCache _memorycache;

        public const string cache_key = "HttpCache_key";
        public const string staleCachekey = "HttpCache_key_stale";
        public const string cache_keyUpdate = "HttpCache_keyUpdate";
        public const string staleCachekeyUpdate = "HttpCache_key_staleUpdate";

        public MemoryCacheHttp(ILogger<MemoryCacheHttp> logger, IMemoryCache memorycache)
        {
            _logger = logger;
            _memorycache = memorycache;
        }

        public void Cache(IReadOnlyList<HttpParse> httpParses)
        {
            try
            {
                if (httpParses == null) return;

                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(13))
                    .SetSlidingExpiration(TimeSpan.FromHours(13));

                var staleoptions = new MemoryCacheEntryOptions()
                   .SetAbsoluteExpiration(TimeSpan.FromHours(13))
                   .SetSlidingExpiration(TimeSpan.FromHours(14));

                _memorycache.Set(cache_key, httpParses, options);
                _memorycache.Set(staleCachekey, httpParses, staleoptions);

                WarningAndInfoLog.LogInfo($"✅ MTProto-кэш обновлён: {httpParses.Count} прокси", _logger);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
            }
        }

        public IReadOnlyList<HttpParse> Get()
        {
            try
            {
                if (_memorycache.TryGetValue(cache_key, out IReadOnlyList<HttpParse>? fresh) && fresh != null)
                {
                    WarningAndInfoLog.LogWarning($"Fresh HTTP пуст, отдаю stale: {fresh.Count} прокси", _logger);
                    return fresh;
                }

                if (_memorycache.TryGetValue(staleCachekey, out IReadOnlyList<HttpParse>? stale) && stale != null)
                {
                    WarningAndInfoLog.LogWarning($"Stale HTTP пуст, отдаю stale: {stale.Count} прокси", _logger);
                    return stale;
                }
                WarningAndInfoLog.LogWarning("🆘 Нет ни fresh, ни stale Http данных", _logger);
                return new List<HttpParse>().AsReadOnly();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<HttpParse>().AsReadOnly();
            }
        }

        public void CacheUpdated(IReadOnlyList<HttpParse> httpParses)
        {
            try
            {
                if (httpParses == null || httpParses.Count == 0)
                    return;

                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(13));

                var updateted = new ChekedHTTP
                {
                    Proxies = httpParses,
                    CheckedAtUtc = DateTime.UtcNow
                };

                _memorycache.Set(cache_keyUpdate, httpParses, options);

                WarningAndInfoLog.LogInfo($"✅ HTTP-кэш обновлён: {httpParses.Count} прокси", _logger);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
            }
        }

        public ChekedHTTP GetUpdated()
        {
            try
            {
                if (_memorycache.TryGetValue(cache_keyUpdate, out ChekedHTTP? chekedHTTP) && chekedHTTP != null)
                {
                    WarningAndInfoLog.LogInfo($"Отдаю updated HTTP-кэш: {chekedHTTP.Proxies.Count} прокси", _logger);
                    return chekedHTTP;
                }
                WarningAndInfoLog.LogWarning("🆘 Нет updated HTTP данных", _logger);
                return new ChekedHTTP();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new ChekedHTTP();
            }
        }
    }
}
