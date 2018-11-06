using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuestradeAPI.Events;

namespace QuestradeAPI
{

    public class Questrade
    {
        static HttpClient authClient;
        static HttpClient apiClient;
        public static WebSocketSharp.WebSocket notificationClient;
        public static WebSocketSharp.WebSocket quoteStreamClient;

        private static AuthenticateResp _auth;

        #region EventHandlers
        public event EventHandler<SuccessAuthEventArgs> OnSuccessfulAuthentication;

        public event EventHandler<UnsuccessfulAuthArgs> OnUnsuccessfulAuthentication;
        
        public event EventHandler<GeneralErrorEventArgs> OnGeneralErrorRecieved;

        public event EventHandler<OrderProcessingErrorEventArgs> OnOrderProcessingErrorRecieved;
        
        public event EventHandler<APICandleReturnArgs> OnCandleRecieved;

        public event EventHandler<APIAccountsReturnArgs> OnAccountsRecieved;

        public event EventHandler<APIAccountBalancesReturnArgs> OnAccountBalancesRecieved;

        public event EventHandler<APISymbolSearchReturnArgs> OnSymbolSearchRecieved;
        #endregion


        /// <summary>
        /// Ctor for a wrapper for the Questrade API
        /// </summary>
        /// <param name="token">Refresh token</param>
        public Questrade(string token = "")
        {
            _auth = new AuthenticateResp();
            _auth.refresh_token = token;
            authClient = new HttpClient();
        }
        #region Authenticate methods

        /// <summary>
        /// Method to run when successful authentication
        /// </summary>
        /// <param name="resp"></param>
        /// <param name="accessTokenExpiryCallback"></param>
        private void PostAuthentication(HttpResponseMessage resp)
        {
            var dateTimeNow = DateTime.Now;
            _auth = JsonConvert.DeserializeObject<AuthenticateResp>(resp.Content.ReadAsStringAsync().Result);
            _auth.expires_in_date = dateTimeNow.AddSeconds(_auth.expires_in);
            apiClient = new HttpClient();
            apiClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", string.Format("{0} {1}", _auth.token_type, _auth.access_token));
            apiClient.BaseAddress = new Uri(_auth.api_server);

            SuccessAuthEventArgs arg = new SuccessAuthEventArgs();
            arg.TokenExpiry = _auth.expires_in_date;
            OnSuccessfulAuthentication(this, arg);
        }

        /// <summary>
        /// Exchanges code from redirect URL parameter for an access token. Read more at https://www.questrade.com/api/documentation/authorization
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="redirectURL"></param>
        /// <param name="code"></param>
        /// <param name="preAuthenticateCallback"></param>
        /// <param name="accessTokenExpiryCallback"></param>
        /// <returns></returns>
        public async Task CodeToAccessToken(string clientId, string redirectURL, string code)
        {
            string req = string.Format("https://login.questrade.com/oauth2/token?client_id={0}&code={1}&grant_type=authorization_code&redirect_uri={2}", clientId, code, redirectURL);

            var resp = await authClient.GetAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                PostAuthentication(resp);
                
            }
            else
            {
                UnsuccessfulAuthArgs arg = new UnsuccessfulAuthArgs();
                arg.resp = resp;
                OnUnsuccessfulAuthentication(this, arg);
            }
        }

        /// <summary>
        /// Request a new authentication token given the current refresh token
        /// </summary>
        /// <param name="preAuthenticateCallback">Method called just prior to authentication request</param>
        /// <param name="accessTokenExpiryCallback">Method called to pass token expiry once authenticated</param>
        /// <returns></returns>
        public async Task Authenticate(string refreshToken = "")
        {
            HttpResponseMessage resp = null;

            if(refreshToken != "")
            {
                _auth.refresh_token = refreshToken;
            }

            resp = await authClient.GetAsync(string.Format("https://login.questrade.com/oauth2/token?grant_type=refresh_token&refresh_token={0}", _auth.refresh_token));

            if (resp.IsSuccessStatusCode)
            {
                PostAuthentication(resp);
            }
            else
            {
                UnsuccessfulAuthArgs arg = new UnsuccessfulAuthArgs();
                arg.resp = resp;
                OnUnsuccessfulAuthentication(this, arg);
            }
        }
        
        /// <summary>
        /// The current refresh token
        /// </summary>
        public string RefreshToken
        {
            get { return _auth.refresh_token; }
        }
        #endregion

