using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.UpdateData
{
    public class ChekedShadow
    {
        public IReadOnlyList<ShadowSocksParse> Proxies { get; set; } = new List<ShadowSocksParse>().AsReadOnly();
        public DateTime CheckedAtUtc { get; set; }
    }
}
