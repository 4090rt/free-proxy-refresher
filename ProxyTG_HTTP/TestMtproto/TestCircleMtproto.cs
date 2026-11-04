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

namespace ProxyTG_HTTP.TestMtproto
{
    public class TestCircleMtproto
    {
        private readonly  ILogger<TestCircleMtproto> _logger;
        private readonly  MemoryCacheMTProtoList _memoryCacheMTProtoList;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly TestMtprtotoTunnel _testMtprtotoTunnel;

        public TestCircleMtproto(ILogger<TestCircleMtproto> logger, MemoryCacheMTProtoList memoryCacheMTProtoList, TestMtprtotoTunnel testMtprtotoTunnel)
        {
            _logger = logger;
            _memoryCacheMTProtoList = memoryCacheMTProtoList;
            _testMtprtotoTunnel = testMtprtotoTunnel;
        }

        public async Task<TesteProxyModelCunt> CircleMethod(CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false); 
            int numberproxy = 0;
            try
            {
                IReadOnlyList<MtProtoParse> mtProtoParses = _memoryCacheMTProtoList.Get();
                ConcurrentBag<MtProtoParse> mtProtoParseBag = new ConcurrentBag<MtProtoParse>();
                TesteProxyModelCunt testeProxyModelCunt = new TesteProxyModelCunt();

                if (mtProtoParses.Count == 0)
                {
                    return new TesteProxyModelCunt();
                }

                await Parallel.ForEachAsync<MtProtoParse>(mtProtoParses,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 10,
                        CancellationToken = cts.Token
                    },
                    async (mtprtoxy, ctsL) =>
                    { 
                        testeProxyModelCunt.Tested = mtProtoParses.Count;

                        var current = Interlocked.Increment(ref numberproxy);

                        var result = await _testMtprtotoTunnel.TestMethod(mtprtoxy, ctsL).ConfigureAwait(false);

                        if (current % 30 == 0)
                        {
                            _logger.LogInformation("Tested 30 proxies");
                        }
                        if (result)
                        {
                            mtProtoParseBag.Add(mtprtoxy);
                        }
                    });
                testeProxyModelCunt.Alive = mtProtoParseBag.Count;
                testeProxyModelCunt.Failed = false;
                _memoryCacheMTProtoList.CacheTested(mtProtoParseBag.ToList());

                _logger.LogInformation("Proxy testing complete. Tested: {Total}, Working: {Working}",
                    mtProtoParses.Count, mtProtoParseBag.Count);

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
