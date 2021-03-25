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
    public partial class frmIps : Form
    {
        public frmIps()
        {
            InitializeComponent();

            var fR = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFr"]);
            var rB = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoRb"]);
            var fcH = new NpgsqlConnection(ConfigurationManager.AppSettings["ConexaoFch"]);

            this.label1.Text += fR.Host;
            this.label2.Text += rB.Host;
            this.label3.Text += fcH.Host;
        }

        private void btnSair_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
