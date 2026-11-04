using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.TestSocks5.CheckTcpTunnel;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestShadowsocks
{
    public class TestShadowTunnelCreate
    {
        private readonly ILogger<TestShadowTunnelCreate> _logger;
        private string _UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

        private static readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;

        public TestShadowTunnelCreate(ILogger<TestShadowTunnelCreate> logger)
        {
            _logger = logger;
        }

        public async Task<bool> Test(ShadowSocksParse shadowSocksParse, CancellationToken cancellationToken, string targetHost = "httpbin.org", int targetPort = 80)
        {
            // MVP (Фаза 0): проверяем, что прокси доступен по TCP.
            // Полноценный ShadowSocks AEAD-handshake — Фаза 2.2.
            if (shadowSocksParse.ServerHost is null || shadowSocksParse.ServerPort is null)
                return false;

            if (!int.TryParse(shadowSocksParse.ServerPort, out int port) || port <= 0 || port > 65535)
                return false;

            TcpClient? tcpClient = null;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(7));
            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(shadowSocksParse.ServerHost, port, cts.Token).ConfigureAwait(false);
                return tcpClient.Connected;
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
                tcpClient?.Dispose();
            }
        }
    }
}
