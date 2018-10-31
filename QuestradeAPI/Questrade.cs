using System;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using WebSocketSharp.Net;

namespace QuestradeAPI
{
    public class StreamPort
    {
        public int streamPort { get; set; }
    }

    public class AuthenticateResp
    {
        public string access_token { get; set; }
        public string api_server { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public string token_type { get; set; }
        public DateTime expires_in_date { get; set; }
    }

    public class Candle
    {
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public double open { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double close { get; set; }
        public int volume { get; set; }
        public double VWAP { get; set; }
    }

    public class Quote
    {
        public int symbol { get; set; }
        public string symbolId { get; set; }
        public string tier { get; set; } //TODO change to enum
        public double bidPrice { get; set; }
        public int bidSize { get; set; }
        public double askPrice { get; set; }
        public int askSize { get; set; }
        public double lastTradeTrHrs { get; set; }
        public double lastTradePrice { get; set; }
        public int lastTradeSize { get; set; }
        public string lastTradeTick { get; set; } //TODO change to enum
        public int volume { get; set; }
        public double openPrice { get; set; }
        public double highPrice { get; set; }
        public double lowPrice { get; set; }
        public bool delay { get; set; }
        public bool isHalted { get; set; }
    }

    public class Quotes
    {
        Quote[] quotes { get; set; }
    }

    public class Candles
    {
        public Candle[] candles { get; set; }
    }

    public enum SecurityType { Stock,Option,Bond,Right,Gold,MutualFund,Index}

    public enum ListingExch { TSX,TSXV, CNSX, MX, NASDAQ, NYSE, ARCA, OPRA, PINX, OTCBB}

    public class Symbol
    {
        public string symbol { get; set; }
        public int symbolId { get; set; }
        public string description { get; set; }
        public SecurityType securityType { get; set; }
        public string listingExchange { get; set; }
        public bool isQuotable { get; set; }
        public bool isTradable { get; set; }
        public string currency { get; set; }
    }

    public class Symbols
    {
        public Symbol[] symbols;
    }

    public class Questrade
    {
        static HttpClient authClient = new HttpClient();
        static HttpClient apiClient = new HttpClient();
        public static WebSocketSharp.WebSocket notificationClient;
        public static WebSocketSharp.WebSocket quoteStreamClient;
        private AuthenticateResp _auth;

        public enum HistoricalGrandularity { OneMinute,TwoMinutes, ThreeMinutes, FourMinutes, FiveMinutes, TenMinutes, FifteenMinutes, TwentyMinutes, HalfHour,OneHour,TwoHour,FourHour,OneDay,OneWeek,OneMonth,OneYear }
        public Questrade() { }

        public Questrade(string token)
        {
            _auth = new AuthenticateResp();
            _auth.refresh_token = token;

            

        }

        public async Task<System.Net.HttpStatusCode> Authenticate(Action<string> preAuthenticateCallback, Action<DateTime> accessTokenExpiryCallback)
        {
            HttpResponseMessage resp = null;

            preAuthenticateCallback("Authenticating...");

            resp = await authClient.GetAsync(string.Format("https://login.questrade.com/oauth2/token?grant_type=refresh_token&refresh_token={0}", _auth.refresh_token));

            if (resp.IsSuccessStatusCode)
            {
                var dateTimeNow = DateTime.Now;
                _auth = JsonConvert.DeserializeObject<AuthenticateResp>(resp.Content.ReadAsStringAsync().Result);
                _auth.expires_in_date = dateTimeNow.AddSeconds(_auth.expires_in);
                apiClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", string.Format("{0} {1}", _auth.token_type, _auth.access_token));
                apiClient.BaseAddress = new Uri(_auth.api_server);
                accessTokenExpiryCallback(_auth.expires_in_date);

                return resp.StatusCode;
            }
            else
            {
                return resp.StatusCode;
            }
        }

        public async Task<Candles> GetCandles(string id,DateTime start, DateTime end,HistoricalGrandularity gran)
        {
            
            var resp = await apiClient.GetAsync(string.Format("v1/markets/candles/{0}?startTime={1}&endTime={2}&interval={3}",id,DateTimeToString(start), DateTimeToString(end),gran.ToString()));


            if (resp.IsSuccessStatusCode)
            {

                var str = resp.Content.ReadAsStringAsync().Result;

                return JsonConvert.DeserializeObject<Candles>(resp.Content.ReadAsStringAsync().Result);
            }
            else
            {
                throw new HttpRequestException(resp.StatusCode.ToString());
            }
        }
        
