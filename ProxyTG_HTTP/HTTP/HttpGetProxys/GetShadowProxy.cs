using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.LogSaveClass;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HttpGetProxys
{
    public class GetShadowProxy
    {
        private readonly ILogger<GetShadowProxy> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LogSave _logSave;
        private readonly string _clientName = "Client_Git_ShadowSocks";

        public GetShadowProxy(ILogger<GetShadowProxy> logger, IHttpClientFactory httpClientFactory, LogSave logSave)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _logSave = logSave;
        }

        public async Task<IReadOnlyList<ShadowSocksParseSS>> HttpGet(CancellationToken cancellation)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);

                using HttpClient httpClient = _httpClientFactory.CreateClient(_clientName);

                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync("", cts.Token).ConfigureAwait(false);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    if (stream != null)
                    {
                        var jsonser = await JsonSerializer.DeserializeAsync<List<ShadowSocksParseSS>>(stream);

                        if (jsonser != null)
                        return jsonser;
                        else
                            return new List<ShadowSocksParseSS>().AsReadOnly();
                    }
                    else
                    {
                        _logger.LogWarning("Список прокси ShadowSocks - пуст");
                        return new List<ShadowSocksParseSS>().AsReadOnly();
                    }
                }
                else
                {
                    // _logger.LogWarning($"Не удалось получить прокси от {clientHttp}: статус {responseMessage.StatusCode}");
                    await _logSave.SaveLog($"Не удалось получить прокси от {_clientName}: статус {httpResponseMessage.StatusCode}", DateTime.UtcNow.ToString());
                    return new List<ShadowSocksParseSS>().AsReadOnly();
                }
            }
            catch (HttpRequestException ex)
            {
                HttpException.LogError(ex, _logger);
                return new List<ShadowSocksParseSS>().AsReadOnly();
            }
            catch (OperationCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<ShadowSocksParseSS>().AsReadOnly();
            }
        }
    }
}
