using CP_SDK.Animation;
using CP_SDK.Chat.Interfaces;

namespace ChatPlex.Chzzk.Chat
{
  public class ChzzkChatChannel : IChatChannel
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsTemp { get; set; }
    public string Prefix { get; set; }
    public bool CanSendMessages { get; set; }
    public bool Live { get; set; }
    public int ViewerCount { get; set; }

    public ChzzkChatChannel(string name)
    {
      Name = name;
      IsTemp = false;
      Prefix = "Chzzk";
      CanSendMessages = true;
      Live = true;
      ViewerCount = 0;
    }
  }

  public class ChzzkChatMessage : IChatMessage
  {
    public string Id { get; set; }
    public bool IsSystemMessage { get; set; }
    public bool IsActionMessage { get; set; }
    public bool IsHighlighted { get; set; }
    public bool IsGiganticEmote { get; set; }

    public bool IsPing { get; set; }
    public string Message { get; set; }
    public IChatUser Sender { get; set; }
    public IChatChannel Channel { get; set; }
    public IChatEmote[] Emotes { get; set; }

    public ChzzkChatMessage(string sender, string message)
    {
      Sender = new ChzzkChatUser(sender);
      Message = message;
      Channel = new ChzzkChatChannel(sender);
    }

    public ChzzkChatMessage(string sender, string message, IChatChannel channel)
    {
      Sender = new ChzzkChatUser(sender);
      Message = message;
      Channel = channel;
    }

  }

  public class ChzzkChatUser : IChatUser
  {
    public string Id { get; set; }

    public string UserName { get; set; }

    public string DisplayName { get; set; }

    public string PaintedName { get; set; }

    public string Color { get; set; }

    public bool IsBroadcaster { get; set; }

    public bool IsModerator { get; set; }

    public bool IsSubscriber { get; set; }

    public bool IsVip { get; set; }

    public IChatBadge[] Badges { get; set; }

    public ChzzkChatUser(string userName)
    {
      UserName = userName;
      DisplayName = userName;
      PaintedName = userName;
      Color = "#000000";
      IsBroadcaster = false;
      IsModerator = false;
      IsSubscriber = false;
      IsVip = false;
    }
  }

  public class ChzzkChatEmote : IChatEmote
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Uri { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public EAnimationType Animation { get; set; }
  }
}