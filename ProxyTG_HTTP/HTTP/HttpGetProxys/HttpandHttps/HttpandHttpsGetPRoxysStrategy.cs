using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.HTTP.HttpGetProxys.HttpandHttps
{
    public interface HttpandHttpsGetPRoxysStrategy
    {
        public Task<IReadOnlyList<ProxyRecord>> RequestHTTP(string client, CancellationToken cancellation);
    }

    public class HttpRequest : HttpandHttpsGetPRoxysStrategy
    {
        public GetProxyHttp _getProxyHttp;
        public ParseHttpandHttps _ParseHttpandHttps;

        public HttpRequest(GetProxyHttp getProxyHttp, ParseHttpandHttps  parseHttpandHttps)
        {
            _getProxyHttp = getProxyHttp;
            _ParseHttpandHttps = parseHttpandHttps;
        }

        public async Task<IReadOnlyList<ProxyRecord>> RequestHTTP(string client, CancellationToken cancellation)
        {
            var result = await _getProxyHttp.RequestHTTP(client, cancellation).ConfigureAwait(false);
            return await _ParseHttpandHttps.ParseMthod<HttpParse>(result, cancellation).ConfigureAwait(false);
        }
    }

    public class HttpsRequest : HttpandHttpsGetPRoxysStrategy
    {
        public GetProxyHttp _getProxyHttps;
        public ParseHttpandHttps _ParseHttpandHttps;
        public HttpsRequest(GetProxyHttp getProxyHttps, ParseHttpandHttps parseHttpandHttps)
        {
            _getProxyHttps = getProxyHttps;
            _ParseHttpandHttps = parseHttpandHttps;
        }
        public async Task<IReadOnlyList<ProxyRecord>> RequestHTTP(string client, CancellationToken cancellation)
        {
            var result = await _getProxyHttps.RequestHTTP(client, cancellation).ConfigureAwait(false);
            return await _ParseHttpandHttps.ParseMthod<HttpsParse>(result, cancellation).ConfigureAwait(false);
        }
    }

    public class HttpRequestStrategy
    {
        HttpandHttpsGetPRoxysStrategy _HttpandHttpsGetPRoxysStrategy;

        public HttpRequestStrategy(HttpandHttpsGetPRoxysStrategy HttpandHttpsGetPRoxysStrategy) => _HttpandHttpsGetPRoxysStrategy = HttpandHttpsGetPRoxysStrategy;

        public void SetStrategy(HttpandHttpsGetPRoxysStrategy HttpandHttpsGetPRoxysStrategy) => _HttpandHttpsGetPRoxysStrategy = HttpandHttpsGetPRoxysStrategy;

        public async Task<IReadOnlyList<ProxyRecord>> MainMethod(string client, CancellationToken cancellationToken)
            => await _HttpandHttpsGetPRoxysStrategy.RequestHTTP(client, cancellationToken).ConfigureAwait(false);
    }
}
