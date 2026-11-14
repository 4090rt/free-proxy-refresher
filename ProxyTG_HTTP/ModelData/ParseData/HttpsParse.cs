using ProxyTG_HTTP.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.ParseData
{
    public record HttpsParse: ProxyRecord
    {
        [JsonPropertyName("HostHttps")]
        public string? Host { get; set; }
        [JsonPropertyName("PortHttps")]
        public string? Port { get; set; }
        [JsonPropertyName("DateGiveHttps")]
        public string? Date { get; set; }
    }
}
