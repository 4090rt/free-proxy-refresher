using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.ParseData
{
    public struct AllStrcut
    {
        public List<HttpParse> listHTTP { get; set; }
        public List<MtProtoParse> listMTPRoto { get; set; }
        public List<HttpParse> listSocks5 { get; set; }
    }
}
