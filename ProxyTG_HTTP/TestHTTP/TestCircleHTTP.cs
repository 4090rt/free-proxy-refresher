using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.Cache;
using ProxyTG_HTTP.ModelData.JsonPtoxyTest;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.TestMtproto;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestHTTP
{
    public class TestCircleHTTP
    {
        private readonly ILogger<TestCircleHTTP> _logger;
        private readonly MemoryCacheHttp _memoryCacheHttp;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly TestHTTPTunnel _testhttpTunnel;

        public TestCircleHTTP(ILogger<TestCircleHTTP> logger, MemoryCacheHttp memoryCacheHttp, TestHTTPTunnel testhttpTunnel)
        {
            _logger = logger;
            _memoryCacheHttp = memoryCacheHttp;
            _testhttpTunnel = testhttpTunnel;
        }

        public async Task<TesteProxyModelCunt> TestCircle(CancellationToken cancellation)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            await _semaphore.WaitAsync().ConfigureAwait(false);
            int parsedproxy = 0;
            try
            {
                IReadOnlyList<HttpParse> httpParse = _memoryCacheHttp.Get();
                ConcurrentBag<HttpParse> httpParses = new ConcurrentBag<HttpParse>();
                TesteProxyModelCunt testeProxyModelCunt = new TesteProxyModelCunt();

                if (httpParse.Count == 0)
                {
                    return new TesteProxyModelCunt();
                }

                await Parallel.ForEachAsync<HttpParse>(httpParse, new ParallelOptions
                {
                    MaxDegreeOfParallelism = 10,
                    CancellationToken = cts.Token
                },
                async (httpproxy, ctsl) =>
                {
                    testeProxyModelCunt.Tested = httpParse.Count;

                    var current = Interlocked.Increment(ref parsedproxy);

                    bool result = await _testhttpTunnel.TunnelCreate(httpproxy, ctsl).ConfigureAwait(false);

                    if (parsedproxy % 30 == 0)
                    {
                        _logger.LogInformation("Tested 30 proxies");
                    }
                    if (result)
                    {
                        httpParses.Add(httpproxy);
                    }
                });
                testeProxyModelCunt.Alive = parsedproxy;
                testeProxyModelCunt.Failed = false;
                _logger.LogInformation("Proxy testing complete. Tested: {Total}, Working: {Working}",
                httpParse.Count, httpParses.Count);
                _memoryCacheHttp.CacheUpdated(httpParses.ToList());

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
