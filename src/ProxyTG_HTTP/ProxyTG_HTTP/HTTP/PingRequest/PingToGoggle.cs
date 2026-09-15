using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.LogSaveClass;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.ModelData.PingData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.PingRequest
{
    public class PingToGoggle
    {
        private readonly ILogger<PingToGoggle> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LogSave _logSave;

        public PingToGoggle(ILogger<PingToGoggle> logger, IHttpClientFactory httpClientFactory, LogSave logSave)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _logSave = logSave;
        }

        public async Task<DataPing> RequestPing()
        {
            try
            {
                _logger.LogInformation("Замер пинга до Google");
                await _logSave.SaveLog("Замер пинга до Google", DateTime.UtcNow.ToString());

                HttpClient client = _httpClientFactory.CreateClient("Client_Google");

                using var cts = new CancellationTokenSource();  

                var timer = System.Diagnostics.Stopwatch.StartNew();

                using HttpResponseMessage response = await client.GetAsync("generate_204", cts.Token).ConfigureAwait(false);
                timer.Stop();
                if (response.IsSuccessStatusCode)
                {
                    var status = response.StatusCode;

                    var ping = timer.ElapsedMilliseconds / 2;

                    var hosttext = System.IO.File.ReadAllText("appsettings.json");
                    var hostgoogle = JsonSerializer.Deserialize<JsonDatStruct>(hosttext).Logging.PingToSerivceURL;

                    _logger.LogInformation($"Пинг до Google: {ping} ms, статус {status}");
                    await _logSave.SaveLog($"Пинг до Google: {ping} ms, статус {status}", DateTime.UtcNow.ToString());

                    return new DataPing
                    {
                        Host = hostgoogle.Google,
                        PingMs = ping,
                        Status = status.ToString(),
                        Error = "-"
                    };
                }
                else
                {
                    var status = response.StatusCode;

                    var hosttext = System.IO.File.ReadAllText("appsettings.json");
                    var hostgoogle = JsonSerializer.Deserialize<JsonDatStruct>(hosttext).Logging.PingToSerivceURL;

                    _logger.LogWarning($"Пинг до Google: неуспешен, статус {status}");
                    await _logSave.SaveLog($"Пинг до Google: неуспешен, статус {status}", DateTime.UtcNow.ToString());

                    return new DataPing
                    {
                        Host = hostgoogle.Google,
                        PingMs = -1,
                        Status = status.ToString(),
                        Error = "Error"
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                HttpException.LogError(ex, _logger);
                return new DataPing();
            }
            catch (InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, _logger);
                return new DataPing();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new DataPing();
            }
        }
    }
}
