using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClosedXML.Excel;
using Integradores.Base;
using Npgsql;
using PuxaRelatorio.Sync;

namespace Apolíneo
{
    public partial class Form1 : Form
    {

        int x;
        int y;
        int mov;

        public Form1()
        {
            InitializeComponent();

            var fR = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFr"]);
            var rB = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoRb"]);
            var fcH = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFch"]);

            //this.label1.Text += fR.Host;
            //this.label2.Text += rB.Host;
            //this.label3.Text += fcH.Host;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //this.Location = Screen.AllScreens[0].WorkingArea.Location;
        }

        protected override void WndProc(ref Message m) //another way to move windows form
        {
            switch (m.Msg)
            {
                case 0x84:
                    base.WndProc(ref m);
                    if ((int)m.Result == 0x1)
                        m.Result = (IntPtr)0x2;
                    return;
            }

            base.WndProc(ref m);
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            mov = 1;
            x = e.X;
            y = e.Y;
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (mov == 1)
            {
                this.SetDesktopLocation(MousePosition.X - x, MousePosition.Y - y);
            }
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            mov = 0;
        }

        private void btnSair_Click(object sender, EventArgs e)
        {
            this.Close();
        }




        // private NpgsqlConnection mConexao = new NpgsqlConnection($"Server=172.30.100.100;User Id=postgres;" +
        //                                                            "Password=123456;Database=whiteriver;");
        private String CONTAS_TROCAS = "8401;8400";
        private String CONTAS_RETORNO = "8402";
        private String CONTAS_PEDIDO = "7505";
        private String CONTAS_MANIFESTO = "8756";
        private String CONTAS_VENDA = "8303";
        public String CONTAS_TROCA_FS = "004";
        //
        //
        // TROCAR IP E NOME DA DATABASE EM TODAS, TODAS, TODAS AS CONEXÕES!!!!!
        //
        //
        List<Object> infoRel = new List<Object>();

