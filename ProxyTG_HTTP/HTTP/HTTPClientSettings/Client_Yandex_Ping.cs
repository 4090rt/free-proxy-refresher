using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using ProxyTG_HTTP.ExceptionBase.LogInfoANDLogWarn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HTTPClientSettings
{
    public class Client_Yandex_Ping
    {
        private readonly ILogger<Client_Yandex_Ping> _logger;

        private readonly string _url = "https://raw.githubusercontent.com/10ium/V2Hub3/refs/heads/main/Split/Normal/shadowsocks";

        public void ClientYandex(IServiceCollection serviceDescriptors)
        {
            try
            {
                var clientdescript = serviceDescriptors.AddHttpClient("Client_Yandex_Ping", client =>
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)" +
                        " AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
                    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("zip, deflate, br");
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

                    client.Timeout = Timeout.InfiniteTimeSpan;

                    client.BaseAddress = new Uri(_url);

                    client.DefaultRequestVersion = HttpVersion.Version20;
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                });
                clientdescript.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromMinutes(5),
                    Polly.Timeout.TimeoutStrategy.Pessimistic,
                    onTimeoutAsync: async (Outcome, TimeSpan, task) =>
                    {
                        WarningAndInfoLog.LogWarning($"Таймаут запроса пинга после {TimeSpan}", _logger);
                        await Task.CompletedTask;
                    }));
                clientdescript.AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromMinutes(1),
                    onBreak: (outcome, timespan) =>
                    {
                        WarningAndInfoLog.LogWarning($"Circuit открыт на {timespan}", _logger);
                    },
                    onHalfOpen: () =>
                    {
                        WarningAndInfoLog.LogInfo("Circuit half-open", _logger);
                    },
                    onReset: () =>
                    {
                        WarningAndInfoLog.LogInfo("Circuit reset", _logger);
                    }));
                clientdescript.AddTransientHttpErrorPolicy(polly => polly.WaitAndRetryAsync(3, retrycount =>
                TimeSpan.FromSeconds(Math.Pow(2, retrycount)) +
                TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                onRetryAsync: async (outcome, timespan, retrycount, context) =>
                {
                    WarningAndInfoLog.LogWarning($"Повтор запроса пинга #{retrycount} через {timespan}", _logger);
                    await Task.CompletedTask;
                }));
                clientdescript.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
                {
                    EnableMultipleHttp2Connections = true,

                    PooledConnectionIdleTimeout = TimeSpan.FromHours(12),
                    PooledConnectionLifetime = TimeSpan.FromHours(24),

                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                        | DecompressionMethods.Brotli,

                    UseCookies = false,
                    UseProxy = false,
                    AllowAutoRedirect = false,

                    MaxConnectionsPerServer = 5
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Возникло исключение {ex.Message} в Client_Yendex_Ping {ex.StackTrace}" + DateTime.UtcNow);
            }
        }
    }
}
