using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ExceptionBase
{
    public static class HttpException
    {
        public static void LogError(HttpRequestException ex, ILogger logger)
        {
            try
            {
                logger.LogError($"❌ Исключение: {ex.Message}");
                logger.LogError($"📚 StackTrace: {ex.StackTrace}");
                logger.LogError($"📎 Внутреннее исключение: {ex.InnerException?.Message ?? "Нет"}");
            }
            catch (Exception exx)
            {
                throw new Exception("Исключение в ExceptionBase" + ex.Message);
            }
        }
    }
}
