using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.Cache;
using ProxyTG_HTTP.ModelData.JsonPtoxyTest;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.TestHTTP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestHTTPS
{
    public class TestCIrcleHTTPS
    {
        private readonly ILogger<TestCIrcleHTTPS> _logger;
        private readonly MemoryCacheHTTPS _memoryCacheHttps;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly TestHTTPSTunnel _testhttpsTunnel;

        public TestCIrcleHTTPS(ILogger<TestCIrcleHTTPS> logger, MemoryCacheHTTPS memoryCacheHttps, TestHTTPSTunnel testhttpsTunnel)
        {
            _logger = logger;
            _memoryCacheHttps = memoryCacheHttps;
            _testhttpsTunnel = testhttpsTunnel;
        }

        public async Task<TesteProxyModelCunt> TestCircle(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            int parsedproxy = 0;
            try
            {
                IReadOnlyList<HttpsParse> httpsParses = _memoryCacheHttps.Get();
                ConcurrentBag<HttpsParse> httpsParsesBag = new ConcurrentBag<HttpsParse>();
                TesteProxyModelCunt testeProxyModelCunts = new TesteProxyModelCunt();

                if (httpsParses == null || httpsParses.Count == 0)
                    return new TesteProxyModelCunt();

                await Parallel.ForEachAsync<HttpsParse>(httpsParses, new ParallelOptions
                {
                    MaxDegreeOfParallelism = 10,
                    CancellationToken = cts.Token
                },
                async (proxy, ctsl) =>
                {
                    testeProxyModelCunts.Alive = httpsParses.Count();

                    Interlocked.Increment(ref parsedproxy);

                    bool result = await _testhttpsTunnel.TunnelCreate(proxy, ctsl).ConfigureAwait(false);

                    if (result)
                    {
                        httpsParsesBag.Add(proxy);
                    }
                });
                _memoryCacheHttps.CacheUpdate(httpsParsesBag.ToList());
                testeProxyModelCunts.Tested = parsedproxy;
                _logger.LogInformation("Proxy testing complete. Tested: {Total}, Working: {Working}", httpsParses.Count);
                testeProxyModelCunts.Failed = false;
                return testeProxyModelCunts;
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
