using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

    public class Questrade
    {
        private string _token;
        private string _accessToken = "";
        private DateTime _accessTokenExpiry;
        static HttpClient authClient = new HttpClient();
        static HttpClient apiClient = new HttpClient();
        private AuthenticateResp _auth;

        public enum HistoricalGrandularity { OneMinute,TwoMinutes, ThreeMinutes, FourMinutes, FiveMinutes, TenMinutes, FifteenMinutes, TwentyMinutes, HalfHour,OneHour,TwoHour,FourHour,OneDay,OneWeek,OneMonth,OneYear }
        public Questrade() { }

        public Questrade(string token)
        {
            _token = token;
        }

        public async Task<AuthenticateResp> Authenticate()
        {
            if(_accessToken == "" || DateTime.Now.Ticks < _accessTokenExpiry.Ticks)
            {
                var resp = await authClient.GetAsync(string.Format("https://login.questrade.com/oauth2/token?grant_type=refresh_token&refresh_token={0}", _token));

                if (resp.IsSuccessStatusCode)
                {
                    var authObj = new AuthenticateResp();
                    _auth = JsonConvert.DeserializeObject<AuthenticateResp>(resp.Content.ReadAsStringAsync().Result);
                    apiClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization",string.Format("{0} {1}", _auth.token_type, _auth.access_token));
                    return _auth;
                }
                else
                {
                    throw new HttpRequestException(resp.StatusCode.ToString());
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
                var authObj = new AuthenticateResp();

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
    }
}
