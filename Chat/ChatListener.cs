using CP_SDK.Chat.SimpleJSON;
using IPA.Utilities;
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
    private readonly Random rand = new Random();
    private readonly ClientWebSocket client = new();

    private CancellationTokenSource _connectionCancellationTokenSource = new();

    private CancellationToken ConnectionCancellationToken => _connectionCancellationTokenSource.Token;

    public ChatConnection()
    {
      int id = rand.Next(1, 11);
      uri = new Uri($"wss://kr-ss{id}.chat.naver.com/chat");
    }

    public async Task Connect()
    {
      Plugin.Log?.Info($"{GetType().Name}: Connect()");
      var liveChannel = await new GetChannelInfo().GetLiveChannel().ConfigureAwait(false);

      await client.ConnectAsync(uri, CancellationToken.None);
      Plugin.Log?.Info($"{GetType().Name}: Connect to {uri}");

      var connectObj = new JObject(
          new JProperty("ver", "3"),
          new JProperty("cmd", 100),
          new JProperty("svcid", "game"),
          new JProperty("cid", liveChannel.Id),
          new JProperty("bdy", new JObject(
              new JProperty("uid", null),
              new JProperty("devType", 2001),
              new JProperty("accTkn", liveChannel.AccessToken),
              new JProperty("auth", "READ")
              )
          ),
          new JProperty("tid", 1)
          );

      // first touch
      string jsonString = connectObj.ToString();
      ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonString));
      await client.SendAsync(bytesToSend, WebSocketMessageType.Text, true, ConnectionCancellationToken).ConfigureAwait(false);
    }

    public async Task Listen(Action<string> onMessage)
    {
      ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[16384]);

      if (client.State == WebSocketState.Open)
      {
        Plugin.Log?.Info("Socket Link Success");
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
            onMessage(serverMsg);
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
      ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg));
      await client.SendAsync(bytesToSend, WebSocketMessageType.Text, true, ConnectionCancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
      _connectionCancellationTokenSource.Cancel();
      _connectionCancellationTokenSource.Dispose();
      client.Dispose();
    }
  }

  public class ChatListener
  {
    public event EventHandler<ChzzkChatMessage> OnMessage = delegate { };
    public event EventHandler<string> OnError = delegate { };
    public event EventHandler<(string, string)> OnDonate = delegate { };

    private ChatConnection _connection = new();
    private DateTime _lastMessageTime = DateTime.MinValue;

    public async Task Init()
    {
      int failureCount = 0;

      while (true)
      {
        try
        {
          await _connection.Connect().ConfigureAwait(false);
          _ = ReconnectIfIdle();

          await _connection.Listen(ParseChat).ConfigureAwait(false);
          Plugin.Log?.Info($"{GetType().Name}: ");
        }
        catch (OperationCanceledException)
        {
          Plugin.Log?.Info($"{GetType().Name}: No chat received at least 30 seconds, try reconnecting...");
        }
        catch (Exception e)
        {
          Plugin.Log?.Error(e);
          failureCount++;
          if (failureCount > 3)
          {
            throw;
          }
          await Task.Delay(1000).ConfigureAwait(false);
        }
      }
    }

    private async Task ReconnectIfIdle()
    {
      _lastMessageTime = DateTime.MinValue;

      while (true)
      {
        await Task.Delay(30000).ConfigureAwait(false);
        bool isIdle = DateTime.Now - _lastMessageTime > TimeSpan.FromMinutes(5);
        if (isIdle)
        {
          break;
        }
      }

      Plugin.Log?.Info($"{GetType().Name}: Reconnect()");
      _connection.Dispose();
      _connection = new ChatConnection();
    }

    private void ParseChat(object ReceiveString)
    {
      JSONNode ReceiveObject = JSON.Parse((string)ReceiveString);

      if (ReceiveObject["bdy"].IsArray)
      {
        _lastMessageTime = DateTime.Now;
        foreach (var (key, chat) in ReceiveObject["bdy"].AsArray)
        {
          var IsAnonymous = JSON.Parse(chat["profile"])["extras"]["isAnonymous"];

          // common chat
          if (IsAnonymous == null)
          {
            ChzzkChatMessage message = new ChzzkChatMessage(chat);
            OnMessage?.Invoke(this, message);
          }
          // anonymous donate
          else if (IsAnonymous == true)
          {
            Plugin.Log.Info("Anonymous Donate");
            Plugin.Log.Info(ReceiveString.ToString());
            // OnMessage?.Invoke(this, ChzzkChatMessage.FromRaw(chat.ToObject<Raw.GameBody>()));
          }
          // donate
          else if (IsAnonymous == false)
          {
            Plugin.Log.Info("Donate");
            Plugin.Log.Info(ReceiveString.ToString());
            // OnMessage?.Invoke(this, ChzzkChatMessage.FromRaw(chat.ToObject<Raw.GameBody>()));
          }
        }
      }
    }
  }
}
