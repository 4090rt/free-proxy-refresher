using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.ReadedJson;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestSocks5.CheckWebProxy
{
    public class TestSocks5WebProxy
    {
        private readonly ILogger<TestSocks5WebProxy> _logger;
        private readonly TestSocksClient _client;
        private readonly string _testUrl;

        public TestSocks5WebProxy(ILogger<TestSocks5WebProxy> logger, TestSocksClient client)
        {
            _logger = logger;
            _client = client;

            JsonDatStruct config = ReadAndDeserializeJson.MethodJsonSync<JsonDatStruct>();
            _testUrl = config.Logging.TestProxyUrl.Url;
        }

        public async Task<bool> TestProxy(HttpParse httpParse, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(_testUrl))
                {
                    _logger.LogError("В appsettings.json не задан Logging.TestProxyUrl.Url");
                    return false;
                }

                using var clienthttp = _client.TestClient(httpParse);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10));

                using HttpResponseMessage responseMessage = await clienthttp
                    .GetAsync(_testUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                    .ConfigureAwait(false);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Прокси {httpParse.IP}:{httpParse.Port} мёртв, статус {responseMessage.StatusCode}");
                    return false;
                }

                _logger.LogInformation($"Прокси {httpParse.IP}:{httpParse.Port} жив, статус {(int)responseMessage.StatusCode}");
                return true;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return false;
            }
        }
    }
}
