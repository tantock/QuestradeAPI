using System;

namespace QuestradeAPI.Websocket.Events
{
    
    public class MessageEventArg : EventArgs
    {
        public string message;
        public DateTime time;
    }

    public class ErrorEventArg : EventArgs
    {
        public DateTime time;
        public System.Net.WebSockets.WebSocketState socketState;
        public System.Net.WebSockets.WebSocketException socketException;
    }
}