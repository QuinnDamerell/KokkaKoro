using Newtonsoft.Json;
using ServiceProtocol;
using ServiceSdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
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
        public readonly int WriteTimeoutMs = 10000;
        public readonly int ResponseTimeoutMs = 300000;

        ILogger m_logger;
        bool m_logMessages = false;
        IWebSocketHandler m_handler;
        ClientWebSocket m_websocket;
        WebsocketState m_state;
        object m_stateLock = new object();
        CancellationTokenSource m_readLoopCancellationToken = new CancellationTokenSource();
        Dictionary<int, PendingRequest> m_pendingRequests = new Dictionary<int, PendingRequest>();
        SemaphoreSlim m_sendingSemaphore = new SemaphoreSlim(1);
        CancellationTokenSource m_broadcastQueueCancellationToken = new CancellationTokenSource();
        Channel<string> m_broadcastMessageQueue;
        Thread m_readLoop;
        Thread m_broadcastMessageLoop;
        Thread m_pingLoop;

        public KokkaKoroClientWebsocket(IWebSocketHandler handler, ILogger logger, bool logMessages = false)
        {
            m_handler = handler;
            m_state = WebsocketState.NotConnected;
            m_logger = logger;
            m_logMessages = logMessages;
        }

        public void SetLogMessages(bool val)
        {
            m_logMessages = val;
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
                m_readLoop = new Thread(ReadLoop);
                m_readLoop.Start();
                m_broadcastMessageQueue = Channel.CreateUnbounded<string>();
                m_broadcastMessageLoop = new Thread(BroadcastMessageLoop);
                m_broadcastMessageLoop.Start();
                m_pingLoop = new Thread(PingLoop);
                m_pingLoop.Start();

                m_logger.Info($"Connected to {url}");
            }
            catch(Exception e)
            {
                await InternalDisconnect();
                m_logger.Error($"Failed to connect to service {url}", e);
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
                    } while (!response.EndOfMessage && (response.CloseStatus == null || response.CloseStatus == WebSocketCloseStatus.Empty));


                    // If we are closed, make sure it's shutdown.
                    if (response.CloseStatus != null && response.CloseStatus != WebSocketCloseStatus.Empty)
                    {
                        m_logger.Info($"Websocket closed from read loop. Closed Status: {response.CloseStatus.ToString()}");
                        await InternalDisconnect();
                        return;
                    }

                    // Handle the message
                    await HandleNewMessage(Encoding.UTF8.GetString(message.ToArray()));
                }
                catch(Exception e)
                {
                    m_logger.Error("Exception thrown in websocket read loop", e);
                    await InternalDisconnect();
                    return;
                }
            }
        }

        private async void BroadcastMessageLoop()
        {
            while(m_state != WebsocketState.Disconnected)
            {
                try
                {
                    // This thread will sit here and wait until we get a broadcast message.
                    string message = await m_broadcastMessageQueue.Reader.ReadAsync(m_broadcastQueueCancellationToken.Token);

                    if(String.IsNullOrWhiteSpace(message))
                    {
                        // This probably means the loop will exit.
                        continue;
                    }

                    // Handle the message.
                    await m_handler.OnGameUpdates(message);
                }
                catch (Exception e)
                {
                    m_logger.Error($"Exception thrown in the BroadcastMessageLoop loop.", e);
                }
            }
        }

        private async void PingLoop()
        {
            // Because the dotnet websocket is dumb, we need to ping to manually detect when
            // connection is lost.
            while(m_state != WebsocketState.Disconnected)
            {
                try
                {
                    // Sleep for half the timeout time.
                    await Task.Delay(KokkaKoroRequest<object>.HeartbeatTimeoutMs / 2);

                    // Now try to send a message, we don't care about the response because as long as it doesn't
                    // throw it's still connected.
                    await SendRequest(new KokkaKoroRequest<object>() { Command = KokkaKoroCommands.Heartbeat, CommandOptions = null });
                }
                catch(Exception e)
                {
                    m_logger.Error("Exception thrown in ping loop.", e);
                    await InternalDisconnect();
                    return;
                }
            }
        }

        private async Task HandleNewMessage(string msg)
        {
            if (m_logMessages)
            {
                Console.Out.WriteLine($"<- {msg}");
            }

            // Parse the response.
            KokkaKoroResponse<object> response = JsonConvert.DeserializeObject<KokkaKoroResponse<object>>(msg);

            if (response.Type == KokkaKoroResponseType.GameLogsUpdate)
            {
                // Since the client can make calls in the middle of handing the broadcasts, we need to let a different thread do the 
                // work so we don't block the receive thread.
                await m_broadcastMessageQueue.Writer.WriteAsync(msg);
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
                        m_logger.Error($"We got a response that didn't have a pending request.");

                        // This is a bad state, but we will just return the response to anyone.
                        foreach (KeyValuePair<int, PendingRequest> p in m_pendingRequests)
                        {
                            // Take the first one and let it go back.
                            m_pendingRequests.Remove(p.Key);
                            p.Value.Response = msg;
                            p.Value.Event.Release();
                            break;
                        }

                        m_logger.Error($"We got a response that didn't have a pending request, and we don't have any outstanding requests.");
                    }
                }
            }
        }

        public async Task Disconnect()
        {      
            await InternalDisconnect(true);
        }

        private async Task InternalDisconnect(bool isFromClient = false)
        {
            lock (m_stateLock)
            {
                if (m_state == WebsocketState.Disconnected)
                {
                    return;
                }
                m_state = WebsocketState.Disconnected;
            }

            m_logger.Info("Websocket disconnected");

            // Send a close message before we exit the read loop since that will leave the socket
            // in a state where it can't be sent.
            if (m_websocket != null)
            {
                await m_sendingSemaphore.WaitAsync(WriteTimeoutMs);
                try
                {
                    var closeTimeout = new CancellationTokenSource(WriteTimeoutMs);
                    Task task = m_websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", closeTimeout.Token);

                    // For some reason, if you await this without a timeout it sometimes hangs for 
                    task.Wait(WriteTimeoutMs);
                }
                catch (Exception e)
                {
                    m_logger.Error("Failed to send ws close message.", e);
                }
                finally
                {
                    m_sendingSemaphore.Release();
                }
            }

            // Stop the read loop.
            m_readLoopCancellationToken.Cancel();
            m_broadcastQueueCancellationToken.Cancel();

            await m_handler.OnDisconnect(isFromClient);
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

                // Send the data    
                {
                    var writeTimeout = new CancellationTokenSource(WriteTimeoutMs);

                    // Only one thread at a time can call sending, so make sure we enforce it.
                    await m_sendingSemaphore.WaitAsync(WriteTimeoutMs);
                    try
                    {
                        if (m_logMessages)
                        {
                            m_logger.Info($"-> {json}");
                        }
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
                m_logger.Error($"Exception thrown in send request logic. {e.StackTrace}", e);
                await InternalDisconnect();
                throw e;
            }      
        }
    }
}