        #region Get Methods
        /// <summary>
        /// Method used to make a GET call to Questrade API
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="client">Authenticated http client</param>
        /// <param name="requestUri">Request URIm</param>
        /// <returns></returns>
        private async Task<APIReturn<T>> ApiGet<T>(HttpClient client,string requestUri)
        {
            APIReturn<T> apiReturn = new APIReturn<T>();
            var resp = await client.GetAsync(requestUri);

            //Parse rate limit headers
            System.Collections.Generic.IEnumerable<string> RemainIEnum;
            bool hasRateLimitRemain = resp.Headers.TryGetValues("X-RateLimit-Remaining", out RemainIEnum);

            System.Collections.Generic.IEnumerable<string> RateResetIEnum;
            bool hasRateReset = resp.Headers.TryGetValues("X-RateLimit-Reset", out RateResetIEnum);

            int numAPICallsRemain;

            if (hasRateLimitRemain && hasRateReset)
            {
                int.TryParse(((string[])RemainIEnum)[0], out numAPICallsRemain);
                int ResetUnixTime;
                int.TryParse(((string[])RateResetIEnum)[0], out ResetUnixTime);
                System.DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                apiReturn.RateReset = dt.AddSeconds(ResetUnixTime).ToLocalTime();
                apiReturn.NumCallsLeft = numAPICallsRemain;
            }

            //Error parse
            string respStr = await resp.Content.ReadAsStringAsync();

            if (respStr.Contains("message"))
            {
                apiReturn.isSuccess = false;
                if (resp.IsSuccessStatusCode)
                {
                    var arg = new OrderProcessingErrorEventArgs();
                    arg.OrderProcesssingErrorResp = JsonConvert.DeserializeObject<OrderProcesssingErrorResp>(resp.Content.ReadAsStringAsync().Result);
                    OnOrderProcessingErrorRecieved(this, arg); //Raise Error Event
                }
                else
                {
                    var arg = new GeneralErrorEventArgs();
                    arg.GeneralErrorResp = JsonConvert.DeserializeObject<GeneralErrorResp>(resp.Content.ReadAsStringAsync().Result);
                    OnGeneralErrorRecieved(this, arg); //Raise Error Event
                }
            }
            else
            {
                apiReturn.isSuccess = true;
                apiReturn.q_obj = JsonConvert.DeserializeObject<T>(resp.Content.ReadAsStringAsync().Result);
            }

            return apiReturn;
        }
        
        /// <summary>
        /// Retrives candlestick data between two dates given a symbol id
        /// </summary>
        /// <param name="id">Symbol ID number</param>
        /// <param name="start">Start date</param>
        /// <param name="end">End Date</param>
        /// <param name="gran">Data granularity</param>
        /// <returns></returns>
        public async Task GetCandles(string id,DateTime start, DateTime end,HistoricalGrandularity gran)
        {
            var obj = await ApiGet<Candles>(apiClient, string.Format("v1/markets/candles/{0}?startTime={1}&endTime={2}&interval={3}", id, DateTimeToString(start), DateTimeToString(end), gran.ToString()));

            if (obj.isSuccess)
            {
                var arg = new APICandleReturnArgs();
                arg.candles = obj.q_obj;
                arg.NumCallsLeft = obj.NumCallsLeft;
                arg.RateReset = obj.RateReset;
                OnCandleRecieved(this, arg);
            }
        }

        /// <summary>
        /// Retrives all accounts accessible by this session
        /// </summary>
        /// <returns></returns>
        public async Task GetAccounts()
        {
            var obj = await ApiGet<Accounts>(apiClient,"v1/accounts");
            if (obj.isSuccess)
            {
                var arg = new APIAccountsReturnArgs();
                arg.accounts = obj.q_obj;
                arg.NumCallsLeft = obj.NumCallsLeft;
                arg.RateReset = obj.RateReset;
                OnAccountsRecieved(this, arg);
            }
        }

        /// <summary>
        /// Retrives an account balance given an account number
        /// </summary>
        /// <param name="id">Account number</param>
        /// <returns></returns>
        public async Task GetAccountBalance(string id)
        {
            var obj = await ApiGet<AccountBalances>(apiClient, string.Format("v1/accounts/{0}/balances", id));
            if (obj.isSuccess)
            {
                var arg = new APIAccountBalancesReturnArgs();
                arg.details = obj.q_obj;
                arg.NumCallsLeft = obj.NumCallsLeft;
                arg.RateReset = obj.RateReset;
                OnAccountBalancesRecieved(this, arg);
            }
        }

        /// <summary>
        /// Retrives a list of symbols given a search query
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="offset">Starting offset on list</param>
        /// <returns></returns>
        public async Task symbolSearch(string query, int offset = 0)
        {
            var obj = await ApiGet<Symbols>(apiClient, string.Format("v1/symbols/search?prefix={0}&offset={1}", query, offset)); //var resp = await apiClient.GetAsync(string.Format("v1/symbols/search?prefix={0}&offset={1}",query,offset));
            if (obj.isSuccess)
            {
                var arg = new APISymbolSearchReturnArgs();
                arg.symbols = obj.q_obj;
                arg.NumCallsLeft = obj.NumCallsLeft;
                arg.RateReset = obj.RateReset;
                OnSymbolSearchRecieved(this, arg);
            }
        }

