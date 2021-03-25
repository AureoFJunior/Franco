using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integradores.Base
{
    public enum TypeDatabase
    {
        Postgresql = 1,
        Oracle = 2,
        Firebird = 3 
    }


    public static class ConfigRuntime
    {
        public static String ConectionBancoOrigem { get; set; }
        public static String ConectionBancoDestino { get; set; }
        public static TypeDatabase TypeDatabasaOrigem { get; set; } = TypeDatabase.Postgresql;

        //public static String ConectionBancoIntegrador { get; set; }
    }
}
