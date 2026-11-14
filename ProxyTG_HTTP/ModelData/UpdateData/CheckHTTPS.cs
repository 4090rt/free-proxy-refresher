using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.UpdateData
{
    public class CheckHTTPS
    {
        public IReadOnlyList<HttpsParse> Proxies { get; set; } = new List<HttpsParse>().AsReadOnly();
        public DateTime CheckedAtUtc { get; set; }
    }
}
