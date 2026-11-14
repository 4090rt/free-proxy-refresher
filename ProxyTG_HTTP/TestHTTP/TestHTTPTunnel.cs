using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestHTTP
{
    public class TestHTTPTunnel
    {
        private readonly ILogger<TestHTTPTunnel> _logger;
        private string _UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
        private readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;

        public TestHTTPTunnel(ILogger<TestHTTPTunnel> logger) => _logger = logger;

        public async Task<bool> TunnelCreate(HttpParse httpParse, CancellationToken cancellationToken, string targetHost = "httpbin.org", int targetPort = 80)
        {
            byte[] buffer = arrayPool.Rent(256);
            TcpClient? tcpClient = null;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(7));

            try
            {
                if (string.IsNullOrWhiteSpace(httpParse.Host) ||
                    !int.TryParse(httpParse.Port, out int proxyPort) ||
                    proxyPort < 1 || proxyPort > 65535)
                    return false;

                    tcpClient = new TcpClient();

                await tcpClient.ConnectAsync(httpParse.Host, int.Parse(httpParse.Port), cts.Token);

                Stream stream = tcpClient.GetStream();

                byte[] packet = CreatePacket(httpParse.Host, httpParse.Port);

                await stream.WriteAsync(packet).ConfigureAwait(false);

                int readed = await stream.ReadAsync(buffer.AsMemory(0, 256), cts.Token).ConfigureAwait(false);

                if (readed <= 0)
                    return false;

                string head = Encoding.ASCII.GetString(buffer, 0, readed);

                return head.StartsWith("HTTP/", StringComparison.Ordinal) && head.Contains(" 200");

            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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
