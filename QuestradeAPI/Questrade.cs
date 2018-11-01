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

    public class Questrade
    {
        static HttpClient authClient;
        static HttpClient apiClient;
        public static WebSocketSharp.WebSocket notificationClient;
        public static WebSocketSharp.WebSocket quoteStreamClient;
        private AuthenticateResp _auth;
        

        public Questrade(string token)
        {
            _auth = new AuthenticateResp();
            _auth.refresh_token = token;
            authClient = new HttpClient();
        }

        public async Task<HttpResponseMessage> Authenticate(Action<string> preAuthenticateCallback, Action<DateTime> accessTokenExpiryCallback)
        {
            HttpResponseMessage resp = null;

            preAuthenticateCallback("Authenticating...");

            resp = await authClient.GetAsync(string.Format("https://login.questrade.com/oauth2/token?grant_type=refresh_token&refresh_token={0}", _auth.refresh_token));

            if (resp.IsSuccessStatusCode)
            {
                var dateTimeNow = DateTime.Now;
                _auth = JsonConvert.DeserializeObject<AuthenticateResp>(resp.Content.ReadAsStringAsync().Result);
                _auth.expires_in_date = dateTimeNow.AddSeconds(_auth.expires_in);
                apiClient = new HttpClient();
                apiClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", string.Format("{0} {1}", _auth.token_type, _auth.access_token));
                apiClient.BaseAddress = new Uri(_auth.api_server);
                accessTokenExpiryCallback(_auth.expires_in_date);

                return resp;
            }
            else
            {
                return resp;
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
