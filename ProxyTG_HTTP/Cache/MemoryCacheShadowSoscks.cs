using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ExceptionBase.LogInfoANDLogWarn;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.Cache
{
    public class MemoryCacheShadowSoscks
    {
        private readonly ILogger<MemoryCacheShadowSoscks> _logger;
        private readonly IMemoryCache _memorycache;

        public const string cache_key = "ShadowCache_key";
        public const string staleCachekey = "ShadowCache_key_stale";
        public const string cache_keyUpdate = "ShadowCache_key";
        public const string staleCachekeyUpdate = "ShadowCache_key_stale";

        public MemoryCacheShadowSoscks(ILogger<MemoryCacheShadowSoscks> logger, IMemoryCache memorycache)
        {
            _logger = logger;
            _memorycache = memorycache;
        }

        public void Cached(IReadOnlyList<ShadowSocksParse> shadowSocksParses)
        {
            try
            {
                if (shadowSocksParses != null && shadowSocksParses.Count > 0)
                { 
                    var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(13))
                    .SetSlidingExpiration(TimeSpan.FromHours(13));

                    var staleoptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(13))
                    .SetSlidingExpiration(TimeSpan.FromHours(14));

                    _memorycache.Set(cache_key, shadowSocksParses, options);

                    _memorycache.Set(staleCachekey, shadowSocksParses, staleoptions);
                    WarningAndInfoLog.LogInfo($"✅ MTProto-кэш обновлён: {shadowSocksParses.Count} прокси", _logger);
                }
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
            }
        }

        public IReadOnlyList<ShadowSocksParse> Get()
        {
            try
            {
                if (_memorycache.TryGetValue(cache_key, out IReadOnlyList<ShadowSocksParse>? cached) && cached != null)
                {
                    return cached;
                }

                if (_memorycache.TryGetValue(staleCachekey, out IReadOnlyList<ShadowSocksParse>? cachedstale) && cachedstale != null)
                {
                    return cachedstale;
                }
                return new List<ShadowSocksParse>().AsReadOnly();
            }
            catch(Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<ShadowSocksParse>().AsReadOnly();
            }
        }

        public void CachedUpdate(IReadOnlyList<ShadowSocksParse> shadowSocksParses)
        {
            try
            {
                if (shadowSocksParses != null && shadowSocksParses.Count > 0)
                {
                    var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(13))
                    .SetSlidingExpiration(TimeSpan.FromHours(13));

                    var staleoptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(13))
                    .SetSlidingExpiration(TimeSpan.FromHours(14));

                    _memorycache.Set(cache_keyUpdate, shadowSocksParses, options);
                    _memorycache.Set(staleCachekeyUpdate, shadowSocksParses, staleoptions);
                }
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
            }
        }

        public IReadOnlyList<ShadowSocksParse> GetUpdate()
        {
            try
            {
                if (_memorycache.TryGetValue(cache_keyUpdate, out IReadOnlyList<ShadowSocksParse>? cached) && cached != null)
                {
                    return cached;
                }

                if (_memorycache.TryGetValue(staleCachekeyUpdate, out IReadOnlyList<ShadowSocksParse>? cachedstale) && cachedstale != null)
                {
                    return cachedstale;
                }
                return new List<ShadowSocksParse>().AsReadOnly();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<ShadowSocksParse>().AsReadOnly();
            }
        }
    }
}
