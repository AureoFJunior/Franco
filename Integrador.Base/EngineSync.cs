using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integradores.Base
{
    public class EngineSync
    {
        public delegate void EventErrorHandler(Exception e, String sql, ref Boolean continuar);
        public delegate void EventProgressHandler(Int64 atual, Int64 maximo, BaseSync engineSync);
        public delegate void EventSQLHandler(String tag, String sql, BaseSync engineSync);
        public delegate void EventInterceptHandler(DbCommand cmd, BaseSync engineSync, ref Boolean cancelar);

        internal Int64 mCount { get; set; } = 0;
        internal Int64 mCountMax { get; set; } = 0;

        public event EventErrorHandler OnError = delegate { };
        public event EventProgressHandler OnStatusChange = delegate { };
        public event EventSQLHandler OnSqlCreate = delegate { };
        public event EventInterceptHandler OnInterceptPreInsert = delegate { };



        public void Start(BaseSync baseSync)
        {
            //=====NpgsqlConnection pgsqlConnectionOrigem = new NpgsqlConnection(ConfigRuntime.ConectionBancoOrigem);
            //=====pgsqlConnectionOrigem.Open();
            DbConnection mConnectionOrigem = ManagerConnection.CreateConnection(ConfigRuntime.TypeDatabasaOrigem, ConfigRuntime.ConectionBancoOrigem);
            mConnectionOrigem.Open();

            NpgsqlConnection pgsqlConnectionDestino = new NpgsqlConnection(ConfigRuntime.ConectionBancoDestino);
            pgsqlConnectionDestino.Open();

            List<String> ordemCamposInsert = null;
            String sqlCount;
            string sqlRead = MontarSQLRead(baseSync, out ordemCamposInsert, out sqlCount);

            this.OnSqlCreate("SQL count", sqlCount, baseSync);
            this.OnSqlCreate("SQL read", sqlRead, baseSync);
            //System.Windows.Forms.Clipboard.SetText(sqlRead);
            //System.Windows.Forms.MessageBox.Show("OK");
            


            DbCommand cmdCount = ManagerConnection.CreateCommand(ConfigRuntime.TypeDatabasaOrigem, sqlCount, mConnectionOrigem);
           
            //=====NpgsqlCommand cmdCount = new NpgsqlCommand(sqlCount, pgsqlConnectionOrigem);
            this.mCountMax = Convert.ToInt64(cmdCount.ExecuteScalar());
            cmdCount.Dispose();
            this.mCountMax ++;
            
            //adiciona campos fixos 
            for (int i = 0; i < baseSync.mFixedReference.Count; i++)
            {
                ordemCamposInsert.Add(baseSync.mFixedReference[i].campoDestino);
            }

            //adiciona campo ID, se houver necessidade
            if (baseSync.mIdGenerateProcess == true)
            {
                ordemCamposInsert.Add(baseSync.mColumnDestinoID);
            }

            DbCommand command = ManagerConnection.CreateCommand(ConfigRuntime.TypeDatabasaOrigem, sqlRead, mConnectionOrigem);
            DbDataReader dr = command.ExecuteReader(System.Data.CommandBehavior.Default);
            //=====NpgsqlCommand command = new NpgsqlCommand(sqlRead, pgsqlConnectionOrigem);
            //=====NpgsqlDataReader dr = command.ExecuteReader(System.Data.CommandBehavior.Default);


            if (baseSync.mDeleteTableDestinoAntesImportar == true)
            {
                //NpgsqlCommand npgsDelete = new NpgsqlCommand("TRUNCATE " + baseSync.mTableDestino + " CASCADE", pgsqlConnectionDestino);
                
                NpgsqlCommand npgsDelete = new NpgsqlCommand("DELETE FROM " + baseSync.mTableDestino, pgsqlConnectionDestino);
                npgsDelete.ExecuteNonQuery();
            }
            //http://stackoverflow.com/questions/11237431/insert-data-from-textbox-to-postgres-sql

            while (dr.Read())
            {
                 
                NpgsqlCommand cmd = new NpgsqlCommand();
                cmd.Connection = pgsqlConnectionDestino;

                //adiciona campos de leitura
                for(int i = 0; i < dr.FieldCount; i++)
                {
                    //System.Windows.Forms.Clipboard.SetText(dr.FieldCount.ToString() + "/" + i.ToString() + "/ " + ordemCamposInsert[i]);
                    Object valor = null;
                    try
                    {
                        valor = dr.GetValue(i);
                    }
                    catch(Exception e)
                    {
                        System.Windows.Forms.MessageBox.Show("Erro:" + e.ToString());
                    }
                    
                    String nomeParametro = ordemCamposInsert[i];

                    if (i < baseSync.mSimpleReference.Count)
                    {
                        if (baseSync.mSimpleReference[i].mConverter != null)
                        {
                            valor = baseSync.mSimpleReference[i].mConverter.ConvertValue(valor);
                        }
                    }

                    //se passou procura pelas ref dos joins
                    if (i >= baseSync.mSimpleReference.Count)
                    {
                        var join =  baseSync.mJoinReference.SelectMany(t => t.joinReferences).Where(t => t.campoDestino == nomeParametro).FirstOrDefault(); 
                        if (join != null)
                        {
                            if (join.mConverter != null)
                            {
                                valor = join.mConverter.ConvertValue(valor);
                            }
                        }

                    }
                    cmd.Parameters.Add(new NpgsqlParameter(nomeParametro, valor));
                    //command.Parameters.Add(new NpgsqlParameter("pw", tb2.Text));
                }

                //adiciona campos fixos
                for (int i = 0; i < baseSync.mFixedReference.Count; i++)
                {
                    Object valor = baseSync.mFixedReference[i].valor;
                    //se tiver uma instrução especial, processa
                    if (valor.ToString().Contains("[%ROW]"))
                    {
                        valor = valor.ToString().Replace("[%ROW]", (this.mCount + 1).ToString());
                    }
                    String nomeParametro = baseSync.mFixedReference[i].campoDestino;
                    cmd.Parameters.Add(new NpgsqlParameter(nomeParametro, valor));
                }

               //adiciona campo ID, se houver necessidade
                if (baseSync.mIdGenerateProcess == true)
                {
                    cmd.Parameters.Add(new NpgsqlParameter(baseSync.mColumnDestinoID, this.mCount));
                }


                //intercepta
                #region 

                Boolean cancelar = false;
                OnInterceptPreInsert(cmd, baseSync, ref cancelar);
                //se cancelar roda o evento para atualizar o contador atual e a UI
                if (cancelar)
                {
                    this.mCount++;
                    OnStatusChange(this.mCount, this.mCountMax, baseSync);
                    continue;
                }


                #endregion
                /*
                if (cmd.Parameters[1].Value.ToString().Equals("00011000161"))
                {
                    System.Windows.Forms.MessageBox.Show("OK");
                }
                */


                String sqlInsert = MontarSQLInsert(baseSync, ordemCamposInsert, cmd.Parameters);
                String sqlUpdate = MontarSQLUpdate(baseSync, ordemCamposInsert, cmd.Parameters);

                //http://www.the-art-of-web.com/sql/upsert/
                String sqlExecute = "";
                if(baseSync.mDeleteTableDestinoAntesImportar == false && baseSync.mInsertOnlyWithoutUpdate == false)
                    sqlExecute = String.Format("WITH upsert AS ({0} RETURNING *) {1} WHERE NOT EXISTS (SELECT * FROM upsert);", sqlUpdate, sqlInsert);
                else
                    sqlExecute = sqlInsert;
                //String sqlTeste = String.Format("{0} IF NOT FOUND THEN {1}; END IF;", sqlUpdate, sqlInsert);

                cmd.CommandText = sqlExecute;
                String tmp = "";
                //for (int i = 0; i < cmd.Parameters.Count; i++)
                //    tmp += cmd.Parameters[i].ParameterName + " : " + cmd.Parameters[i].Value.ToString() + Environment.NewLine;

                //System.Windows.Forms.Clipboard.SetText(tmp);

                try
                {
                    this.mCount ++;
                    if (this.mCount > this.mCountMax)
                        this.mCountMax = this.mCount;
                    OnStatusChange(this.mCount, this.mCountMax, baseSync);
                    int count = cmd.ExecuteNonQuery();
                }
                catch(Exception e)
                {
                    bool continaur = true;

                    //String aux = "";
                    //for(int i = 0; i < cmd.Parameters.Count; i++)
                    //{
                    //    aux += cmd.Parameters[i].SourceColumn + "/" + cmd.Parameters[i].Value?.ToString();
                    //}

                    
                    //System.Windows.Forms.Clipboard.SetText(aux);
                    //System.Windows.Forms.MessageBox.Show(aux);

                    this.OnError(e, sqlExecute, ref continaur);

                    if (continaur == false)
                    {
                        break;
                    }
                }
            }

            if (String.IsNullOrEmpty(baseSync.mCommandSQLFinish) == false)
            {
                NpgsqlCommand cmd = new NpgsqlCommand(baseSync.mCommandSQLFinish, pgsqlConnectionDestino);
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }

            //======pgsqlConnectionOrigem.Close();
            //======pgsqlConnectionOrigem.Dispose();
            dr.Close();
            mConnectionOrigem.Close();
            mConnectionOrigem.Dispose();

            pgsqlConnectionDestino.Close();
            pgsqlConnectionDestino.Dispose();
        }

        //a ordem que processa a lista já grava os campos do banco destino
        private String MontarSQLRead(BaseSync baseSync, out List<String> camposOrdemInsert, out String sqlCount)
        {
            camposOrdemInsert = new List<string>();

            String sql = "";
            sqlCount = "";

            sql = "SELECT ";
            //sqlCount = baseSync.mSelectDistinct == false? "SELECT COUNT (*) " : "SELECT DISTINCT COUNT (*) ";

            StringBuilder sqlDistinctOn = new StringBuilder();

            if (baseSync.mDistinctOnColumns.Length > 0)
            {
                sqlDistinctOn.Append("distinct on ( ");
                for (int i = 0; i < baseSync.mDistinctOnColumns.Length; i++)
                {
                    sqlDistinctOn.Append(baseSync.mDistinctOnColumns[i]);
                    sqlDistinctOn.Append(",");
                }

                sqlDistinctOn.Remove(sqlDistinctOn.Length - 1, 1);
                sqlDistinctOn.Append(") ");

            }


            StringBuilder sqlCampos = new StringBuilder();

            //gera comando simples referencia 
            for (int i = 0; i < baseSync.mSimpleReference.Count; i++){
                
                //nomedatabela.nomedocampo,
                if (String.IsNullOrEmpty(baseSync.mSimpleReference[i].mBeforeCommandForRead) == false)
                    sqlCampos.Append(baseSync.mSimpleReference[i].mBeforeCommandForRead);

                //se nao for uma coluna virtual entao adiciona Tabela.Coluna
                if (baseSync.mSimpleReference[i].mIsVirtualColumn == false)
                {
                    sqlCampos.Append(baseSync.mTableOrigem);
                    sqlCampos.Append(".");
                }
                //senao apenas Coluna
                sqlCampos.Append(baseSync.mSimpleReference[i].campoOrigem);
                
                if (String.IsNullOrEmpty(baseSync.mSimpleReference[i].mAfterCommandForRead) == false)
                    sqlCampos.Append(baseSync.mSimpleReference[i].mAfterCommandForRead);

                sqlCampos.Append(",");
                //camposOrdemInsert.Add(baseSync.mTableDestino + "." +baseSync.mSimpleReference[i].campoDestino);
                camposOrdemInsert.Add(baseSync.mSimpleReference[i].campoDestino);
            }

            //gera comando join
            for (int i = 0; i < baseSync.mJoinReference.Count; i++){
                //nomedatabela.nomedocampo,

                for(int j = 0; j < baseSync.mJoinReference[i].joinReferences.Count; j++)
                {
                    sqlCampos.Append(baseSync.mJoinReference[i].tabela2);
                    sqlCampos.Append(".");
                    sqlCampos.Append(baseSync.mJoinReference[i].joinReferences[j].campoOrigem);
                    sqlCampos.Append(",");
                    camposOrdemInsert.Add(baseSync.mJoinReference[i].joinReferences[j].campoDestino);
                }
                
                //camposOrdemInsert.Add(baseSync.mSimpleReference[i].campoDestino);
            }

            //gera join
            String sqlJoin = " ";
            //if (baseSync.mJoinReference.Count > 0)
               

            for (int i = 0; i < baseSync.mJoinReference.Count; i++) {
                //sqlJoin += " LEFT JOIN ";
                sqlJoin += " " + baseSync.mJoinReference[i].tipoJoin + " ";

                //for(int k = 0; k < baseSync.mJoinReference[i].joinReferences)
                sqlJoin += baseSync.mJoinReference[i].tabela2;
                sqlJoin += " ON ";

                for(int j = 0; j < baseSync.mJoinReference[i].joinOnReference.Count; j++)
                {
                    if (baseSync.mJoinReference[i].joinOnReference[j].campoOnTabela1_IsValue == true)
                    {
                        sqlJoin += " " + baseSync.mJoinReference[i].joinOnReference[j].campoOnTabela1;
                    }
                    else
                    {
                        sqlJoin += " " + baseSync.mTableOrigem + "." + baseSync.mJoinReference[i].joinOnReference[j].campoOnTabela1;
                    }
                    
                    sqlJoin += "=";
                    sqlJoin += baseSync.mJoinReference[i].tabela2 + "." + baseSync.mJoinReference[i].joinOnReference[j].campoOnTabela2;
                    //sqlJoin += " AND";
                    sqlJoin += $" {baseSync.mJoinReference[i].joinOnReference[j].proxOperador}";
                }

                //sqlJoin = sqlJoin.Remove(sqlJoin.Length - 3, 3); //remove o ultimo "AND"
                sqlJoin = sqlJoin.Remove(sqlJoin.LastIndexOf(' '), sqlJoin.Length - sqlJoin.LastIndexOf(' ')) + "  "; //remove o ultimo "AND"

            }

            if (baseSync.mSelectDistinct == true)
                sql += " DISTINCT ";

            sql += sqlDistinctOn.ToString();

            sql += sqlCampos.ToString(0, sqlCampos.Length -1); //ja remove virgula

            //só tira a virgula se tiver, pode ser que tenha só 1 campo (prodempresas)
            String sqlToSqlCount = sql;
            if (sql.IndexOf(',') != -1)
                sqlToSqlCount = sql.Substring(0, sql.IndexOf(',')) + " ";
            
            

            sql += " FROM " + baseSync.mTableOrigem + " ";
            sql += sqlJoin;

            sqlToSqlCount += " FROM " + baseSync.mTableOrigem + " ";
            sqlToSqlCount += sqlJoin;

            //sqlCount += " FROM " + baseSync.mTableOrigem + " ";
            //sqlCount += sqlJoin;

            sql += " " + baseSync.mWhere;
            sqlToSqlCount += " " + baseSync.mWhere;
            //sqlCount += " " + baseSync.mWhere;

            //String sqlToSqlCount = sql;
            //remove o order by do sqlcount
            /*
            int indexOrderBy = sqlToSqlCount.IndexOf("order by");
            if (indexOrderBy > 0)
                sqlToSqlCount = sqlToSqlCount.Remove(indexOrderBy);
            */

            sqlCount += "SELECT COUNT(*) FROM ";
            sqlCount += "(";

            sqlCount += sqlToSqlCount;
            //sqlCount += sqlToSqlCount;

            sqlCount += ") as tmp";

            return sql;

        }

        private String MontarSQLInsert(BaseSync baseSync, List<String> ordemCamposInsert, NpgsqlParameterCollection parametros)
        {
            String sql = "INSERT INTO " + baseSync.mTableDestino ;

            StringBuilder camposInsert = new StringBuilder("(");
            for(int i = 0; i < ordemCamposInsert.Count; i++)
            {
                camposInsert.Append(ordemCamposInsert[i]);
                camposInsert.Append(",");
            }

            camposInsert = camposInsert.Remove(camposInsert.Length -1, 1);
            camposInsert.Append(")");

            //values
            sql += camposInsert.ToString();

            //sql += " VALUES ";
            sql += " SELECT ";

            //StringBuilder camposValues = new StringBuilder("(");
            StringBuilder camposValues = new StringBuilder(" ");
            for(int i = 0; i < parametros.Count; i++)
            {
                camposValues.Append(":" + parametros[i].ParameterName);
                camposValues.Append(",");
            }

            camposValues = camposValues.Remove(camposValues.Length -1, 1);
            
            //camposValues.Append(")");
            camposValues.Append(" ");

            sql += camposValues.ToString();


            return sql;

        }

        private String MontarSQLUpdate(BaseSync baseSync, List<String> ordemCamposInsert, NpgsqlParameterCollection parametros)
        {
            String sql = "UPDATE " + baseSync.mTableDestino;
            sql += " SET ";

            for(int i = 0; i < ordemCamposInsert.Count; i++)
            {
                sql += ordemCamposInsert[i];
                sql += "=";
                sql += ":" + parametros[i].ParameterName + ",";
            }

            sql = sql.Remove(sql.Length -1 , 1); //remove virguala
            sql += " WHERE ";
            //sql += baseSync.mColumnDestinoID + " = " + ":" + baseSync.mColumnOrigemID; 
            sql += baseSync.mColumnDestinoID + " = " + ":" + baseSync.mColumnDestinoID; 
            
            return sql;
            
        }

        public string RemoveAccents(string text)
        {
            StringBuilder sbReturn = new StringBuilder();
            var arrayText = text.Normalize(NormalizationForm.FormD).ToCharArray();
            foreach (char letter in arrayText)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(letter) != UnicodeCategory.NonSpacingMark)
                    sbReturn.Append(letter);
            }
            return sbReturn.ToString();
        }

        public String GetSqlRead(BaseSync baseSync)
        {
            List<String> ordemCamposInsert = null;
            String sqlCount;
            string sqlRead = MontarSQLRead(baseSync, out ordemCamposInsert, out sqlCount);
            return sqlRead;
        }

        public DbDataReader GetReader(BaseSync baseSync)
        {
            DbConnection mConnectionOrigem = ManagerConnection.CreateConnection(ConfigRuntime.TypeDatabasaOrigem, ConfigRuntime.ConectionBancoOrigem);
            mConnectionOrigem.Open();

            List<String> ordemCamposInsert = null;
            String sqlCount;
            string sqlRead = MontarSQLRead(baseSync, out ordemCamposInsert, out sqlCount);

            DbCommand command = ManagerConnection.CreateCommand(ConfigRuntime.TypeDatabasaOrigem, sqlRead, mConnectionOrigem);
            DbDataReader dr = command.ExecuteReader(System.Data.CommandBehavior.Default);
            return dr;
        }
    }
}
