using ProxyTG_HTTP.Cache;
using ProxyTG_HTTP.ModelData.ParseData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.ControllerStatsModel
{
    public struct ControllerStats
    {
        public int ProxiesAll { get; set; }
        public int ProxiesTested { get; set; }
        public DateTime DateTime { get; set; }
    }
}
