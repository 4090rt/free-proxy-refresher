using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.DbPath;
using ProxyTG_HTTP.DataBase.GetAllLogsRequest;
using ProxyTG_HTTP.DataBase.PoolSQLiteConnection;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.DataBase
{
    public class AddNewLogs
    {
        private readonly ILogger<AddNewLogs> _logger;
        private readonly PoolSQLite _poolSQLite;
        private readonly DBPathCLass _dbpath;

        public AddNewLogs(ILogger<AddNewLogs> logger, PoolSQLite poolSQLite, DBPathCLass dBPathCLass)
        {
            _logger = logger;
            _dbpath = dBPathCLass;
            _poolSQLite = poolSQLite;
        }

        public async Task AddLOgs(LogModel logModel)
        {
            SQLiteConnection connection = null;
            SQLiteTransaction sQLiteTransaction = null;
            try
            {
                if (string.IsNullOrEmpty(logModel.LogText) || string.IsNullOrEmpty(logModel.LogDate))
                    return;

                connection = _poolSQLite.ConnectionOpen();
                sQLiteTransaction = connection.BeginTransaction();

                string command = "INSERT INTO  LogBase (LogText, Date) VALUES (@T, @D)";

                await using (SQLiteCommand sQLiteCommand = new SQLiteCommand(command, connection, sQLiteTransaction))
                {
                    sQLiteCommand.Parameters.AddWithValue("@T", logModel.LogText);
                    sQLiteCommand.Parameters.AddWithValue("@D", logModel.LogDate);

                    await sQLiteCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    await sQLiteTransaction.CommitAsync().ConfigureAwait(false);
                }
            }
            catch (SQLiteException ex)
            {
                SQLiteExceptionLog.LogError(ex, _logger);
                try
                {
                    if (sQLiteTransaction != null && sQLiteTransaction.Connection != null
                        && sQLiteTransaction.Connection.State != System.Data.ConnectionState.Open)
                    {
                        await sQLiteTransaction.RollbackAsync().ConfigureAwait(false);
                        _logger.LogInformation("Транзакция успешно откачена");
                    }
                    else
                        return;
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError($"Ошибка при откате транзакции: {rollbackEx.Message}", rollbackEx);
                }
            }
            catch (InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, _logger);
                try
                {

                }
                catch (Exception rollbackEx)
                {
                    if (sQLiteTransaction != null && sQLiteTransaction.Connection != null
                        && sQLiteTransaction.Connection.State != System.Data.ConnectionState.Open)
                    {
                        await sQLiteTransaction.RollbackAsync().ConfigureAwait(false);
                        _logger.LogInformation("Транзакция успешно откачена");
                    }
                    else
                        return;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                try
                {
                    if (sQLiteTransaction != null && sQLiteTransaction.Connection != null
                        && sQLiteTransaction.Connection.State != System.Data.ConnectionState.Open)
                    {
                        await sQLiteTransaction.RollbackAsync().ConfigureAwait(false);
                        _logger.LogInformation("Транзакция успешно откачена");
                    }
                    else
                        return;
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError($"Ошибка при откате транзакции: {rollbackEx.Message}", rollbackEx);
                }
            }
            finally
            {
                if (connection != null)
                {
                    connection.Dispose();
                }
                if (sQLiteTransaction != null)
                {
                    sQLiteTransaction.Dispose();
                }
            }
        }
    }
}
