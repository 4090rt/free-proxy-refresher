using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.AddLog;
using ProxyTG_HTTP.DataBase.DbPath;
using ProxyTG_HTTP.DataBase.PoolSQLiteConnection;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.DataBase.LogSaveClass
{
    public class LogSave
    {
        private readonly ILogger<LogSave> _logger;
        private readonly PoolSQLite _poolSQLite;
        private readonly DBPathCLass _dbPathCLass;
        private readonly AddNewLogs _addNewLogs;

        public LogSave(ILogger<LogSave> logger, PoolSQLite poolSQLite, DBPathCLass dbPathCLass, AddNewLogs addNewLogs)
        {
            _logger = logger;
            _dbPathCLass = dbPathCLass;
            _poolSQLite = poolSQLite;
            _addNewLogs = addNewLogs;
        }

        public async Task SaveLog(string log, string Date)
        {
            try
            {
                LogModel logModel;

                if (string.IsNullOrEmpty(log) || string.IsNullOrEmpty(Date))
                    return;

                logModel = new LogModel
                {
                    LogDate = Date,
                    LogText = log
                };

                bool result = await _addNewLogs.AddLOgs(logModel).ConfigureAwait(false);

                if (result)
                {
                    _logger.LogInformation("Успешно сохранено");
                }
                else
                {
                    return;
                }
            }
            catch (InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, _logger);
                return;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return;
            }
        }
    }
}
