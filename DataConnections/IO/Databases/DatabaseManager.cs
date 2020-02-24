using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Reflection;
using System.IO;
using System.IO.IsolatedStorage;
using System.Security.Cryptography;
using System.Data.Common;

namespace RootNav.Data.IO.Databases
{
    public abstract class DatabaseManager : IDisposable
    {
        protected bool disposed;

        protected DbConnection connection = null;
      
        public String DatabaseName
        {
            get
            {
                if (connection != null)
                    return connection.Database;
                else
                    return "";
            }
        }

        public DbConnection Connection
        {
            get { return connection; }
            set { connection = value; }
        }

        public abstract bool IsOpen
        {
            get;
        }

        public abstract bool ValidateConnection(string connectionString, out int errorNo);

        public abstract bool Open(string connectionString);

        public abstract void Close();

        public abstract bool Write(String tag, bool alllaterals, List<Dictionary<String, object>> records, byte[] imageData = null);

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (connection != null)
                    {
                        connection.Close();
                        connection.Dispose();
                    }  
                }
                connection = null;
                disposed = true;
            }
        }
        #endregion
    }
}
