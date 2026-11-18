using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.TestHTTP;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
namespace ProxyTG_HTTP.TestHTTPS
{
    public class TestHTTPSTunnel
    {
        private readonly ILogger<TestHTTPSTunnel> _logger;
        private string _UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
        private readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;

        public TestHTTPSTunnel(ILogger<TestHTTPSTunnel> logger) => _logger = logger;

        public async Task<bool> TunnelCreate(HttpsParse httpsParse,CancellationToken cancellation, string targetHost = "httpbin.org", int targetPort = 80)
        {
            if (string.IsNullOrWhiteSpace(httpsParse.Host) ||
                     !int.TryParse(httpsParse.Port, out int proxyPort) ||
                    proxyPort < 1 || proxyPort > 65535)
                return false;

            byte[] buffer = arrayPool.Rent(256);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            cts.CancelAfter(TimeSpan.FromSeconds(7));
            TcpClient tcpClient = null;
            try
            {
                tcpClient = new TcpClient();

                await tcpClient.ConnectAsync(httpsParse.Host, int.Parse(httpsParse.Port), cts.Token);

                await using SslStream sslStream = new SslStream(tcpClient.GetStream(), leaveInnerStreamOpen: false,
                   userCertificateValidationCallback: (_, _, _, _) => true);

                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = httpsParse.Host,
                    EnabledSslProtocols = SslProtocols.None,
                    CertificateRevocationCheckMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck
                }, cts.Token).ConfigureAwait(false);

                byte[] packet = CreatePacket(targetHost, targetPort.ToString());

                await sslStream.WriteAsync(packet).ConfigureAwait(false);

                await sslStream.FlushAsync().ConfigureAwait(false);

                int read = await sslStream.ReadAsync(buffer.AsMemory(0, 256), cts.Token).ConfigureAwait(false);

                if (read <= 0)
                    return false;

                string head = Encoding.ASCII.GetString(buffer, 0, read);

                var statusLine = head.Split("\r\n", 2)[0];
                var parts = statusLine.Split(' ');
                return head.StartsWith("HTTP/", StringComparison.Ordinal)
                       && parts.Length >= 2 && int.TryParse(parts[1], out int code)
                       && code is >= 200 and < 300;
            }
            catch (AuthenticationException ex)
            {
                tcpClient?.Dispose();
                return false;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                tcpClient?.Dispose();
                throw;
            }
            catch (Exception)
            {
                tcpClient?.Dispose();
                return false;
            }
            finally
            {
                tcpClient?.Dispose();
                arrayPool.Return(buffer);
            }
        }

        public byte[] CreatePacket(string host, string port)
        {
            try
            {
                // Валидация порта (опционально, чтобы сохранить строгость оригинального кода)
                if (!int.TryParse(port, out int portNum) || portNum < 1 || portNum > 65535)
                {
                    throw new ArgumentException("Некорректный номер порта", nameof(port));
                }

                // Формируем стандартный HTTP CONNECT запрос.
                // Формат:
                // CONNECT host:port HTTP/1.1\r\n
                // Host: host:port\r\n
                // \r\n
                string request = $"CONNECT {host}:{port} HTTP/1.1\r\nHost: {host}:{port}\r\n\r\n";

                // Переводим строку в массив байтов (для HTTP обычно используется ASCII или UTF-8)
                return Encoding.ASCII.GetBytes(request);
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                throw;
            }
        }
    }
}
