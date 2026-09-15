using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.GetAllLogsRequest;
using ProxyTG_HTTP.DataBase.LogSaveClass;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.MailKit;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.SendToMail
{
    public class SendLogToMail
    {
        private readonly ILogger<SendLogToMail> _logger;
        private readonly AllLogsRequest _allLogsRequest;
        private readonly MailKitClient _mailKitClient;
        private readonly MailKitClientYandex _mailKitClientyan;
        private readonly LogSave _logSave;

        public SendLogToMail(ILogger<SendLogToMail> logger, AllLogsRequest allLogsRequest, MailKitClientYandex mailKitClientyan, MailKitClient mailKitClient, LogSave logSave)
        {
            _logger = logger;
            _allLogsRequest = allLogsRequest;
            _mailKitClientyan = mailKitClientyan;
            _mailKitClient = mailKitClient;
            _logSave = logSave;
        }

        public async Task ToSendMail()
        {
            var json = File.ReadAllText("appsettings.json");
            var persejson = JsonSerializer.Deserialize<JsonDatStruct>(json);

            if (string.IsNullOrEmpty(persejson.Logging.StrategyMailKit.Strategy))
                return;
            string? strategy = persejson.Logging.StrategyMailKit.Strategy;
            try
            {
                _logger.LogInformation($"Отправка логов на почту через стратегию: {strategy}");
                await _logSave.SaveLog($"Отправка логов на почту через стратегию: {strategy}", DateTime.UtcNow.ToString());

                var resultinBd = await _allLogsRequest.AllLogs().ConfigureAwait(false);

                if (resultinBd.Count != 0)
                {
                    var sendStrategy = FactoryClass.MethodFactory(strategy, _mailKitClient, _mailKitClientyan);
                    await sendStrategy.Strategy(resultinBd).ConfigureAwait(false);

                    _logger.LogInformation("Запрос на отправку логов на почту выполнен");
                    await _logSave.SaveLog("Запрос на отправку логов на почту выполнен", DateTime.UtcNow.ToString());
                }
                else
                {
                    return;
                }
            }
            catch (InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, _logger);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
            }
        }
    }
}
