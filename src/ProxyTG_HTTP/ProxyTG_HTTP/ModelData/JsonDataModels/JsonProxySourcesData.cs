using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.JsonDataModels
{
    public struct JsonProxySourcesData
    {
        public string MTProto { get; set; }
        public string HTTP { get; set; }
        public string Socks5 { get; set; }
    }
}