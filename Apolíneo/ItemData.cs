using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apolíneo
{
    public class ItemData<T>
    {
        public T Codigo { get; set; }
        public String Descricao { get; set; }

        public ItemData(T codigo , String descricao)
        {
            this.Codigo = codigo;
            this.Descricao = descricao;
        }
        public override string ToString()
        {
            return this.Descricao ?? "NULL";
        }
    }
}
