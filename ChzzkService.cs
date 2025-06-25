using CP_SDK;
using CP_SDK.Chat;
using CP_SDK.Chat.Services;
using CP_SDK.Chat.Interfaces;
using UnityEngine;
using CP_SDK.Unity.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using ChatPlex.Chzzk.Chat;

namespace ChatPlex.Chzzk
{
  public class ChzzkService : ChatServiceBase, IChatService
  {
    public string DisplayName { get; } = "Chzzk";
    public Color AccentColor { get; } = ColorU.WithAlpha("#08FFA6", 0.75f);

    public ReadOnlyCollection<(IChatService, IChatChannel)> Channels => m_Channels.Select(x => (this as IChatService, x)).ToList().AsReadOnly();

    private List<IChatChannel> m_Channels = new List<IChatChannel>();
    private readonly ChatListener listener;

    private Task m_ListenerInitTask;

    public ChzzkService()
    {
      Plugin.Log?.Info($"{GetType().Name}: Awake()");

      listener = new ChatListener();
      listener.OnMessage += ChzzkSocket_OnMessageReceived;
    }

    public void Start()
    {
      m_ListenerInitTask = Task.Run(listener.Init);
    }

    public void Stop()
    {

    }

    public void RecacheEmotes()
    {

    }

    public string WebPageHTMLForm()
    {
      return "";
    }

    public string WebPageHTML()
    {
      return "";
    }

    public string WebPageJS()
    {
      return "";
    }

    public string WebPageJSValidate()
    {
      return "";
    }

    public void WebPageOnPost(Dictionary<string, string> data)
    {

    }

    public void JoinTempChannel(string channel, string user, string message, bool notify)
    {

    }

    public void LeaveTempChannel(string channel)
    {

    }

    public bool IsInTempChannel(string channel)
    {
      return false;
    }

    public void LeaveAllTempChannel(string channel)
    {

    }

    public string PrimaryChannelName()
    {
      return "";
    }

    public void SendTextMessage(IChatChannel channel, string message)
    {

    }

    public bool IsConnectedAndLive()
    {
      return true;
    }

    private void ChzzkSocket_OnMessageReceived(object sender, ChzzkChatMessage e)
    {
      try
      {
        Plugin.Log?.Debug($"{nameof(ChzzkSocket_OnMessageReceived)}(): {e}");
        m_OnTextMessageReceivedCallbacks.InvokeAll(this, e);

        if (!m_Channels.Contains(e.Channel))
        {
          m_Channels.Add(e.Channel);
        }
      }
      catch (Exception ex)
      {
        Plugin.Log.Error(ex.Message);
      }
    }
  }
}
