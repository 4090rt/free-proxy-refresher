using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HttpGetProxys.MtprotoandSocks5
{
    public interface MTTPROTOandSOCKS5HttpGetPRoxysFabric
    {
        public Task<List<ProxyData>> HttpClients(CancellationToken cancellationToken);
    }

    public class HttpClient_Git_MTProto : MTTPROTOandSOCKS5HttpGetPRoxysFabric
    {
        public RequestMTProto _requestMTProto;

        public HttpClient_Git_MTProto(RequestMTProto requestMTProto) => _requestMTProto = requestMTProto;

        public async Task<List<ProxyData>> HttpClients(CancellationToken cancellationToken)
        {
            var client = "Client_GIT_MTProto";

            StrategyClass strategyClass = new StrategyClass(_requestMTProto);
            List<ProxyData> list = await strategyClass.MainMethod(client, cancellationToken).ConfigureAwait(false);

            return list;
        }
    }

    public class HttpClient_Git_Socks5 : MTTPROTOandSOCKS5HttpGetPRoxysFabric
    {
        public RequestSocks5 _requestSocks5;

        public HttpClient_Git_Socks5(RequestSocks5 requestSocks5) => _requestSocks5 = requestSocks5;

        public async Task<List<ProxyData>> HttpClients(CancellationToken cancellationToken)
        {
            var client = "Client_GIT_SOCKS5";

            StrategyClass strategyClass = new StrategyClass(_requestSocks5);
            List<ProxyData> list = await strategyClass.MainMethod(client,cancellationToken).ConfigureAwait(false);

            return list;
        }
    }

    public class Fabric_GIT
    {
        public RequestMTProto _requestMTProto;
        public RequestSocks5 _requestSocks5;

        public Fabric_GIT(RequestMTProto requestMTProto, RequestSocks5 requestSocks5)
        { 
            _requestMTProto = requestMTProto;
            _requestSocks5 = requestSocks5;
        }

        public  MTTPROTOandSOCKS5HttpGetPRoxysFabric HttpGetPRoxysFabric(string type_client)
        {
            return type_client.ToLower() switch
            {
                "mtproto" => new HttpClient_Git_MTProto(_requestMTProto),
                "socks5" => new HttpClient_Git_Socks5(_requestSocks5),
                _ => throw new ArgumentException($"Неизвестный тип прокси: {type_client}")
            };
        }
    }
}
