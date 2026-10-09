using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.Cache;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;
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

        public async Task TestMethod()
        {
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                List<HttpParse> listProxy = _memoryCacheSocks5List.Get();
                List<HttpParse> lsitProxyTested = new List<HttpParse>();

                if (listProxy != null && listProxy.Count != 0)
                {
                    foreach (var item in listProxy)
                    {
                        var result = await _testSocks5WebProxy.TestProxy(item).ConfigureAwait(false);

                        if (result == true)
                        {
                            lsitProxyTested.Add(item);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    _memoryCacheSocks5List.CacheTested(lsitProxyTested);
                }
                else
                {
                    _logger.LogWarning("Полынй список прокси пуст!");
                    return;
                }
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
