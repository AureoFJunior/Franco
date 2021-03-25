using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Apolíneo
{
    public partial class frmProdutos : Form
    {
        public frmProdutos()
        {
            InitializeComponent();
            var lista = getItens();
            setDtGrid(lista);
            
        }
        List<Produtos> getItens()
        {

            var conn = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFch"]);

            String sql = $"select * from fechamento " +
                $" inner join fechamentodet1220 on fech_transacao = fchd_transacao" +
                $" right join produtos on prod_codigo = fchd_prod_codigo ";

            conn.Open();
            var cmd = new NpgsqlCommand(sql, conn);
            var dr = cmd.ExecuteReader();


            List<Produtos> itens = new List<Produtos>();

            //Set the data grid view infos, and convert his values, if needed.
            while (dr.Read())
            {
                var item = new Produtos();
                item.prodCodigo = dr.GetSafeValue<Int64>("prod_codigo");
                item.prodCodigoRB = dr.GetSafeValue<Int64>("prod_codigo_rb");
                item.fsProdNome = dr.GetSafeValue<String>("prod_descricao");

                itens.Add(item);
            }
            conn.Close();
            return itens;

        }
        void setDtGrid(List<Produtos> itens)
        {
            //Personalize the rows and columns.
            this.dtGerenciadorProd.RowsDefaultCellStyle.BackColor = System.Drawing.Color.LightBlue;
            this.dtGerenciadorProd.RowsDefaultCellStyle.ForeColor = System.Drawing.Color.Black;
            this.dtGerenciadorProd.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.LightGoldenrodYellow;
            this.dtGerenciadorProd.AlternatingRowsDefaultCellStyle.ForeColor = System.Drawing.Color.Black;

            this.dtGerenciadorProd.DataSource = itens.ConvertToDatatable();

            if (itens.Count > 0)
            {
                this.dtGerenciadorProd.Columns["prodCodigo"].HeaderText = "Código do Produto Frassini";
                this.dtGerenciadorProd.Columns["prodCodigo"].Width = 120;
                this.dtGerenciadorProd.Columns["prodCodigoRB"].HeaderText = "Código do Produto Rio Branco";
                this.dtGerenciadorProd.Columns["prodCodigoRB"].Width = 120;
                this.dtGerenciadorProd.Columns["fsProdNome"].HeaderText = "Descrição do produto";
                this.dtGerenciadorProd.Columns["fsProdNome"].Width = 240;
               
            }
        

            

            this.dtGerenciadorProd.EnableDoubleBuffered(true);
            this.dtGerenciadorProd.CellValidating -= DtGerenciadorProd_CellValidating;
            this.dtGerenciadorProd.CellEndEdit -= dtGerenciadorProd_CellEndEdit;
            this.dtGerenciadorProd.CellValidating += DtGerenciadorProd_CellValidating;
            this.dtGerenciadorProd.CellEndEdit += dtGerenciadorProd_CellEndEdit;


            //this.dtGerenciadorProd.

        }

        private void DtGerenciadorProd_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            //Validar código, caso já exista, emitir erro e não deixar inserir caracteres inválidos/deixar em branco
            //ou mesmo nulo.
            if (dtGerenciadorProd.IsCurrentCellDirty)
            {
                var compare = e.FormattedValue;
                for (int i = 0; i < dtGerenciadorProd.Rows.Count; i++)
                {
                    var comparador = dtGerenciadorProd[1, i].Value?.ToString() ?? "";
                    if (compare.Equals(comparador))
                    {
                        MessageBox.Show("Codigo já cadastrado ou codigo invalido", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        e.Cancel = true;
                        break;
                    }
                }
            }
            
        }


        private void dtGerenciadorProd_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            void setUpdateProd()
            {
                var conn = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFch"]);
                

                var codigoProduto = dtGerenciadorProd[0, e.RowIndex].Value;

                var codigoRiob = dtGerenciadorProd[1, e.RowIndex].Value;
                var prodDescricao = dtGerenciadorProd[2, e.RowIndex].Value;

                String sql = $" update produtos set ";

                if (codigoRiob != null && codigoRiob != "")
                    sql += $" prod_codigo_rb = '{codigoRiob}' where prod_codigo = '{codigoProduto}' ";

                else if (prodDescricao != null && prodDescricao != "")
                    sql += $" prod_descricao = '{prodDescricao}' where prod_codigo = '{codigoProduto}' ";

                else
                {
                    sql = "";
                }

                conn.Open();
                var cmd = new NpgsqlCommand(sql, conn);
                var dr = cmd.ExecuteReader();
                dr.Read();
                dr.Close();

                conn.Close();
            }

            setUpdateProd();
        }

        
        /*private void dtGerenciadorProd_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            
        }*/
    }
}
