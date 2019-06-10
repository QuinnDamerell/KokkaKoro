using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class Utils
    {
        static string s_serviceLocalAddress = null;
        
        // Indicates if we are running on Azure or not.
        public static bool IsRunningOnAzure()
        {
            string location = Environment.GetEnvironmentVariable("location");
            return String.IsNullOrWhiteSpace(location) ? false : location.ToLower() == "azure";
        }

        // If we have a local service address, this returns it. If not, null.
        public static string GetServiceLocalAddress()
        {
            return s_serviceLocalAddress;
        }

        // Sets the local address if known.
        public static void SetServiceLocalAddress(string addr)
        {
            s_serviceLocalAddress = addr;
        }
    }
}
