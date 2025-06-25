using ChatPlex.Chzzk.Configuration;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPlex.Chzzk.Chat
{
  record class ChatChannelAccess(string Id);
  record class DeadChannel(string Id) : ChatChannelAccess(Id);
  record class LiveChannel(string Id, string AccessToken, string ExtraToken) : ChatChannelAccess(Id);

  class GetChannelInfo
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

      Plugin.Log?.Debug($"{typeof(GetChannelInfo).Name}: SendRequest()");

      // Get Channel Information
      string channelResponse = await client.DownloadStringTaskAsync($"https://api.chzzk.naver.com/polling/v2/channels/{channelId}/live-status").ConfigureAwait(false);
      if (cancellationToken.IsCancellationRequested)
      {
        return null;
      }

      JObject liveStatus = JObject.Parse(channelResponse);
      string? chatChannelId = liveStatus["content"]?["chatChannelId"]?.Value<string>();
      if (chatChannelId == null)
      {
        Plugin.Log?.Error("Channel Info Access Fail! - Chat Channel Id is null");
        return null;
      }

      string? livePollingStatusJson = liveStatus["content"]?["livePollingStatusJson"]?.Value<string>();
      if (livePollingStatusJson == null)
      {
        Plugin.Log?.Warn("Channel Info Access Fail! - livePollingStatusJson is null");
      }
      else
      {
        JObject livePollingStatus = JObject.Parse(livePollingStatusJson);
        string? status = livePollingStatus["status"]?.Value<string>();
        if (status == "STOPPED")
        {
          return new DeadChannel(chatChannelId);
        }
      }

      Plugin.Log?.Info($"{typeof(GetChannelInfo).Name}: SendRequest() - Chat Channel Id: {chatChannelId}");

      // Get Access Token
      string tokenResponse = await client.DownloadStringTaskAsync($"https://comm-api.game.naver.com/nng_main/v1/chats/access-token?channelId={chatChannelId}&chatType=STREAMING").ConfigureAwait(false);
      JObject tokenJson = JObject.Parse(tokenResponse);
      var content = tokenJson["content"];
      string? accessToken = content?["accessToken"]?.Value<string>();
      string? extraToken = content?["extraToken"]?.Value<string>();

      Plugin.Log?.Info($"{typeof(GetChannelInfo).Name}: Access: {accessToken}, Extra: {extraToken}");
      if (accessToken == null || extraToken == null)
      {
        Plugin.Log?.Error("Channel Info Access Fail! - Access Token or Extra Token is null");
        return null;
      }

      return new LiveChannel(chatChannelId, accessToken, extraToken);
    }
  }
}
