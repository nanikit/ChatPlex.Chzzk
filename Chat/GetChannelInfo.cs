using ChatPlex.Chzzk.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

  record class ChatChannelAccess(string Id);
  record class DeadChannel(string Id) : ChatChannelAccess(Id);
  record class LiveChannel(string Id, string AccessToken, string ExtraToken) : ChatChannelAccess(Id);

  class GetChannelInfo2
  {
    public async Task<LiveChannel> GetLiveChannel(CancellationToken cancellationToken = default)
    {
      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var channel = await GetChannelAccess(cancellationToken).ConfigureAwait(false);
        if (channel is LiveChannel liveChannel)
        {
          return liveChannel;
        }

        Plugin.Log?.Info("Channel is not live. Waiting for 10 seconds...");
        await Task.Delay(10000, cancellationToken).ConfigureAwait(false);
      }
    }

    private async Task<ChatChannelAccess?> GetChannelAccess(CancellationToken cancellationToken = default)
    {
      var client = new WebClient();
      client.Headers.Clear();
      client.Headers.Add("Accept", "application/json");
      client.Encoding = Encoding.UTF8;

      // Get Channel Id from Settings
      string channelId = PluginConfig.Instance.ChannelId;

      Plugin.Log?.Debug($"{typeof(GetChannelInfo2).Name}: SendRequest()");

      // Get Channel Information
      string channelResponse = await client.DownloadStringTaskAsync($"https://api.chzzk.naver.com/polling/v2/channels/{channelId}/live-status").ConfigureAwait(false);
      if (cancellationToken.IsCancellationRequested)
      {
        return null;
      }

      JObject liveStatus = JObject.Parse(channelResponse);
      string? chatChannelId = liveStatus["content"]?["chatChannelId"]?.ToObject<string>();
      if (chatChannelId == null)
      {
        Plugin.Log?.Error("Channel Info Access Fail! - Chat Channel Id is null");
        return null;
      }

      string? livePollingStatusJson = liveStatus["content"]?["livePollingStatusJson"]?.ToObject<string>();
      if (livePollingStatusJson == null)
      {
        Plugin.Log?.Warn("Channel Info Access Fail! - livePollingStatusJson is null");
      }
      else
      {
        JObject livePollingStatus = JObject.Parse(livePollingStatusJson);
        string? status = livePollingStatus["status"]?.ToObject<string>();
        if (status == "STOPPED")
        {
          return new DeadChannel(chatChannelId);
        }
      }

      Plugin.Log?.Info($"{typeof(GetChannelInfo2).Name}: SendRequest() - Chat Channel Id: {chatChannelId}");

      // Get Access Token
      string tokenResponse = await client.DownloadStringTaskAsync($"https://comm-api.game.naver.com/nng_main/v1/chats/access-token?channelId={chatChannelId}&chatType=STREAMING").ConfigureAwait(false);
      JObject tokenJson = JObject.Parse(tokenResponse);
      var content = tokenJson["content"];
      string? accessToken = content?["accessToken"]?.ToObject<string>();
      string? extraToken = content?["extraToken"]?.ToObject<string>();

      Plugin.Log?.Info($"{typeof(GetChannelInfo2).Name}: Access: {accessToken}, Extra: {extraToken}");
      if (accessToken == null || extraToken == null)
      {
        Plugin.Log?.Error("Channel Info Access Fail! - Access Token or Extra Token is null");
        return null;
      }

      return new LiveChannel(chatChannelId, accessToken, extraToken);
    }
  }
}
