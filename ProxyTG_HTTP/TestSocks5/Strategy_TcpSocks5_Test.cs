using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ModelData.JsonPtoxyTest;
using ProxyTG_HTTP.TestSocks5.CheckTcpTunnel;
using ProxyTG_HTTP.TestSocks5.CheckWebProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestSocks5
{
     public interface Strategy_TcpSocks5_Test
     {
        public Task<TesteProxyModelCunt> TestMethod(CancellationToken token = default);
     }

    public class UserWebProxy : Strategy_TcpSocks5_Test
    {
        private readonly TestCicle _testCicle;
        private readonly ILogger<UserWebProxy> _logger;

        public UserWebProxy(TestCicle testCicle, ILogger<UserWebProxy> logger)
        {
            _logger = logger;
            _testCicle = testCicle;
        }

        public async Task<TesteProxyModelCunt> TestMethod(CancellationToken token = default)
        {
            try
            {
                return await _testCicle.TestMethod(token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new TesteProxyModelCunt { Failed = true };
            }
        }
    }

    public class UseTcpTunnel : Strategy_TcpSocks5_Test
    {
        private readonly ILogger<UseTcpTunnel> _logger;
        private readonly TestCircle _testCircle;

        public UseTcpTunnel(ILogger<UseTcpTunnel> logger, TestCircle testCircle)
        {
            _logger = logger;
            _testCircle = testCircle;
        }
        public async Task<TesteProxyModelCunt> TestMethod(CancellationToken token = default)
        {
            try
            {
                return await _testCircle.Circle(token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new TesteProxyModelCunt { Failed = true };
            }
        }
    }

    public class StrategyCLassTcp
    {
        Strategy_TcpSocks5_Test _Strategy_TcpSocks5_Test;

        public StrategyCLassTcp (Strategy_TcpSocks5_Test Strategy_TcpSocks5_Test)
            => _Strategy_TcpSocks5_Test = Strategy_TcpSocks5_Test;

        public void SetstrategyTcp(Strategy_TcpSocks5_Test Strategy_TcpSocks5_Tests)
            => _Strategy_TcpSocks5_Test = Strategy_TcpSocks5_Tests;

        public async Task<TesteProxyModelCunt> TestMethod(CancellationToken token = default) => await _Strategy_TcpSocks5_Test.TestMethod(token).ConfigureAwait(false);
    }
}
