using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.LogSaveClass;
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
        private readonly LogSave _logSave;

        public GetProxys(ILogger<GetProxys> logger, IHttpClientFactory httpClientFactory, LogSave logSave)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _logSave = logSave;
        }

        public async Task<ReadOnlyMemory<byte>> GetMethod(string clientHttp)
        {
            try
            {
                if (string.IsNullOrEmpty(clientHttp))
                    return new ReadOnlyMemory<byte>();

                _logger.LogInformation($"Загрузка списка прокси от {clientHttp}");
                await _logSave.SaveLog($"Загрузка списка прокси от {clientHttp}", DateTime.UtcNow.ToString());

                var client = _httpClientFactory.CreateClient(clientHttp);

                HttpResponseMessage responseMessage = await client.GetAsync("").ConfigureAwait(false);
                if (responseMessage.IsSuccessStatusCode)
                {
                    ReadOnlyMemory<byte> readOnlyMemory = await 
                        responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                    if (readOnlyMemory.IsEmpty)
                        return new ReadOnlyMemory<byte>();

                    _logger.LogInformation($"Получен список прокси от {clientHttp}: {readOnlyMemory.Length} байт");
                    await _logSave.SaveLog($"Получен список прокси от {clientHttp}: {readOnlyMemory.Length} байт", DateTime.UtcNow.ToString());

                    return readOnlyMemory;
                }
                else
                {
                    ReadOnlyMemory<byte> readOnlyMemory = await
                         responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                    _logger.LogWarning($"Не удалось получить прокси от {clientHttp}: статус {responseMessage.StatusCode}");
                    await _logSave.SaveLog($"Не удалось получить прокси от {clientHttp}: статус {responseMessage.StatusCode}", DateTime.UtcNow.ToString());

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
