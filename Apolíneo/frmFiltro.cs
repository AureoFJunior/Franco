using Microsoft.Office.Interop.Excel;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Apolíneo
{
    public partial class frmFiltro : Form
    {
        private FiltroRelatorio filtro = null;
        //Define IP and DB name as fixed.
        private const String IP = "192.168.194.100";
        private const String BASE = "interativo";
        public frmFiltro()
        {
            InitializeComponent();

            List<ItemData<Int64>> itens = new List<ItemData<long>>();
            itens.Add(new ItemData<long>(-1, "(Todos)"));
            itens.Add(new ItemData<long>(101, "André Dambroz"));
            itens.Add(new ItemData<long>(102, "Alex Dutra"));
            itens.Add(new ItemData<long>(103, "Fabiano Paulleti"));
            itens.Add(new ItemData<long>(104, "Renan Fonseca"));
            itens.Add(new ItemData<long>(105, "Marcelo Nascimento"));
            itens.Add(new ItemData<long>(106, "Leonir"));
            itens.Add(new ItemData<long>(107, "Saimon Soares"));
            itens.Add(new ItemData<long>(108, "Marcelo Schiochet"));
            itens.Add(new ItemData<long>(700, "TESTES"));

            cbVend.DataSource = itens;
        }


        public FiltroRelatorio ShowDialogForResult()
        {
            base.ShowDialog();
            return filtro;

        }

        private void btnConfirmar_Click(object sender, EventArgs ea)
        {
            //Get the value of the txt boxes and storage in variables with the get and set atributes. 
            filtro = new FiltroRelatorio();
            
           

            String msgErro = "";

            //Date validation, and error as send to the user, if the date is incorret or invalid.
            try
            {
                if (maskedTextBox1.MaskCompleted)
                    filtro.DateManifesto = DateTime.ParseExact(maskedTextBox1.Text, "dd/MM/yyyy", CultureInfo.CreateSpecificCulture("pt-BR"));
            }catch(Exception e)
            {
                msgErro = "Data Inválida ";
            }

            if (filtro.DateManifesto == DateTime.MinValue) {
                msgErro = "Data Inválida";
            }

            if (!String.IsNullOrEmpty(msgErro))
            {
                MessageBox.Show(msgErro, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                filtro = null;
                return;
            }

            

            if(cbVend.SelectedItem == null)
            {
                MessageBox.Show("Vendedor não encontrado", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var sel = (cbVend.SelectedItem as ItemData<Int64>);
            if (sel.Codigo == -1)
            {
                filtro.VendedorCode = null;
            }
            else
            {
                filtro.VendedorCode = sel.Codigo.ToString();
            }



            //SyncInterativo.Sincronizar();

            this.Close();
        }

        //Replace the backspace button for enter button.

        private void maskedTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                maskedTextBox1.FindForm().SelectNextControl(maskedTextBox1, true, true, true, false);
                e.SuppressKeyPress = true;
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                cbVend.FindForm().SelectNextControl(cbVend, true, true, true, false);
                e.SuppressKeyPress = true;
            }
        }
    }

}
