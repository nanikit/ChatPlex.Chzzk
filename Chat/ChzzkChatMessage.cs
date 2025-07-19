using System;
using CP_SDK.Animation;
using CP_SDK.Chat.SimpleJSON;
using CP_SDK.Chat.Interfaces;
using System.Collections.Generic;

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

    private static readonly Dictionary<string, ChzzkChatChannel> channels = [];

    public ChzzkChatChannel(string name)
    {
      Name = name;
      Id = name;
      IsTemp = false;
      Prefix = "";
      CanSendMessages = true;
      Live = true;
      ViewerCount = 0;
    }

    public static ChzzkChatChannel GetChannel(string name)
    {
      if (channels.ContainsKey(name))
      {
        return channels[name];
      }

      return new ChzzkChatChannel(name);
    }
  }

  public record ChzzkChatMessage : IChatMessage
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
    public IChatEmote[] Emotes { get; set; } = [];

    public ChzzkChatMessage(string id, string message, IChatUser sender, IChatChannel channel)
    {
      Id = id;
      Message = message;
      Sender = sender;
      Channel = channel;
    }

    public static ChzzkChatMessage FromRaw(JSONNode body)
    {
      string id = body["msgTime"].Value;
      string message = body["msg"].Value;

      IChatUser sender = new ChzzkChatUser(JSON.Parse(body["profile"]), (string)body["uid"]);
      IChatChannel channel = ChzzkChatChannel.GetChannel((string)body["cid"]);

      var emotes = new List<ChzzkChatEmote>();
      var emojis = JSON.Parse(body["extras"])?["emojis"];
      if (emojis != null)
      {
        foreach (var emoji in emojis.AsObject)
        {
          string name = $"{{:{emoji.Key}:}}";
          foreach (var index in FindAllIndexes(message, name))
          {
            var emote = new ChzzkChatEmote()
            {
              Id = $"chzzk-{emoji.Key}",
              Name = name,
              Uri = emoji.Value.Value,
              StartIndex = index,
              EndIndex = index + emoji.Key.Length + 3,
              Animation = EAnimationType.AUTODETECT,
            };
            emotes.Add(emote);
          }
        }
      }
      emotes.Reverse();

      return new ChzzkChatMessage(id, message, sender, channel)
      {
        Emotes = [.. emotes]
      };
    }

    private static IEnumerable<int> FindAllIndexes(string text, string searchString)
    {
      int index = 0;
      while ((index = text.IndexOf(searchString, index)) != -1)
      {
        yield return index;
        index += searchString.Length;
      }
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

    public ChzzkChatUser(JSONNode profile, string uid)
    {
      Id = uid;
      UserName = (string)profile["nickname"];
      DisplayName = UserName;
      PaintedName = UserName;
      Color = "#FFFFFF";
      IsBroadcaster = (string)profile["userRoleCode"] == "streamer";
      IsModerator = false;
      IsSubscriber = false;
      IsVip = false;
      Badges = new IChatBadge[] { };
    }
  }

  public class ChzzkChatBadge : IChatBadge
  {
    public EBadgeType Type { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }

    public ChzzkChatBadge(JSONNode badge)
    {
      Type = EBadgeType.Image;
      Id = (string)badge["imageUrl"];
      Name = (string)badge["imageUrl"];
      Content = (string)badge["imageUrl"];
    }
  }

  public record ChzzkChatEmote : IChatEmote
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Uri { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public EAnimationType Animation { get; set; }
  }
}