        private async void btnStart_Click_1(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            //Initialize the filter window.
            frmFiltro frmFiltro = new frmFiltro();
            var filtr = frmFiltro.ShowDialogForResult();


            if (filtr != null)
            {
                #region Ler dados do produtos
                //mConexao.Open();
                //Requests recebe uma lista com as transações dos pedidos.

                //carrega da tabela fecha pedidos para descobrir os vendedores

                var requestsRb = BancoPadaria.getRequests(filtr);
                //Loop, para cada pedido, ele chama o método de obter referenciadas e passa como parâmetro 
                //a conta(nesse caso, do manifesto) e a lista de pedidos na posição iterável "i" para obter os manifestos
                // retornando um erro caso não houver manifesto vinculado ao pedido.
                for (int i = 0; i < requestsRb.Count; i++)
                {
                    await CallImport(requestsRb[i], filtr.DateManifesto);
                }
                
                #endregion
            }

            BancoPadaria.Dispose();
            MessageBox.Show("Execução finalizada", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnStart.Enabled = true;

            

        }

        private Task CallImport(String transacaoPedido, DateTime dataFiltro)
        {
             return Task.Run(() => { ImportaDados(transacaoPedido, dataFiltro); });

        }

        private void ImportaDados(String transacaoPedido, DateTime dataFiltro)
        {


            var transacaoPedidoRb = setAndGetSequence();

            //Set the main header.
            Cabecalho cabecalho = new Cabecalho();
            cabecalho.dateImport = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            cabecalho.dateFech = dataFiltro.ToString("yyyy-MM-dd");

            cabecalho.transRequest = transacaoPedido;
            cabecalho.importSeq = transacaoPedidoRb;
            cabecalho.status = "N";


            String codVendedorAtualPR = BancoPadaria.obterCodigoVendedor(transacaoPedido);

            var relacaoVendedores = BancoFechamento.getVendedores();

            String codVendedorFrassini = relacaoVendedores[codVendedorAtualPR];

            VerificaFechamento(codVendedorFrassini, cabecalho.dateFech);

            cabecalho.vendCod = codVendedorFrassini;

            var detalhesGeral = carregarDadosPadaria(transacaoPedido, dataFiltro);

            setProdCodFrass(ref detalhesGeral);

            //var produtosFechamento = setDet(detalhesGeral, transacaoPedido);

            String manifestoFr = "";
            carregarDadosFrassini(transacaoPedido, dataFiltro, detalhesGeral, cabecalho.vendCod, out manifestoFr);
            cabecalho.transManifest = manifestoFr;


            var comuns = detalhesGeral.Where(t => t.prodCodigo > 0 && t.prodCodigoRB > 0);

            setTableDets(detalhesGeral, cabecalho);



        }

        private void VerificaFechamento(String vendedor, String dataFech)
        {
            var conne = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFch"]);
            conne.Open();
            String sql = $"select fech_data_fechamento, fech_vend_codigo from fechamento where fech_data_fechamento = '{dataFech}' and fech_vend_codigo = '{vendedor}' and fech_status = 'N'";
            var cmd = new NpgsqlCommand(sql, conne);
            //cmd.Parameters.AddWithValue("fech_data_fechamento", NpgsqlTypes.NpgsqlDbType.Date, dataFech);
            var dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                DateTime comparaData = dr.GetSafeValue<DateTime>("fech_data_fechamento");
                String comparaVendedor = dr.GetSafeValue<String>("fech_vend_codigo");

                if (comparaData.ToString("yyyy-MM-dd") == dataFech && comparaVendedor == vendedor)
                {
                    var dialogResult = MessageBox.Show($"Detectamos que o fechamento do vendedor {vendedor} da data {dataFech} já foi importado, deseja cancelar o movimento e reimportar?", "Opções: ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dialogResult == DialogResult.Yes)
                    {
                        //var transaction = conne.BeginTransaction();
                        var newConne = conne.CloneWith(conne.ConnectionString);
                        try
                        {
                            sql = $" update fechamento set fech_status = 'C' where fech_data_fechamento = '{dataFech}' " +
                                  $" and fech_vend_codigo = '{vendedor}'";

                            newConne.Open();
                            cmd = new NpgsqlCommand(sql, newConne);

                            dr = cmd.ExecuteReader();
                            dr.Read();
                            dr.Close();

                            newConne.Close();
                            //int linhasInseridas = cmd.ExecuteNonQuery();
                            //transaction.Commit();


                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Erro inesperado: " + e.Message.ToString(), "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        return;
                    }
                    conne.Close();
                }
            }

        }


        public void setTableDets(List<Det> detalhesGeral, Cabecalho cabecalho)
        {

            var conn = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFch"]);


            conn.Open();
            var transaction = conn.BeginTransaction();


            try
            {

                /*String sql = " INSERT INTO fechamento(fech_transacao, fech_vend_codigo, fech_status, fech_data_fechamento," +
                             " fech_data_import, fech_manifesto, fech_pr_transpedido )" +
                            $" VALUES ('{cabecalho.importSeq}', '{cabecalho.vendCod}', '{cabecalho.status}', '{cabecalho.dateFech}'," +
                            $" '{cabecalho.dateImport}', '{cabecalho.transManifest}', '{cabecalho.transRequest}')";*/


                String sql = " INSERT INTO fechamento(fech_transacao, fech_vend_codigo, fech_status, fech_data_fechamento," +
                             " fech_data_import, fech_manifesto, fech_pr_transpedido )" +
                            $" VALUES (:fech_transacao, :fech_vend_codigo, :fech_status, :fech_data_fechamento, " +
                            $" :fech_data_import, :fech_manifesto, :fech_pr_transpedido )";

                var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("fech_transacao", cabecalho.importSeq);
                cmd.Parameters.AddWithValue("fech_vend_codigo", cabecalho.vendCod);
                cmd.Parameters.AddWithValue("fech_status", cabecalho.status);
                cmd.Parameters.AddWithValue("fech_data_fechamento", new DateTimeConverter().ConvertFromString(cabecalho.dateFech));
                cmd.Parameters.AddWithValue("fech_data_import", new DateTimeConverter().ConvertFromString(cabecalho.dateImport));
                cmd.Parameters.AddWithValue("fech_manifesto", cabecalho.transManifest);
                cmd.Parameters.AddWithValue("fech_pr_transpedido", cabecalho.transRequest);


                int linhasInseridas = cmd.ExecuteNonQuery();

                for (int i = 0; i < detalhesGeral.Count; i++)
                {
                    /*sql = " INSERT INTO fechamentodet1220(fchd_transacao, fchd_prod_codigo, fchd_qtde_carga, fchd_pr_qtde_venda, fchd_pr_qtde_troca, fchd_pr_qtde_retorno, " +
                                 "fchd_fr_qtde_venda, fchd_fr_qtde_troca, fchd_fr_qtde_retorno, fchd_pr_valorprod, fchd_pr_valortotal, fchd_pr_valorcomissao )" +
                                $" VALUES ('{cabecalho.importSeq}', '{detalhesGeral[i].prodCodigo}', '{detalhesGeral[i].qtdeCarga}', " +
                                $" '{detalhesGeral[i].prQtdeVenda}', '{detalhesGeral[i].prQtdeTroca}', '{detalhesGeral[i].prQtdeRetorno}', " +
                                $" '{detalhesGeral[i].fsQtdeVenda}', '{detalhesGeral[i].fsQtdeTroca}', 0, '{detalhesGeral[i].prValorProd}'," +
                                $" '{detalhesGeral[i].prValorTotal}', '{detalhesGeral[i].prComissao}')";*/


                    sql = " INSERT INTO fechamentodet1220(fchd_transacao, fchd_prod_codigo, fchd_qtde_carga, fchd_pr_qtde_venda, fchd_pr_qtde_troca, fchd_pr_qtde_retorno, " +
                                 " fchd_fr_qtde_venda, fchd_fr_qtde_troca, fchd_fr_qtde_retorno, fchd_pr_valorprod, fchd_pr_valortotal, fchd_pr_valorcomissao )" +
                                 $" VALUES (:fchd_transacao, :fchd_prod_codigo, :fchd_qtde_carga,:fchd_pr_qtde_venda, :fchd_pr_qtde_troca, :fchd_pr_qtde_retorno," +
                                 $" :fchd_fr_qtde_venda, :fchd_fr_qtde_troca, :fchd_fr_qtde_retorno, :fchd_pr_valorprod, :fchd_pr_valortotal, :fchd_pr_valorcomissao)";

                    cmd = new NpgsqlCommand(sql, conn);

                    cmd.Parameters.AddWithValue("fchd_transacao", cabecalho.importSeq);
                    cmd.Parameters.AddWithValue("fchd_prod_codigo", detalhesGeral[i].prodCodigo);
                    cmd.Parameters.AddWithValue("fchd_qtde_carga", detalhesGeral[i].qtdeCarga);
                    cmd.Parameters.AddWithValue("fchd_pr_qtde_venda", detalhesGeral[i].prQtdeVenda);
                    cmd.Parameters.AddWithValue("fchd_pr_qtde_troca", detalhesGeral[i].prQtdeTroca);
                    cmd.Parameters.AddWithValue("fchd_pr_qtde_retorno", detalhesGeral[i].prQtdeRetorno);
                    cmd.Parameters.AddWithValue("fchd_fr_qtde_venda", detalhesGeral[i].fsQtdeVenda);
                    cmd.Parameters.AddWithValue("fchd_fr_qtde_troca", detalhesGeral[i].fsQtdeTroca);
                    cmd.Parameters.AddWithValue("fchd_fr_qtde_retorno", 0);
                    cmd.Parameters.AddWithValue("fchd_pr_valorprod", detalhesGeral[i].prValorProd);
                    cmd.Parameters.AddWithValue("fchd_pr_valortotal", detalhesGeral[i].prValorTotal);
                    cmd.Parameters.AddWithValue("fchd_pr_valorcomissao", detalhesGeral[i].prComissao);


                    linhasInseridas = cmd.ExecuteNonQuery();

                }
                transaction.Commit();
            }
            catch (Exception e)
            {
                transaction.Rollback();
                MessageBox.Show("Erro inesperado: " + e.Message.ToString(), "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


            conn.Close();

        }

        public List<Det> carregarDadosPadaria(String transacaoPedido, DateTime dataFiltro)
        {
            var manifestosRb = BancoPadaria.obterTransacaoReferenciadas(transacaoPedido, CONTAS_MANIFESTO);

            //if (manifestosRb.Count == 0)
            //{
            //MessageBox.Show("Nenhum manifesto encontrado na data informada", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //return;
            //}

            var detalhes = new List<Det>();
            //BancoPadaria.obterTransacaoReferenciadas(manifestos, CONTAS_VENDA);
            //Variável que chama a função de obtenção de produtos do PEDIDO, retornando uma lista do tipo "produtos originais"
            //com suas próprias propriedades. 
            //Carregado.
            var produtosPedidoRb = BancoPadaria.getProdsFromRequests(new List<String>() { transacaoPedido }, CONTAS_PEDIDO);

            detalhes.AddRange(produtosPedidoRb.Select(t => new Det() { prodCodigoRB = t.prodCodRb, qtdeCarga = t.requestQtd }));


            //Variável que chama a função de obtenção de produtos do MANIFESTO, retornando uma lista do tipo "produtos manifesto"
            //com suas próprias propriedades, incluindo quantidade de venda, retorno e troca.

            //Vendas.
            var produtosVendidosRb = BancoPadaria.getProdsFromManifests(manifestosRb, CONTAS_VENDA.SplitSQL(';'));

            for (int j = 0; j < produtosVendidosRb.Count; j++)
            {
                var itemCarregado = detalhes.Where(t => t.prodCodigoRB == produtosPedidoRb[j].prodCodRb).FirstOrDefault();
                if (itemCarregado == null)
                {
                    itemCarregado = new Det()
                    {
                        prodCodigoRB = produtosPedidoRb[j].prodCodRb
                    };
                    detalhes.Add(itemCarregado);
                }
                itemCarregado.prQtdeVenda = produtosPedidoRb[j].selledProd;
                itemCarregado.prValorProd = produtosPedidoRb[j].valueProd;
                itemCarregado.prComissao = produtosPedidoRb[j].valueComm;
                itemCarregado.prValorTotal = produtosPedidoRb[j].valueTotal;

            }

            //Trocas.
            var produtosTrocaRb = BancoPadaria.getProdsFromManifests(new List<String>() { transacaoPedido }, CONTAS_TROCAS.SplitSQL(';'));

            for (int j = 0; j < produtosTrocaRb.Count; j++)
            {
                var itemTroca = detalhes.Where(t => t.prodCodigoRB == produtosTrocaRb[j].prodCodRb).FirstOrDefault();
                if (itemTroca == null)
                {
                    itemTroca = new Det()
                    {
                        prodCodigoRB = produtosTrocaRb[j].prodCodRb
                    };
                    detalhes.Add(itemTroca);
                }
                itemTroca.prQtdeTroca = produtosTrocaRb[j].tradedProd;
            }

            //Retornos.
            var produtosRetornoRb = BancoPadaria.getProdsFromManifests(new List<String>() { transacaoPedido }, CONTAS_RETORNO.SplitSQL(';'));

            for (int k = 0; k < produtosRetornoRb.Count; k++)
            {
                var itemRetorno = detalhes.Where(t => t.prodCodigoRB == produtosRetornoRb[k].prodCodRb).FirstOrDefault();
                if (itemRetorno == null)
                {
                    itemRetorno = new Det()
                    {
                        prodCodigoRB = produtosRetornoRb[k].prodCodRb,
                    };
                    detalhes.Add(itemRetorno);
                }
                itemRetorno.prQtdeRetorno = produtosRetornoRb[k].returnProd;
            }

            return detalhes;
        }

        private void carregarDadosFrassini(string transacaoPedido, DateTime dataFiltro, List<Det> detalhesGeral, String vendedor, out String manifestoFr)
        {
            //Carrega trocas, retorno e vendas da Frassini, adicioando-as ao model "detalhes".
            var manifestosFs = BancoFrassini.getManifesto(dataFiltro, vendedor);
            manifestoFr = manifestosFs;
            //var a = detalhesGeral;
            List<Det> integra = new List<Det>();

            BancoFrassini.getTradeFrass(CONTAS_TROCA_FS, detalhesGeral, dataFiltro, vendedor);

            BancoFrassini.getSelledFrass(manifestosFs, "7005", detalhesGeral);
            /*for (int i = 0; i < detalhesGeral.Count; i++) 
            {
                detalhesGeral[i].fsQtdeTroca = integra[i].fsQtdeTroca;
            }*/


            /*var detDet = detalhesGeral;
            for (int i = 0; i < detalhesGeral.Count; i++)
            {

                detDet[i].fs = 0;
            }*/
        }

        public Int64 setAndGetSequence()
        {
            //Cria e avança uma sequência, a qual é a atribuida a transação gerada no momento da importação.
            var nConexao = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFch"]);

            nConexao.Open();
            String sql = " select  nextval('public.\"importSequence\" ')";
            List<Int64> numberSequence = new List<Int64>();
            var cmd2 = new NpgsqlCommand(sql, nConexao);


            Int64 ret = Convert.ToInt64(cmd2.ExecuteScalar());
            cmd2.Dispose();
            nConexao.Close();
            return ret;

        }

        public void setProdCodFrass(ref List<Det> detalhes)
        {
            //Chave-Valor, transforma os produtos de ambas em um dicionário para juntá-las.
            var dic = setDict();
            foreach (var item in detalhes)
            {
                if (dic.ContainsKey(item.prodCodigoRB))
                {
                    item.prodCodigo = dic[item.prodCodigoRB];
                }
                else
                {

                    //MessageBox.Show($"Código inexistente {item.prodCodigoRB}-", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    item.prodCodigo = -1;
                }
            }
        }

        public Dictionary<Int64, Int64> setDict()
        {
            String sql2 = " select prod_codigo, prod_codigo_rb from produtos  ";
            var jConexao = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFch"]);

            jConexao.Open();
            var cmd = new NpgsqlCommand(sql2, jConexao);
            var dr = cmd.ExecuteReader();

            var codigos = new Dictionary<Int64, Int64>();
            while (dr.Read())
            {
                Int64 prodCodFs = dr.GetSafeValue<Int64>("prod_codigo");
                Int64 prodCodRB = dr.GetSafeValue<Int64>("prod_codigo_rb");
                if (prodCodRB > 0)
                {
                    try
                    {
                        codigos.Add(prodCodRB, prodCodFs);
                    }
                    catch (Exception f)
                    {
                        MessageBox.Show(f.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }


            }
            jConexao.Close();
            return codigos;
        }

        /*public List<Det> setDet(List<Det> c, String transacaoPedido)
        {
            for (int i = 0; i < c.Count; i++)
            {
                c[i].prValorProd = 0;
                c[i].prValorTotal = 0;
                c[i].prComissao = 0;

                c[i].fsQtdeRetorno = 0;
                //c[i].fsQtdeTroca = 0;
                //c[i].fsQtdeVenda = 0;
            }

            return c;
        }*/

        private void btnConsultar_Click_1(object sender, EventArgs e)
        {
            btnConsultar.Enabled = false;
            frmFiltro frmFiltro = new frmFiltro();
            var filtr = frmFiltro.ShowDialogForResult();
            List<Det> itens = new List<Det>();
            var item = getItens(filtr);
            setDtGrid(item);
            btnConsultar.Enabled = true;


        }
        List<Det> getItens(FiltroRelatorio filtr)
        {
            if (filtr != null)
            {
                var conn = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFch"]);


                String sql = $"select * from fechamento " +
                    $" inner join fechamentodet1220 on fech_transacao = fchd_transacao" +
                    $" left join produtos on prod_codigo = fchd_prod_codigo " +
                    $" where fech_data_fechamento = '{filtr.DateManifesto}' and fech_status = 'N' ";
                if (!String.IsNullOrEmpty(filtr.VendedorCode))
                    sql += $" and fech_vend_codigo = '{filtr.VendedorCode}' ";

                conn.Open();
                var cmd = new NpgsqlCommand(sql, conn);
                var dr = cmd.ExecuteReader();


                List<Det> itens = new List<Det>();

                //Set the data grid view infos, and convert his values, if needed.
                while (dr.Read())
                {
                    var item = new Det();
                    item.fchTransacao = dr.GetSafeValue<String>("fchd_transacao");
                    item.prodCodigo = dr.GetSafeValue<Int64>("fchd_prod_codigo");
                    item.prodCodigoRB = dr.GetSafeValue<Int64>("prod_codigo_rb");
                    item.fsProdNome = dr.GetSafeValue<String>("prod_descricao");
                    item.qtdeCarga = dr.GetSafeValue<Int64>("fchd_qtde_carga");
                    item.prQtdeVenda = dr.GetSafeValue<Int64>("fchd_pr_qtde_venda");
                    item.prQtdeTroca = dr.GetSafeValue<Int64>("fchd_pr_qtde_troca");
                    item.prQtdeRetorno = dr.GetSafeValue<Int64>("fchd_pr_qtde_retorno");
                    item.fsQtdeVenda = dr.GetSafeValue<Int64>("fchd_fr_qtde_venda");
                    item.fsQtdeTroca = dr.GetSafeValue<Int64>("fchd_fr_qtde_troca");
                    item.fsQtdeRetorno = dr.GetSafeValue<Int64>("fchd_fr_qtde_retorno");
                    item.prValorProd = dr.GetSafeValue<Decimal>("fchd_pr_valorprod");
                    item.prValorTotal = dr.GetSafeValue<Decimal>("fchd_pr_valortotal");
                    item.prComissao = dr.GetSafeValue<Decimal>("fchd_pr_valorcomissao");



                    itens.Add(item);
                }
                conn.Close();
                return itens;
            }
            else
            {
                List<Det> vazio = new List<Det>();
                return vazio;
            }

        }

        DataTable setDtGrid(List<Det> itens)
        {
            var dtGrid = new DataTable();
            //Personalize the rows and columns.
            this.dtGrid.RowsDefaultCellStyle.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.dtGrid.RowsDefaultCellStyle.ForeColor = System.Drawing.Color.Black;
            this.dtGrid.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.SkyBlue;
            this.dtGrid.AlternatingRowsDefaultCellStyle.ForeColor = System.Drawing.Color.Black;

            this.dtGrid.DataSource = itens.ConvertToDatatable();

            if (itens.Count > 0)
            {
                this.dtGrid.Columns["fchTransacao"].HeaderText = "Transação";
                //this.dtGrid.Columns["fchTransacao"].Width = 80;

                this.dtGrid.Columns["prodCodigo"].HeaderText = "Código Produto";
                this.dtGrid.Columns["prodCodigo"].Width = 120;
                this.dtGrid.Columns["prodCodigoRB"].HeaderText = "Código Rio Branco";
                this.dtGrid.Columns["prodCodigoRB"].Width = 120;
                this.dtGrid.Columns["fsProdNome"].HeaderText = "Nome";
                this.dtGrid.Columns["fsProdNome"].Width = 240;
                this.dtGrid.Columns["qtdeCarga"].HeaderText = "Qtd Carregada";
                this.dtGrid.Columns["qtdeCarga"].Width = 120;
                this.dtGrid.Columns["prQtdeVenda"].HeaderText = "Qtd Venda Rio Branco";
                this.dtGrid.Columns["prQtdeVenda"].Width = 120;
                this.dtGrid.Columns["prQtdeTroca"].HeaderText = "Qtd Troca Rio Branco";
                this.dtGrid.Columns["prQtdeTroca"].Width = 120;
                this.dtGrid.Columns["prQtdeRetorno"].HeaderText = "Qtd Retorno Rio Branco";
                this.dtGrid.Columns["prQtdeRetorno"].Width = 120;
                this.dtGrid.Columns["fsQtdeVenda"].HeaderText = "Qtd Venda Frassini";
                this.dtGrid.Columns["fsQtdeVenda"].Width = 120;
                this.dtGrid.Columns["fsQtdeTroca"].HeaderText = "Qtd Troca Frassini";
                this.dtGrid.Columns["fsQtdeTroca"].Width = 120;
                this.dtGrid.Columns["fsQtdeRetorno"].HeaderText = "Qtd Retorno Frassini";
                this.dtGrid.Columns["fsQtdeRetorno"].Width = 120;
                this.dtGrid.Columns["prValorProd"].HeaderText = "Valor Produto Rio Branco";
                this.dtGrid.Columns["prValorProd"].Width = 120;
                this.dtGrid.Columns["prValorTotal"].HeaderText = "Valor Total Produto Rio Branco";
                this.dtGrid.Columns["prValorTotal"].Width = 120;
                this.dtGrid.Columns["prComissao"].HeaderText = "Comissão";
                this.dtGrid.Columns["prComissao"].Width = 120;
            }





            this.dtGrid.EnableDoubleBuffered(true);
            return dtGrid;

        }

        private async void btnExportar_Click_1(object sender, EventArgs e)
        {
            btnExportar.Enabled = false;
            //setExcell();
            //setXlsFile();
            //var item = getItens(filtr);
            await CallExcel(this.dtGrid.DataSource as DataTable);
            btnExportar.Enabled = true;
            MessageBox.Show("Exportado com exito!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Task CallExcel(DataTable table)
        {
            return Task.Run(() => { setExcelNotExcelFile(table); });
        }

        private void setExcelNotExcelFile(DataTable table)
        {
            //var dt = setDtGrid(item);
            //Exporting to Excel
            string caminho = "C:\\Excel\\";
            if (!Directory.Exists(caminho))
            {
                Directory.CreateDirectory(caminho);
            }
            using (XLWorkbook wb = new XLWorkbook())
            {
                var data = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                //var hora = DateTime.Now.ToString("HH:mm");
                //hora.Replace(':', '-');
                wb.Worksheets.Add(table, "Planilha");
                wb.SaveAs(caminho + $"Exportacao{data}.xlsx");
            }

        }
        private void setXlsFile()
        {
            try
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "Excel Documents (*.xls)|*.xls";
                saveFileDialog1.FileName = "Planilia.xls";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string fname = saveFileDialog1.FileName;
                    StreamWriter wr = new StreamWriter(fname);
                    for (int i = 0; i < dtGrid.Columns.Count; i++)
                    {
                        wr.Write(dtGrid.Columns[i].ToString().ToUpper() + "\t");
                    }
                    wr.WriteLine();

                    //write rows to excel file
                    for (int i = 0; i < (dtGrid.Rows.Count); i++)
                    {
                        for (int j = 0; j < dtGrid.Columns.Count; j++)
                        {
                            if (dtGrid.Rows[i].Cells[j].Value != null)
                            {
                                wr.Write(Convert.ToString(dtGrid.Rows[i].Cells[j].Value) + "\t");
                            }
                            else
                            {
                                wr.Write("\t");
                            }
                        }
                        //go to next line
                        wr.WriteLine();
                    }
                    //close file
                    wr.Close();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error Create Excel Sheet!");
            }
        }
        private void setExcell()
        {
            //Convert to a Excel format and save it with the correct columns.
            if (dtGrid.Rows.Count > 0)
            {
                Microsoft.Office.Interop.Excel.Application xcelApp = new Microsoft.Office.Interop.Excel.Application();
                xcelApp.Application.Workbooks.Add(Type.Missing);
                for (int i = 1; i < dtGrid.Columns.Count + 1; i++)
                {
                    xcelApp.Cells[1, i] = dtGrid.Columns[i - 1].HeaderText;

                }

                for (int i = 0; i < dtGrid.Rows.Count; i++)
                {
                    for (int j = 0; j < dtGrid.Columns.Count; j++)
                    {
                        Object valor = dtGrid.Rows[i].Cells[j].Value;
                        if (valor is DateTime? && (valor as DateTime?).HasValue)
                        {
                            valor = (valor as DateTime?).Value.ToString("dd/MM/yyyy");
                        }
                        xcelApp.Cells[i + 2, j + 1].EntireColumn.NumberFormat = "";
                        xcelApp.Cells[i + 2, j + 1] = valor;
                        xcelApp.Cells[i + 2, j + 1].EntireColumn.NumberFormat = "";

                    }

                }
                xcelApp.Columns.AutoFit();
                xcelApp.Visible = true;

            };
        }

        private void btnGerenciar_Click_1(object sender, EventArgs e)
        {
            btnGerenciar.Enabled = false;
            frmProdutos produtos = new frmProdutos();
            produtos.ShowDialog();
            btnGerenciar.Enabled = true;

        }


        private void btnSync_Click_1(object sender, EventArgs e)
        {
            btnSync.Enabled = false;
            ConfigRuntime.ConectionBancoDestino = ConfigurationManager.AppSettings["ConexaoFch"];

            ConfigRuntime.ConectionBancoOrigem = ConfigurationManager.AppSettings["ConexaoFr"];
            ConfigRuntime.TypeDatabasaOrigem = TypeDatabase.Postgresql;
            var sync = new SyncInterativo.SyncProdutosInterativo();
            sync.SyncNow();
            sync.Engine.OnError += Engine_OnError;
            btnSync.Enabled = true;
            MessageBox.Show("Sincronizado com exito!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Engine_OnError(Exception e, string sql, ref bool continuar)
        {
            MessageBox.Show("Erro ao importar:" + e.Message.ToString(), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            continuar = false;
        }

        private void btnIp_Click(object sender, EventArgs e)
        {
            frmIps ips = new frmIps();
            ips.ShowDialog();


        }
    }
}



    /*
        private async void btnExportar_Click(object sender, EventArgs e)
        {
            btnExportar.Enabled = false;
            btnExportar.Enabled = true;

        }
              
    


    }
    */



