using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.JsonDataModels
{
    public struct JsonLogLevelData
    {
        public string Default { get; set; }
        [JsonPropertyName("Microsoft.AspNetCore")]
        public string Microsoft { get; set; }
    }
}