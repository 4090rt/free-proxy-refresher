using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HTTPClientSettings
{
    public class Client_GIT_HTTPS
    {
        private readonly ILogger<Client_GIT_HTTPS> _logger;

        public Client_GIT_HTTPS(ILogger<Client_GIT_HTTPS> logger) => _logger = logger;

        public void ClientHttps(IServiceCollection serviceDescriptors)
        {
            try
            {
                var clientbuilder = serviceDescriptors.AddHttpClient("clientHttps", client =>
                {
                    client.DefaultRequestHeaders.Accept.ParseAdd("aplication/json");
                    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("zip, deflate, br");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                    client.Timeout = Timeout.InfiniteTimeSpan;

                    var url = ReadedJson.ReadAndDeserializeJson.MethodJsonSync<JsonDatStruct>();
                    client.BaseAddress = new Uri(url.Logging.ProxySources.Https);

                    client.DefaultVersionPolicy = System.Net.Http.HttpVersionPolicy.RequestVersionOrHigher;
                    client.DefaultRequestVersion = System.Net.HttpVersion.Version20;
                });
                clientbuilder.AddResilienceHandler("resiliencePolicy", clientbuilderRes =>
                {
                    clientbuilderRes.AddTimeout(new HttpTimeoutStrategyOptions
                    {
                        Timeout = TimeSpan.FromMinutes(3),
                        OnTimeout = args =>
                        {
                            _logger.LogWarning($"⏰ Таймаут: {args.Timeout}");
                            return ValueTask.CompletedTask;
                        }
                    });

                    clientbuilderRes.AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
                    {
                        Delay = TimeSpan.FromSeconds(7),
                        MaxDelay = TimeSpan.FromSeconds(30),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        MaxRetryAttempts = 5,
                        OnRetry = args =>
                        {
                            _logger.LogWarning(
                                    $"🔁 Попытка #{args.AttemptNumber} через {args.RetryDelay}. " +
                                    $"Причина: {args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString()}");
                            return ValueTask.CompletedTask;
                        }
                    });
                });
                clientbuilder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,

                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,

                    PooledConnectionIdleTimeout = TimeSpan.FromHours(12),
                    PooledConnectionLifetime = TimeSpan.FromHours(24),

                    UseCookies = false,
                    UseProxy = true,
                    AllowAutoRedirect = true,

                    MaxAutomaticRedirections = 15,
                    MaxConnectionsPerServer = 10,
                });
            }
            catch(Exception ex)
            {
                _logger.LogError($"Возникло исключение {ex.Message} в Client_GIT_SOCKS5 {ex.StackTrace}" + DateTime.UtcNow);
            }
        }
    }
}
