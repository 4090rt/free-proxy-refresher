using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.JsonDataModels
{
    public struct JsonLoggingData
    {
        public JsonLogLevelData LogLevel { get; set; }
        public JsonProxySourcesData ProxySources { get; set; }
        public JsonLogRetention LogReterningDay { get; set; }
        public JsonStrategyMailKit StrategyMailKit { get; set; }
        public JsonPingToServiceURLData PingToSerivceURL { get; set; }
        public PDFilepath PDFilepath { get; set; }
    }
}