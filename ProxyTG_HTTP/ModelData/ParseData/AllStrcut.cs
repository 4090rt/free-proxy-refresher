using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.ParseData
{
    public struct AllStrcut
    {
        public IReadOnlyList<MtProtoParse> listMTPRoto { get; set; }
        public IReadOnlyList<HttpParse> listSocks5 { get; set; }
    }
}
