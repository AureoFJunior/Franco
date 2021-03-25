using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuxaRelatorio
{
    //Products carrieds in manifest.
    public class prods
    {
        public Int64 selledProd { get; set; }
        public Int64 requestQtd { get; set; }
        public Int64 prodCodRb { get; set; }
        public Int64 tradedProd { get; set; }
        public Int64 returnProd { get; set; }
        public String transationProd { get; set; }
        public Decimal valueProd { get; set; }
        public Decimal valueComm { get; set; }
        public Decimal valueTotal { get; set; }

    }

    
}
