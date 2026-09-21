using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using ProxyTG_HTTP.ExceptionBase.LogInfoANDLogWarn;
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

                var result = ReadAndDeserializeJson.MethodJsonSync<JsonDatStruct>();
                client.BaseAddress = new Uri(result.Logging.PingToSerivceURL.Google);

                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

            })
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromMinutes(1),
                Polly.Timeout.TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    WarningAndInfoLog.LogWarning($"Таймаут запроса к Google после {timespan}", _logger);
                    return Task.CompletedTask;
                }
            ))
            .AddTransientHttpErrorPolicy(polly => polly.CircuitBreakerAsync(
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
                }))
            .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, retrycount =>
            TimeSpan.FromSeconds(Math.Pow(2, retrycount)) +
            TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
            onRetry: (outcome, timespan, retrycount, context) =>
            {
                WarningAndInfoLog.LogWarning($"Повтор запроса к Google #{retrycount} через {timespan}", _logger);
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
