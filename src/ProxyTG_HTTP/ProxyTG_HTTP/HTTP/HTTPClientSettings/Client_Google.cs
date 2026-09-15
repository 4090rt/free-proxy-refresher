using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HTTPClientSettings
{
    public class Client_Google
    {
        private readonly ILogger<Client_Google> _logger;

        public Client_Google(ILogger<Client_Google> logger) => _logger = logger;

        public void Client_SettingsGoogle(IServiceCollection serviceDescriptors)
        {
            var client = serviceDescriptors.AddHttpClient("Client_Google", client =>
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("zip, deflate, br");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var urlValue = File.ReadAllText("appsettings.json");
                var result = JsonSerializer.Deserialize<JsonDatStruct>(urlValue).Logging.PingToSerivceURL;
                client.BaseAddress = new Uri(result.Google);

                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

            })
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromMinutes(0.30),
                Polly.Timeout.TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    Console.WriteLine($"⏰ Request timed out after {timespan}");
                    return Task.CompletedTask;
                }
            ))
            .AddTransientHttpErrorPolicy(polly => polly.CircuitBreakerAsync(
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
                }))
            .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, retrycount =>
            TimeSpan.FromSeconds(Math.Pow(2, retrycount)) +
            TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
            onRetry: (outcome, timespan, retrycount, context) =>
            {
                Console.WriteLine($"🔄 Retry {retrycount} after {timespan}");
            }))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
            {
                EnableMultipleHttp2Connections = true,

                AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.Deflate | DecompressionMethods.GZip,

                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),
                PooledConnectionLifetime = TimeSpan.FromMinutes(20),

                UseCookies = false,

                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,

                MaxConnectionsPerServer = 10,
            });
        }
    }
}
