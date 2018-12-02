﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net.WebSockets;
using System.Threading.Tasks;
using QuestradeAPI.Websocket.Events;

namespace QuestradeAPI.Websocket
{
    public class QuestradeWebsocket
    {
        ClientWebSocket ws = new ClientWebSocket();

        public event EventHandler<MessageEventArg> OnConnect;

        public event EventHandler<MessageEventArg> OnReceive;

        public event EventHandler<ErrorEventArg> OnError;

        public event EventHandler<MessageEventArg> OnClose;

        public void ConnectAsync(Uri uri, System.Threading.CancellationToken cancel)
        {
            Task.Run(() =>
            {
                try
                {
                    ws.ConnectAsync(uri, cancel);
                    if (ws.State == WebSocketState.Open)
                    {
                        MessageEventArg arg = new MessageEventArg();
                        arg.message = "Connection Established";
                        OnConnect(this, arg);
                        RecieveAsync(cancel);
                    }
                }
                catch (WebSocketException ex)
                {
                    ErrorEventArg arg = new ErrorEventArg();
                    arg.time = DateTime.Now;
                    arg.socketState = ws.State;
                    arg.socketException = ex;
                    OnError(this, arg);
                }
            }, cancel);
            
        }

        private async void RecieveAsync(System.Threading.CancellationToken cancel)
        {
            try
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[10000]);
                WebSocketReceiveResult result = await ws.ReceiveAsync(buffer, cancel);
                if(ws.State == WebSocketState.CloseReceived)
                {
                    MessageEventArg arg = new MessageEventArg();
                    arg.time = DateTime.Now;
                    if(ws.CloseStatusDescription != "")
                    {
                        arg.message = ws.CloseStatusDescription;
                    }
                    else
                    {
                        arg.message = "Websocket closed";
                    }
                    
                    OnClose(this, arg);
                }
                else
                {
                    MessageEventArg arg = new MessageEventArg();
                    if (result.Count != 0 || result.CloseStatus == WebSocketCloseStatus.Empty)
                    {
                        arg.time = DateTime.Now;
                        string message = Encoding.ASCII.GetString(buffer.Array,
                             buffer.Offset, result.Count);
                        arg.message = message;
                        OnReceive(this, arg);
                        RecieveAsync(cancel);
                    }
                }
                
            }
            catch(WebSocketException ex)
            {
                ErrorEventArg arg = new ErrorEventArg();
                arg.time = DateTime.Now;
                arg.socketState = ws.State;
                arg.socketException = ex;
                OnError(this, arg);
            }
            
        }

        public void SendAsync(string message, Uri uri, System.Threading.CancellationToken cancel, System.Text.Encoding encoding)
        {
            Task.Run(() =>
            {
                try
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(encoding.GetBytes(message.ToCharArray()));

                    ws.SendAsync(buffer, WebSocketMessageType.Text, true, cancel);

                    System.Diagnostics.Debug.WriteLine("Sent success.");
                }
                catch (WebSocketException ex)
                {
                    ErrorEventArg arg = new ErrorEventArg();
                    arg.time = DateTime.Now;
                    arg.socketState = ws.State;
                    arg.socketException = ex;
                    OnError(this, arg);
                }
            }, cancel);
            
        }

        public void CloseAsync(System.Threading.CancellationToken cancel)
        {
            Task.Run(() =>
            {
                if (ws.State == WebSocketState.Open)
                {
                    ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancel);
                }

                ws.Dispose();
            }, cancel);
            
        }

        public WebSocketState State
        {
            get { return ws.State; }
        }
    }


}
