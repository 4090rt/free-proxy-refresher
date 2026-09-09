using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.GetAllLogsRequest;
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

        public SendLogToMail(ILogger<SendLogToMail> logger, AllLogsRequest allLogsRequest, MailKitClientYandex mailKitClientyan, MailKitClient mailKitClient)
        {
            _logger = logger;
            _allLogsRequest = allLogsRequest;
            _mailKitClientyan = mailKitClientyan;
            _mailKitClient = mailKitClient;
        }

        public async Task ToSendMail()
        {
            var json = File.ReadAllText("appsettings.json");
            var persejson = JsonSerializer.Deserialize<JsonDatStruct>(json);

            if (string.IsNullOrEmpty(persejson.MailKit.ToString()))
                return;

            string strategy = persejson.MailKit.ToString();
            try
            {
                var resultinBd = await _allLogsRequest.AllLogs().ConfigureAwait(false);

                if (resultinBd.Count != 0)
                {
                    FactoryClass.MethodFactory(strategy, _mailKitClient, _mailKitClientyan);
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
