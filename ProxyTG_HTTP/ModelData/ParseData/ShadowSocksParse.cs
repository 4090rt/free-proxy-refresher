using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.ParseData
{
    public struct ShadowSocksParse
    {
        [JsonPropertyName("Host")]
        public string? ServerHost { get; set; }
        [JsonPropertyName("Port")]
        public int? ServerPort { get; set; }
        [JsonPropertyName("Method")]
        public string? MethodServer { get; set;}
        [JsonPropertyName("Password")]
        public string? ServerPassword { get; set; }
    }
}
