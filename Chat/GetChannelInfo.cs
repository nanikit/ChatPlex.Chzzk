using ChatPlex.Chzzk.Configuration;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPlex.Chzzk.Chat
{
  record class ChatChannelAccess();
  record class NotFoundChannel() : ChatChannelAccess();
  record class NotCreatedChannel() : ChatChannelAccess();
  record class LimitedChannel() : ChatChannelAccess();
  record class DeadChannel() : ChatChannelAccess();
  record class LiveChannel(string Id, string AccessToken, string ExtraToken) : ChatChannelAccess();

  class GetChannelInfo
  {
    public async Task<LiveChannel> GetLiveChannel(CancellationToken cancellationToken = default)
    {
      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var channel = await GetChannelAccess(cancellationToken).ConfigureAwait(false);
        switch (channel)
        {
          case LiveChannel liveChannel:
            return liveChannel;
          case NotFoundChannel:
            Plugin.Log?.Warn($"Channel ID({PluginConfig.Instance.ChannelId}) is not found. Please check your configuration.");
            break;
          case DeadChannel:
            Plugin.Log?.Info("Channel is not live. Waiting for 10 seconds...");
            break;
          case NotCreatedChannel:
            Plugin.Log?.Warn("Have you ever streamed on Chzzk? Please stream first.");
            break;
          case LimitedChannel:
            Plugin.Log?.Warn("Cannot get channel ID. Are you streaming for adult only?");
            retryCount++;
            break;
          default:
            break;
        }

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
      string channelResponse;
      try
      {
        channelResponse = await client.DownloadStringTaskAsync($"https://api.chzzk.naver.com/polling/v3.1/channels/{channelId}/live-status").ConfigureAwait(false);
      }
      catch (WebException exception)
      {
        if (exception.Message.Contains("404"))
        {
          return new NotFoundChannel();
        }
        else
        {
          throw;
        }
      }

      cancellationToken.ThrowIfCancellationRequested();

      JObject liveStatus = JObject.Parse(channelResponse);

      var content = liveStatus["content"];
      if (content == null)
      {
        return new NotCreatedChannel();
      }

      string? livePollingStatusJson = content["livePollingStatusJson"]?.Value<string>();
      if (livePollingStatusJson == null)
      {
        Plugin.Log?.Warn("Channel Info Access Fail! - livePollingStatusJson is null");
      }
      else
      {
        JObject livePollingStatus = JObject.Parse(livePollingStatusJson);
        string? status = livePollingStatus["status"]?.Value<string>();
        if (status != "STARTED")
        {
          return new DeadChannel();
        }
      }

      string? chatChannelId = content["chatChannelId"]?.Value<string>();
      if (chatChannelId == null)
      {
        return new LimitedChannel();
      }

      Plugin.Log?.Info($"{typeof(GetChannelInfo).Name}: SendRequest() - Chat Channel Id: {chatChannelId}");

      // Get Access Token
      string tokenResponse = await client.DownloadStringTaskAsync($"https://comm-api.game.naver.com/nng_main/v1/chats/access-token?channelId={chatChannelId}&chatType=STREAMING").ConfigureAwait(false);
      JObject tokenJson = JObject.Parse(tokenResponse);
      var tokenContent = tokenJson["content"];
      string? accessToken = tokenContent?["accessToken"]?.Value<string>();
      string? extraToken = tokenContent?["extraToken"]?.Value<string>();

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
