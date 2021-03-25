using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integradores.Base.Converters.Defaults
{
    public class ConvertNumberPhone : IConverter
    {
        public object ConvertValue(object value)
        {
            Object retorno = DBNull.Value;
            if (value != null)
            {
                retorno = string.Join("", value.ToString().ToCharArray().Where(Char.IsDigit));

                if (retorno.ToString().Length > 12)
                {
                    retorno = retorno.ToString().Substring(0, 12);
                }
                //retorno = Regex.Match(value.ToString(), @"\d+").Value;
            }
            return retorno;

        }
    }
}
