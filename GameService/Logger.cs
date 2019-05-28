using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class Logger
    {
        public static void Info(string msg)
        {
            Console.WriteLine(msg);
        }

        public static void Error(string msg, Exception e = null)
        {
            Console.WriteLine($"{msg} - {(e == null ? "" : e.Message)}");
        }
    }
}
