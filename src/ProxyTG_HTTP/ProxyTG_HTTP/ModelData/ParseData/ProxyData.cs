using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.ParseData
{
    public enum ProxyType
    {
        Http,
        MTProto
    }

    public struct ProxyData
    {
        public ProxyType Type { get; set; }
        public string Server { get; set; }
        public string Port { get; set; }
        public string? Secret { get; set; }
    }
}