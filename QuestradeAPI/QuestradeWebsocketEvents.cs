using System;

namespace QuestradeAPI.Websocket.Events
{
    
    public class MessageEventArg : EventArgs
    {
        public string message;
        public DateTime time;
    }
}