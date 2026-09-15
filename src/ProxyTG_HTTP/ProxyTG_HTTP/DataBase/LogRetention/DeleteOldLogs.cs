using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.AddLog;
using ProxyTG_HTTP.DataBase.DbPath;
using ProxyTG_HTTP.DataBase.LogSaveClass;
using ProxyTG_HTTP.DataBase.PoolSQLiteConnection;
using ProxyTG_HTTP.ExceptionBase;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.DataBase.LogRetention
{
    public class DeleteOldLogs
    {
        private readonly ILogger<DeleteOldLogs> _logger;
        private readonly PoolSQLite _poolSQLite;
        private readonly DBPathCLass _dbPathCLass;
        private readonly LogSave _logSave;

        public DeleteOldLogs(ILogger<DeleteOldLogs> logger, PoolSQLite poolSQLite, DBPathCLass dbPathCLass, LogSave logSave)
        {
            _logger = logger;
            _dbPathCLass = dbPathCLass;
            _poolSQLite = poolSQLite;
            _logSave = logSave;
        }

        public async Task<bool> LogsDeleteMethod(int day)
        {
            SQLiteConnection connection = new SQLiteConnection();
            SQLiteTransaction sQLiteTransaction = null;
            try
            {
                connection = _poolSQLite.ConnectionOpen();
                sQLiteTransaction = connection.BeginTransaction();

                var cutoffDate = DateTime.UtcNow.AddDays(-day).ToString("yyyy-MM-dd HH:mm:ss");
                string comand = "DELETE FROM LogBase WHERE Date < @cutoffDate";

                int result;
                await using (SQLiteCommand command = new SQLiteCommand(comand, connection, sQLiteTransaction))
                {
                    command.Parameters.AddWithValue("@cutoffDate", cutoffDate);
                    result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                await sQLiteTransaction.CommitAsync().ConfigureAwait(false);

                _logger.LogInformation($"Очистка логов старше {day} дн.: удалено {result} записей");
                await _logSave.SaveLog($"Очистка логов старше {day} дн.: удалено {result} записей", DateTime.UtcNow.ToString());

                return result >= 0;
            }
            catch (SQLiteException ex)
            {
                SQLiteExceptionLog.LogError(ex, _logger);

                try
                {
                    if (sQLiteTransaction != null && sQLiteTransaction.Connection != null &&
                        sQLiteTransaction.Connection.State != System.Data.ConnectionState.Open)
                    {
                        await sQLiteTransaction.RollbackAsync().ConfigureAwait(false);
                        _logger.LogInformation("Транзакция успешно откачена");
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError($"Ошибка при откате транзакции: {rollbackEx.Message}", rollbackEx);
                    return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, _logger);
                try
                {
                    if (sQLiteTransaction != null && sQLiteTransaction.Connection != null &&
                        sQLiteTransaction.Connection.State != System.Data.ConnectionState.Open)
                    {
                        await sQLiteTransaction.RollbackAsync().ConfigureAwait(false);
                        _logger.LogInformation("Транзакция успешно откачена");
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError($"Ошибка при откате транзакции: {rollbackEx.Message}", rollbackEx);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);

                try
                {
                    if (sQLiteTransaction != null && sQLiteTransaction.Connection != null &&
                        sQLiteTransaction.Connection.State != System.Data.ConnectionState.Open)
                    {
                        await sQLiteTransaction.RollbackAsync().ConfigureAwait(false);
                        _logger.LogInformation("Транзакция успешно откачена");
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError($"Ошибка при откате транзакции: {rollbackEx.Message}", rollbackEx);
                    return false;
                }
            }
            finally
            {
                if (connection != null)
                {
                    _poolSQLite.ConnectionClose(connection);
                }
                if (sQLiteTransaction != null)
                {
                    sQLiteTransaction.Dispose();
                }
            }
        }
    }
}
