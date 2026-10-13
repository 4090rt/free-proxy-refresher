using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.ReadedJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Resilience;

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

                    client.Timeout = Timeout.InfiniteTimeSpan;

                    var result = ReadAndDeserializeJson.MethodJsonSync<JsonDatStruct>();
                    client.BaseAddress = new Uri(result.Logging.ProxySources.MTProto);

                    client.DefaultRequestVersion = HttpVersion.Version20;
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                });
                clientBuilder.AddResilienceHandler("mtproto", builder =>
                {
                    builder.AddTimeout(TimeSpan.FromMinutes(5));

                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = int.MaxValue, // колво попыток
                        BackoffType = DelayBackoffType.Exponential, // увелечение хадержек между попытками
                        Delay = TimeSpan.FromSeconds(1), // задержка
                        MaxDelay = TimeSpan.FromSeconds(30),// макс задержка
                        UseJitter = true, // джиттер
                        OnRetry = args => //лог
                        {
                            _logger.LogWarning(
                                $"🔁 Попытка #{args.AttemptNumber} через {args.RetryDelay}. " +
                                $"Причина: {args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString()}");
                            return ValueTask.CompletedTask;
                        }
                    });

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
