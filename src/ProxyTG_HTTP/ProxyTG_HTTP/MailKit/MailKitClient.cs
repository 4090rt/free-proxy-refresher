using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.ReadedJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.MailKit
{
    public class MailKitClient
    {
        private readonly ILogger<MailKitClient> _logger;

        public MailKitClient(ILogger<MailKitClient> logger)
        {
            _logger = logger;
        }

        public async Task SendMail(List<LogModel> logModels, string? attachmentpath = null)
        {
            try
            {
                string smtpHost = "smtp.gmail.com";
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

                if (!string.IsNullOrEmpty(attachmentpath) && File.Exists(attachmentpath))
                    builder.Attachments.Add(attachmentpath);

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
