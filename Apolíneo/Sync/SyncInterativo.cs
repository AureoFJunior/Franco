using Integradores.Base;
using Integradores.Base.Converters.Defaults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuxaRelatorio.Sync
{
    public static class SyncInterativo
    {

        //Sync with the DB and bring new datas, if these already exist, it doesn't do anything.
            public class SyncProdutosInterativo : BaseSync
            {

                public SyncProdutosInterativo()
                {


                    SetTables("produtos", "produtos");

                    IdReference("prod_codigo", "prod_codigo");
                    Add("prod_codigo", "prod_codigo", new ConvertToInt64());
                    Add("prod_descricao", "prod_descricao");
                    AddFixed("I", "prod_ativo");
                    
                
                    // SetTables("funcionarios", "vendedores");
                    //IdReference("func_codigo", "vend_codigo");
                    // Add("func_codigo", "vend_codigo", new ConvertWords());
                    // Add("func_nome", "vend_nome");
                    //AddFixed("I", "prod_ativo");

                    Where(" WHERE prod_status = 'N' ");

                }


        }
    }
}

        