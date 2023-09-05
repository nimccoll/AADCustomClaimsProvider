using System;
using System.Data;
using System.Data.SqlClient;

namespace CustomClaimAPI
{
    public class SqlHelper : IDisposable
    {
        #region Declarations

        private SqlConnection _cnnSql;
        private SqlTransaction _trnSql;
        private SqlCommand _cmdSql;
        private SqlDataAdapter _daSql;

        internal string DatabaseName { get; set; }
        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.  Initialize connection using connection string from the config file.
        /// </summary>
        public SqlHelper()
        {
            _cnnSql = new SqlConnection(Environment.GetEnvironmentVariable("SQLAZURECONNSTR_ConnString", EnvironmentVariableTarget.Process));
        }

        /// <summary>
        /// Initialize connection using the supplied connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        public SqlHelper(string connectionString)
        {
            _cnnSql = new SqlConnection(connectionString);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Begin a database transaction.
        /// </summary>
        public void BeginTransaction()
        {
            if (_cnnSql.State != ConnectionState.Open) _cnnSql.Open();
            _trnSql = _cnnSql.BeginTransaction();
        }

        /// <summary>
        /// Close the database connection and free the resources.
        /// </summary>
        public void Close()
        {
            if (_cnnSql.State != ConnectionState.Open) _cnnSql.Close();
            _cnnSql.Dispose();
        }

        /// <summary>
        /// Commit the database transaction.
        /// </summary>
        public void Commit()
        {
            if (_trnSql == null) throw new ApplicationException("Transaction is not initialized.");
            else _trnSql.Commit();
        }

        /// <summary>
        /// Rollback the database transaction.
        /// </summary>
        public void Rollback()
        {
            if (_trnSql == null) throw new ApplicationException("Transaction is not initialized.");
            else _trnSql.Rollback();
        }

        /// <summary>
        /// Execute a Sql command that does not return a resultset.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <returns>Rows affected</returns>
        public int Execute(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList)
        {
            return Execute(command, commandType, ref parameterList, 30);
        }

        /// <summary>
        /// Execute a Sql command that does not return a resultset.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <param name="timeout">The command timeout value</param>
        /// <returns>Rows affected</returns>
        public int Execute(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList, int timeout)
        {
            int result;

            BuildCommand(ref command, ref commandType, ref parameterList);
            _cmdSql.CommandTimeout = timeout;

            if (_cnnSql.State != ConnectionState.Open) _cnnSql.Open();
            result = _cmdSql.ExecuteNonQuery();

            _cmdSql.Parameters.Clear();
            _cmdSql = null;

            return result;
        }

        /// <summary>
        /// Execute a Sql command and return the results in a dataset.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <returns>Results of the Sql command in a dataset</returns>
        public DataSet ExecuteDataSet(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList)
        {
            return ExecuteDataSet(command, commandType, ref parameterList, 30);
        }

        /// <summary>
        /// Execute a Sql command and return the results in a dataset.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <param name="timeout">The command timeout value</param>
        /// <returns>Results of the Sql command in a dataset</returns>
        public DataSet ExecuteDataSet(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList, int timeout)
        {
            DataSet ds = new DataSet();

            BuildCommand(ref command, ref commandType, ref parameterList);
            _cmdSql.CommandTimeout = timeout;

            _daSql = new SqlDataAdapter();
            _daSql.SelectCommand = _cmdSql;

            if (_cnnSql.State != ConnectionState.Open) _cnnSql.Open();
            _daSql.Fill(ds);

            _cmdSql.Parameters.Clear();
            _cmdSql = null;

            return ds;
        }

        /// <summary>
        /// Execute a Sql command and return the results in a dataset.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <returns>Results of the Sql command in a datatable</returns>
        public DataTable ExecuteDataTable(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList)
        {
            return ExecuteDataTable(command, commandType, ref parameterList, 30);
        }

        /// <summary>
        /// Execute a Sql command and return the results in a dataset.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <param name="timeout">The command timeout value</param>
        /// <returns>Results of the Sql command in a datatable</returns>
        public DataTable ExecuteDataTable(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList, int timeout)
        {
            DataTable dt = new DataTable();

            BuildCommand(ref command, ref commandType, ref parameterList);
            _cmdSql.CommandTimeout = timeout;

            _daSql = new SqlDataAdapter();
            _daSql.SelectCommand = _cmdSql;

            if (_cnnSql.State != ConnectionState.Open) _cnnSql.Open();
            _daSql.Fill(dt);

            _cmdSql.Parameters.Clear();
            _cmdSql = null;

            return dt;
        }

        /// <summary>
        /// Execute a Sql command and return the results in a datareader.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <returns>Results of a Sql command in a datareader</returns>
        public SqlDataReader ExecuteDataReader(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList)
        {
            return ExecuteDataReader(command, commandType, ref parameterList, CommandBehavior.Default, 30);
        }

        /// <summary>
        /// Execute a Sql command and return the results in a datareader.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <param name="behavior">The command behavior value</param>
        /// <returns>Results of a Sql command in a datareader</returns>
        public SqlDataReader ExecuteDataReader(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList, CommandBehavior behavior)
        {
            return ExecuteDataReader(command, commandType, ref parameterList, behavior, 30);
        }

        /// <summary>
        /// Execute a Sql command and return the results in a datareader.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <param name="timeout">The command timeout value</param>
        /// <returns>Results of a Sql command in a datareader</returns>
        public SqlDataReader ExecuteDataReader(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList, int timeout)
        {
            return ExecuteDataReader(command, commandType, ref parameterList, CommandBehavior.Default, timeout);
        }

        /// <summary>
        /// Execute a Sql command and return the results in a datareader.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <param name="behavior">The command behavior value</param>
        /// <param name="timeout">The command timeout value</param>
        /// <returns>Results of a Sql command in a datareader</returns>
        public SqlDataReader ExecuteDataReader(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList, CommandBehavior behavior, int timeout)
        {
            SqlDataReader dr;

            BuildCommand(ref command, ref commandType, ref parameterList);
            _cmdSql.CommandTimeout = timeout;

            if (_cnnSql.State != ConnectionState.Open) _cnnSql.Open();
            dr = _cmdSql.ExecuteReader(behavior);

            _cmdSql.Parameters.Clear();
            _cmdSql = null;

            return dr;
        }

        /// <summary>
        /// Execute a Sql command that returns a one row, one column result.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <returns>Result of the Sql command</returns>
        public Object ExecuteScalar(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList)
        {
            return ExecuteScalar(command, commandType, ref parameterList, 30);
        }

        /// <summary>
        /// Execute a Sql command that returns a one row, one column result.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <param name="timeout">The command timeout value</param>
        /// <returns>Result of the Sql command</returns>
        public Object ExecuteScalar(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList, int timeout)
        {
            Object result;

            BuildCommand(ref command, ref commandType, ref parameterList);
            _cmdSql.CommandTimeout = timeout;

            if (_cnnSql.State != ConnectionState.Open) _cnnSql.Open();
            result = _cmdSql.ExecuteScalar();

            _cmdSql.Parameters.Clear();
            _cmdSql = null;

            return result;
        }

        /// <summary>
        /// Execute a Sql command and return the results in an XML reader.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <returns>Results of a Sql command in an XML reader</returns>
        public System.Xml.XmlReader ExecuteXmlReader(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList)
        {
            return ExecuteXmlReader(command, commandType, ref parameterList, 30);
        }

        /// <summary>
        /// Execute a Sql command and return the results in an XML reader.
        /// </summary>
        /// <param name="command">The Sql command</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        /// <param name="timeout">The command timeout value</param>
        /// <returns>Results of a Sql command in an XML reader</returns>
        public System.Xml.XmlReader ExecuteXmlReader(string command, CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList, int timeout)
        {
            System.Xml.XmlReader xr;

            BuildCommand(ref command, ref commandType, ref parameterList);
            _cmdSql.CommandTimeout = timeout;

            if (_cnnSql.State != ConnectionState.Open) _cnnSql.Open();
            xr = _cmdSql.ExecuteXmlReader();

            _cmdSql.Parameters.Clear();
            _cmdSql = null;

            return xr;
        }

        /// <summary>
        /// Construct a Sql Command object.
        /// </summary>
        /// <param name="command">The Sql command to be executed</param>
        /// <param name="commandType">The type of the Sql command (text, stored procedure, etc.)</param>
        /// <param name="parameterList">A list of SqlParameters</param>
        private void BuildCommand(ref string command, ref CommandType commandType, ref System.Collections.Generic.List<SqlParameter> parameterList)
        {
            _cmdSql = new SqlCommand();
            _cmdSql.Connection = _cnnSql;
            _cmdSql.CommandText = command;
            _cmdSql.CommandType = commandType;

            foreach (SqlParameter p in parameterList)
            {
                _cmdSql.Parameters.Add(p);
            }

            if (_trnSql != null) _cmdSql.Transaction = _trnSql;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
		/// Disposes the SqlHelper object
		/// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the SqlHelper object
        /// </summary>
        /// <param name="disposing">Determines if the object is currently disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_cnnSql.State != ConnectionState.Open)
            {
                _cnnSql.Close();
                _cnnSql.Dispose();
            }

            if (_trnSql != null)
            {
                _trnSql.Dispose();
            }

            if (_cmdSql != null)
            {
                _cmdSql.Dispose();
            }

            if (_daSql != null)
            {
                _daSql.Dispose();
            }
        }

        #endregion
    }
}
