using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace KokkaKoro
{
    public class Service
    {
        KokkaKoroClientWebsocket m_websocket;

        public Service()
        {
        }

        public async Task<bool> ConnectAsync(int? localPort = null)
        {
            m_websocket = new KokkaKoroClientWebsocket();
            string url = $"ws://{ (localPort.HasValue ? $"localhost:{localPort.Value}" : "test.com") }/ws";
            return await m_websocket.Connect(url);
        }
        
        public void GetGames()
        {

           // m_websocket.SendAsync();
        }
    }
}
