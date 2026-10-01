using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;

namespace ProxyTG_HTTP.Cache
{
    /// <summary>
    /// Проверенный список SOCK5-прокси вместе с временем самой проверки.
    /// </summary>
    public class CheckedSocks5
    {
        public List<HttpParse> Proxies { get; set; } = new List<HttpParse>();

        public DateTime CheckedAtUtc { get; set; }
    }
}
