using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
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
    public class MemoryCacheMTProtoList
    {
        private readonly ILogger<MemoryCacheMTProtoList> _logger;
        private readonly IMemoryCache _memorycache;

        public const string cache_key = "MtpRotoCache_key";
        public const string staleCachekey = "MtpRotoCache_key_stale";

        public MemoryCacheMTProtoList(ILogger<MemoryCacheMTProtoList> logger, IMemoryCache memorycache)
        {
            _logger = logger;
            _memorycache = memorycache;
        }

        public void Cache(List<MtProtoParse> parseHttps)
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

                    // WarningAndInfoLog.LogInfo($"✅ MTProto-кэш обновлён: {parseHttps.Count} прокси", _logger);
                }
                else
                {
                    // WarningAndInfoLog.LogWarning("Результат запроса MTProto пуст, кэш не обновлён", _logger);
                }
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
            }
        }

        public IReadOnlyList<MtProtoParse> Get()
        {
            try
            {
                if (_memorycache.TryGetValue(cache_key, out List<MtProtoParse>? fresh) && fresh != null)
                {
                    // WarningAndInfoLog.LogInfo($"Отдаю fresh MTProto-кэш: {fresh.Count} прокси", _logger);
                    return fresh.AsReadOnly();
                }

                if (_memorycache.TryGetValue(staleCachekey, out List<MtProtoParse>? stale) && stale != null)
                {
                    // WarningAndInfoLog.LogWarning($"Fresh MTProto пуст, отдаю stale: {stale.Count} прокси", _logger);
                    return stale.AsReadOnly();
                }
                // WarningAndInfoLog.LogWarning("🆘 Нет ни fresh, ни stale MTProto данных", _logger);
                return new List<MtProtoParse>().AsReadOnly();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<MtProtoParse>().AsReadOnly();
            }
        }
    }
}
