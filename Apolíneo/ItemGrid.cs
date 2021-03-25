using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apolíneo
{
    public class ItemGrid
    {

        public String VendCode { get; set; }
        public String VendNome { get; set; }
        public String Manifesto { get; set; }
        public String EmpresaNome { get; set; }
        public Decimal ValorOriginal { get; set; }
        public Decimal ValorLiquido { get; set; }
        public Int64 NumDoc { get; set; }
        public DateTime DateManifesto { get; set; }


    }
}
