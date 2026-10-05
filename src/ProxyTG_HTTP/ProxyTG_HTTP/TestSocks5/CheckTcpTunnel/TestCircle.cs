using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.Cache;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.TestSocks5.CheckWebProxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestSocks5.CheckTcpTunnel
{
    public class TestCircle
    {
        private readonly ILogger<TestCircle> _logger;
        private readonly TestSocs5TunnelCreate _tunnelCreate;
        private static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly MemoryCacheSocks5List _memoryCacheSocks5List;

        public TestCircle(ILogger<TestCircle> logger, TestSocs5TunnelCreate tunnelCreate, MemoryCacheSocks5List memoryCacheSocks5List)
        {
            _logger = logger;
            _tunnelCreate = tunnelCreate;
            _memoryCacheSocks5List = memoryCacheSocks5List;
        }

        public async Task Circle()
        {
            using var cts = new CancellationTokenSource();
            try
            {
                await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

                IEnumerable<HttpParse> listProxy = _memoryCacheSocks5List.Get();
                ConcurrentBag<HttpParse> lsitProxyTested = new ConcurrentBag<HttpParse>();

                if (listProxy == null)
                {
                    _logger.LogWarning("Полынй список прокси пуст!");
                    return;
                }

                await Parallel.ForEachAsync<HttpParse>(listProxy,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 10,
                        CancellationToken = cts.Token
                    },
                    async (proxy, token) =>
                    {
                        bool proxyresult = await _tunnelCreate.Test(proxy, token).ConfigureAwait(false);
                        if (proxyresult)
                            lsitProxyTested.Add(proxy);
                    });
                _memoryCacheSocks5List.CachedTestSocks5_2(lsitProxyTested.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}
