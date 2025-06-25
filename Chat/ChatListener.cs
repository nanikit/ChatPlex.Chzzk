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
  public class ChatListener
  {
    public event EventHandler<ChzzkChatMessage> OnMessage = delegate { };
    public event EventHandler<string> OnError = delegate { };
    public event EventHandler<(string, string)> OnDonate = delegate { };

    private readonly ClientWebSocket client = new ClientWebSocket();
    private readonly Uri uri;
    private readonly string pingMsg = "{\"ver\":\"2\",\"cmd\":0}";
    private readonly string pongMsg = "{\"ver\":\"2\",\"cmd\":10000}";
    private readonly Random rand = new Random();

    private CancellationTokenSource _connectionCancellationTokenSource = new();
    private DateTime _lastMessageTime = DateTime.MinValue;

    private CancellationToken ConnectionCancellationToken => _connectionCancellationTokenSource.Token;
    private int _failureCount = 0;

    public ChatListener()
    {
      int id = rand.Next(1, 11);
      uri = new Uri($"wss://kr-ss{id}.chat.naver.com/chat");
    }

    public async Task Init()
    {
      while (true)
      {
        try
        {
          await Connect().ConfigureAwait(false);
          Plugin.Log?.Info($"{GetType().Name}: ");
        }
        catch (OperationCanceledException)
        {
          Plugin.Log?.Info($"{GetType().Name}: OperationCanceledException");
        }
        catch (Exception e)
        {
          Plugin.Log?.Error(e.Message);
          _failureCount++;
          if (_failureCount > 3)
          {
            throw;
          }
          await Task.Delay(1000, ConnectionCancellationToken).ConfigureAwait(false);
        }
      }
    }

    private async Task Connect()
    {
      Plugin.Log?.Info($"{GetType().Name}: Init()");
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

      await Listen().ConfigureAwait(false);
      Plugin.Log?.Info($"{GetType().Name}: Init() success");
    }

    private async Task Listen()
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

      _ = ReconnectIfIdle();
      while (client.State == WebSocketState.Open)
      {
        WebSocketReceiveResult result = await client.ReceiveAsync(bytesReceived, ConnectionCancellationToken).ConfigureAwait(false);
        string serverMsg = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
        try
        {
          if (serverMsg == pingMsg) Send(pongMsg);
          else
          {
            ParseChat(serverMsg);
          }
        }
        catch (Exception e)
        {
          Plugin.Log.Error(serverMsg);
          Plugin.Log.Error(e.Message);

          OnError?.Invoke(this, e.Message);
        }
      }

      await client.CloseOutputAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None).ConfigureAwait(false);
    }

    private async Task ReconnectIfIdle()
    {
      _lastMessageTime = DateTime.MinValue;

      while (true)
      {
        await Task.Delay(10000, ConnectionCancellationToken).ConfigureAwait(false);
        bool isIdle = DateTime.Now - _lastMessageTime > TimeSpan.FromSeconds(30);
        if (isIdle)
        {
          break;
        }
      }

      Plugin.Log?.Info($"{GetType().Name}: Reconnect()");
      _connectionCancellationTokenSource.Cancel();
      _connectionCancellationTokenSource.Dispose();
      _connectionCancellationTokenSource = new();
    }

    private async void Send(string msg)
    {
      ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg));
      await client.SendAsync(bytesToSend, WebSocketMessageType.Text, true, ConnectionCancellationToken).ConfigureAwait(false);
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
