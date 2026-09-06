using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.DataBase.DbPath
{
    public class DBPathCLass
    {
        public string MethodPath()
        {
            string projectDirectory = System.IO.Directory.GetCurrentDirectory();
            string dbPath = System.IO.Path.Combine(projectDirectory, "DataBase.db");
            return dbPath;
        }
    }
}
