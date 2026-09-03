using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HTTPClientSettings
{
    public class Client_GIT_MTProto
    {
        private readonly ILogger<Client_GIT_MTProto> _logger;

        public Client_GIT_MTProto(ILogger<Client_GIT_MTProto> logger) => _logger = logger;

        public void Client_SettingsGit(IServiceCollection serviceDescriptors)
        {
            try
            {
                var clientBuilder = serviceDescriptors.AddHttpClient("Client_GIT_MTProto", client =>
                {
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("zip, deflate, br");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                    client.BaseAddress = new Uri("https://raw.githubusercontent.com/SoliSpirit/mtproto/master/all_proxies.txt");

                    client.DefaultRequestVersion = HttpVersion.Version20;
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                });
                clientBuilder.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromMinutes(0.30),
                    Polly.Timeout.TimeoutStrategy.Pessimistic,
                    onTimeoutAsync:(context, timespan, task) =>
                    {
                        _logger.LogWarning($"⏰ Request timed out after {timespan} from Client_1" + DateTime.UtcNow);
                        return Task.CompletedTask;
                    }));
                clientBuilder.AddTransientHttpErrorPolicy(polly => polly.CircuitBreakerAsync
                (handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onHalfOpen: () =>
                {
                    _logger.LogWarning("⚠️ Circuit half-open from Client_1" + DateTime.UtcNow);
                },
                onBreak: (outcome, timespan) =>
                {
                    _logger.LogWarning($"🔌 Circuit opened for {timespan} from Client_1" + DateTime.UtcNow);
                },
                onReset: () =>
                {
                    _logger.LogWarning("✅ Circuit reset from Client_1" + DateTime.UtcNow);
                }));
                clientBuilder.AddTransientHttpErrorPolicy(pollicy => pollicy.WaitAndRetryAsync(3, retryCount =>
                TimeSpan.FromSeconds(Math.Pow(2, retryCount)) +
                TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                onRetryAsync: (outcome, timespan, retrycount, task) =>
                {
                    _logger.LogWarning($"⏰ Request timed out after {timespan} from Client_1" + DateTime.UtcNow);
                    return Task.CompletedTask;
                }));
                clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,

                    PooledConnectionIdleTimeout = TimeSpan.FromHours(12),
                    PooledConnectionLifetime = TimeSpan.FromHours(24),

                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,

                    UseCookies = false,
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 15,
                    MaxConnectionsPerServer = 10,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Возникло исключение {ex.Message} в Client_1 {ex.StackTrace}" + DateTime.UtcNow);
                return;
            }
        }
    }
}
