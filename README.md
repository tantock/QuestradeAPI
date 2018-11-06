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

namespace Example
{
    class Program
    {
        static Questrade qTrade;
        static Task AfterSuccessfulAuthTask = new Task(async () => await AfterSuccessfulAuth());

        private static async Task AfterSuccessfulAuth()
        {
            var Accounts = await qTrade.GetAccounts();
            //Error handling
            if (Accounts.isSuccess)
            {
                var list = Accounts.q_obj.accounts; //Access q_obj for data
                for(int i = 0; i < list.Length; i++)
                {
                    Console.WriteLine(string.Format("{0} {1} : {2}"
                            , list[i].clientAccountType
                            , list[i].type, list[i].number));
                }
            }
            else
            {
                if (Accounts.errorType == ErrorType.General)
                {
                    var Error = Accounts.generalError;
                    //Handle error here
                }
                else
                {
                    var Error = Accounts.orderError;
                    //Handle error here
                }
            }

            //Subscribe to Level 1 data stream
            Console.Write("Symbol Id to stream: ");
            string symbolId = Console.ReadLine();
            //Starts stream. Return object is used for error handling.
            var portQuote = await qTrade.StreamQuote(symbolId, WebsocketQuoteMsgWrapperCallback); 

            //Subscribe to notification stream
            var portNotif = await qTrade.SubToOrderNotif(WebsocketNotificationMsgWrapperCallback);

        }

        static void Main(string[] args)
        {
            Console.Write("Refresh token: ");
            string refreshToken = Console.ReadLine();

            qTrade = new Questrade(refreshToken); //Initialize object

            //Add method to events when raised
            qTrade.SuccessfulAuthentication += QTrade_SuccessfulAuthentication;
            qTrade.UnsuccessfulAuthentication += QTrade_UnsuccessfulAuthentication;

            Task.Run(() => qTrade.Authenticate()); //Make authentication

            System.Diagnostics.Process.GetCurrentProcess().WaitForExit();
        }


        private static void QTrade_SuccessfulAuthentication(object sender, QuestradeAPI.SuccessAuthEventArgs e)
        {
            Console.WriteLine(string.Format("Access token will expire on: {0} {1}"
            , e.TokenExpiry.ToLongDateString(), e.TokenExpiry.ToLongTimeString()));

            if (!(AfterSuccessfulAuthTask.Status == TaskStatus.Running))
            {
                AfterSuccessfulAuthTask.Start();
            }
        }

        static void QTrade_UnsuccessfulAuthentication(object sender, QuestradeAPI.UnsuccessfulAuthArg e)
        {
            Console.WriteLine("Authentication unsuccessful. " + e.resp.ReasonPhrase);
            if (e.resp.StatusCode == (System.Net.HttpStatusCode)400)
            {
                Console.Write("Enter a valid token: ");
                string token = Console.ReadLine();
                Task.Run(() => qTrade.Authenticate(token));
            }

        }

        private static void WebsocketQuoteMsgWrapperCallback(string message, DateTime messageTime)
        {
            if (!message.Contains("success"))
            {
                var quoteResp = Questrade.JsonToQuotes(message);
                for (int i = 0; i < quoteResp.quotes.Length; i++)
                {
                    Console.WriteLine(string.Format("{0} - Bid: {1}, BidSize: {2}, Ask: {3}, AskSize: {4}",
                    messageTime.ToString("HH:mm:ss"), quoteResp.quotes[i].bidPrice, quoteResp.quotes[i].bidSize,
                    quoteResp.quotes[i].askPrice, quoteResp.quotes[i].askSize));
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
