using ChatPlex.Chzzk.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;

namespace ChatPlex.Chzzk.Chat
{
  class GetChannelInfo
  {
    readonly WebClient client = new WebClient();
    public string UId { get; set; } = "";
    public string ChatChannelId { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string ExtraToken { get; set; } = "";

    // Set Request Header
    public GetChannelInfo()
    {
      client.Headers.Clear();
      client.Headers.Add("Accept", "application/json");
      client.Encoding = Encoding.UTF8;

      SendRequest();
    }

    public void SendRequest()
    {
      // Get Channel Id from Settings
      string channelId = PluginConfig.Instance.ChannelId;

      try
      {
        string response = "";
        Plugin.Log?.Info($"{GetType().Name}: SendRequest()");

        // Get User Information
        // response = client.DownloadString("https://comm-api.game.naver.com/nng_main/v1/user/getUserStatus");
        // JObject myInfo = JObject.Parse(response);
        // UId = (string)myInfo["content"]["userIdHash"];
        // UId = UId ?? "";


        // Plugin.Log?.Info($"{GetType().Name}: SendRequest() - User Id: {UId}");

        // Get Channel Information
        response = client.DownloadString($"https://api.chzzk.naver.com/polling/v2/channels/{channelId}/live-status");
        JObject liveStatus = JObject.Parse(response);
        ChatChannelId = (string)liveStatus["content"]["chatChannelId"];

        Plugin.Log?.Info($"{GetType().Name}: SendRequest() - Chat Channel Id: {ChatChannelId}");

        // Get Access Token
        response = client.DownloadString($"https://comm-api.game.naver.com/nng_main/v1/chats/access-token?channelId={ChatChannelId}&chatType=STREAMING");
        JObject getAccessToken = JObject.Parse(response);
        AccessToken = (string)getAccessToken["content"]["accessToken"];
        ExtraToken = (string)getAccessToken["content"]["extraToken"];

        Plugin.Log?.Info($"{GetType().Name}: SendRequest() - Access Token: {AccessToken}");
        Plugin.Log?.Info($"{GetType().Name}: SendRequest() - Extra Token: {ExtraToken}");

        Plugin.Log?.Info("Channel Info Access Success");
      }
      catch (Exception ex)
      {
        Plugin.Log?.Error("Channel Info Access Fail!");
        Plugin.Log?.Error(ex.StackTrace);
      }
    }
  }
}