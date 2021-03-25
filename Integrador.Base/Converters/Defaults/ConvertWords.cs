using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integradores.Base.Converters.Defaults
{
    public class ConvertWords : IConverter
    {
        Int32 mMaxLenght = -1;
        String mDefaultIfEmpty = null;
        FormaterType mType = FormaterType.None;

        public enum FormaterType
        {
            None,
            UpperCase,
            TitleCase
        }

        public ConvertWords(Int32 maxLenght, FormaterType formaterType = FormaterType.TitleCase)
        {
            mMaxLenght = maxLenght;
            mType = formaterType;
        }

        public ConvertWords(String defaultIfEmpty)
        {
            this.mDefaultIfEmpty = defaultIfEmpty;
        }

        public ConvertWords(FormaterType formaterType)
        {
            mType = formaterType;
        }

        public ConvertWords()
        {

        }

        public object ConvertValue(object value)
        {
            Object retorno = DBNull.Value;
            if (value != null)
            {
                retorno = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToString().ToLower());
                //se tiver sido configurado
                if (mMaxLenght > 0)
                {
                    //se for maior, entao aplica o substring
                    if (retorno.ToString().Length > mMaxLenght)
                    {
                        retorno = retorno.ToString().Substring(0, mMaxLenght);
                    }
                }

                if (mDefaultIfEmpty != null && String.IsNullOrEmpty(value.ToString()))
                {
                    retorno = mDefaultIfEmpty;
                }
                //converte o encoding
                if (mType == FormaterType.TitleCase)
                {
                    Encoding iso = Encoding.GetEncoding("ISO-8859-1");
                    Encoding utf8 = Encoding.UTF8;
                    byte[] utfBytes = utf8.GetBytes(retorno.ToString());
                    byte[] isoBytes = Encoding.Convert(utf8, iso, utfBytes);
                    retorno = iso.GetString(isoBytes);
                }
                else if (mType == FormaterType.UpperCase)
                {
                    retorno = retorno.ToString().ToUpper();
                }
                
            }
            return retorno;
        }
    }
}
