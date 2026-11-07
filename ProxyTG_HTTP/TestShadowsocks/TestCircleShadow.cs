using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.Cache;
using ProxyTG_HTTP.ModelData.JsonPtoxyTest;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestShadowsocks
{
    public class TestCircleShadow
    {
        private readonly ILogger<TestCircleShadow> _logger;
        private readonly TestShadowTunnelCreate _testShadowTunnelCreate;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly MemoryCacheShadowSoscks _memoryCacheShadowSoscks;

        public TestCircleShadow(ILogger<TestCircleShadow> logger, TestShadowTunnelCreate testShadowTunnelCreate, MemoryCacheShadowSoscks memoryCacheShadowSoscks)
        {
            _logger = logger;
            _testShadowTunnelCreate = testShadowTunnelCreate;
            _memoryCacheShadowSoscks = memoryCacheShadowSoscks;
        }

        public async Task<TesteProxyModelCunt> CircleMethod(CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            int proxyyestedcount = 0;
            try
            {
                IReadOnlyList<ShadowSocksParse> shadowSocksParses = _memoryCacheShadowSoscks.Get();
                ConcurrentBag<ShadowSocksParse> shadowSocksParses1 = new ConcurrentBag<ShadowSocksParse>();
                TesteProxyModelCunt testeProxyModelCunt = new TesteProxyModelCunt();

                if (shadowSocksParses == null)
                {
                    return new TesteProxyModelCunt();
                }

                await Parallel.ForEachAsync<ShadowSocksParse>(shadowSocksParses, new ParallelOptions
                {
                    MaxDegreeOfParallelism = 10,
                    CancellationToken = cts.Token
                },
                async (shadowsocks, ctsl) =>
                { 
                    testeProxyModelCunt.Tested = shadowSocksParses.Count;

                    var current = Interlocked.Increment(ref proxyyestedcount);

                    var result = await _testShadowTunnelCreate.Test(shadowsocks, ctsl).ConfigureAwait(false);

                    if (current % 30 == 0)
                    {
                        _logger.LogInformation($"Tested {current} proxies");
                    }
                    if (result)
                    {
                        shadowSocksParses1.Add(shadowsocks);
                    }
                });

                testeProxyModelCunt.Alive = shadowSocksParses1.Count;
                testeProxyModelCunt.Failed = false;
                _memoryCacheShadowSoscks.CachedUpdate(shadowSocksParses1.ToList());
                _logger.LogInformation("Proxy testing complete. Tested: {Total}, Working: {Working}",
                   shadowSocksParses.Count, shadowSocksParses1.Count);

                return testeProxyModelCunt;
            }
            catch (OperationCanceledException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new TesteProxyModelCunt();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
