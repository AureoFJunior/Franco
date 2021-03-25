using Integradores.Base.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuxaRelatorio.Sync
{
    public class ConvertToInt64 : IConverter
    {
        public object ConvertValue(object value)
        {
            return Convert.ToInt64(value);
        }
    }
}
