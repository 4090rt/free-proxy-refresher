using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using ProxyTG_HTTP.ExceptionBase.LogInfoANDLogWarn;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.ReadedJson;
using System;
using System.Net;

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

                    var result = ReadAndDeserializeJson.MethodJsonSync<JsonDatStruct>();
                    client.BaseAddress = new Uri(result.Logging.ProxySources.Socks5);

                    client.DefaultRequestVersion = HttpVersion.Version20;
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                });
                clientBuilder.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromMinutes(1),
                    Polly.Timeout.TimeoutStrategy.Pessimistic,
                    onTimeoutAsync: (context, timespan, task) =>
                    {
                        _logger.LogWarning($"⏰ Request timed out after {timespan} from Client_GIT_SOCKS5" + DateTime.UtcNow);
                        return Task.CompletedTask;
                    }));
                clientBuilder.AddTransientHttpErrorPolicy(polly => polly.CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromMinutes(1),
                    onHalfOpen: () =>
                    {
                        WarningAndInfoLog.LogInfo("Cистема CircuitBreaker SOCKS5: half-open", _logger);
                    },
                    onBreak: (outcome, timespan) =>
                    {
                        WarningAndInfoLog.LogWarning($"Cистема CircuitBreaker SOCKS5: circuit opened for {timespan}", _logger);
                    },
                    onReset: () =>
                    {
                        WarningAndInfoLog.LogInfo("Cистема CircuitBreaker SOCKS5: circuit reset", _logger);
                    }));
                clientBuilder.AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, retrycount =>
                    TimeSpan.FromSeconds(Math.Pow(2, retrycount)) +
                    TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                    onRetry: (outcome, timespan, retrycount, context) =>
                    {
                        WarningAndInfoLog.LogWarning($"Повтор запроса SOCKS5 #{retrycount} через {timespan}", _logger);
                    }));
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
