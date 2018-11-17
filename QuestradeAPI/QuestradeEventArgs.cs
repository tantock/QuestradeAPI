using System;
using System.Collections.Generic;
using System.Text;

namespace QuestradeAPI.Events
{
    public class SuccessAuthEventArgs : EventArgs
    {
        public DateTime TokenExpiry;
    }

    public class UnsuccessfulAuthArgs : EventArgs
    {
        public System.Net.Http.HttpResponseMessage resp;
    }

    public class GeneralErrorEventArgs : EventArgs
    {
        public GeneralErrorResp GeneralErrorResp;
        public ErrorType ErrorType { get { return ErrorType.General; } }
    }

    public class OrderProcessingErrorEventArgs : EventArgs
    {
        public OrderProcesssingErrorResp OrderProcesssingErrorResp;
        public ErrorType ErrorType { get { return ErrorType.Order; } }
    }

    public abstract class APIReturnEventArgs : EventArgs
    {
        public DateTime RateReset;
        public int NumCallsLeft;
        public DateTime time;
    }

    public abstract class APIStreamEventArgs : EventArgs
    {
        public DateTime time;
    }

    public class APICandleReturnArgs : APIReturnEventArgs
    {
        public int id;
        public DateTime startTime;
        public DateTime endTime;
        public HistoricalGrandularity interval;
        public Candles candles;
    }

    public class APIAccountsReturnArgs : APIReturnEventArgs
    {
        public Accounts accounts;
    }

    public class APIAccountBalancesReturnArgs : APIReturnEventArgs
    {
        public AccountBalances details;
    }

    public class APISymbolSearchReturnArgs : APIReturnEventArgs
    {
        public Symbols symbols;
    }

    public class APIGetErrorArg : EventArgs
    {
        public System.Net.Http.HttpRequestException httpRequestException;
    }

    public class APIQuoteReturnArgs : APIReturnEventArgs
    {
        public Quotes quotes;
    }

    public class APIStreamQuoteRecievedArgs : APIStreamEventArgs
    {
        public Quotes quotes;
    }

    public class APIOrderNotificationRecievedArg : APIStreamEventArgs
    {
        public OrderNotification OrderNotif;
    }

    public class APIOrderExecNotificationRecievedArg : APIStreamEventArgs
    {
        public ExecutionNotification OrderExecNotif;
    }

}
