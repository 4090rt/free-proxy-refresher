using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.LogSaveClass;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.HTTP.HttpGetProxys;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.ModelData.PingData;
using ProxyTG_HTTP.ReadedJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static TerraPDF.Helpers.Color;

namespace ProxyTG_HTTP.HTTP.PingRequest
{
    public class PingToYandex
    {
        private readonly ILogger<PingToYandex> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LogSave _logSave;
        private readonly string _clientName = "Client_Yandex_Ping";

        public PingToYandex(ILogger<PingToYandex> logger, IHttpClientFactory httpClientFactory, LogSave logSave)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _logSave = logSave;
        }

        public async Task<DataPing> Requestping(CancellationToken cancellation)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);

                using HttpClient cleint = _httpClientFactory.CreateClient(_clientName);

                var timer = System.Diagnostics.Stopwatch.StartNew();

                HttpResponseMessage httpResponseMessage = await cleint.GetAsync("", cts.Token).ConfigureAwait(false);

                timer.Stop();
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    var ping = timer.ElapsedMilliseconds / 2;

                    var statuscode = httpResponseMessage.StatusCode;

                    var hostyandex= (await ReadAndDeserializeJson.MethodJson<JsonDatStruct>().ConfigureAwait(false)).Logging.PingToSerivceURL;

                    return new DataPing
                    {
                        Host = hostyandex.Yandex,
                        PingMs = ping,
                        Status = statuscode.ToString(),
                        Error = "-"
                    };
                }
                else
                {
                    _logger.LogWarning("Ответ пуст");
                    return new DataPing();
                }
            }
            catch (HttpRequestException ex)
            {
                HttpException.LogError(ex, _logger);
                return new DataPing();
            }
            catch (OperationCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new DataPing();
            }
        }
    }
}
