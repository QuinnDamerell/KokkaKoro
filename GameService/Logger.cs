using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class Logger
    {
        public static void Info(string msg)
        {
            Write(msg);
            System.Diagnostics.Trace.TraceInformation(msg);
        }

        public static void Error(string msg, Exception e = null)
        {
            Write($"{msg} - {(e == null ? "" : e.Message)}");
            System.Diagnostics.Trace.TraceError(msg);
        }

        private static void Write(string msg)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture)}] {msg}");
        }
    }
}
