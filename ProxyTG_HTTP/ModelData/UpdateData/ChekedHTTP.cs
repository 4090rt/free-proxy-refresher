using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.UpdateData
{
    public class ChekedHTTP
    {
        public IReadOnlyList<HttpParse> Proxies { get; set; } = new List<HttpParse>().AsReadOnly();

        public DateTime CheckedAtUtc { get; set; }
    }
}