        #endregion

        private string DateTimeToString(DateTime date)
        {
            return string.Format("{0:yyyy-MM-ddTHH:mm:ss}{0:zzz}", date);
        }

        #region JSON deserializer
        /// <summary>
        /// Deserializes JSON response and returns a Order Error object
        /// </summary>
        /// <param name="json">JSON response</param>
        /// <returns></returns>
        public static OrderProcesssingErrorResp JsonToOrderProcessingErrorResp(string json)
        {
            return JsonConvert.DeserializeObject<OrderProcesssingErrorResp>(json);
        }

        /// <summary>
        /// Deserializes JSON response and returns a General Error object
        /// </summary>
        /// <param name="json">JSON response</param>
        /// <returns></returns>
        public static GeneralErrorResp JsonToGeneralErrorResp(string json)
        {
            return JsonConvert.DeserializeObject<GeneralErrorResp>(json);
        }

        /// <summary>
        /// Deserializes JSON response and returns a Orders object
        /// </summary>
        /// <param name="json">JSON response</param>
        /// <returns></returns>
        public static Orders JsonToOrders(string json)
        {
            return JsonConvert.DeserializeObject<Orders>(json);
        }

        /// <summary>
        /// Deserializes JSON response and returns a Quotes object
        /// </summary>
        /// <param name="json">JSON response</param>
        /// <returns></returns>
        public static Quotes JsonToQuotes(string json)
        {
            return JsonConvert.DeserializeObject<Quotes>(json);
        }

        /// <summary>
        /// Deserializes JSON response and returns a Execution object
        /// </summary>
        /// <param name="json">JSON response</param>
        /// <returns></returns>
        public static Executions JsonToExecution(string json)
        {
            return JsonConvert.DeserializeObject<Executions>(json);
        }

        /// <summary>
        /// Deserializes JSON response and returns a Execution notification object
        /// </summary>
        /// <param name="json">JSON response</param>
        /// <returns></returns>
        public static ExecutionNotification JsonToExecutionNotif(string json)
        {
            return JsonConvert.DeserializeObject<ExecutionNotification>(json);
        }

        /// <summary>
        /// Deserializes JSON response and returns a Execution notification object
        /// </summary>
        /// <param name="json">JSON response</param>
        /// <returns></returns>
        public static OrderNotification JsonToOrderNotif(string json)
        {
            return JsonConvert.DeserializeObject<OrderNotification>(json);
        }

        #endregion


        #region Streaming methods
        public enum streamType { RawSocket, WebSocket }

        private Action<string, DateTime> SubToOrderNotif_Callback;

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

        private Action<string, DateTime> StreamQuote_Callback;

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
        public async Task SubToOrderNotif(Action<string, DateTime> OnMessageCallback)
        {

            var resp = await ApiGet<StreamPort>(apiClient,string.Format("v1/notifications?mode={0}", streamType.WebSocket.ToString()));//Requests server to send notification to port

            if (resp.isSuccess)
            {
                var api_base = new Uri(_auth.api_server);
                SubToOrderNotif_Callback = OnMessageCallback;

                notificationClient = new WebSocketSharp.WebSocket(string.Format("{0}{1}:{2}/", @"wss://", api_base.Host, resp.q_obj.streamPort));


                notificationClient.OnMessage += SubToOrderNotift_OnMessage;

                notificationClient.Connect();

                notificationClient.Send(_auth.access_token);
                
            }
        }

        /// <summary>
        /// Sends a request to Questrade to push quote data to this Websocket client. Data is recieved through the OnMessageCallback.
        /// </summary>
        /// <param name="ids">Comma seperated symbol id</param>
        /// <param name="OnMessageCallback">Callback to pass message from this websocket client</param>
        /// <returns></returns>
        public async Task StreamQuote(string ids, Action<string, DateTime> OnMessageCallback)
        {
            var resp = await ApiGet<StreamPort>(apiClient,string.Format("v1/markets/quotes?ids={0}&stream=true&mode={1}", ids, streamType.WebSocket.ToString()));//Requests server to send notification to port

            if (resp.isSuccess)
            {
                //Setup Websocket to recieve data from
                var api_base = new Uri(_auth.api_server);
                StreamQuote_Callback = OnMessageCallback;

                quoteStreamClient = new WebSocketSharp.WebSocket(string.Format("{0}{1}:{2}/", @"wss://", api_base.Host, resp.q_obj.streamPort));

                quoteStreamClient.OnMessage += StreamQuote_OnMessage;

                quoteStreamClient.Connect();

                quoteStreamClient.Send(_auth.access_token);
            }
            
        }
        #endregion

    }
}
