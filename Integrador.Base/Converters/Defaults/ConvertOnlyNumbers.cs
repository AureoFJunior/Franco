using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integradores.Base.Converters.Defaults
{
    public class ConvertOnlyNumbers : IConverter
    {
        public object ConvertValue(object value)
        {
            String texto = new String(value.ToString().Where(Char.IsDigit).ToArray());
            if (texto.Length == 0)
                texto = "0";

            return texto;
        }
    }
}
