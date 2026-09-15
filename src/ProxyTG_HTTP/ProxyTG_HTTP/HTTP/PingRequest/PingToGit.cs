using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.LogSaveClass;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.ModelData.PingData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.PingRequest
{
    public class PingToGit
    {
        private readonly ILogger<PingToGit> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LogSave _logSave;

        public PingToGit(ILogger<PingToGit> logger, IHttpClientFactory httpClientFactory, LogSave logSave)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _logSave = logSave;
        }

        public async Task<DataPing> RequestPing()
        {
            try
            { 
                _logger.LogInformation("Замер пинга до GIT");
                await _logSave.SaveLog("Замер пинга до GIT", DateTime.UtcNow.ToString());

                HttpClient client = _httpClientFactory.CreateClient("Client_Git_Ping");

                using var cts = new CancellationTokenSource();

                var timer = System.Diagnostics.Stopwatch.StartNew();

                using HttpResponseMessage responseMessage = await client.GetAsync("", cts.Token).ConfigureAwait(false);
                timer.Stop();
                if (responseMessage.IsSuccessStatusCode)
                {
                    var status = responseMessage.StatusCode;

                    var ping = timer.ElapsedMilliseconds / 2;

                    var hosttext = System.IO.File.ReadAllText("appsettings.json");
                    var hostgit = JsonSerializer.Deserialize<JsonDatStruct>(hosttext).Logging.PingToSerivceURL;

                    _logger.LogInformation($"Пинг до GIT: {ping} ms, статус {status}");
                    await _logSave.SaveLog($"Пинг до GIT: {ping} ms, статус {status}", DateTime.UtcNow.ToString());

                    return new DataPing
                    {
                        Host =  hostgit.GIT,
                        PingMs = ping,
                        Status = status.ToString(),
                        Error = "-"
                    };
                }
                else
                {
                    var status = responseMessage.StatusCode;

                    var hosttext = System.IO.File.ReadAllText("appsettings.json");
                    var hostgit = JsonSerializer.Deserialize<JsonDatStruct>(hosttext).Logging.PingToSerivceURL;

                    _logger.LogWarning($"Пинг до GIT: неуспешен, статус {status}");
                    await _logSave.SaveLog($"Пинг до GIT: неуспешен, статус {status}", DateTime.UtcNow.ToString());

                    return new DataPing
                    {
                        Host = hostgit.GIT,
                        PingMs = -1,
                        Status = status.ToString(),
                        Error = "-"
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
