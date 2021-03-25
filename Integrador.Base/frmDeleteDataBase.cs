using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Integradores.Base
{
    public partial class frmDeleteDataBase : Form
    {
        private String mConnection = "";
        public frmDeleteDataBase(String connection)
        {
            InitializeComponent();

            List<String> tables = ReadTables(connection);

            this.mConnection = connection;

            (this.checkedListBox1 as ListBox).DataSource = tables;

            
            this.Shown += (object sender, EventArgs e) =>
            {
                if(this.Owner != null)
                    Location = new Point(Owner.Location.X + Owner.Width / 2 - Width / 2, Owner.Location.Y + Owner.Height / 2 - Height / 2);
            };

        }

        public List<String> ReadTables(String connection)
        {
            Regex regex = new Regex(@"(\w+)=(\w+)");

            regex = new Regex(@"[^;]*");
            MatchCollection match = regex.Matches(connection);

            String nomeBanco = "Erro de leitura";
            foreach (Match m in match)
            {
                String grupo = m.Groups[0].Value;
                if (grupo.ToLower().Trim().StartsWith("database"))
                {
                    regex = new Regex(@"[^=]*");
                    var a = regex.Matches(m.Groups[0].Value);
                    nomeBanco = a[2].Groups[0].Value;
                    break;
                }
            }

            this.lblStatus.Text = "Database: " + nomeBanco;

            NpgsqlConnection pgsqlConnectionOrigem = new NpgsqlConnection(connection);
            pgsqlConnectionOrigem.Open();

            String sql = "SELECT * FROM information_schema.tables WHERE table_catalog = '" + nomeBanco + "' and table_schema = 'public'";

            NpgsqlCommand command = new NpgsqlCommand(sql, pgsqlConnectionOrigem);
            NpgsqlDataReader dr = command.ExecuteReader(System.Data.CommandBehavior.Default);
            
            List<String> tables = new List<string>();
            while (dr.Read())
            {
                String tabela = dr["table_name"].ToString();

                tables.Add(tabela);
            }

            dr.Close();
            command.Dispose();

            pgsqlConnectionOrigem.Close();

            return tables;
            
        }

        private void btnApagar_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Deseja continuar?", "Aviso", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No)
                return;

            NpgsqlConnection pgsqlConnectionOrigem = new NpgsqlConnection(mConnection);
            pgsqlConnectionOrigem.Open();

            for(int i = 0; i < this.checkedListBox1.CheckedItems.Count; i++)
            {
                String tabela = this.checkedListBox1.CheckedItems[i].ToString();
                
                String sql = "DELETE FROM " + tabela;

                this.lblStatus.Text = "Deletando tabela " + tabela;

                Application.DoEvents();
                Application.DoEvents();

                NpgsqlCommand command = new NpgsqlCommand(sql, pgsqlConnectionOrigem);

                command.ExecuteNonQuery();
                command.Dispose();

                Thread.Sleep(100);
            }

            this.lblStatus.Text = "Tarefa concluida";

            MessageBox.Show("Tarefa concluida", "Mensagem", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
