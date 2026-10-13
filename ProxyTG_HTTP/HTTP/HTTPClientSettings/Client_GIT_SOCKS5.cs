using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using ProxyTG_HTTP.ExceptionBase.LogInfoANDLogWarn;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.ReadedJson;
using System;
using System.Net;
using Microsoft.Extensions.Http.Resilience;

namespace ProxyTG_HTTP.HTTP.HTTPClientSettings
{
    public class Client_GIT_SOCKS5
    {
        private readonly ILogger<Client_GIT_SOCKS5> _logger;

        public Client_GIT_SOCKS5(ILogger<Client_GIT_SOCKS5> logger) => _logger = logger;

        public void Client_SettingsGit_SOCKS5(IServiceCollection serviceDescriptors)
        {
            try
            {
                var clientBuilder = serviceDescriptors.AddHttpClient("Client_GIT_SOCKS5", client =>
                {
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("zip, deflate, br");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                    client.Timeout = Timeout.InfiniteTimeSpan;

                    var result = ReadAndDeserializeJson.MethodJsonSync<JsonDatStruct>();
                    client.BaseAddress = new Uri(result.Logging.ProxySources.Socks5);

                    client.DefaultRequestVersion = HttpVersion.Version20;
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                });
                clientBuilder.AddResilienceHandler("socks5", builder =>
                {
                    builder.AddTimeout(new HttpTimeoutStrategyOptions
                    {
                        Timeout = TimeSpan.FromMinutes(5),
                        OnTimeout = args =>
                        {
                            _logger.LogWarning($"⏰ Таймаут: {args.Timeout}");
                            return ValueTask.CompletedTask;
                        }
                    });

                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 5,
                        BackoffType = DelayBackoffType.Exponential,
                        Delay = TimeSpan.FromSeconds(5),
                        MaxDelay = TimeSpan.FromSeconds(30),
                        UseJitter = true,
                        OnRetry = args =>
                        {
                            _logger.LogWarning(
                                 $"🔁 Попытка #{args.AttemptNumber} через {args.RetryDelay}. " +
                                $"Причина: {args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString()}");
                            return ValueTask.CompletedTask;
                        }
                    });
                });
                clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
                {
                    EnableMultipleHttp2Connections = true,

                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,

                    PooledConnectionIdleTimeout = TimeSpan.FromHours(12),
                    PooledConnectionLifetime = TimeSpan.FromHours(24),

                    UseCookies = false,
                    AllowAutoRedirect = true,

                    MaxAutomaticRedirections = 15,
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
