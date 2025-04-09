using CP_SDK;
using CP_SDK.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPlex.Chzzk
{
    [Serializable]
    public class SessionResponse
    {
        [JsonProperty] public string url;
    }

    class ChzzkSession
    {
        private string url = null;

        private string token = null;

        private WebClientUnity m_WebClient = new WebClientUnity("https://openapi.chzzk.naver.com", TimeSpan.FromSeconds(10), true);
        private WebClientCore m_WebClientCore = new WebClientCore("https://openapi.chzzk.naver.com", TimeSpan.FromSeconds(10), true);

        static public ChzzkSession Create()
        {
            ChzzkSession session = new ChzzkSession();
            session.RequestSession();

            return session;
        }

        private void RequestSession()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

            m_WebClient.GetAsync("/open/v1/sessions/auth/client", cancellationTokenSource.Token, response =>
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    SessionResponse sessionResponse = JsonConvert.DeserializeObject<SessionResponse>(response.BodyString);
                    url = sessionResponse.url;

                    Connect();
                }
            });
        }

        private void Connect()
        {   
            // m_SocketIOClient = new SocketIOClient.Client(url);
            // m_SocketIOClient.Connect();

            // m_SocketIOClient.On("connect", (response) =>
            // {
            //     Plugin.Log?.Info("Connected to Chzzk");
            // });

            // m_SocketIOClient.On("SYSTEM", (response) =>
            // {
            //     Plugin.Log?.Info("SYSTEM: " + response);
            // });
        }

        private ChzzkSession()
        {
            m_WebClient.SetHeader("Client-Id", "f0716dbc-87c3-4900-8999-ceb182c5ef80");
            m_WebClient.SetHeader("Client-Secret", "MzmAVIkoZN-KHK_QNpZdWnDX5mQzvjrft5LUxNnuO1Y");
            m_WebClient.SetHeader("Content-Type", "application/json");
        }
    }
}
