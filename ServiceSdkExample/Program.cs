using KokkaKoro;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceSdkExample
{
    class Program
    {
        static AutoResetEvent m_doneEvent;

        static void Main(string[] args)
        {
            m_doneEvent = new AutoResetEvent(false);
            DoWork();
            m_doneEvent.WaitOne();
        }

        public static async void DoWorkWrapper()
        {
            await DoWork();
            m_doneEvent.Set();
        }

        public static async Task DoWork()
        {
            // Create a new service object
            Service kokkaService = new Service();

            // Connect to the service
            if (await kokkaService.ConnectAsync(51052))
            {
                return;
            }



            
        }
    }
}
