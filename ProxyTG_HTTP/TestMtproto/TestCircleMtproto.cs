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

        public async Task<List<MtProtoParse>> CircleMethod(CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                IReadOnlyList<MtProtoParse> mtProtoParses = _memoryCacheMTProtoList.Get();
                ConcurrentBag<MtProtoParse> mtProtoParseBag = new ConcurrentBag<MtProtoParse>();

                if (mtProtoParses.Count == 0)
                {
                    return new List<MtProtoParse>();
                }

                await Parallel.ForEachAsync<MtProtoParse>(mtProtoParses,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 10,
                        CancellationToken = cts.Token
                    },
                    async (mtprtoxy, cts) =>
                    { 
                        var result = await _testMtprtotoTunnel.TestMethod(mtprtoxy, cts).ConfigureAwait(false);

                        if (result)
                        {
                            mtProtoParseBag.Add(mtprtoxy);
                        }
                    });
                _memoryCacheMTProtoList.CacheTested(mtProtoParseBag.ToList());
                return mtProtoParseBag.ToList();
            }
            catch (OperationCanceledException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<MtProtoParse>();
            }
            finally
            { 
                _semaphore.Release();
            }
        }

    }
}
