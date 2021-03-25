using Integradores.Base.Converters;
using Integradores.Base.Interface;
using Integradores.Base.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Integradores.Base.Types.JoinReference;

namespace Integradores.Base
{
    public class BaseSync : iSync
    {
        internal String mTableOrigem = "";
        internal String mTableDestino = "";

        internal String mColumnOrigemID = "";
        internal String mColumnDestinoID = "";

        internal Boolean mDeleteTableDestinoAntesImportar = false;
        internal Boolean mIdGenerateProcess = false;
        internal Boolean mInsertOnlyWithoutUpdate = false;

        internal Boolean mSelectDistinct = false;

        internal String[] mDistinctOnColumns = new string[0];

        internal String mWhere = "";

        internal List<SimpleReference> mSimpleReference = new List<SimpleReference>();
        internal List<JoinReference> mJoinReference = new List<JoinReference>();
        internal List<FixedReference> mFixedReference = new List<FixedReference>();

        internal String mCommandSQLFinish = null;

        public EngineSync Engine { get; internal set; } = new EngineSync();

        public String TableDestiny { get { return this.mTableDestino; } }
        public String TableSource { get { return this.mTableOrigem; } }



        protected void SetTables(String tableOrigem, String tableDest)
        {
            this.mTableOrigem = tableOrigem;
            this.mTableDestino = tableDest;
        }

        protected void DistinctOn(String[] distinctColumns)
        {
            this.mDistinctOnColumns = distinctColumns;
        }

        protected SimpleReference Add(String campoBaseOrigem, String campoBaseDestino)
        {
            SimpleReference reference = new SimpleReference(campoBaseOrigem, campoBaseDestino);
            mSimpleReference.Add(reference);
            return reference;
        }

        protected SimpleReference Add(String campoBaseOrigem, String campoBaseDestino, IConverter converter)
        {
            SimpleReference reference = new SimpleReference(campoBaseOrigem, campoBaseDestino);
            reference.mConverter = converter;
            mSimpleReference.Add(reference);
            return reference;
        }



        protected void AddJoin(String tabela2, String campoOnTabela1, String campoOnTabela2, String campoBaseOrigem, String campoBaseDestino)
        {
            AddJoin(tabela2, campoOnTabela1, campoOnTabela2, campoBaseOrigem, campoBaseDestino, null);
        }

        protected void AddJoin(String tabela2, String campoOnTabela1, String campoOnTabela2, String campoBaseOrigem, String campoBaseDestino, IConverter converter)
        {
            List<JoinOnReference> list = new List<JoinOnReference>();
            list.Add(new JoinOnReference(campoOnTabela1, campoOnTabela2));

            var listReference = new List<SimpleReference>();
            listReference.Add(new SimpleReference(campoBaseOrigem, campoBaseDestino, converter));
            AddJoin(tabela2, list, listReference);
        }

        protected void AddJoin(String tabela2, List<JoinOnReference> joinOnReference, List<SimpleReference> joinReference)
        {
            AddJoin(tabela2, joinOnReference, joinReference, " INNER JOIN ");
        }

        protected void AddJoin(String tabela2, List<JoinOnReference> joinOnReference, List<SimpleReference> joinReference, String tipoJoin)
        {
            mJoinReference.Add(new JoinReference(tabela2, joinOnReference, joinReference, tipoJoin));
        }

        protected void AddFixed(String valor, String campoBaseDestino)
        {
            mFixedReference.Add(new FixedReference(valor, campoBaseDestino));
        }

        protected void IdReference(String columnOrigemID, String columnDestinoID)
        {
            this.mColumnOrigemID = columnOrigemID;
            this.mColumnDestinoID = columnDestinoID;
        }

        protected void IdReference(String columnDestinoID)
        {
            IdReference("", columnDestinoID);
        }

        protected void DeleteTableDestinyBeforeImport()
        {
            this.mDeleteTableDestinoAntesImportar = true;
        }

        protected void InsertOnlyWithoutUpdate()
        {
            this.mInsertOnlyWithoutUpdate = true;
        }

        protected void IdGenerate()
        {
            this.mIdGenerateProcess = true;
        }

        protected void SelectDistinct()
        {
            mSelectDistinct = true;
        }

        protected void Where(string where)
        {
            this.mWhere = where;
        }


        protected void CommandSQLFinish(String cmd)
        {
            this.mCommandSQLFinish = cmd;
        }


        public void SyncNow()
        {
            Engine.Start(this);
        }

        public String GetSqlForRead()
        {
            return Engine.GetSqlRead(this);
        }
    }
}
