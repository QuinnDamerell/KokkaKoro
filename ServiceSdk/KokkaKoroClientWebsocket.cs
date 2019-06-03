using Newtonsoft.Json;
using ServiceProtocol;
using ServiceSdk;
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
        public readonly int ResponseTimeoutMs = 60000;

        ClientWebSocket m_websocket;
        WebsocketState m_state;
        object m_stateLock = new object();
        CancellationTokenSource m_readLoopCancellationToken = new CancellationTokenSource();
        Dictionary<int, PendingRequest> m_pendingRequests = new Dictionary<int, PendingRequest>();
        SemaphoreSlim m_sendingSemaphore = new SemaphoreSlim(1);

        public KokkaKoroClientWebsocket()
        {
            m_state = WebsocketState.NotConnected;
        }

        public async Task Connect(string url)
        {
            // Check our state.
            lock(m_stateLock)
            {
                if(m_state != WebsocketState.NotConnected)
                {
                    throw new KokkaKoroException("Websocket not connected", false);
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

                Logger.Info($"Connected to {url}");
            }
            catch(Exception e)
            {
                await InternalDisconnect();
                Logger.Error($"Failed to connect to service {url}", e);
                throw new KokkaKoroException($"Failed to connect to service: {e.Message}", false);
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
                        Logger.Info($"Websocket closed from read loop");
                        await InternalDisconnect();
                        return;
                    }

                    // Handle the message
                    HandleNewMessage(Encoding.UTF8.GetString(message.ToArray()));
                }
                catch(Exception e)
                {
                    Logger.Error("Exception thrown in websocket read loop", e);
                    await InternalDisconnect();
                    return;
                }
            }
        }

        private void HandleNewMessage(string msg)
        {
            Logger.Info($"<- {msg}");

            // Parse the response.
            KokkaKoroResponse<object> response = JsonConvert.DeserializeObject<KokkaKoroResponse<object>>(msg);
            if(response.Type == KokkaKoroResponseType.GameLogUpdate)
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
                        pending.Event.Release();
                    }
                    else
                    {
                        Logger.Error($"We got a response that didn't have a pending request.");

                        // This is a bad state, but we will just return the response to anyone.
                        foreach (KeyValuePair<int, PendingRequest> p in m_pendingRequests)
                        {
                            // Take the first one and let it go back.
                            m_pendingRequests.Remove(p.Key);
                            p.Value.Response = msg;
                            p.Value.Event.Release();
                            break;
                        }

                        Logger.Error($"We got a response that didn't have a pending request, and we don't have any outstanding requests.");
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

            Logger.Info("Websocket disconnected");

            // Stop the read loop.
            m_readLoopCancellationToken.Cancel();

            // Send a close message
            if (m_websocket != null)
            {
                try
                {                  
                    var writeTimeout = new CancellationTokenSource(WriteTimeoutMs);
                    await m_websocket.SendAsync(new ArraySegment<byte>(), WebSocketMessageType.Close, true, writeTimeout.Token);                    
                }
                catch { }

                // And close the websocket.
                try
                {                    
                    var closeTimeout = new CancellationTokenSource(WriteTimeoutMs);
                    await m_websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", closeTimeout.Token);                    
                }
                catch { }
            }
        }

        public async Task<string> SendRequest(KokkaKoroRequest<object> request)
        {
            lock (m_stateLock)
            {
                if (m_state != WebsocketState.Connected)
                {
                    throw new KokkaKoroException("Websocket not connected", false);
                }
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

                Logger.Info($"-> {json}");

                // Send the data    
                {
                    var writeTimeout = new CancellationTokenSource(WriteTimeoutMs);

                    // Only one thread at a time can call sending, so make sure we enforce it.
                    await m_sendingSemaphore.WaitAsync(WriteTimeoutMs);
                    try
                    {
                        await m_websocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, writeTimeout.Token);
                    }
                    catch(Exception e)
                    {
                        throw e;
                    }
                    finally
                    {
                        m_sendingSemaphore.Release();
                    }
                }

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

                // Grab the response if we got one
                string response = pending.Response;
                if(String.IsNullOrWhiteSpace(response))
                {
                    throw new KokkaKoroException("Request didn't get a response", true);
                }
                return response;
            }
            catch(Exception e)
            {
                Logger.Error($"Exception thrown in send request logic.", e);
                await InternalDisconnect();
                throw e;
            }      
        }
    }
}
