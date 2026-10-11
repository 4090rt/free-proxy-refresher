using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ModelData.JsonPtoxyTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestSocks5
{
    public interface Fabric_TcpSocks5_Test
    {
        public Task<TesteProxyModelCunt> TestFactoryMethod(CancellationToken token = default);
    }

    public class MetodWebProxy: Fabric_TcpSocks5_Test 
    {
        private readonly UserWebProxy _userwebproxy;
        private readonly ILogger<MetodWebProxy> _logger;
        public MetodWebProxy(UserWebProxy userwebproxy, ILogger<MetodWebProxy> logger)
        {
            _userwebproxy = userwebproxy;
            _logger = logger;
        }
        public async Task<TesteProxyModelCunt> TestFactoryMethod(CancellationToken token = default)
        {
            try
            {
                return await _userwebproxy.TestMethod(token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new TesteProxyModelCunt { Failed = true };
            }
        }
    }

    public class MetodTcpTunnel : Fabric_TcpSocks5_Test
    {
        private readonly ILogger<MetodTcpTunnel> _logger;
        private readonly UseTcpTunnel _useTcpTunnel;

        public MetodTcpTunnel(ILogger<MetodTcpTunnel> logger, UseTcpTunnel useTcpTunnel)
        {
            _logger = logger;
            _useTcpTunnel = useTcpTunnel;
        }
        public async Task<TesteProxyModelCunt> TestFactoryMethod(CancellationToken token = default)
        {
            try
            {
                return await _useTcpTunnel.TestMethod(token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new TesteProxyModelCunt { Failed = true };
            }
        }
    }

    public class ProxyTestFactory
    {
        private readonly ILogger<MetodTcpTunnel> _logger;
        private readonly UseTcpTunnel _useTcpTunnel;

        private readonly UserWebProxy _userwebproxy;
        private readonly ILogger<MetodWebProxy> _loggerweb;

        public ProxyTestFactory(ILogger<MetodTcpTunnel> logger, UseTcpTunnel useTcpTunnel,
            UserWebProxy userwebproxy, ILogger<MetodWebProxy> loggerweb)
        {
            _logger = logger;
            _useTcpTunnel = useTcpTunnel;
            _userwebproxy = userwebproxy;
            _loggerweb = loggerweb;
        }

        public Fabric_TcpSocks5_Test FactoryMethod(int strategy)
        {
            return strategy switch
            {
                1 => new MetodWebProxy(_userwebproxy, _loggerweb),
                2 => new MetodTcpTunnel(_logger, _useTcpTunnel),
                _ => throw new InvalidOperationException($"Неверный номер метода проверки: {strategy}")
            };
        }

        // выбор по содержимому TestProxy_Settings.json
        public Fabric_TcpSocks5_Test FactoryMethod(string method)
        {
            return method switch
            {
                ReadedJson.JsonWriteTcpTests.HttpMethod => FactoryMethod(1),
                ReadedJson.JsonWriteTcpTests.TcpMethod => FactoryMethod(2),
                _ => throw new InvalidOperationException($"Неизвестный метод проверки: {method}")
            };
        }
    }
}
