using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Net;

namespace OptionEdge.API.FlatTrade
{
    public delegate void OnSocketConnectHandler();
    public delegate void OnSocketCloseHandler();
    public delegate void OnSocketErrorHandler(string Message);
    public delegate void OnSocketDataHandler(byte[] Data, int Count, string MessageType);

    internal class WebSocket : IWebSocket
    {
        ClientWebSocket _ws;
        string _url;
        int _bufferLength;

        public event OnSocketConnectHandler OnConnect;
        public event OnSocketCloseHandler OnClose;
        public event OnSocketDataHandler OnData;
        public event OnSocketErrorHandler OnError;

        CancellationTokenSource _cts = null;

        public WebSocket(int BufferLength = 2000000)
        {            
            _bufferLength = BufferLength;
        }

        public bool IsConnected()
        {
            if(_ws is null)
                return false;
            
            return _ws.State == WebSocketState.Open;
        }

        public async Task ConnectAsync(string url, Dictionary<string, string> headers = null)
        {
            _url = url;
            _cts = new CancellationTokenSource(); // Store a cancellation token source
            byte[] buffer = new byte[_bufferLength];

            try
            {
                _ws = new ClientWebSocket();
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        _ws.Options.SetRequestHeader(header.Key, header.Value);
                    }
                }

                await _ws.ConnectAsync(new Uri(_url), _cts.Token);
                OnConnect?.Invoke();

                // Start receiving data
                _ = Task.Run(() => ReceiveLoopAsync(buffer, _cts.Token));
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Error while connecting websocket. Message: {e}");
                OnClose?.Invoke();
            }
        }

        private async Task ReceiveLoopAsync(byte[] buffer, CancellationToken token)
        {
            try
            {
                while (_ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", token);
                        OnClose?.Invoke();
                        break;
                    }

                    int totalBytes = result.Count;
                    bool endOfMessage = result.EndOfMessage;

                    while (!endOfMessage)
                    {
                        var tempResult = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer, totalBytes, buffer.Length - totalBytes), token);
                        totalBytes += tempResult.Count;
                        endOfMessage = tempResult.EndOfMessage;
                    }

                    try
                    {
                        OnData?.Invoke(buffer, totalBytes, result.MessageType.ToString());
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke($"Error in socket data processing handler. {ex}");
                    }
                }
            }
            catch (Exception e)
            {
                if (IsConnected())
                    OnError?.Invoke($"Error while receiving data. Message: {e.Message}");
                else
                    OnError?.Invoke($"WebSocket connection lost: {e}");
            }
        }


        public void Send(string Message)
        {
            if (_ws.State == WebSocketState.Open)
                try
                {
                    _ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(Message)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                }
                catch (Exception e)
                {
                    OnError?.Invoke("Error while sending data. Message:  " + e.Message);
                }
        }

        public void Close(bool Abort = false)
        {
            if(_ws.State == WebSocketState.Open)
            {
                try
                {
                    if (Abort)
                        _ws.Abort();
                    else
                    {
                        _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait();
                        OnClose?.Invoke();
                    }
                }
                catch (Exception e)
                {
                    OnError?.Invoke("Error while closing connection. Message: " + e.Message);
                }
            }
        }
    }
}
