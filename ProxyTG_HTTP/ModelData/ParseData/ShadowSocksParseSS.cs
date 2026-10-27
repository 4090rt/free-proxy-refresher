using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.ParseData
{
    public class ShadowSocksParseSS
    {
        [JsonPropertyName("SS")]
        public string? SS { get; set; }
    }
}
