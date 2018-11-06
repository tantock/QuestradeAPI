using System;
using QuestradeAPI;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using QuestradeAPI.Events;
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
            }
            else if (!message.Contains("success"))
            {
                var orderNotif = Questrade.JsonToOrderNotif(message);
            }
        }

        private static bool CodeToToken(ref Questrade client, string clientId, string code, string redirectUrl)
        {
            try
            {
                client.CodeToAccessToken(clientId, redirectUrl, code).RunSynchronously();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return false;
        }

        private static void MenuLoop(Questrade qTrade)
        {
            string menuEntry;
            try
            {
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
                    string consoleEntry;
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

                                                    Task.Run(() => qTrade.GetCandles(idStr, start, end, (QuestradeAPI.HistoricalGrandularity)candleGranularity));


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
                            Console.WriteLine(string.Format("\n{0,-10}\t{1,-10}\t{2,-10}\t{3,-10}\n--------------------------------------------------------------", "Symbol", "Symbol ID", "Exchange", "Description"));

                            Task.Run(() => qTrade.symbolSearch(consoleEntry));


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
                            Task.Run(() => qTrade.Authenticate());
                            break;
                        case "7":
                            Task.Run(() => qTrade.GetAccounts());
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

                            Task.Run(() => qTrade.GetAccountBalance(accNum.ToString()));

                            break;
                        case "9":
                            Console.WriteLine(qTrade.RefreshToken);
                            break;
                    }
                    Console.WriteLine();
                } while (menuEntry != "0");
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            
            Environment.Exit(0);
        }

        static Questrade qTrade = new Questrade();

        static Task menuLoopTask = new Task(() => MenuLoop(qTrade));

        static void Main(string[] args)
        {

            string initialToken = "";
            string consoleEntry = "";

            #region Initial Authentication

            Console.Write("Please enter the refresh token to exchange for an access token or enter -1 to exit: ");
            consoleEntry = Console.ReadLine();
            if (consoleEntry != "-1")
            {
                initialToken = consoleEntry;
                qTrade = new Questrade(initialToken);

                qTrade.OnAccountBalancesRecieved += QTrade_OnAccountBalancesRecieved;
                qTrade.OnAccountsRecieved += QTrade_OnAccountsRecieved;
                qTrade.OnCandleRecieved += QTrade_OnCandleRecieved;
                qTrade.OnGeneralErrorRecieved += QTrade_OnGeneralErrorRecieved;
                qTrade.OnOrderProcessingErrorRecieved += QTrade_OnOrderProcessingErrorRecieved;
                qTrade.OnSuccessfulAuthentication += QTrade_OnSuccessfulAuthentication;
                qTrade.OnSymbolSearchRecieved += QTrade_OnSymbolSearchRecieved;
                qTrade.OnUnsuccessfulAuthentication += QTrade_OnUnsuccessfulAuthentication;

                Task.Run(() => qTrade.Authenticate());

                Process.GetCurrentProcess().WaitForExit();
            }
            #endregion

            

        }

        private static void QTrade_OnSymbolSearchRecieved(object sender, APISymbolSearchReturnArgs e)
        {
            var list = e.symbols.symbols;
            for (int i = 0; i < list.Length; i++)
            {
                Console.WriteLine(string.Format("{0,-10}\t{1,-10}\t{2,-10}\t{3,-10}", list[i].symbol, list[i].symbolId, list[i].listingExchange, list[i].description));
            }
        }

        private static void QTrade_OnUnsuccessfulAuthentication(object sender, UnsuccessfulAuthArgs e)
        {
            Console.WriteLine("Authentication unsuccessful. " + e.resp.ReasonPhrase);
            if (e.resp.StatusCode == (HttpStatusCode)400)
            {
                Console.Write("Enter a valid token: ");
                string token = Console.ReadLine();
                Task.Run(() => qTrade.Authenticate(token));
            }
        }

        private static void QTrade_OnSuccessfulAuthentication(object sender, SuccessAuthEventArgs e)
        {
            Console.WriteLine(string.Format("Access token will expire on: {0} {1}", e.TokenExpiry.ToLongDateString(), e.TokenExpiry.ToLongTimeString()));
            if (!(menuLoopTask.Status == TaskStatus.Running))
            {
                menuLoopTask.Start();
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

        private static void QTrade_OnCandleRecieved(object sender, APICandleReturnArgs e)
        {
            var candleReq = e.candles;
            Console.WriteLine("\nOpen\tHigh\tLow\tClose\tVolume\n--------------------------------------------------------------");
            for (int i = 0; i < candleReq.candles.Length; i++)
            {
                Console.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t", candleReq.candles[i].open, candleReq.candles[i].high, candleReq.candles[i].low, candleReq.candles[i].close, candleReq.candles[i].volume));
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

        private static void QTrade_OnAccountBalancesRecieved(object sender, APIAccountBalancesReturnArgs e)
        {
            Console.WriteLine("Combined balances:");
            for(int i = 0; i < e.details.combinedBalances.Length; i++)
            {
                var c = e.details.combinedBalances;
                Console.WriteLine(string.Format("{0} - Buying power: {1}, Cash: {2}, MaintExcess: {3}, Market Value: {4}, Total Equity: {5}",c[i].currency, c[i].buyingPower, c[i].cash, c[i].maintenanceExcess, c[i].marketValue, c[i].totalEquity));
            }
        }
    }


}
