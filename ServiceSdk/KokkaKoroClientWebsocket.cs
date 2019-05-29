using Newtonsoft.Json;
using ServiceProtocol;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KokkaKoro
{
    public enum WebsocketState
    {
        NotConnected,
        Connecting,
        Connected,
        Sending,
        WaitingToRecieve,
        Disconnected
    }

    class PendingRequest
    {
        public SemaphoreSlim Event = new SemaphoreSlim(0);
        public string Response;
    }

    class KokkaKoroClientWebsocket
    {
        public readonly int WriteTimeoutMs = 30000;
        public readonly int ResponseTimeoutMs = 30000;

        ClientWebSocket m_websocket;
        WebsocketState m_state;
        object m_stateLock = new object();
        CancellationTokenSource m_readLoopCancellationToken = new CancellationTokenSource();
        Dictionary<int, PendingRequest> m_pendingRequests = new Dictionary<int, PendingRequest>();

        public KokkaKoroClientWebsocket()
        {
            m_state = WebsocketState.NotConnected;
        }

        public async Task<bool> Connect(string url)
        {
            // Check our state.
            lock(m_stateLock)
            {
                if(m_state != WebsocketState.NotConnected)
                {
                    return false;
                }
                m_state = WebsocketState.Connecting;
            }

            try
            {
                m_websocket = new ClientWebSocket();
                CancellationToken token = new CancellationToken();
                await m_websocket.ConnectAsync(new Uri(url), token);
                lock(m_stateLock)
                {
                    m_state = WebsocketState.Connected;
                }

                // Start the read loop.
                ReadLoop();

                return true;
            }
            catch(Exception e)
            {
                await InternalDisconnect();
                return false;
            }      
        }

        private async void ReadLoop()
        {
            while(m_state != WebsocketState.Disconnected)
            {
                try
                {                 
                    // Try to read off the socket
                    WebSocketReceiveResult response;
                    var message = new List<byte>();
                    var buffer = new byte[40000];
                    do
                    {
                        response = await m_websocket.ReceiveAsync(new ArraySegment<byte>(buffer), m_readLoopCancellationToken.Token);
                        message.AddRange(new ArraySegment<byte>(buffer, 0, response.Count));
                    } while (!response.EndOfMessage && response.CloseStatus == WebSocketCloseStatus.Empty);

                    // If we are closed, make sure it's shutdown.
                    if (response.CloseStatus != null && response.CloseStatus != WebSocketCloseStatus.Empty)
                    {
                        await InternalDisconnect();
                        return;
                    }

                    // Handle the message
                    HandleNewMessage(Encoding.UTF8.GetString(message.ToArray()));
                }
                catch(Exception e)
                {
                    await InternalDisconnect();
                    return;
                }
            }
        }

        private void HandleNewMessage(string msg)
        {  
            // Parse the response.
            KokkaKoroResponse<object> response = JsonConvert.DeserializeObject<KokkaKoroResponse<object>>(msg);
            if(response.Type == KokkaKoroResponseType.GameUpdate)
            {
                // This is a broadcast message
                // ToDo try catch this.
            }
            else
            {
                // This is a response, there should be a request waiting.
                lock (m_pendingRequests)
                {
                    if (m_pendingRequests.ContainsKey(response.RequestId))
                    {
                        PendingRequest pending = m_pendingRequests[response.RequestId];
                        m_pendingRequests.Remove(response.RequestId);
                        pending.Response = msg;
                        //pending.Event.Release();
                    }
                    else
                    {
                        // This is a bad state, but we will just return the response to anyone.
                        foreach (KeyValuePair<int, PendingRequest> p in m_pendingRequests)
                        {
                            // Take the first one and let it go back.
                            m_pendingRequests.Remove(p.Key);
                            p.Value.Response = msg;
                            p.Value.Event.Release();
                            break;
                        }
                    }
                }
            }
        }

        public async Task Disconnect()
        {      
            await InternalDisconnect();
        }

        private async Task InternalDisconnect()
        {
            lock (m_stateLock)
            {
                if (m_state == WebsocketState.Disconnected)
                {
                    return;
                }
                m_state = WebsocketState.Disconnected;
            }

            // Stop the read loop.
            m_readLoopCancellationToken.Cancel();

            // If we have a websocket close it.
            try
            {
                if (m_websocket != null)
                {
                    var writeTimeout = new CancellationTokenSource(WriteTimeoutMs);
                    await m_websocket.SendAsync(new ArraySegment<byte>(), WebSocketMessageType.Close, true, writeTimeout.Token);
                    var closeTimeout = new CancellationTokenSource(WriteTimeoutMs);
                    await m_websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", closeTimeout.Token);
                }
            }
            catch { }
        }

        public async Task<string> SendRequest(KokkaKoroRequest<object> request)
        {
            lock (m_stateLock)
            {
                if (m_state != WebsocketState.Connected)
                {
                    return null;
                }
                m_state = WebsocketState.Sending;
            }

            try
            {
                // Convert to json.
                string json = JsonConvert.SerializeObject(request);

                // Convert to bytes
                Byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);

                // Create a pending response object.
                PendingRequest pending = new PendingRequest();
                lock (m_pendingRequests)
                {
                    m_pendingRequests.Add(request.RequestId, pending);
                }

                // Send the data          
                var writeTimeout = new CancellationTokenSource(WriteTimeoutMs);
                await m_websocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, writeTimeout.Token);

                // Wait for the message to come back
                await pending.Event.WaitAsync(ResponseTimeoutMs);

                // Make sure we remove the pending request.
                lock (m_pendingRequests)
                {
                    if (m_pendingRequests.ContainsKey(request.RequestId))
                    {
                        m_pendingRequests.Remove(request.RequestId);
                    }
                }

                // Grab the repsonse if we got one
                string response = pending.Response;
                if(String.IsNullOrWhiteSpace(response))
                {
                    return null;
                }
                return response;
            }
            catch(Exception e)
            {
                await InternalDisconnect();
                return null;
            }      
        }
    }
}
