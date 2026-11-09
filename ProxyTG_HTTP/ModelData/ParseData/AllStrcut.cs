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
        public IReadOnlyList<MtpParse> listSocks5 { get; set; }
        public IReadOnlyList<ShadowSocksParse> listShadowSocks { get; set; }
    }
}
