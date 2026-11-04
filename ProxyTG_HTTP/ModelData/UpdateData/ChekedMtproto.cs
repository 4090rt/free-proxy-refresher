using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.UpdateData
{
    public class ChekedMtproto
    {
        public IReadOnlyList<MtProtoParse> Proxies { get; set; } = new List<MtProtoParse>().AsReadOnly();

        public DateTime CheckedAtUtc { get; set; }
    }
}
