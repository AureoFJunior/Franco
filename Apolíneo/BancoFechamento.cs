using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apolíneo
{
    public class Det 
    {
        public String fchTransacao { get; set; }
        public Int64 prodCodigo { get; set; }

        public Int64 prodCodigoRB { get; set; }
        public String fsProdNome { get; set; }
        public Int64 qtdeCarga { get; set; }
        public Int64 prQtdeVenda { get; set; }
        public Int64 prQtdeTroca { get; set; }
        public Int64 prQtdeRetorno { get; set; }
        public Decimal prValorProd { get; set; }
        public Decimal prValorTotal { get; set; }
        public Decimal prComissao { get; set; }
        
        public Int64 fsQtdeVenda { get; set; }
        public Int64 fsQtdeTroca { get; set; }
        public Int64 fsQtdeRetorno { get; set; }
        

    }

    

    public class Cabecalho
    {
        public String vendCod { get; set; }
        public String status { get; set; }
        public Int64 importSeq { get; set; }
        public String transRequest { get; set; }
        public String dateImport { get; set; }
        public String dateFech { get; set; }
        public String transManifest { get; set; }
    }


    public class BancoFechamento
    {
        
        public static Dictionary<String, String> getVendedores()
        {
            List<String> codVendedoresRb = new List<String>();
            List<String> codVendedoresFs = new List<String>();
            //List<String> codVendedores = new List<String>();
            Dictionary<String, String> vendedoresFrassinixRb = new Dictionary<String, String>();
             var frasscon = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFch"]);
            frasscon.Open();
            String sql = " select vend_codigo_rb, vend_codigo from vendedores where vend_codigo_rb is not null AND vend_codigo_rb <> ''";

            var cmdFrass = new NpgsqlCommand(sql, frasscon);

            var dr = cmdFrass.ExecuteReader();

            while (dr.Read())
            {
                vendedoresFrassinixRb.Add(dr.GetSafeValue<String>("vend_codigo_rb"), dr.GetSafeValue<String>("vend_codigo"));
            }
                

            dr.Close();

            /*
            for(int i = 0; i < codVendedoresRb.Count; i++)
            {
                vendedoresFrassinixRb.Add(codVendedoresRb[i], codVendedoresFs[i]);
                var codVend = codVendedoresRb[i];
                
            }
            */



            return vendedoresFrassinixRb;




        }
    }
}
