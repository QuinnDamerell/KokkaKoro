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

    class KokkaKoroClientWebsocket
    {
        public readonly int WriteTimeoutMs = 10000;
        public readonly int ResponseTimeoutMs = 10000;

        ClientWebSocket m_websocket;
        WebsocketState m_state;
        object m_stateLock = new object();
        CancellationTokenSource m_readLoopCancellationToken = new CancellationTokenSource();


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
                
                    // Try to read off the socket
                    WebSocketReceiveResult response;
                    var message = new List<byte>();
                    var buffer = new byte[40096];
                    do
                    {
                        response = await m_websocket.ReceiveAsync(new ArraySegment<byte>(buffer), m_readLoopCancellationToken.Token);
                        message.AddRange(new ArraySegment<byte>(buffer, 0, response.Count));
                    } while (!response.EndOfMessage && response.CloseStatus == WebSocketCloseStatus.Empty);

                    // If we are closed, make sure it's shutdown.
                    if (response.CloseStatus != WebSocketCloseStatus.Empty)
                    {
                        await InternalDisconnect();
                        return;
                    }

                }
                catch(Exception e)
                {

                }
            }
        }

        public async Task Disconnect()
        {
      
            await InternalDisconnect();
        }

        private async Task InternalDisconnect(bool ingoreState)
        {
            lock (m_stateLock)
            {
                if (m_state == WebsocketState.Disconnected)
                {
                    return;
                }
                m_state = WebsocketState.Disconnected;
            }

            // Set the state            
            m_state = WebsocketState.Disconnected;

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

        public async Task<bool> SendMessage(KokkaKoroRequest<object> request)
        {
            lock (m_stateLock)
            {
                if (m_state != WebsocketState.Connected)
                {
                    return false;
                }
                m_state = WebsocketState.Sending;
            }

            try
            {
                // Convert to json.
                string json = JsonConvert.SerializeObject(request);

                // Convert to bytes
                Byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);

                // Send the data          
                var writeTimeout = new CancellationTokenSource(WriteTimeoutMs);
                await m_websocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, writeTimeout.Token);

                // Requests always result in responses, so wait for it.
                var readTimeout = new CancellationTokenSource(ResponseTimeoutMs);

                m_websocket.sen
            }
            catch(Exception e)
            {

            }

         
        }
    }
}
