using ServiceProtocol;
using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
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
            string url = localPort.HasValue ? $"ws://localhost:{localPort.Value}/ws" : $"wss://kokkakoro.azurewebsites.net/ws";
            return await m_websocket.Connect(url);
        }
        
        public async Task<List<KokkaKoroGame>> GetGames()
        {
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.ListGames
            };
            string response = await m_websocket.SendRequest(request);
            return null;
           // m_websocket.SendAsync();
        }
    }
}