        private string DateTimeToString(DateTime date)
        {
            return string.Format("{0:yyyy-MM-ddTHH:mm:ss}{0:zzz}",date);
        }

        public async Task<Symbols> symbolSearch(string query)
        {
            var resp = await apiClient.GetAsync(string.Format("v1/symbols/search?prefix={0}",query));

            if (resp.IsSuccessStatusCode)
            {
                var result = resp.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<Symbols>(resp.Content.ReadAsStringAsync().Result);
            }
            else
            {
                throw new HttpRequestException(resp.StatusCode.ToString());
            }
        }
        
        #region Streaming methods
        public enum streamType { RawSocket, WebSocket }

        Action<string, DateTime> SubToOrderNotif_Callback;

        /// <summary>
        /// This method calls the SubToOrderNotif_Callback method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SubToOrderNotift_OnMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
            if (e.IsText)
            {
                SubToOrderNotif_Callback(e.Data, DateTime.Now);
            }
            if (e.IsBinary)
            {
                throw new NotImplementedException();
            }
        }

        Action<string, DateTime> StreamQuote_Callback;

        /// <summary>
        /// This method calls the StreamQuote_Callback method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StreamQuote_OnMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
            if (e.IsText)
            {
                StreamQuote_Callback(e.Data, DateTime.Now);
            }
            if (e.IsBinary)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Sends a request to Questrade to push notifcations to this Websocket client. Data is recieved through the OnMessageCallback.
        /// </summary>
        /// <param name="OnMessageCallback">Callback to pass message from this websocket client</param>
        /// <returns></returns>
        public async Task<bool> SubToOrderNotif(Action<string, DateTime> OnMessageCallback)
        {

            var resp = await apiClient.GetAsync(string.Format("v1/notifications?mode={0}", streamType.WebSocket.ToString()));//Requests server to send notification to port

            if (resp.IsSuccessStatusCode)
            {
                var result = resp.Content.ReadAsStringAsync().Result;
                var port = JsonConvert.DeserializeObject<StreamPort>(resp.Content.ReadAsStringAsync().Result); //Gets what port to connect to


                var api_base = new Uri(_auth.api_server);
                SubToOrderNotif_Callback = OnMessageCallback;

                notificationClient = new WebSocketSharp.WebSocket(string.Format("{0}{1}:{2}/", @"wss://", api_base.Host, port.streamPort));


                notificationClient.OnMessage += SubToOrderNotift_OnMessage;

                notificationClient.Connect();

                notificationClient.Send(_auth.access_token);

                return true;
            }
            else
            {
                throw new HttpRequestException(resp.StatusCode.ToString());
            }

        }

        /// <summary>
        /// Sends a request to Questrade to push quote data to this Websocket client. Data is recieved through the OnMessageCallback.
        /// </summary>
        /// <param name="ids">Comma seperated symbol id</param>
        /// <param name="OnMessageCallback">Callback to pass message from this websocket client</param>
        /// <returns></returns>
        public async Task<bool> StreamQuote(string ids, Action<string, DateTime> OnMessageCallback)
        {
            var resp = await apiClient.GetAsync(string.Format("v1/markets/quotes?ids={0}&stream=true&mode={1}", ids, streamType.WebSocket.ToString()));//Requests server to send notification to port
            if (resp.IsSuccessStatusCode)
            {
                //Setup Websocket to recieve data from
                var result = resp.Content.ReadAsStringAsync().Result;
                var port = JsonConvert.DeserializeObject<StreamPort>(resp.Content.ReadAsStringAsync().Result);//Gets what port to connect to


                var api_base = new Uri(_auth.api_server);
                StreamQuote_Callback = OnMessageCallback;

                quoteStreamClient = new WebSocketSharp.WebSocket(string.Format("{0}{1}:{2}/", @"wss://", api_base.Host, port.streamPort));

                quoteStreamClient.OnMessage += StreamQuote_OnMessage;

                quoteStreamClient.Connect();

                quoteStreamClient.Send(_auth.access_token);

                return true;
            }
            else
            {
                throw new HttpRequestException(resp.StatusCode.ToString());
            }
        }
        #endregion

    }
}
