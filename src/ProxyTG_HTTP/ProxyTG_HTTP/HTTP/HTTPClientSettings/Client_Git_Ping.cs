using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HTTPClientSettings
{
    public class Client_Git_Ping
    {
        private readonly ILogger<Client_Git_Ping> _logger;

        public Client_Git_Ping(ILogger<Client_Git_Ping> logger) => _logger = logger;

        public void Client_SettingsGit_Ping(IServiceCollection serviceDescriptors)
        {
            var clientdescript = serviceDescriptors.AddHttpClient("Client_Git_Ping", client =>
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("zip, deflate, br");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var urlValue = File.ReadAllText("appsettings.json");
                var result = JsonSerializer.Deserialize<JsonDatStruct>(urlValue).Logging.PingToSerivceURL;
                client.BaseAddress = new Uri(result.GIT);

                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            });
            clientdescript.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromMinutes(0.30),
                    Polly.Timeout.TimeoutStrategy.Pessimistic,
                    onTimeoutAsync: (outcome, timespan, task) =>
                    {
                        Console.WriteLine($"⏰ Request timed out after {timespan}");
                        return Task.CompletedTask;
                    }
                ));
            clientdescript.AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (outcome, timespan) =>
                {
                    Console.WriteLine($"🔌 Circuit opened for {timespan}");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("⚠️ Circuit half-open");
                },
                onReset: () =>
                {
                    Console.WriteLine("✅ Circuit reset");
                }));
            clientdescript.AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, retrycount =>
            TimeSpan.FromSeconds(Math.Pow(2, retrycount)) +
            TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
            onRetry: (outcome, timespan, retrycount, context) =>
            {
                Console.WriteLine($"🔄 Retry {retrycount} after {timespan}");
            }));
            clientdescript.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
            {
                EnableMultipleHttp2Connections = true,

                AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.Deflate | DecompressionMethods.GZip,

                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),
                PooledConnectionLifetime = TimeSpan.FromMinutes(20),

                UseCookies = false,

                MaxConnectionsPerServer = 10,

                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 15,
            });
        }
    }
}
