
namespace Apolíneo
{
    partial class frmProdutos
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dtGerenciadorProd = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dtGerenciadorProd)).BeginInit();
            this.SuspendLayout();
            // 
            // dtGerenciadorProd
            // 
            this.dtGerenciadorProd.AllowUserToAddRows = false;
            this.dtGerenciadorProd.AllowUserToDeleteRows = false;
            this.dtGerenciadorProd.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(44)))), ((int)(((byte)(51)))));
            this.dtGerenciadorProd.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dtGerenciadorProd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtGerenciadorProd.Location = new System.Drawing.Point(0, 0);
            this.dtGerenciadorProd.Name = "dtGerenciadorProd";
            this.dtGerenciadorProd.RowHeadersVisible = false;
            this.dtGerenciadorProd.Size = new System.Drawing.Size(501, 450);
            this.dtGerenciadorProd.TabIndex = 0;
            // 
            // frmProdutos
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(44)))), ((int)(((byte)(51)))));
            this.ClientSize = new System.Drawing.Size(501, 450);
            this.Controls.Add(this.dtGerenciadorProd);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmProdutos";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Gerenciador de Produtos";
            ((System.ComponentModel.ISupportInitialize)(this.dtGerenciadorProd)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dtGerenciadorProd;
    }
}