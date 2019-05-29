using GameService.ServiceCore;
using Newtonsoft.Json;
using ServiceProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace GameService.WebsocketsHelpers
{
    public interface IWebSocketMessageHandler
    {
        string OnMessage(BetterWebsocket bsock, string message);

        void OnClosed(BetterWebsocket bsock);
    }

    public class WebsocketManager : IWebSocketMessageHandler
    {
        static WebsocketManager s_instance = new WebsocketManager();
        public static WebsocketManager Get()
        {
            return s_instance;
        }

        Dictionary<Guid, BetterWebsocket> m_activeConnections = new Dictionary<Guid, BetterWebsocket>();

        public void NewConnection(WebSocket socket, TaskCompletionSource<object> tcs)
        {
            BetterWebsocket bsock = new BetterWebsocket(Guid.NewGuid(), socket, tcs, this);
            lock(m_activeConnections)
            {
                m_activeConnections.Add(bsock.GetId(), bsock);
            }
        }

        public string OnMessage(BetterWebsocket bsock, string message)
        {
            // Send the message to the command handler
            KokkaKoroResponse<object> result = GameMaster.Get().HandleCommand(bsock.GetId().ToString(), message);

            // Seralize the response and return it.
            return JsonConvert.SerializeObject(result);
        }

        public void OnClosed(BetterWebsocket bsock)
        {
            // Remove it from our map when closed.
            lock (m_activeConnections)
            {
                if(m_activeConnections.ContainsKey(bsock.GetId()))
                {
                    m_activeConnections.Remove(bsock.GetId());
                }
            }
        }
    }
}
