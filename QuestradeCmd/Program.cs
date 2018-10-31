using System;
using QuestradeAPI;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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
            Console.WriteLine(messageTime.ToShortTimeString() + message);
        }

        private static void WebsocketNotificationMsgWrapperCallback(string message, DateTime messageTime)
        {
            Console.WriteLine(messageTime.ToShortTimeString() + message);
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
                authStatusCode = client.Authenticate(printLine, printAccessToken).Result;


                switch (authStatusCode)
                {
                    case HttpStatusCode.OK:
                        return true;
                    case System.Net.HttpStatusCode.BadRequest:
                        Console.WriteLine("Invalid Token");
                        break;
                    /*case System.Net.HttpStatusCode.NotFound:
                        Console.WriteLine("404 not found.");
                        break;
                    case System.Net.HttpStatusCode.TooManyRequests:
                        Console.WriteLine("Too many requests.");
                        break;*/
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
                Console.WriteLine("0. Exit");
                menuEntry = Console.ReadLine();
                Console.WriteLine();
                switch (menuEntry)
                {

                    #region Download candlestick data case
                    case "1":
                        DateTime start = new DateTime();
                        DateTime end = new DateTime();
                        Console.Write("Start date(yyyy/MM/dd): ");
                        consoleEntry = Console.ReadLine();
                        bool correctFormat = false;
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
                            int id;
                            string idStr;
                            correctFormat = false;
                            do
                            {
                                consoleEntry = Console.ReadLine();
                                if (consoleEntry != "0")
                                {
                                    correctFormat = int.TryParse(consoleEntry, out id);
                                    if (!correctFormat)
                                    {
                                        Console.Write("Incorrect format entered. Please try again. To exit, enter 0: ");
                                        consoleEntry = Console.ReadLine();

                                        correctFormat = int.TryParse(consoleEntry, out id);
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
                                                Console.WriteLine(string.Format("{0}. {1}", i + 1, ((Questrade.HistoricalGrandularity)i).ToString()));
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

                                                var candleReq = qTrade.GetCandles(idStr, start, end, (Questrade.HistoricalGrandularity)candleGranularity).Result;

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
                        var result = qTrade.symbolSearch(consoleEntry).Result;
                        Console.WriteLine(string.Format("\n{0,-10}\t{1,-10}\t{2,-10}\t{3,-10}\n--------------------------------------------------------------", "Symbol", "Symbol ID", "Exchange", "Description"));
                        for (int i = 0; i < result.symbols.Length; i++)
                        {
                            Console.WriteLine(string.Format("{0,-10}\t{1,-10}\t{2,-10}\t{3,-10}", result.symbols[i].symbol, result.symbols[i].symbolId, result.symbols[i].listingExchange, result.symbols[i].description));
                        }
                        break;
                    case "3":
                        Task.Run(() => qTrade.SubToOrderNotif(WebsocketNotificationMsgWrapperCallback));
                        printLine("Connecting...");
                        break;
                    case "4":
                        Console.WriteLine("Please enter the symbol ID: ");
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
                }
                Console.WriteLine();
            } while (menuEntry != "0");
            
            
        }


        
    }


}
