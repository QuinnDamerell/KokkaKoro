using GameService.ServiceCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameService.WebsocketsHelpers
{
    public class BetterWebsocket
    {
        WebSocket m_socket;
        Guid m_id;
        string m_userName;
        TaskCompletionSource<object> m_websocketCompletion;
        IWebSocketMessageHandler m_handler;

        CancellationTokenSource m_readCancelToken = new CancellationTokenSource();
        CancellationTokenSource m_writeCancelToken = new CancellationTokenSource();
        object m_closeLock = new object();
        bool m_isClosed = false;
        Thread m_readLoop;

        public BetterWebsocket(Guid id, WebSocket socket, TaskCompletionSource<object> tcs, IWebSocketMessageHandler handler)
        {
            m_socket = socket;
            m_id = id;
            m_handler = handler;

            // Due to the way kestrel handles websockets
            // we need to block the middleware function that called us until the socket is done.
            // Otherwise the socket will be closed from under us.
            m_websocketCompletion = tcs;

            // Start a new thread for the read loop.
            m_readLoop = new Thread(ReadLoop);
            m_readLoop.Start();
        }

        private async void ReadLoop()
        {
            while(!m_isClosed)
            {
                try
                {
                    WebSocketReceiveResult response;
                    var message = new List<byte>();
                    var buffer = new byte[4096];
                    do
                    {
                        response = await m_socket.ReceiveAsync(new ArraySegment<byte>(buffer), m_readCancelToken.Token);
                        message.AddRange(new ArraySegment<byte>(buffer, 0, response.Count));
                    } while (!response.EndOfMessage && response.CloseStatus == WebSocketCloseStatus.Empty);

                    if(response.CloseStatus != null && response.CloseStatus != WebSocketCloseStatus.Empty)
                    {
                        CloseWebSocket(false);
                        return;
                    }

                    // Handle the message
                    string result = await m_handler.OnMessage(this, Encoding.UTF8.GetString(message.ToArray()));

                    // Send the result back.
                    Byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);

                    //Sends data back.                     
                    await m_socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, m_writeCancelToken.Token);

                    // Check the state
                    if(m_socket.State != WebSocketState.Open)
                    {
                        CloseWebSocket(true);
                    }
                }
                catch(Exception e)
                {
                    Logger.Error("Exception in websocket close.", e);
                    CloseWebSocket(true);
                }             
            }
        }

        private void CloseWebSocket(bool wasError)
        {
            // Ensure we only do this once.
            lock(m_closeLock)
            {
                if(m_isClosed)
                {
                    return;
                }
                m_isClosed = true;
            }

            // Stop reading and writing.
            m_readCancelToken.Cancel();
            m_writeCancelToken.Cancel();

            // Try to write a close message.
            try
            {
                CancellationToken token = new CancellationToken();
                m_socket.CloseAsync(wasError ? WebSocketCloseStatus.InternalServerError : WebSocketCloseStatus.NormalClosure, "closed", token);
            }
            catch(Exception e)
            {
                Logger.Error("Failed to close websocket.", e);
            }

            // Make sure the handler returns, see comment there this is set.
            m_websocketCompletion.SetResult(1);

            // Inform the handler we are now closed.
            m_handler.OnClosed(this);

            // Lose the ref to the read loop thread.
            m_readLoop = null;
        }

        public Guid GetId()
        {
            return m_id;
        }

        public bool HasUserName()
        {
            return !String.IsNullOrWhiteSpace(m_userName);
        }

        public string GetUserName()
        {
            return m_userName;
        }

        public void SetUserName(string str)
        {
            m_userName = str;
        }
    }
}
