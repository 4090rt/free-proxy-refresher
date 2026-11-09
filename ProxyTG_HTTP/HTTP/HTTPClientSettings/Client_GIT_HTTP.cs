using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.ReadedJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HTTPClientSettings
{
    public class Client_GIT_HTTP
    {
        private readonly ILogger<Client_GIT_HTTP> _logger;

        public Client_GIT_HTTP(ILogger<Client_GIT_HTTP> logger) => _logger = logger;

        public void ClientHTTP(IServiceCollection serviceDescriptors)
        {
            try
            {
                var clientbuilder = serviceDescriptors.AddHttpClient("ClientHttp", client =>
                {
                    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("zip, deflate, br");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

                    client.Timeout = Timeout.InfiniteTimeSpan;

                    var url = ReadAndDeserializeJson.MethodJsonSync<JsonDatStruct>();
                    client.BaseAddress = new Uri(url.Logging.ProxySources.Http);

                    client.DefaultRequestVersion = System.Net.HttpVersion.Version20;
                    client.DefaultVersionPolicy = System.Net.Http.HttpVersionPolicy.RequestVersionOrHigher;
                });
                clientbuilder.AddResilienceHandler("http", builder =>
                {
                    builder.AddTimeout(new HttpTimeoutStrategyOptions
                    {
                        Timeout = TimeSpan.FromMinutes(3),
                        OnTimeout = args =>
                        {
                            _logger.LogWarning($"⏰ Таймаут: {args.Timeout}");
                            return ValueTask.CompletedTask;
                        }
                    });

                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        Delay = TimeSpan.FromSeconds(5),
                        MaxDelay = TimeSpan.FromSeconds(30),
                        UseJitter = true,
                        MaxRetryAttempts = 5,
                        BackoffType = DelayBackoffType.Exponential,
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

                    MaxAutomaticRedirections = 15,
                    AllowAutoRedirect = true,
                    MaxConnectionsPerServer = 10,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Возникло исключение {ex.Message} в Client_GIT_SOCKS5 {ex.StackTrace}" + DateTime.UtcNow);
            }
        }

    }
}
