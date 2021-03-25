using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integradores.Base
{
    public class ManagerConnection
    {

        /// <summary>
        /// Retorna a abstração da classe de conexao
        /// </summary>
        /// <param name="typeOfDatabase"></param>
        /// <param name="strConnection"></param>
        /// <returns></returns>
        public static DbConnection CreateConnection(TypeDatabase typeOfDatabase, String strConnection)
        {
            DbConnection connection = null;
            switch (typeOfDatabase)
            {
                case TypeDatabase.Postgresql:
                    connection = new Npgsql.NpgsqlConnection(strConnection);
                    break;
                case TypeDatabase.Oracle:
                    connection = new Oracle.ManagedDataAccess.Client.OracleConnection(strConnection);
                    break;
                case TypeDatabase.Firebird:
                    connection = new FbConnection(strConnection);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return connection;
        }

        public static DbCommand CreateCommand(TypeDatabase typeOfDatabase, String cmdText, DbConnection connection)
        {
            DbCommand cmd = null;
            switch (typeOfDatabase)
            {
                case TypeDatabase.Postgresql:
                    cmd = new Npgsql.NpgsqlCommand(cmdText, (Npgsql.NpgsqlConnection)connection);
                    break;
                case TypeDatabase.Oracle:
                    cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(cmdText, (Oracle.ManagedDataAccess.Client.OracleConnection)connection);
                    break;
                case TypeDatabase.Firebird:
                    cmd = new FirebirdSql.Data.FirebirdClient.FbCommand(cmdText, (FbConnection)connection);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return cmd;
        }
    }
}
