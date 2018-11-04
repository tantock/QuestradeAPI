using System;
using QuestradeAPI;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace QuestradeCmd
{
    class Program
    {
        public static void printLine(string message)
        {
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
            Debug.WriteLine(message);//TODO remove debug line

            if (message.Contains("executions"))
            {
                var executionNotif = Questrade.JsonToExecutionNotif(message);
            }
            else if(!message.Contains("success"))
            {
                var orderNotif = Questrade.JsonToOrderNotif(message);
            }
        }

        private static void printAccessToken(DateTime expiry)
        {
            Console.WriteLine(string.Format("Access token will expire on: {0} {1}", expiry.ToLongDateString(), expiry.ToLongTimeString()));
        }

        private static bool authenticate(ref Questrade client)
        {
            try
            {

                HttpStatusCode authStatusCode;
                var resp = client.Authenticate(printLine, printAccessToken).Result;
                authStatusCode = resp.StatusCode;
                switch (authStatusCode)
                {
                    case HttpStatusCode.OK:
                        return true;
                    case System.Net.HttpStatusCode.BadRequest:
                        Console.WriteLine("Invalid Token");
                        break;
                    case System.Net.HttpStatusCode.NotFound:
                        Console.WriteLine("404 not found.");
                        break;
                    case System.Net.HttpStatusCode.TooManyRequests:
                        Console.WriteLine("Too many requests.");
                        break;
                    default:
                        Console.WriteLine(authStatusCode.ToString());
                        break;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;   
            }
            return false;
        }

        private static bool CodeToToken(ref Questrade client, string clientId, string code, string redirectUrl)
        {
            try
            {

                HttpStatusCode authStatusCode;
                var resp = client.CodeToAccessToken(clientId, redirectUrl, code, printLine, printAccessToken).Result;
                authStatusCode = resp.StatusCode;
                switch (authStatusCode)
                {
                    case HttpStatusCode.OK:
                        return true;
                    case System.Net.HttpStatusCode.BadRequest:
                        Console.WriteLine("Invalid Token");
                        break;
                    case System.Net.HttpStatusCode.NotFound:
                        Console.WriteLine("404 not found.");
                        break;
                    case System.Net.HttpStatusCode.TooManyRequests:
                        Console.WriteLine("Too many requests.");
                        break;
                    default:
                        Console.WriteLine(authStatusCode.ToString());
                        break;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return false;
        }

        static void Main(string[] args)
        {
            
            string initialToken = "";
            string consoleEntry = "";
            string menuEntry = "";
            Questrade qTrade = null;
            bool isAuthenticated = false;

            #region Initial Authentication
            do
            {
                Console.Write("Please enter the refresh token to exchange for an access token or enter -1 to exit: ");
                consoleEntry = Console.ReadLine();
                if (consoleEntry != "-1")
                {
                    initialToken = consoleEntry;
                    qTrade = new Questrade(initialToken);
                    isAuthenticated = authenticate(ref qTrade);
                }
                else
                {
                    Environment.Exit(0);
                }
            } while (!isAuthenticated);
            Console.WriteLine("Authentication Success.");
            #endregion
            do
            {
                Console.WriteLine("********Questrade API Command-Line Interface*********\nPlease Select a number from the menu below:");
                Console.WriteLine("1. Retrieve and print candlestick data");
                Console.WriteLine("2. Query by name");
                Console.WriteLine("3. Subscribe to order notifications");
                Console.WriteLine("4. Connect to quote stream");
                Console.WriteLine("5. Disconnect from quote stream");
                Console.WriteLine("6. Reauthenticate");
                Console.WriteLine("7. Retrive Accounts");
                Console.WriteLine("8. Account Info");
                Console.WriteLine("9. Print refresh token");
                Console.WriteLine("0. Exit");
                menuEntry = Console.ReadLine();
                Console.WriteLine();
                bool correctFormat = false;
                int symbolId;
                int accNum;
                switch (menuEntry)
                {

                    #region Download candlestick data case
                    case "1":
                        DateTime start = new DateTime();
                        DateTime end = new DateTime();
                        Console.Write("Start date(yyyy/MM/dd): ");
                        consoleEntry = Console.ReadLine();
                        do
                        {
                            if (consoleEntry != "0")
                            {
                                correctFormat = DateTime.TryParse(consoleEntry, out start);
                                if (!correctFormat)
                                {
                                    Console.Write("Incorrect format entered. Please try again. To exit, enter 0\nStart date: ");
                                    consoleEntry = Console.ReadLine();
                                    correctFormat = DateTime.TryParse(consoleEntry, out start);
                                }
                                if (correctFormat)
                                {
                                    correctFormat = false;
                                    Console.Write("End date(yyyy/MM/dd): ");
                                    consoleEntry = Console.ReadLine();
                                    correctFormat = DateTime.TryParse(consoleEntry, out end);
                                    if (!correctFormat)
                                    {
                                        Console.Write("Incorrect format entered. Please try again. To exit, enter 0\nEnd date: ");
                                        consoleEntry = Console.ReadLine();
                                        correctFormat = DateTime.TryParse(consoleEntry, out end);
                                    }
                                }
                            }
                            else
                            {

                                break;
                            }
                        } while (!correctFormat);

                        if (correctFormat)
                        {
                            Console.Write("Enter ticker ID number: ");
                            correctFormat = false;
                            string idStr;
                            do
                            {
                                consoleEntry = Console.ReadLine();
                                if (consoleEntry != "0")
                                {
                                    correctFormat = int.TryParse(consoleEntry, out symbolId);
                                    if (!correctFormat)
                                    {
                                        Console.Write("Incorrect format entered. Please try again. To exit, enter 0: ");
                                        consoleEntry = Console.ReadLine();

                                        correctFormat = int.TryParse(consoleEntry, out symbolId);
                                    }
                                    if (correctFormat)
                                    {
                                        //"16529510"
                                        idStr = consoleEntry;
                                        correctFormat = false;
                                        int candleGranularity;
                                        Console.WriteLine("Please enter the candle time frame:");
                                        do
                                        {
                                            for (int i = 0; i < 15; i++)
                                            {
                                                Console.WriteLine(string.Format("{0}. {1}", i + 1, ((QuestradeAPI.HistoricalGrandularity)i).ToString()));
                                            }
                                            consoleEntry = Console.ReadLine();

                                            correctFormat = int.TryParse(consoleEntry, out candleGranularity);
                                            if (!correctFormat || candleGranularity < 1 || candleGranularity > 16)
                                            {
                                                Console.Write("Invalid selection. Please try again: ");
                                            }
                                            else
                                            {
                                                candleGranularity -= 1;

                                                var resp = qTrade.GetCandles(idStr, start, end, (QuestradeAPI.HistoricalGrandularity)candleGranularity).Result;

                                                var candleReq = resp.q_obj;
                                                Console.WriteLine("\nOpen\tHigh\tLow\tClose\tVolume\n--------------------------------------------------------------");
                                                for (int i = 0; i < candleReq.candles.Length; i++)
                                                {
                                                    Console.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t", candleReq.candles[i].open, candleReq.candles[i].high, candleReq.candles[i].low, candleReq.candles[i].close, candleReq.candles[i].volume));
                                                }
                                            }

                                        } while (!correctFormat);


                                    }
                                }
                                else
                                {

                                    break;
                                }
                            } while (!correctFormat);


                        }

                        break;
                    #endregion

                    case "2":
                        Console.Write("Please enter a search query: ");
                        consoleEntry = Console.ReadLine();
                        int numQueries;
                        Console.Write("Number of queries to return: ");
                        correctFormat = int.TryParse(Console.ReadLine(), out numQueries);
                        List<Symbols> resultList = new List<Symbols>();
                        int offset;
                        for(int i = 0; i < numQueries; i++)
                        {
                            offset = i * 20;
                            resultList.Add(qTrade.symbolSearch(consoleEntry, offset).Result.q_obj);
                        }
                        Console.WriteLine(string.Format("\n{0,-10}\t{1,-10}\t{2,-10}\t{3,-10}\n--------------------------------------------------------------", "Symbol", "Symbol ID", "Exchange", "Description"));
                        List<string> outputList = new List<string>();
                        int count = 0;
                        for(int j = 0; j < numQueries; j++)
                        {
                            for (int i = 0; i < resultList[j].symbols.Length; i++)
                            {
                                
                                outputList.Add(string.Format("{0,-10}\t{1,-10}\t{2,-10}\t{3,-10}", resultList[j].symbols[i].symbol, resultList[j].symbols[i].symbolId, resultList[j].symbols[i].listingExchange, resultList[j].symbols[i].description));
                                Console.WriteLine(outputList[count]);
                                count++;
                            }
                        }
                        Console.Write("Do you want to save to file? (y or n): ");
                        string c = Console.ReadLine();
                        
                        if(c == "y" || c == "Y")
                        {
                            Console.Write("Enter file name/path: ");
                            consoleEntry = Console.ReadLine();
                            while(consoleEntry == "")
                            {
                                consoleEntry = Console.ReadLine();
                            }
                            try
                            {
                                using (System.IO.StreamWriter file = new System.IO.StreamWriter(consoleEntry, true))
                                {
                                    for (int i = 0; i < outputList.Count; i++)
                                    {
                                        file.WriteLine(outputList[i]);
                                    }
                                    Console.WriteLine("File written successfully to: {0}", System.IO.Directory.GetCurrentDirectory());
                                }
                                
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            
                        }
                        
                        
                        break;
                    case "3":
                        Task.Run(() => qTrade.SubToOrderNotif(WebsocketNotificationMsgWrapperCallback));
                        printLine("Connecting...");
                        break;
                    case "4":
                        Console.Write("Please enter the symbol ID: ");
                        consoleEntry = Console.ReadLine();
                        Task.Run(() => qTrade.StreamQuote(consoleEntry, WebsocketQuoteMsgWrapperCallback));
                        printLine("Connecting...");
                        break;
                    case "5":
                        Questrade.quoteStreamClient.Close();
                        break;
                    case "6":
                        var isSuccess = authenticate(ref qTrade);

                        if (isSuccess)
                        {
                            Console.WriteLine("Successully reauthenticated.");
                        }
                        else
                        {
                            Console.WriteLine("Reauthentication unsuccessful.");
                        }
                        break;
                    case "7":
                        var AccountList = Task.Run(() => qTrade.GetAccounts()).Result.q_obj;

                        for(int i = 0; i < AccountList.accounts.Length; i++)
                        {
                            Console.WriteLine(string.Format("{0} {1} : {2}"
                                ,AccountList.accounts[i].clientAccountType, AccountList.accounts[i].type, AccountList.accounts[i].number));

                        }
                        break;
                    case "8":
                        Console.Write("Enter the account number: ");
                        consoleEntry = Console.ReadLine();
                        correctFormat = int.TryParse(consoleEntry, out accNum);
                        while (!correctFormat && consoleEntry != "0")
                        {
                            Console.Write("Invalid entry. Please try again.");
                            consoleEntry = Console.ReadLine();
                            correctFormat = int.TryParse(consoleEntry, out accNum);
                        }

                        var AccountInfo = Task.Run(() => qTrade.GetAccountBalance(accNum.ToString())).Result.q_obj;

                        break;
                    case "9":
                        Console.WriteLine(qTrade.RefreshToken);
                        break;
                }
                Console.WriteLine();
            } while (menuEntry != "0");
            
            
        }
    }


}
