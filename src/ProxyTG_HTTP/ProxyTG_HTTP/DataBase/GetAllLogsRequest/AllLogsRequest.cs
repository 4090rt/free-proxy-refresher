using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.DbPath;
using ProxyTG_HTTP.DataBase.PoolSQLiteConnection;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.DataBase.GetAllLogsRequest
{
    public class AllLogsRequest
    {
        private readonly ILogger<AllLogsRequest> _logger;
        private readonly PoolSQLite _poolSQLite;
        private readonly DBPathCLass _dbpath;

        public AllLogsRequest(ILogger<AllLogsRequest> logger, PoolSQLite poolSQLite, DBPathCLass dBPathCLass)
        {
            _logger = logger;
            _dbpath = dBPathCLass;
            _poolSQLite = poolSQLite;
        }

        public async Task<List<LogModel>> AllLogs(LogModel logmodel)
        {
            SQLiteConnection connection = null;
            List<LogModel> listLogs = new List<LogModel>();
            try
            {
                connection = _poolSQLite.ConnectionOpen();

                string command = "SELECT * FROM LogBase";

                await using (SQLiteCommand sQLiteCommand = new SQLiteCommand(command, connection))
                {
                    await using (var result = await sQLiteCommand.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (result != null)
                        {
                            var log = result.GetOrdinal("LogText");
                            var date = result.GetOrdinal("Date");

                            while (await result.ReadAsync().ConfigureAwait(false))
                            {
                                new LogModel
                                {
                                    LogText = result.IsDBNull(log) ? string.Empty : log.ToString(),
                                    LogDate = result.IsDBNull(date) ? string.Empty : date.ToString()
                                };
                                listLogs.Add(logmodel);
                            }
                            return listLogs;
                        }
                        else
                            return new List<LogModel>();
                    }
                }
            }
            catch (SQLiteException ex)
            {
                SQLiteExceptionLog.LogError(ex, _logger);
                return new List<LogModel>();
            }
            catch (InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, _logger);
                return new List<LogModel>();
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return new List<LogModel>();
            }
            finally
            {
                if (connection != null)
                {
                    connection.Dispose();
                }
            }
        }
    }
}
