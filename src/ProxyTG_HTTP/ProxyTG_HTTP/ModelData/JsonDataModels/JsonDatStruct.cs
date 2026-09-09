using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.JsonDataModels
{
    public struct JsonDatStruct
    {
        public JsonLogLevelData LogLevel { get; set; }
        public JsonUriData Uri { get; set; }
        public JsonLogRetention Retention {get;set;}
        public JsonStrategyMailKit MailKit { get; set; }
    }
}
