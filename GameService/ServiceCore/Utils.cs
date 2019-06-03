using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class Utils
    {
        static string s_serviceLocalAddress = "";

        public static bool IsRunningOnAzure()
        {
            string location = Environment.GetEnvironmentVariable("location");
            return String.IsNullOrWhiteSpace(location) ? false : location.ToLower() == "azure";
        }

        public static string GetServiceLocalAddress()
        {
            return s_serviceLocalAddress;
        }

        public static void SetServiceLocalAddress(string addr)
        {
            s_serviceLocalAddress = addr;
        }
    }
}
