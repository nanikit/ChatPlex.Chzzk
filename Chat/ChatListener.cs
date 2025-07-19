using CP_SDK.Chat.Interfaces;
using CP_SDK.Chat.SimpleJSON;
using IPA.Utilities;
using System;
using System.Threading.Tasks;

namespace ChatPlex.Chzzk.Chat
{

  public class ChatListener : IDisposable
  {
    public event Action<ChzzkChatMessage> OnMessage = delegate { };
    public event Action<IChatChannel> OnConnect = delegate { };
    public event Action<string> OnError = delegate { };
    public event Action<(string, string)> OnDonate = delegate { };

    private ChatConnection _connection = new();
    private DateTime _lastMessageTime = DateTime.MinValue;
    private bool _isFirstConnection = true;
    private bool _isDisposed = false;

    public ChatListener()
    {
      Reconnect();
    }

    public async Task Init()
    {
      int failureCount = 0;

      while (!_isDisposed)
      {
        try
        {
          await _connection.Connect().ConfigureAwait(false);
          _ = ReconnectIfIdle();

          await _connection.Listen().ConfigureAwait(false);
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

    public void Dispose()
    {
      _isDisposed = true;
      _connection.Dispose();
      _lastMessageTime = DateTime.MinValue;
      Plugin.Log?.Info($"{GetType().Name}: Dispose()");
    }

    private async Task ReconnectIfIdle()
    {
      _lastMessageTime = DateTime.MinValue;

      while (true)
      {
        await Task.Delay(30000).ConfigureAwait(false);
        if (_isDisposed)
        {
          return;
        }

        bool isIdle = DateTime.Now - _lastMessageTime > TimeSpan.FromMinutes(5);
        if (isIdle)
        {
          break;
        }
      }

      Plugin.Log?.Info($"{GetType().Name}: Reconnect()");
      Reconnect();
    }

    private async Task ForwardFirstConnection()
    {
      if (_isFirstConnection)
      {
        string? channelName = null;

        try
        {
          channelName = await new GetChannelInfo().GetChannelName().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
          Plugin.Log.Warn(exception);
        }

        OnConnect(new ChzzkChatChannel(channelName ?? "unknown"));
        _isFirstConnection = false;
      }
    }

    private void Reconnect()
    {
      _connection.Dispose();
      _connection = new ChatConnection();
      _connection.OnConnect += () => _ = ForwardFirstConnection();
      _connection.OnMessage += ParseChat;
    }

    private void ParseChat(string json)
    {
      JSONNode ReceiveObject = JSON.Parse(json);
      Plugin.Log.Info($"{GetType().Name}: ParseChat() {json}");

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
            OnMessage.Invoke(message);
          }
          // anonymous donate
          else if (IsAnonymous == true)
          {
            Plugin.Log.Info("Anonymous Donate");
            Plugin.Log.Info(json.ToString());
            // OnMessage?.Invoke(this, ChzzkChatMessage.FromRaw(chat.ToObject<Raw.GameBody>()));
          }
          // donate
          else if (IsAnonymous == false)
          {
            Plugin.Log.Info("Donate");
            Plugin.Log.Info(json.ToString());
            // OnMessage?.Invoke(this, ChzzkChatMessage.FromRaw(chat.ToObject<Raw.GameBody>()));
          }
        }
      }
    }
  }
}
