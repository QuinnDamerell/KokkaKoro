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
        IWebSocketMessageHandler m_handler;
        CancellationTokenSource m_readCancelToken = new CancellationTokenSource();
        CancellationTokenSource m_writeCancelToken = new CancellationTokenSource();
        object m_closeLock = new object();
        bool m_isClosed = false;

        public BetterWebsocket(Guid id, WebSocket socket, IWebSocketMessageHandler handler)
        {
            m_socket = socket;
            m_id = id;
            m_handler = handler;
            ReadLoop();
        }

        public Guid GetId()
        {
            return m_id;
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

                    if(response.CloseStatus != WebSocketCloseStatus.Empty)
                    {
                        CloseWebSocket(false);
                        return;
                    }

                    // Handle the message
                    string result = m_handler.OnMessage(this, Encoding.UTF8.GetString(message.ToArray()));

                    // Send the result back
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

            // Inform the handler we are now closed.
            m_handler.OnClosed(this);
        }
    }
}
