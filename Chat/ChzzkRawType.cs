using System.Collections.Generic;

namespace ChatPlex.Chzzk.Chat
{
  public class ChzzkRawType
  {
    public string svcid { get; set; }
    public string ver { get; set; }
    public List<Chat> bdy { get; set; }
    public int cmd { get; set; }
    public string tid { get; set; }
    public string cid { get; set; }
  }

  public class Chat
  {
    public string svcid { get; set; }
    public string ver { get; set; }
    public List<Chat> bdy { get; set; }
    public int cmd { get; set; }
    public string tid { get; set; }
    public string cid { get; set; }
  }
}
