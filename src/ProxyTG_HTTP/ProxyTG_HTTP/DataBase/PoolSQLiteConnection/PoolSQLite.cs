using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.DbPath;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.DataBase.PoolSQLiteConnection
{
    public class PoolSQLite
    {
        private readonly ILogger<PoolSQLite> _logger;
        private  readonly object _Lock = new object();
        private readonly Stack<SQLiteConnection> _aviable = new Stack<SQLiteConnection>();
        private readonly List<SQLiteConnection> _InUse = new List<SQLiteConnection>();
        private readonly int _maxcountPool = 10;
        private readonly DBPathCLass _dBPathCLass;

        private string _path;

        public PoolSQLite(ILogger<PoolSQLite> logger, DBPathCLass dBPathCLass)        {
            _logger = logger;
            _dBPathCLass = dBPathCLass;
            _path = _dBPathCLass.MethodPath();
        }

        public SQLiteConnection CreateConnection()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection($"Data Source={_path}");
                connection.Open();
                return connection;
            }
            catch (SQLiteException ex)
            {
                _logger.LogError("Не удалось создать новое соединение для пула!" + ex.Message + ex.StackTrace + ex.InnerException);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Не удалось создать новое соединение для пула! Не обработанное исключение" + ex.Message + ex.StackTrace + ex.InnerException);
                throw;
            }
        }

        public SQLiteConnection ConnectionOpen()
        {
            try
            {
                lock (_Lock)
                {
                    SQLiteConnection connection = null;

                    if (_aviable == null)
                        connection = CreateConnection();
                    else if (_aviable.Count > 0)
                    {
                        connection = _aviable.Pop();

                        if (connection.State != System.Data.ConnectionState.Open)
                        {
                            connection.Dispose();
                            connection = CreateConnection();
                        }
                    }
                    else if (_aviable?.Count < _maxcountPool)
                    {
                        connection = CreateConnection();
                    }
                    else
                    {
                        throw new Exception("Пулл SQL  соединений занят");
                    }
                    _InUse.Add(connection);
                    return connection;
                }          
            }
            catch (SQLiteException ex)
            {
                _logger.LogError("Не удалось получить соединение из пула!" + ex.Message + ex.StackTrace + ex.InnerException);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Не удалось получить соединение из пула! Не обработанное исключение" + ex.Message + ex.StackTrace + ex.InnerException);
                throw;
            }
        }

        public void ConnectionClose(SQLiteConnection connection)
        {
            try
            {
                lock (_Lock)
                {
                    if (connection == null)
                        return;

                    if (_InUse.Contains(connection))
                    {
                        _InUse.Remove(connection);

                        if (connection.State == System.Data.ConnectionState.Open)
                        {
                            _aviable.Push(connection);
                        }
                        else
                        {
                            connection.Dispose();
                        }
                    }
                    else
                    {
                        throw new Exception("Соединение не найдено");
                    }
                }
            }
            catch (SQLiteException ex)
            {
                _logger.LogError("Не удалось получить соединение из пула!" + ex.Message + ex.StackTrace + ex.InnerException);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Не удалось получить соединение из пула! Не обработанное исключение" + ex.Message + ex.StackTrace + ex.InnerException);
                throw;
            }
        }

    }
}
