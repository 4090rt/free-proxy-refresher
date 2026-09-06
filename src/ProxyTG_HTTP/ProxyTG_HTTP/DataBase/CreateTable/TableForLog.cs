using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.DbPath;
using ProxyTG_HTTP.DataBase.PoolSQLiteConnection;
using ProxyTG_HTTP.ExceptionBase;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.DataBase.CreateTable
{
    public class TableForLog
    {
        private readonly ILogger<TableForLog> _logger;
        private readonly PoolSQLite _poolSQLite;
        private readonly DBPathCLass _dbpath;
        private bool _ischeked = false;

        public TableForLog(ILogger<TableForLog> logger, PoolSQLite poolSQLite, DBPathCLass dbpath)
        {
            _logger = logger;
            _poolSQLite = poolSQLite;
            _dbpath = dbpath;
        }

        public async Task InithializateCreateTable()
        {
            if (_ischeked == true) return;

            if (_ischeked == false)
            {
                bool result = await TableCreate();
                _ischeked = result;
            }
        }

        public async Task<bool> TableCreate()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _poolSQLite.ConnectionOpen();
                string command = "CREATE TABLE IF NOT EXISTS LogBase(" +
                    "LogText, TEXT NOT NULL" +
                    "Date TEXT NOT NULL,";

                await using (SQLiteCommand sQLiteCommand = new SQLiteCommand(command, connection))
                { 
                    int result = await sQLiteCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    bool exec = Convert.ToInt32(result) == 1;
                    return exec;    
                }
            }
            catch (SQLiteException ex)
            {
                SQLiteExceptionLog.LogError(ex, _logger);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                InvalidOperationLog.LogError(ex, _logger);
                return false;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return false;
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
