using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HttpGet
{
    public class GetProxys
    {
        private readonly ILogger<GetProxys> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public GetProxys(ILogger<GetProxys> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ReadOnlyMemory<byte>> GetMethod(string clientHttp)
        {
            try
            {
                if (string.IsNullOrEmpty(clientHttp))
                    return new ReadOnlyMemory<byte>();

                var client = _httpClientFactory.CreateClient(clientHttp);

                HttpResponseMessage responseMessage = await client.GetAsync("").ConfigureAwait(false);
                if (responseMessage.IsSuccessStatusCode)
                {
                    ReadOnlyMemory<byte> readOnlyMemory = await 
                        responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                    if (readOnlyMemory.IsEmpty)
                        return new ReadOnlyMemory<byte>();

                    return readOnlyMemory;
                }
                else
                {
                    ReadOnlyMemory<byte> readOnlyMemory = await
                         responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                    if (readOnlyMemory.IsEmpty)
                        return new ReadOnlyMemory<byte>();

                    return readOnlyMemory;
                }

            }
            catch (HttpRequestException ex)
            {
                HttpException.LogError(ex,_logger);
                return new ReadOnlyMemory<byte>();
            }
            catch (InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, _logger);
                return new ReadOnlyMemory<byte>();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new ReadOnlyMemory<byte>();
            }
        }
    }
}
