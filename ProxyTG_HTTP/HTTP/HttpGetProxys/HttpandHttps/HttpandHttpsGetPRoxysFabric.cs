using ProxyTG_HTTP.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HttpGetProxys.HttpandHttps
{
    public interface HttpandHttpsGetPRoxysFabric
    {
        public Task<IReadOnlyList<ProxyRecord>> CreateHttpRequest(CancellationToken cancellationToken);
    }

    public class CreateHttp : HttpandHttpsGetPRoxysFabric
    {
        public HttpRequest _httpRequest;

        public CreateHttp(HttpRequest httpRequest) => _httpRequest = httpRequest;

        const string client = "ClientHttp";

        public async Task<IReadOnlyList<ProxyRecord>> CreateHttpRequest(CancellationToken cancellationToken)
        {
            HttpRequestStrategy httpRequest = new HttpRequestStrategy(_httpRequest);
            return await httpRequest.MainMethod(client, cancellationToken).ConfigureAwait(false);
        }
    }

    public class CreateHttps : HttpandHttpsGetPRoxysFabric
    {
        public HttpsRequest _httpsRequest;
        public CreateHttps(HttpsRequest httpsRequest) => _httpsRequest = httpsRequest;

        const string client = "clientHttps";

        public async Task<IReadOnlyList<ProxyRecord>> CreateHttpRequest(CancellationToken cancellationToken)
        {
            HttpRequestStrategy httpRequest = new HttpRequestStrategy(_httpsRequest);
            return await httpRequest.MainMethod(client, cancellationToken).ConfigureAwait(false);
        }
    }

    public class FabriClassHttp
    {
        public readonly HttpRequest _createHttp;
        public readonly HttpsRequest _createHttps;

        public FabriClassHttp(HttpRequest createHttp, HttpsRequest createHttps)
        { 
            _createHttp = createHttp;
            _createHttps = createHttps;
        }

        public HttpandHttpsGetPRoxysFabric FabricMethod(string type_client)
        {
            return type_client.ToLower() switch
            {
                "http" => new CreateHttp(_createHttp),
                "https" => new CreateHttps(_createHttps)
            };
        }
    }
}
