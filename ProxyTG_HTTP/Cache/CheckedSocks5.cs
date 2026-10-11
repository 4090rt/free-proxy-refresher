using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;

namespace ProxyTG_HTTP.Cache
{
    public class CheckedSocks5
    {
        public IReadOnlyList<HttpParse> Proxies { get; set; } = new List<HttpParse>().AsReadOnly();

        public DateTime CheckedAtUtc { get; set; }
    }
}
