using System.Collections.Generic;

namespace ChatPlex.Chzzk.Chat.Raw
{
  public class ChzzkRawMessage
  {
    public string svcid { get; set; }
    public string ver { get; set; }
    public List<GameBody> bdy { get; set; }
    public int cmd { get; set; }
    public string tid { get; set; }
    public string cid { get; set; }
  }

  public class GameBody
  {
    public string svcid { get; set; }
    public string cid { get; set; }
    public int mbrCnt { get; set; }
    public string uid { get; set; }
    public string profile { get; set; }
    public string msg { get; set; }
    public int msgTypeCode { get; set; }
    public string msgStatusType { get; set; }
    public string extras { get; set; }
    public long ctime { get; set; }
    public long utime { get; set; }
    public string msgTid { get; set; }
    public long msgTime { get; set; }
  }

  public class Profile
  {
    public string userIdHash { get; set; }
    public string nickname { get; set; }
    public string profileImageUrl { get; set; }
    public string userRoleCode { get; set; }
    public Badge badge { get; set; }
    public Title title { get; set; }
    public bool verifiedMark { get; set; }
    public List<ActivityBadge> activityBadges { get; set; }
    public StreamingProperty streamingProperty { get; set; }
    public List<ViewerBadge> viewerBadges { get; set; }
  }

  public class Badge
  {
    public string imageUrl { get; set; }
  }

  public class Title
  {
    public string name { get; set; }
    public string color { get; set; }
  }

  public class ActivityBadge
  {
    public string id { get; set; }
  }

  public class StreamingProperty
  {
    public string nicknameColor { get; set; }
    public List<string> activatedAchievementBadgeIds { get; set; }
  }

  public class ViewerBadge
  {
    public string id { get; set; }
  }
}
