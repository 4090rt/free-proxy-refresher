using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HttpGetProxys
{
    public interface HttpGetPRoxysFabric
    {
        public Task<List<ProxyData>> HttpClients();
    }

    public class HttpClient_Git_MTProto : HttpGetPRoxysFabric
    {
        public RequestMTProto _requestMTProto;

        public HttpClient_Git_MTProto(RequestMTProto requestMTProto) => _requestMTProto = requestMTProto;

        public async Task<List<ProxyData>> HttpClients()
        {
            var client = "Client_GIT_MTProto";

            StrategyClass strategyClass = new StrategyClass(_requestMTProto);
            List<ProxyData> list = await strategyClass.MainMethod(client).ConfigureAwait(false);

            return list;
        }
    }

    public class HttpClient_Git_Http: HttpGetPRoxysFabric
    {
        public RequestHttp _requestHttp;

        public HttpClient_Git_Http(RequestHttp requestMTProto) => _requestHttp = requestMTProto;

        public async Task<List<ProxyData>> HttpClients()
        {
            var client = "Client_GIT_HTTP";

            StrategyClass strategyClass = new StrategyClass(_requestHttp);
            List<ProxyData>  list = await strategyClass.MainMethod(client).ConfigureAwait(false);

            return list;
        }
    }

    public class Fabric_GIT
    {
        public RequestHttp _requestHttp;
        public RequestMTProto _requestMTProto;

        public Fabric_GIT(RequestHttp requestHttp, RequestMTProto requestMTProto)
        { 
            _requestHttp = requestHttp;
            _requestMTProto = requestMTProto;
        }

        public  HttpGetPRoxysFabric HttpGetPRoxysFabric(string type_client)
        {
            return type_client.ToLower() switch
            {
                "mtproto" => new HttpClient_Git_MTProto(_requestMTProto),
                "http" => new HttpClient_Git_Http(_requestHttp),
                _ => throw new ArgumentException($"Неизвестный тип прокси: {type_client}")
            };
        }
    }
}
