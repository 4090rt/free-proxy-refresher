using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.LogSaveClass;
using ProxyTG_HTTP.ExceptionBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HttpGetProxys.HttpandHttps
{
    public class GetProxyHttp
    {
        private readonly ILogger<GetProxyHttp> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LogSave _logSave;

        public GetProxyHttp(ILogger<GetProxyHttp> logger, IHttpClientFactory httpClientFactory, LogSave logSave)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _logSave = logSave;
        }

        public async Task<ReadOnlyMemory<byte>> RequestHTTP(string client, CancellationToken cancellation)
        {
            try
            {
                if (string.IsNullOrEmpty(client))
                    return new ReadOnlyMemory<byte>();

                using HttpClient httpClient = _httpClientFactory.CreateClient(client);

                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync("", cancellation).ConfigureAwait(false);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    ReadOnlyMemory<byte> readOnlyMemory = await httpResponseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                    if (readOnlyMemory.IsEmpty)
                        return new ReadOnlyMemory<byte>();

                    // _logger.LogInformation($"Получен список прокси от {clientHttp}: {readOnlyMemory.Length} байт");
                    await _logSave.SaveLog($"Получен список прокси от {client}: {readOnlyMemory.Length} байт", DateTime.UtcNow.ToString());

                    return readOnlyMemory;
                }
                else
                {
                    // _logger.LogWarning($"Не удалось получить прокси от {clientHttp}: статус {responseMessage.StatusCode}");
                    await _logSave.SaveLog($"Не удалось получить прокси от {client}: статус {httpResponseMessage.StatusCode}", DateTime.UtcNow.ToString());
                    return new ReadOnlyMemory<byte>();
                }
            }
            catch (OperationCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                HttpException.LogError(ex, _logger);
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
