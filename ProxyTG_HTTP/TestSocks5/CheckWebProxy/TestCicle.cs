using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.Cache;
using ProxyTG_HTTP.ModelData.JsonPtoxyTest;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestSocks5.CheckWebProxy
{
    public class TestCicle
    {
        private readonly ILogger<TestCicle> _logger;
        private static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly TestSocks5WebProxy _testSocks5WebProxy;
        private readonly MemoryCacheSocks5List _memoryCacheSocks5List;

        public TestCicle(ILogger<TestCicle> logger, TestSocks5WebProxy testSocks5WebProxy, MemoryCacheSocks5List memoryCacheSocks5List)
        {
            _logger = logger;
            _testSocks5WebProxy = testSocks5WebProxy;
            _memoryCacheSocks5List = memoryCacheSocks5List;
        }

        public bool IsRunning => _semaphoreSlim.CurrentCount == 0;

        public async Task<TesteProxyModelCunt> TestMethod(CancellationToken token = default)
        {
            await _semaphoreSlim.WaitAsync(token).ConfigureAwait(false);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            int numberProxy = 0;
            try
            {
                TesteProxyModelCunt testeProxyModelCunt = new TesteProxyModelCunt();
                IReadOnlyList<HttpParse> listProxy = _memoryCacheSocks5List.Get();
                ConcurrentBag<HttpParse> lsitProxyTested = new ConcurrentBag<HttpParse>();

                testeProxyModelCunt.Tested = listProxy.Count;

                if (listProxy.Count != 0)
                {
                    await Parallel.ForEachAsync<HttpParse>(listProxy,
                       new ParallelOptions
                       {
                           MaxDegreeOfParallelism = 10,
                           CancellationToken = cts.Token
                       },
                       async (proxy, token) =>
                       {
                           int current = Interlocked.Increment(ref numberProxy);

                           if (current % 30 == 0)
                           {
                               _logger.LogInformation("Tested 30 proxies");
                           }

                           bool result = await _testSocks5WebProxy.TestProxy(proxy, token).ConfigureAwait(false);
                           if (result)
                           {
                               lsitProxyTested.Add(proxy);
                           }
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
                    return new TesteProxyModelCunt {Failed = false };
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
