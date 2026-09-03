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
    public class Client_GIT_HTTP
    {
        private readonly ILogger<Client_GIT_HTTP> _logger;

        public Client_GIT_HTTP(ILogger<Client_GIT_HTTP> logger) => _logger = logger;

        public void Client_SettingsGit_HTTP(IServiceCollection serviceDescriptors)
        {
            var clientbuilder = serviceDescriptors.AddHttpClient("Client_GIT_HTTP", client =>
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("zip, deflate, br");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                client.BaseAddress = new Uri("https://raw.githubusercontent.com/proxmint/free-proxy-list/main/proxies/socks5.txt");

                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            });
            clientbuilder.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                 TimeSpan.FromMinutes(0.30),
                 Polly.Timeout.TimeoutStrategy.Pessimistic,
                 onTimeoutAsync: (context, timespan, task) =>
                 {
                     _logger.LogWarning($"⏰ Request timed out after {timespan} from Client_1" + DateTime.UtcNow);
                     return Task.CompletedTask;
                 }
            ));
            clientbuilder.AddTransientHttpErrorPolicy(polly => polly.CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onHalfOpen: () =>
                {
                    Console.WriteLine("✅ Circuit reset");
                },
                onBreak: (outcome, timespan) =>
                {
                    Console.WriteLine($"🔌 Circuit opened for {timespan}");
                },
                onReset: () =>
                {
                    Console.WriteLine("⚠️ Circuit half-open");
                }));
            clientbuilder.AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, retrycount =>
            TimeSpan.FromSeconds(Math.Pow(2, retrycount)) +
            TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
            onRetry: (outcome, timespan, retrycount, context) =>
            {
                 Console.WriteLine($"🔄 Retry {retrycount} after {timespan}");
            }));
            clientbuilder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
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
    }
}
