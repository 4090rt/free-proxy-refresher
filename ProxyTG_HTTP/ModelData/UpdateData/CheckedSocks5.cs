using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;

namespace ProxyTG_HTTP.ModelData.UpdateData
{
    public class CheckedSocks5
    {
        public IReadOnlyList<MtpParse> Proxies { get; set; } = new List<MtpParse>().AsReadOnly();

        public DateTime CheckedAtUtc { get; set; }
    }
}
