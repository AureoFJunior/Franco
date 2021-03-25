using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integradores.Base.Converters
{
    public interface IConverter
    {
        Object ConvertValue(Object value);
    }
}
