using ProxyTG_HTTP.ModelData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.MailKit
{
    public interface MailKitStrategyFactory
    {
        public Task Strategy(List<LogModel> logModels, string? attachmentPath = null);
    }

    public class StrategyGoogle: MailKitStrategyFactory
    {
        MailKitClient _mailKitClient;

        public StrategyGoogle(MailKitClient mailKitClient)
        {
            _mailKitClient = mailKitClient;
        }

        public async Task Strategy(List<LogModel> logModels, string? attachmentPath = null)
        {
            await _mailKitClient.SendMail(logModels, attachmentPath).ConfigureAwait(false);
        }
    }

    public class StrategyYandex : MailKitStrategyFactory
    {
        MailKitClientYandex _MailKitClientYandex;

        public StrategyYandex(MailKitClientYandex mailKitClientYandex)
        {
            _MailKitClientYandex = mailKitClientYandex;
        }

        public async Task Strategy(List<LogModel> logModels, string? attachmentPath = null)
        {
            await _MailKitClientYandex.SendMail(logModels, attachmentPath).ConfigureAwait(false);
        }
    }

    public static class FactoryClass
    {
        public static MailKitStrategyFactory MethodFactory(string strategy, MailKitClient mailKitClient, MailKitClientYandex mailKitClientYande)
        {
            return strategy?.ToLower() switch
            {
                "google" => new StrategyGoogle(mailKitClient),
                "yandex" => new StrategyYandex(mailKitClientYande),
                _ => throw new InvalidOperationException($"Неверная стратегия отправки почты: {strategy}")
            };
        }
    }
}
