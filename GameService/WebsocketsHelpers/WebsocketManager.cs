using GameService.ServiceCore;
using Newtonsoft.Json;
using ServiceProtocol;
using ServiceProtocol.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace GameService.WebsocketsHelpers
{
    public interface IWebSocketMessageHandler
    {
        Task<string> OnMessage(BetterWebsocket bsock, string message);

        void OnClosed(BetterWebsocket bsock);
    }

    public class WebsocketManager : IWebSocketMessageHandler
    {
        static WebsocketManager s_instance = new WebsocketManager();
        public static WebsocketManager Get()
        {
            return s_instance;
        }

        // A list of active connections.
        // Note we use the GUID here because we let multiple connections use the same user name.
        Dictionary<Guid, BetterWebsocket> m_activeConnections = new Dictionary<Guid, BetterWebsocket>();

        public void NewConnection(WebSocket socket, TaskCompletionSource<object> tcs)
        {
            // When we get a new websocket, add it to the pending name connections. 
            // It will stay here until they send over a user name.
            BetterWebsocket bsock = new BetterWebsocket(Guid.NewGuid(), socket, tcs, this);
            lock(m_activeConnections)
            {
                m_activeConnections.Add(bsock.GetId(), bsock);
            }
        }

        public async Task<string> OnMessage(BetterWebsocket bsock, string message)
        {
            // A user name must be set before any other message can be sent. 
            // But the command handler will handle the rejection.            

            // Send the message to the command handler
            KokkaKoroResponse<object> result = await GameMaster.Get().HandleCommand(bsock.GetUserName(), message);

            // If we see a user name accepted message returned, grab the user name.
            if(result.Data != null && result.Data is LoginResponse loginResponse)
            {
                bsock.SetUserName(loginResponse.UserName);
            }

            // Serialize the response and return it.
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

        public async Task<bool> BroadcastMessageAsync(List<string> userNames, KokkaKoroResponse<object> message)
        {
            // Only allow game updates to be broadcast like this.
            if(message.Type != KokkaKoroResponseType.GameLogsUpdate)
            {
                return false;
            }

            string jsonStr;
            try
            {
                jsonStr = JsonConvert.SerializeObject(message);
            }
            catch(Exception e)
            {
                Logger.Error("Failed to serialize broadcast message", e);
                return false;
            }

            // Build a list of clients we need to send to.
            List<BetterWebsocket> client = new List<BetterWebsocket>();
            lock (m_activeConnections)
            {
                foreach(KeyValuePair<Guid, BetterWebsocket> p in m_activeConnections)
                {
                    foreach(string name in userNames)
                    {
                        if (p.Value.GetUserName().Equals(name))
                        {
                            client.Add(p.Value);
                            break;
                        }
                    }
                }
            }

            foreach(BetterWebsocket sock in client)
            {
                await sock.SendMessage(jsonStr);
            }
            return true;
        }

        public bool BroadcastMessage(List<string> userNames, KokkaKoroResponse<object> message, bool block = true)
        {
            if (block)
            {
                var task = BroadcastMessageAsync(userNames, message);
                task.Wait();
                return task.Result;
            }
            else
            {
                var task = BroadcastMessageAsync(userNames, message);
                task.ContinueWith(t => {}, TaskContinuationOptions.OnlyOnFaulted);
                return true;
            }
        }
    }
}
