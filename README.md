# QuestradeAPI v3.0.0b
Welcome to a .NET Standard 2.0 implementation to access Questrade's API.

## Features
- OAuth 2.0 authorization flow
- Make API GET calls for:
    - Accounts
    - Account Balances
    - Candlestick Data
    - Symbol search queries
    - Request L1 and order notification stream via Websocket
- Deserialized JSON response into class objects
- External error parsing (Parse errors recieved from Questrade servers)
- Rate limit data and rate limit reset time
- Raises events when data is recieved

## Usage
For documentation on how to retrieve a refresh token, visit https://www.questrade.com/api/documentation/getting-started.

Call the Authenticate method to retrieve a new access token once it has expired. 
Note: any current streaming tasks will have to be restarted when a new access token is retrieved.
```
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestradeAPI;
using QuestradeAPI.Events;

namespace Example
{
    class Program
    {
        static Questrade qTrade;
        static Task AfterSuccessfulAuthTask = new Task(() => AfterSuccessfulAuth());

        private static void AfterSuccessfulAuth()
        {
            Task.Run(() => qTrade.GetAccounts());

            //Subscribe to Level 1 data stream
            string symbolId = "5953026";
            //Starts stream
            Task.Run(() => qTrade.StreamQuote(symbolId, WebsocketQuoteMsgWrapperCallback));

            //Subscribe to notification stream
            Task.Run(() => qTrade.SubToOrderNotif(WebsocketNotificationMsgWrapperCallback));

        }

        static void Main(string[] args)
        {
            Console.Write("Refresh token: ");
            string refreshToken = Console.ReadLine();

            qTrade = new Questrade(refreshToken); //Initialize object

            //Add method to events when raised
            qTrade.OnSuccessfulAuthentication += QTrade_OnSuccessfulAuthentication;
            qTrade.OnUnsuccessfulAuthentication += QTrade_OnUnsuccessfulAuthentication;
            qTrade.OnAccountsRecieved += QTrade_OnAccountsRecieved;

            Task.Run(() => qTrade.Authenticate()); //Make authentication

            System.Diagnostics.Process.GetCurrentProcess().WaitForExit();
        }
        

        private static void QTrade_OnUnsuccessfulAuthentication(object sender, UnsuccessfulAuthArgs e)
        {
            Console.WriteLine("Authentication unsuccessful. " + e.resp.ReasonPhrase);
            if (e.resp.StatusCode == (System.Net.HttpStatusCode)400)
            {
                Console.Write("Enter a valid token: ");
                string token = Console.ReadLine();
                Task.Run(() => qTrade.Authenticate(token));
            }
        }

        private static void QTrade_OnSuccessfulAuthentication(object sender, SuccessAuthEventArgs e)
        {
            Console.WriteLine(string.Format("Access token will expire on: {0} {1}", e.TokenExpiry.ToLongDateString(), e.TokenExpiry.ToLongTimeString()));

            if (!(AfterSuccessfulAuthTask.Status == TaskStatus.Running))
            {
                AfterSuccessfulAuthTask.Start();
            }
        }


        private static void QTrade_OnAccountsRecieved(object sender, APIAccountsReturnArgs e)
        {
            for (int i = 0; i < e.accounts.accounts.Length; i++)
            {
                Console.WriteLine(string.Format("{0} {1} : {2}"
                    , e.accounts.accounts[i].clientAccountType, e.accounts.accounts[i].type, e.accounts.accounts[i].number));

            }
        }

        private static void QTrade_OnOrderProcessingErrorRecieved(object sender, OrderProcessingErrorEventArgs e)
        {
            Console.WriteLine(string.Format("Error code: {0}. {1} Order ID: {2}", e.OrderProcesssingErrorResp.code, e.OrderProcesssingErrorResp.message, e.OrderProcesssingErrorResp.orderId));
        }

        private static void QTrade_OnGeneralErrorRecieved(object sender, GeneralErrorEventArgs e)
        {
            Console.WriteLine(string.Format("Error code: {0}. {1}", e.GeneralErrorResp.code, e.GeneralErrorResp.message));
        }

        private static void WebsocketQuoteMsgWrapperCallback(string message, DateTime messageTime)
        {
            if (!message.Contains("success"))
            {
                var quoteResp = Questrade.JsonToQuotes(message);
                for (int i = 0; i < quoteResp.quotes.Length; i++)
                {
                    Console.WriteLine(string.Format("{0} - Bid: {1}, BidSize: {2}, Ask: {3}, AskSize: {4}",
                    messageTime.ToString("HH:mm:ss"), quoteResp.quotes[i].bidPrice, quoteResp.quotes[i].bidSize, quoteResp.quotes[i].askPrice, quoteResp.quotes[i].askSize));
                }

            }
        }

        private static void WebsocketNotificationMsgWrapperCallback(string message, DateTime messageTime)
        {
            if (message.Contains("executions"))
            {
                var executionNotif = Questrade.JsonToExecutionNotif(message);
                //Do something with notification
            }
            else if (!message.Contains("success"))
            {
                var orderNotif = Questrade.JsonToOrderNotif(message);
                //Do something with notification
            }
        }
    }
}

```
