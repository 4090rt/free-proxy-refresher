using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Polly;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Net;

namespace ProxyTG_HTTP.TestSocks5.CheckWebProxy
{
    public class TestSocksClient
    {
        private readonly ILogger<TestSocksClient> _logger;

        public TestSocksClient(ILogger<TestSocksClient> logger)
        {
            _logger = logger;
        }

        public HttpClient TestClient(HttpParse p)
        {
            try
            {
                var handler = new SocketsHttpHandler
                {
                    UseProxy = true,
                    Proxy = new WebProxy(new Uri($"socks5://{p.IP}:{p.Port}")),
                    ConnectTimeout = TimeSpan.FromSeconds(7),

                    MaxConnectionsPerServer = 1,
                    UseCookies = false,
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 5,
                    AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    EnableMultipleHttp2Connections = false
                };

                var policy = Policy.TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromSeconds(7),
                    Polly.Timeout.TimeoutStrategy.Pessimistic,
                    onTimeoutAsync: async (context, timespan, task) =>
                    {
                        _logger.LogWarning($"⏰ Request timed out after {timespan} from Client_1" + DateTime.UtcNow);
                        await Task.CompletedTask;
                    });

                var client = new HttpClient(new PolicyHttpMessageHandler(policy)
                {
                    InnerHandler = handler
                });

                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");

                client.DefaultRequestVersion = HttpVersion.Version11;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

                return client;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                throw;
            }
        }
    }
}
