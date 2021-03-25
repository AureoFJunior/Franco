using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apolíneo
{
    class BancoFrassini
    {
        public static NpgsqlConnection mConexao = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFr"]);


        public static List<Det> getTradeFrass(String dctoNumber, List<Det> detalhesGeral, DateTime filtro, String vendedor)
        {
            mConexao.Open();
            //List<Det> detFrass = new List<Det>();
            
            String sql = $" select * from pendest  " +
                         $" where pest_cpes_codigo  = '{dctoNumber}' and pest_datamvto = '{filtro.ToString("yyyy-MM-dd")}' " +
                         $" and pest_func_codigo = '{vendedor}' order by pest_datamvto desc ";
            
            
            var cmd = new NpgsqlCommand(sql, mConexao);
            var dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                Int64 codProdutoFs = dr.GetSafeValue<Int64>("pest_prod_codigo");
                Int64 pdQtd = dr.GetSafeValue<Int64>("pest_qtde");

                var item = detalhesGeral.Where(t => t.prodCodigo == codProdutoFs).FirstOrDefault();
                if (item != null)
                    item.fsQtdeTroca = pdQtd;
                else
                {
                    detalhesGeral.Add(new Det()
                    {
                        prodCodigoRB = -1,
                        prodCodigo = codProdutoFs,
                        fsQtdeTroca = pdQtd
                    });

                }

                /*
                for (int i = 0; i < detalhesGeral.Count; i++)
                {
                    if(detalhesGeral[i].prodCodigo == codProdutoFs)
                    {
                        //detalhesGeral[i].prodCodigo = codProdutoFs;
                        detalhesGeral[i].fsQtdeTroca = pdQtd;
                    }

                }
                
                /*if (dctoNumber.Contains(""))
                {
                    Int64 pdQtd = dr.GetSafeValue<Int64>("qutde");
                    detFrass.Add(new Det()
                    {
                        prodCodigo = codProduto,
                        fsQtdeRetorno = pdQtd,

                    });
                }*/

            }
            dr.Close();
            mConexao.Close();
            return detalhesGeral;
        }
        public static List<Det> getSelledFrass(String manifesto, String dctoNumber, List<Det> detalhesGeral)
        {
            mConexao.Open();

            String sql = " select mved_prod_codigo, SUM(mved_qtde) as qtde from movestd " +
                        $" left join refdctos on refd_transacao = mved_transacao " +
                        $" where mved_cnta_conta = '{dctoNumber}' and refd_transacaoref = '{manifesto}' " +
                        $" and mved_status = 'N' group by mved_prod_codigo ";

            
            var cmd = new NpgsqlCommand(sql, mConexao);
            var dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                
                if (dctoNumber.Contains("7005"))
                {
                    Int64 codProdutoFs = dr.GetSafeValue<Int64>("mved_prod_codigo");
                    Int64 pdQtd = dr.GetSafeValue<Int64>("qtde");

                    var item = detalhesGeral.Where(t => t.prodCodigo == codProdutoFs).FirstOrDefault();
                    if (item != null)
                        item.fsQtdeVenda = pdQtd;
                    else
                    {
                        detalhesGeral.Add(new Det()
                        {
                            prodCodigoRB = -1,
                            prodCodigo = codProdutoFs,
                            fsQtdeVenda = pdQtd
                        });

                    }
                    //detalhesGeral[i].prodCodigo = pdCod;

                }
            }

            mConexao.Close();
            return detalhesGeral;
        }

        public static String getManifesto(DateTime filtro, String vendedor)
        {
            mConexao.Open();

            String sql =  $" select * from movestc where mvec_dataentsai = '{filtro.ToString("yyyy-MM-dd")}' and mvec_func_codigo  = '{vendedor}' " +
                          $"and mvec_cnta_conta in ('7004','7002')";

            var cmd = new NpgsqlCommand(sql, mConexao);
            var dr = cmd.ExecuteReader();
            dr.Read();
            String manifestoFs = dr.GetSafeValue<String>("mvec_transacao");
            dr.Close();


            mConexao.Close();
            return manifestoFs;
        }

        

    }
}
