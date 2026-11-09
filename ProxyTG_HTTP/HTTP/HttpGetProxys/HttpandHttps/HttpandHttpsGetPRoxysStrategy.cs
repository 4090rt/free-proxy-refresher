using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HttpGetProxys.HttpandHttps
{
    public interface HttpandHttpsGetPRoxysStrategy
    {
        public Task<ReadOnlyMemory<byte>> RequestHTTP(string client, CancellationToken cancellation);
    }

    public class HttpRequest : HttpandHttpsGetPRoxysStrategy
    {
        public GetProxyHttp _getProxyHttp;

        public HttpRequest(GetProxyHttp getProxyHttp) => _getProxyHttp = getProxyHttp;

        public async Task<ReadOnlyMemory<byte>> RequestHTTP(string client, CancellationToken cancellation)
        {
            return await _getProxyHttp.RequestHTTP(client, cancellation);
        }
    }

    public class HttpsRequest : HttpandHttpsGetPRoxysStrategy
    {
        public GetProxyHttp _getProxyHttps;
        public HttpsRequest(GetProxyHttp getProxyHttps) => _getProxyHttps = getProxyHttps;
        public async Task<ReadOnlyMemory<byte>> RequestHTTP(string client, CancellationToken cancellation)
        {
            return await _getProxyHttps.RequestHTTP(client, cancellation);
        }
    }

    public class HttpRequestStrategy
    {
        HttpandHttpsGetPRoxysStrategy _HttpandHttpsGetPRoxysStrategy;

        public HttpRequestStrategy(HttpandHttpsGetPRoxysStrategy HttpandHttpsGetPRoxysStrategy) => _HttpandHttpsGetPRoxysStrategy = HttpandHttpsGetPRoxysStrategy;

        public void SetStrategy(HttpandHttpsGetPRoxysStrategy HttpandHttpsGetPRoxysStrategy) => _HttpandHttpsGetPRoxysStrategy = HttpandHttpsGetPRoxysStrategy;

        public async Task<ReadOnlyMemory<byte>> MainMethod(string client, CancellationToken cancellationToken) => await _HttpandHttpsGetPRoxysStrategy.RequestHTTP(client, cancellationToken).ConfigureAwait(false);
    }
}
