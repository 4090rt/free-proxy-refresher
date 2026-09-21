using Microsoft.Extensions.Logging;
using MimeKit;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.ReadedJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.MailKit
{
    public class MailKitClientYandex
    {
        private readonly ILogger<MailKitClientYandex> _logger;

        public MailKitClientYandex(ILogger<MailKitClientYandex> logger)
        {
            _logger = logger;
        }

        public async Task SendMail(List<LogModel> logModels, string? attachmentPath = null)
        {
            try
            {
                string smtpHost = "smtp.yandex.ru";
                int port = 587;

                var persejson = await ReadAndDeserializeJson.MethodJson<JsonDatStruct>().ConfigureAwait(false);

                string username = persejson.Logging.StrategyMailKit.Mail;
                string password = persejson.Logging.StrategyMailKit.Password;

                var message = new MimeMessage();

                message.From.Add(new MailboxAddress("LogProxy", username));
                message.To.Add(MailboxAddress.Parse(persejson.Logging.StrategyMailKit.recipientsemail));
                message.Subject = "Logs";

                var html = "<table border='1'><tr><th>Log</th><th>Date</th><tr>";

                foreach (var item in logModels)
                {
                    html += $"<tr><td>{item.LogText}</td><td>{item.LogDate}</td><tr>";
                }

                html += "</table>";

                var builder = new BodyBuilder();
                builder.HtmlBody = html;

                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                    builder.Attachments.Add(attachmentPath);

                message.Body = builder.ToMessageBody();

                using (var client = new global::MailKit.Net.Smtp.SmtpClient())
                {
                    try
                    {
                        using var cts = new CancellationTokenSource();

                        await client.ConnectAsync(smtpHost, port, global::MailKit.Security.SecureSocketOptions.StartTls, cts.Token).ConfigureAwait(false);

                        await client.AuthenticateAsync(username, password, cts.Token).ConfigureAwait(false);

                        await client.SendAsync(message, cts.Token).ConfigureAwait(false);

                        await client.DisconnectAsync(false, cts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ExceptionLog.LogError(ex, _logger);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
            }
        }        
    }
}
