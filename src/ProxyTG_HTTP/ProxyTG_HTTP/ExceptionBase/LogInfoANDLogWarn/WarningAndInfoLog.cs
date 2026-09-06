using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ExceptionBase.LogInfoANDLogWarn
{
    public static class WarningAndInfoLog
    {
        public static void LogWarning(string message, ILogger logger)
        {
            logger.LogWarning($"⚠️ {message}");
        }

        public static void LogInfo(string message, ILogger logger)
        {
            logger.LogInformation($"ℹ️ {message}");
        }
    }
}
