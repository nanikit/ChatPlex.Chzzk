using ChatPlex.Chzzk.Configuration;
using IPA;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;
using IPA.Config;
using IPA.Config.Stores;

namespace ChatPlex.Chzzk
{
  [Plugin(RuntimeOptions.SingleStartInit)]
  public class Plugin
  {
    internal static Plugin Instance { get; private set; }
    internal static IPALogger Log { get; private set; }

    [Init]
    /// <summary>
    /// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
    /// [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
    /// Only use [Init] with one Constructor.
    /// </summary>
    public void Init(IPALogger logger, Config conf)
    {
      Instance = this;
      Log = logger;
      PluginConfig.Instance = conf.Generated<PluginConfig>();
      Log.Info("ChatPlex.Chzzk initialized.");
    }

    [OnStart]
    public void OnApplicationStart()
    {
      Log.Info("OnApplicationStart");
      new GameObject("ChatPlex.ChzzkController").AddComponent<ChzzkController>();

      var service = new ChzzkService();
      CP_SDK.Chat.Service.RegisterExternalService(service);
    }
  }
}

namespace System.Runtime.CompilerServices
{
  internal class IsExternalInit { }
}
