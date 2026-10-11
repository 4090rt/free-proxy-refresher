using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.Cache;
using ProxyTG_HTTP.ModelData.JsonPtoxyTest;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.TestSocks5.CheckWebProxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        public async Task<TesteProxyModelCunt> Circle(CancellationToken token = default)
        {
            await _semaphoreSlim.WaitAsync(token).ConfigureAwait(false);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            int numberproxy = 0;
            try
            {
                IReadOnlyList<HttpParse> listProxy = _memoryCacheSocks5List.Get();
                TesteProxyModelCunt testeProxyModelCunt = new TesteProxyModelCunt();
                ConcurrentBag<HttpParse> lsitProxyTested = new ConcurrentBag<HttpParse>();

                if (listProxy.Count != 0)
                {
                    testeProxyModelCunt.Tested = listProxy.Count;

                    await Parallel.ForEachAsync<HttpParse>(listProxy,
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism = 10,
                            CancellationToken = cts.Token
                        },
                        async (proxy, token) =>
                        {
                            var current = Interlocked.Increment(ref numberproxy);

                            if (current % 30 == 0)
                            {
                                _logger.LogInformation("Tested 30 proxies");
                            }
                            bool proxyresult = await _tunnelCreate.Test(proxy, token).ConfigureAwait(false);
                            if (proxyresult)
                                lsitProxyTested.Add(proxy);
                        });
                    _memoryCacheSocks5List.CacheTested(lsitProxyTested.ToList());
                    testeProxyModelCunt.Alive = lsitProxyTested.Count;
                    testeProxyModelCunt.Failed = false;

                    _logger.LogInformation("Proxy testing complete. Tested: {Total}, Working: {Working}",
                  listProxy.Count, lsitProxyTested.Count);

                    return testeProxyModelCunt;
                }
                else
                {
                    return new TesteProxyModelCunt {Failed = false};   
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new TesteProxyModelCunt { Failed = true };
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}
