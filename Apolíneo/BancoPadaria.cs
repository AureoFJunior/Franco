using Npgsql;
using PuxaRelatorio;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apolíneo
{
    public static class BancoPadaria 
    {
        //Connect with the database (Padaria Rio Branco).
        public static NpgsqlConnection mConexao = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoRb"]);
                                                                   
        public static List<prods> prodList = new List<prods>();
        
        
        //Get the requests where the doct code = 7505.
        public static List<String> getRequests(FiltroRelatorio filtro)
        {
            AbrirConexao();

            

            List<String> where = new List<String>();
            

            if (filtro.DateManifesto != null)
            {
                //"yyyy-MM-dd" is the format of the date.
                where.Add($"  pvec_database = '{filtro.DateManifesto.ToString("yyyy-MM-dd")}' ");
            }


            where.Add(" mdoc_status = 'N' ");
            where.Add(" mdoc_dcto_codigo = '7505' ");
            var vendedor = BancoFechamento.getVendedores();

            var sqlVendedor = vendedor.Select(t => t.Key).ToList().JoinArraySQL();
            where.Add($" pvec_vend_codigo in ({sqlVendedor})");

            //Get the tables in postgresql.

            //Get the requests.
            String sql = $" select mdoc_transacao, pvec_vend_codigo, pvec_database from movdctos " +
                   " inner join pedvendac on mdoc_transacao = pvec_transacao ";




            if (where.Count > 0)
            {
                sql += " where ";
            }


            for (int i = 0; i < where.Count; i++)
            {
                sql += " " + where[i];
                if (i != where.Count - 1)
                    sql += " and ";

            }

            //Get the requests and find the manifest.

            var cmd = new NpgsqlCommand(sql, mConexao);
            var dr = cmd.ExecuteReader();

            List<String> infos = new List<String>();
            //String requestTransation;
            
            while (dr.Read())
            {
                infos.Add(dr.GetSafeValue<String>("mdoc_transacao"));
            }


            dr.Close();

            return infos;
        }
        //2 methods, that makes the same thing, they get the transations, using the reference and doc number.
        public static List<String> obterTransacaoReferenciadas(String transacaoRef, String dctoNumber)
        {

            return obterTransacaoReferenciadas(new List<String>() { transacaoRef }, dctoNumber);
            
        }

        public static List<String> obterTransacaoReferenciadas(List<String> requestsTransations, String dctoNumber)
        {
            AbrirConexao();

            List<String> manifestsTransations = new List<String>();
            for (int i = 0; i < requestsTransations.Count; i++)
            {

                String sql = " select refd_transacao from refdctos " +
                        "inner join movdctos on  refd_transacao = mdoc_transacao " +
                        $"where refd_transacaoref = '{requestsTransations[i]}' and " +
                        $"mdoc_dcto_codigo = '{dctoNumber}' and mdoc_status = 'N'";



                var cmd = new NpgsqlCommand(sql, mConexao);
                var dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    manifestsTransations.Add(dr.GetSafeValue<String>("refd_transacao"));
                }
                dr.Close();

            }
            return manifestsTransations;
        }
        //Get the produts from a request (the base doc).

        public static String obterCodigoVendedor(String transacao)
        {
            AbrirConexao();

            //String sql = $" select mprc_vend_codigo from movprodc where mprc_transacao = '{transacao}'";

            String sql = $"select pvec_vend_codigo from pedvendac  where pvec_transacao = '{transacao}'";

            NpgsqlCommand cmd = new NpgsqlCommand(sql, mConexao);
            var dr = cmd.ExecuteReader();
            dr.Read();

            var codigoVendedor = Convert.ToString(dr.GetSafeValue<String>("pvec_vend_codigo"));
            dr.Close();
            //Não encontra o código do vendedor.
            return codigoVendedor.ToString();
        }
        public static List<prods> getProdsFromRequests(List<String> requestsProd, String dctoNumber)
        {
            AbrirConexao();
            prodList.Clear();

            for (int i = 0; i < requestsProd.Count; i++)
            {
                String sql = "select pved_prod_codigo, SUM(pved_qtde) as sum from pedvendad " +
                              " inner join pedvendac on pvec_numero=pved_numero " +
                             $" where pvec_transacao='{requestsProd[i]}' " +
                             " group by pved_prod_codigo ";

                var cmd = new NpgsqlCommand(sql, mConexao);
                var dr = cmd.ExecuteReader();


                while (dr.Read())
                {
                    Int64 prodCod = dr.GetSafeValue<Int64>("pved_prod_codigo");
                    Int64 pdQtd = dr.GetSafeValue<Int64>("sum");
                    prodList.Add(new prods() { prodCodRb = prodCod, requestQtd = pdQtd });
                    
                }
                
                dr.Close();
            }
                return prodList;
        }
        //Get the products from a manifest (from transport).
        public static List<prods> getProdsFromManifests(List<String> manifestProd, String dctoNumber)
        {
            AbrirConexao();
            prodList.Clear();

            //Get the manifest transations and get the products.
            for (int i = 0; i < manifestProd.Count; i++)
            {

                String sql = "";
                if (dctoNumber.Contains("8303"))
                {

                    sql = " select mprd_prod_codigo, round(SUM(mprd_qtde), 2) as qutde, round(SUM(momd_valorun * momd_qutde), 2) totalvendido," +
                         " round(SUM(momd_valorun), 2) as valorun, " +
                         " round(SUM(momd_qutde * momd_comissao_un), 2) as comissaototal from movprodd00 " +
                         " left join movmobiledet on momd_transacao = mprd_transacao " +
                         "  where mprd_transacao in (select refd_transacao from refdctos inner join movdctos on refd_transacao = mdoc_transacao" +
                         $"  where refd_transacaoref = '{manifestProd[i]}' and mdoc_dcto_codigo in ({dctoNumber}) and mdoc_status = 'N') " +
                         "  group by mprd_prod_codigo ";
                }
                else
                {
                    sql = " select mprd_prod_codigo, mprd_transacao, SUM(mprd_qtde) as qutde " +
                           " from movprodd00 " +
                           " where mprd_transacao in( " +
                           " select refd_transacao from refdctos " +
                           " inner join movdctos on refd_transacao = mdoc_transacao " +
                           $" where refd_transacaoref = '{manifestProd[i]}' and " +
                           $" mdoc_dcto_codigo in ({dctoNumber}) and mdoc_status = 'N') " +
                           " group by mprd_prod_codigo, mprd_transacao ";
                }


                var cmd = new NpgsqlCommand(sql, mConexao);
                var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    Int64 codProduto = dr.GetSafeValue<Int64>("mprd_prod_codigo");

                    if (dctoNumber.Contains("8402"))
                    {
                        Int64 pdQtd = dr.GetSafeValue<Int64>("qutde");
                        prodList.Add(new prods()
                        {
                            prodCodRb = codProduto,
                            returnProd = pdQtd,

                        });
                    }
                    if (dctoNumber.Contains("8401") || dctoNumber.Contains("8400"))
                    {
                        Int64 pdQtd = dr.GetSafeValue<Int64>("qutde");
                        prodList.Add(new prods()
                        {
                            prodCodRb = codProduto,
                            tradedProd = pdQtd,
                            

                        });
                    }
                    if (dctoNumber.Contains("8303"))
                    {
                        Int64 pdQtd = dr.GetSafeValue<Int64>("qutde");
                        Decimal valueP = dr.GetSafeValue<Decimal>("valorun");
                        Decimal valueC = dr.GetSafeValue<Decimal>("comissaototal");
                        Decimal valueT = dr.GetSafeValue<Decimal>("totalvendido");

                        prodList.Add(new prods()
                        {
                            prodCodRb = codProduto,
                            selledProd = pdQtd,
                            valueProd = valueP,
                            valueComm = valueC,
                            valueTotal = valueT,

                        });
                    }

                }
                dr.Close();
            }
            return prodList;
        }

        private static void AbrirConexao()
        {
            if (mConexao.State != System.Data.ConnectionState.Open)
                mConexao.Open();
        }

        private static void FecharConexao()
        {
            if (mConexao.State != System.Data.ConnectionState.Closed)
                mConexao.Close();
        }

        public static void Dispose()
        {
            FecharConexao();
            prodList.Clear();
        }
    }
}
