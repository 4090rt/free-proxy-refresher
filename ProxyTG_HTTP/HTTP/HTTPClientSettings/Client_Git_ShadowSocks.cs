using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using SimpleW.Observability;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HTTPClientSettings
{
    public class Client_Git_ShadowSocks
    {
        private readonly ILogger<Client_Git_ShadowSocks> _logger;
        private readonly string _gitUrl = "https://raw.githubusercontent.com/10ium/V2Hub3/refs/heads/main/Split/Normal/shadowsocks";

        public void ClientShadow(IServiceCollection serviceDescriptors)
        {
            try
            {
                var clientsh = serviceDescriptors.AddHttpClient("Client_Git_ShadowSocks", client =>
                {
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("zip, deflate, br");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)" +
                        " AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

                    client.BaseAddress = new Uri(_gitUrl);

                    client.DefaultRequestVersion = HttpVersion.Version20;
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                });
                clientsh.AddResilienceHandler("shadow1", Fbuilder =>
                {
                    Fbuilder.AddTimeout(new HttpTimeoutStrategyOptions
                    {
                        Timeout = TimeSpan.FromMinutes(5),
                        OnTimeout = args =>
                        {
                            _logger.LogWarning($"⏰ Таймаут: {args.Timeout}");
                            return ValueTask.CompletedTask;
                        }
                    });

                    Fbuilder.AddRetry(new HttpRetryStrategyOptions
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
                clientsh.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
                {
                    EnableMultipleHttp2Connections = true,

                    PooledConnectionIdleTimeout = TimeSpan.FromHours(12),
                    PooledConnectionLifetime = TimeSpan.FromHours(24),

                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                    | DecompressionMethods.Brotli,

                    UseCookies = false,
                    AllowAutoRedirect = true,
                    MaxConnectionsPerServer = 5,
                    MaxAutomaticRedirections = 10
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Возникло исключение {ex.Message} в Client_GIT_ShawodSocks{ex.StackTrace}" + DateTime.UtcNow);
            }
        }
    }
}
