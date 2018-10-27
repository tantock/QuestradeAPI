using System;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuestradeAPI
{
    public class AuthenticateResp
    {
        public string access_token { get; set; }
        public string api_server { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public string token_type { get; set; }
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
        private string _token;
        private string _accessToken = "";
        static HttpClient authClient = new HttpClient();
        static HttpClient apiClient = new HttpClient();
        private AuthenticateResp _auth = null;

        public enum HistoricalGrandularity { OneMinute,TwoMinutes, ThreeMinutes, FourMinutes, FiveMinutes, TenMinutes, FifteenMinutes, TwentyMinutes, HalfHour,OneHour,TwoHour,FourHour,OneDay,OneWeek,OneMonth,OneYear }
        public Questrade() { }

        public Questrade(string token)
        {
            _token = token;
        }

        public async Task<HttpStatusCode> Authenticate(Action<string> authenticateCallback)
        {
            if(_auth == null)
            {
                HttpResponseMessage resp = null;

                authenticateCallback("Authenticating...");

                resp = await authClient.GetAsync(string.Format("https://login.questrade.com/oauth2/token?grant_type=refresh_token&refresh_token={0}", _token));

                if (resp.IsSuccessStatusCode)
                {
                    var authObj = new AuthenticateResp();
                    _auth = JsonConvert.DeserializeObject<AuthenticateResp>(resp.Content.ReadAsStringAsync().Result);
                    apiClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", string.Format("{0} {1}", _auth.token_type, _auth.access_token));
                    return resp.StatusCode;
                }
                else
                {
                    return resp.StatusCode;
                }


            }
            else
            {
                throw new ArgumentException(string.Format("_accessToken: {0}", _accessToken));
            }
        }

        public async Task<Candles> GetCandles(string id,DateTime start, DateTime end,HistoricalGrandularity gran)
        {
            
            var resp = await apiClient.GetAsync(string.Format("{0}v1/markets/candles/{1}?startTime={2}&endTime={3}&interval={4}", _auth.api_server,id,DateTimeToString(start), DateTimeToString(end),gran.ToString()));


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
            var resp = await apiClient.GetAsync(string.Format("{0}v1/symbols/search?prefix={1}", _auth.api_server,query));

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
    }
}
