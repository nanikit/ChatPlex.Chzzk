using ChatPlex.Chzzk.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPlex.Chzzk.Chat
{
  public class ChatConnection : IDisposable
  {
    private readonly Uri uri;
    private readonly string pingMsg = "{\"ver\":\"2\",\"cmd\":0}";
    private readonly string pongMsg = "{\"ver\":\"2\",\"cmd\":10000}";
    private readonly ClientWebSocket client = new();

    private readonly CancellationTokenSource _connectionCancellationTokenSource = new();
    private ChatChannel? _channel;

    private CancellationToken ConnectionCancellationToken => _connectionCancellationTokenSource.Token;

    record ChatChannel(string Id, string AccessToken, string ExtraToken);

    public event Action OnConnect = delegate { };
    public event Action<string> OnMessage = delegate { };

    public ChatConnection()
    {
      int id = new Random().Next(1, 6);
      uri = new Uri($"wss://kr-ss{id}.chat.naver.com/chat");
    }

    public async Task Connect()
    {
      Plugin.Log?.Info($"{GetType().Name}: Connect()");
      _channel = await GetLiveChannel().ConfigureAwait(false);

      await client.ConnectAsync(uri, CancellationToken.None);
      Plugin.Log?.Info($"{GetType().Name}: Connect to {uri}");

      var connectObj = new JObject(
          new JProperty("ver", "3"),
          new JProperty("cmd", 100),
          new JProperty("svcid", "game"),
          new JProperty("cid", _channel.Id),
          new JProperty("bdy", new JObject(
              new JProperty("uid", null),
              new JProperty("devType", 2001),
              new JProperty("accTkn", _channel.AccessToken),
              new JProperty("auth", "READ")
              )
          ),
          new JProperty("tid", 1)
          );

      // first touch
      string jsonString = connectObj.ToString();
      ArraySegment<byte> bytesToSend = new(Encoding.UTF8.GetBytes(jsonString));
      await client.SendAsync(bytesToSend, WebSocketMessageType.Text, true, ConnectionCancellationToken).ConfigureAwait(false);
    }

    public async Task Listen()
    {
      ArraySegment<byte> bytesReceived = new(new byte[16384]);

      if (client.State == WebSocketState.Open)
      {
        Plugin.Log?.Info("Socket Link Success");
        OnConnect();
      }
      else
      {
        Plugin.Log?.Error("Socket Link Fail");
      }

      while (client.State == WebSocketState.Open)
      {
        WebSocketReceiveResult result = await client.ReceiveAsync(bytesReceived, ConnectionCancellationToken).ConfigureAwait(false);
        string serverMsg = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
        try
        {
          if (serverMsg == pingMsg)
          {
            await Send(pongMsg);
          }
          else
          {
            OnMessage(serverMsg);
          }
        }
        catch (Exception e)
        {
          Plugin.Log.Error(serverMsg);
          Plugin.Log.Error(e.Message);
        }
      }

      Plugin.Log?.Info($"{GetType().Name}: Listen() end");

      await client.CloseOutputAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None).ConfigureAwait(false);
      client.Dispose();
    }

    private async Task Send(string msg)
    {
      ArraySegment<byte> bytesToSend = new(Encoding.UTF8.GetBytes(msg));
      await client.SendAsync(bytesToSend, WebSocketMessageType.Text, true, ConnectionCancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
      _connectionCancellationTokenSource.Cancel();
      _connectionCancellationTokenSource.Dispose();
      client.Dispose();
    }

    private async Task<ChatChannel> GetLiveChannel(CancellationToken cancellationToken = default)
    {
      var client = new HttpApiClient();
      int retryCount = 0;

      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();

        int delaySeconds = Math.Min(10 + retryCount, 30);

        var status = await client.GetLiveStatus(PluginConfig.Instance.ChannelId).ConfigureAwait(false);
        switch (status)
        {
          case Live liveChannel:
            var (accessToken, extraToken) = await client.GetAccessToken(liveChannel.ChatChannelId).ConfigureAwait(false);
            return new ChatChannel(liveChannel.ChatChannelId, accessToken, extraToken);
          case NotFound:
            Plugin.Log?.Warn($"Channel ID({PluginConfig.Instance.ChannelId}) is not found. Please check your configuration.");
            retryCount++;
            break;
          case NotLive:
            Plugin.Log?.Info($"Channel is not live. Waiting for {delaySeconds} seconds...");
            retryCount++;
            break;
          case NotCreated:
            Plugin.Log?.Warn("Have you ever streamed on Chzzk? Please stream first.");
            retryCount++;
            break;
          case Limited:
            Plugin.Log?.Warn("Cannot get channel ID. Are you streaming for adult only?");
            retryCount++;
            break;
          default:
            retryCount = 0;
            break;
        }

        await Task.Delay(delaySeconds * 1000, cancellationToken).ConfigureAwait(false);
      }
    }
  }
}
