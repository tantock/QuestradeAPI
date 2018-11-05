# QuestradeAPI
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
using System.Net;
using System.Threading.Tasks;
using QuestradeAPI;

namespace Example
{
    class Program
    {
        public static void Main (string[] args)
        {
            string initialToken = "_exampleRefreshTokenFromQuestrade";
            
            //Initialization
            Questrade qTrade = new Questrade(initialToken);
            
            //Authentication
            var resp = qTrade.Authenticate(printLine, printAccessToken).Result;
            bool isAuthenticated = false;
            
            if(resp.StatusCode == HttpStatusCode.OK)
            {
                isAuthenticated = true;
                //Make API call
                var AccountList = qTrade.GetAccounts().Result;

                //Error handling
                if (AccountList.isSuccess)
                {
                    var list = AccountList.q_obj; //Returned deserialized object from JSON response
                    for (int i = 0; i < list.accounts.Length; i++)
                    {
                        Console.WriteLine(string.Format("{0} {1} : {2}"
                            , list.accounts[i].clientAccountType
                            , list.accounts[i].type, list.accounts[i].number));
                    }
                    //Rate limit data for this type of call
                    Console.WriteLine("Number of calls remaining: " + AccountList.NumCallsLeft);

                    //Rate limit reset time
                    Console.WriteLine("Rate limit reset on: " + AccountList.RateReset.ToLongDateString() 
                    + " " + AccountList.RateReset.ToLongTimeString());
                }
                else
                {
                    if(AccountList.errorType == ErrorType.General)
                    {
                        var Error = AccountList.generalError;
                        //Handle error here
                    }
                    else
                    {
                        var Error = AccountList.orderError;
                        //Handle error here
                    }
                }

                //Subscribe to Level 1 data stream
                string symbolId = "123456";
                Task.Run(() => qTrade.StreamQuote(symbolId, WebsocketQuoteMsgWrapperCallback));

                //Subscribe to notification stream
                Task.Run(() => qTrade.SubToOrderNotif(WebsocketNotificationMsgWrapperCallback));
            }
            else
            {
                Console.WriteLine("Failed to authenticate");
            }
        }
        
        private static void printAccessToken(DateTime expiry)
        {
            //Optional
            Console.WriteLine(string.Format("Access token will expire on: {0} {1}", expiry.ToLongDateString(), expiry.ToLongTimeString()));
        }

        public static void printLine(string message)
        {
            //Optional
            Console.WriteLine(message);
        }
        
        private static void WebsocketQuoteMsgWrapperCallback(string message, DateTime messageTime)
        {
            if (!message.Contains("success"))
            {
                var quoteResp = Questrade.JsonToQuotes(message);
                for(int i = 0; i < quoteResp.quotes.Length; i++)
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
            else if(!message.Contains("success"))
            {
                var orderNotif = Questrade.JsonToOrderNotif(message);
                //Do something with notification
            }
        }
    }
}
```
