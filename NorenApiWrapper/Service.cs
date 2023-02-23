using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;          //Needs to be added

namespace NorenRestApiWrapper
{

    public class RESTClient
    {
        private HttpClient client = new HttpClient();
        private string _endPoint;
        private readonly ILogger<RESTClient> _logger;

        public OnResponse onSessionClose;
        public string endPoint
        {
            get => _endPoint;

            set
            {
                _endPoint = value;

                if (client.BaseAddress == null)
                    client.BaseAddress = new Uri(endPoint);
            }
        }


        //Default Constructor
        public RESTClient()
        {
            _logger = new LoggerFactory().CreateLogger<RESTClient>();

            client.DefaultRequestHeaders
                  .Accept
                  .Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header         
            client.DefaultRequestHeaders.ExpectContinue = false;
        }

        public async void makeRequest(BaseApiResponse response, string uri, string message, string key = null)
        {


            string strResponseValue = string.Empty;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);



            if (key != null)
                request.Content = new StringContent(message + "&" + key,
                                                Encoding.UTF8,
                                                "application/json");//CONTENT-TYPE header
            else
                request.Content = new StringContent(message,
                                                Encoding.UTF8,
                                                "application/json");//CONTENT-TYPE 
            _logger.LogTrace("Request:" + uri + " " + message);
            //Console.WriteLine("Request:" + uri + " " + message);

            await client.SendAsync(request)
                  .ContinueWith(async responseTask =>
                  {
                      string data = String.Empty;
                      _logger.LogTrace("Response: {0}", responseTask.Status);
                      //Console.WriteLine("Response: {0}", responseTask.Status);

                      if (responseTask.Exception?.InnerExceptions?.Count > 0)
                      {
                          _logger.LogError("Exception: {0}", responseTask.Exception.InnerException);
                          //Console.WriteLine("Exception: {0}", responseTask.Exception.InnerException);
                      }
                      if (responseTask.IsCompleted)
                      {
                          data = await responseTask.Result.Content.ReadAsStringAsync();

                          _logger.LogTrace("Response data: {0}", data);
                          //Console.WriteLine("Response data: {0}", data);

                          if (data == "{\"stat\":\"Not_Ok\",\"emsg\":\"Session Expired : Invalid Session Key\"}")
                          {
                              //Console.WriteLine("call logout");
                              LogoutResponse logout = new LogoutResponse();
                              logout.emsg = "Session Expired : Invalid Session Key";
                              logout.stat = "Not_Ok";

                              onSessionClose?.Invoke(logout, false);
                          }

                          else
                              response.OnMessageNotify(responseTask.Result, data);
                      }
                  });


            return;

        }
    }
}