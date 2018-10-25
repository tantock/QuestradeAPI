using System;
using QuestradeAPI;
using System.Diagnostics;

namespace QuestradeCmd
{
    class Program
    {
        static void Main(string[] args)
        {
            
            string initialToken = "";
            initialToken = Console.ReadLine();

            Debug.WriteLine(initialToken);

            Questrade qTrade = new Questrade(initialToken);

            var resp = qTrade.Authenticate().Result;
            
            Console.WriteLine(resp.refresh_token);
            Debug.WriteLine(resp.refresh_token);

            

            var candleReq = qTrade.GetCandles("16529510", new DateTime(2018, 10, 24), DateTime.Now, Questrade.HistoricalGrandularity.OneMinute).Result;

            for(int i = 0; i < candleReq.candles.Length; i++)
            {
                Debug.WriteLine(candleReq.candles[i].open);
            }
        }
    }


}
