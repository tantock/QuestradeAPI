﻿using System;
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
            //Starts stream. Return object is used for error handling.
            Task.Run(() => qTrade.StreamQuote(symbolId));

            //Subscribe to notification stream
            Task.Run(() => qTrade.SubToOrderNotif());

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
            qTrade.OnQuoteStreamRecieved += QTrade_OnStreamRecieved;
            qTrade.OnOrderNotifRecieved += QTrade_OnOrderNotifRecieved;

            Task.Run(() => qTrade.Authenticate()); //Make authentication

            System.Diagnostics.Process.GetCurrentProcess().WaitForExit();
        }

        private static void QTrade_OnStreamRecieved(object sender, APIStreamQuoteRecievedArgs e)
        {
            var quoteResp = e.quotes;
            for (int i = 0; i < quoteResp.quotes.Length; i++)
            {
                Console.WriteLine(string.Format("{0} - Bid: {1}, BidSize: {2}, Ask: {3}, AskSize: {4}",
                e.time.ToString("HH:mm:ss"), quoteResp.quotes[i].bidPrice, quoteResp.quotes[i].bidSize, quoteResp.quotes[i].askPrice, quoteResp.quotes[i].askSize));
            }
        }

        private static void QTrade_OnOrderNotifRecieved(object sender, APIOrderNotificationRecievedArg e)
        {
            for (int i = 0; i < e.OrderNotif.orders.Length; i++)
            {
                Console.WriteLine(string.Format("{0} - Account: {1}, Symbol: {2}", e.time.ToString("HH:mm:ss"), e.OrderNotif.accountNumber, e.OrderNotif.orders[i].symbol));
            }
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
        
    }
}
