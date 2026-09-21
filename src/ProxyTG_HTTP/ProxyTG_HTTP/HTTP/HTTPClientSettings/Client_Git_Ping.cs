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
using System.Net.Http.Headers;
using System.Text;
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

                var result = ReadAndDeserializeJson.MethodJsonSync<JsonDatStruct>();
                client.BaseAddress = new Uri(result.Logging.PingToSerivceURL.GIT);

                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            });
            clientdescript.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromMinutes(1),
                    Polly.Timeout.TimeoutStrategy.Pessimistic,
                    onTimeoutAsync: (outcome, timespan, task) =>
                    {
                        WarningAndInfoLog.LogWarning($"Таймаут запроса пинга после {timespan}", _logger);
                        return Task.CompletedTask;
                    }
                ));
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
            clientdescript.AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, retrycount =>
            TimeSpan.FromSeconds(Math.Pow(2, retrycount)) +
            TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
            onRetry: (outcome, timespan, retrycount, context) =>
            {
                WarningAndInfoLog.LogWarning($"Повтор запроса пинга #{retrycount} через {timespan}", _logger);
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
