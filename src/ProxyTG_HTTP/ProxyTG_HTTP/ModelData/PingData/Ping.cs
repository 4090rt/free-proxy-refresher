using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.PingData
{
    public struct DataPing
    {
        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("ping")]
        public long PingMs { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }
    }
}
