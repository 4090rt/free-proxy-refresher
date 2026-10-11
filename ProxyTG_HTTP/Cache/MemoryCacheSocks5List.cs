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

        public const string cache_key = "Socks5Cache_key";
        public const string staleCachekey = "Socks5Cache_key_stale";
        public const string cache_key_tested = "Socks5Cache_key_tested";

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

                    // WarningAndInfoLog.LogInfo($"✅ SOCKS5-кэш обновлён: {parseSocks5.Count} прокси", _logger);
                }
                else
                {
                    // WarningAndInfoLog.LogWarning("Результат запроса SOCKS5 пуст, кэш не обновлён", _logger);
                }
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
                if (_memorycache.TryGetValue(cache_key, out List<HttpParse>? fresh) && fresh != null)
                {
                    // WarningAndInfoLog.LogInfo($"Отдаю fresh SOCKS5-кэш: {fresh.Count} прокси", _logger);
                    return fresh.AsReadOnly();
                }

                if (_memorycache.TryGetValue(staleCachekey, out List<HttpParse>? stale) && stale != null)
                {
                    // WarningAndInfoLog.LogWarning($"Fresh SOCKS5 пуст, отдаю stale: {stale.Count} прокси", _logger);
                    return stale.AsReadOnly();
                }
                // WarningAndInfoLog.LogWarning("🆘 Нет ни fresh, ни stale SOCKS5 данных", _logger);
                return new List<HttpParse>().AsReadOnly();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<HttpParse>();
            }
        }

        public void CacheTested(List<HttpParse> parseTested)
        {
            try
            {
                if (parseTested == null)
                {
                    _logger.LogWarning("список прокси не найден");
                    // WarningAndInfoLog.LogWarning("Результат проверки SOCKS5 пуст, кэш не обновлён", _logger);
                    return;
                }

                if (parseTested.Count == 0)
                    _logger.LogWarning("Проверка завершена: живых прокси 0");

                var entry = new CheckedSocks5
                {
                    Proxies = parseTested,
                    CheckedAtUtc = DateTime.UtcNow
                };

                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(13));

                _memorycache.Set(cache_key_tested, entry, options);
                // WarningAndInfoLog.LogInfo($"✅ SOCKS5-кэш тестированный обновлён: {parseTested.Count} прокси работают, проверка {entry.CheckedAtUtc}", _logger);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
            }
        }


        public CheckedSocks5? GetTestedEntry()
        {
            try
            {
                if (TryGetTestedEntry(out CheckedSocks5? cached) && cached != null)
                {
                    return cached;
                }
                return null;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return null;
            }
        }

        private bool TryGetTestedEntry(out CheckedSocks5? entry)
        {
            return _memorycache.TryGetValue(cache_key_tested, out entry) && entry != null;
        }
    }
}
