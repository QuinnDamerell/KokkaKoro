﻿using GameService.ServiceCore;
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
        // Note we use the GUID here because we let multiple connections use the same username.
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

            // If we see a user name accpeted mesasge returned, grab the user name.
            if(result.Data != null && result.Data is SetUserNameResponse setUserName)
            {
                bsock.SetUserName(setUserName.AcceptedUserName);
            }

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
