using CP_SDK;
using CP_SDK.Chat.Services;
using ChatPlexSDK_BS;
using CP_SDK.Chat.Interfaces;
using UnityEngine;
using CP_SDK.Unity.Extensions;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace ChatPlex.Chzzk
{
    public class ChzzkService : ChatServiceBase, IChatService
    {
        public string DisplayName { get; } = "Chzzk";
        public Color AccentColor { get; } = ColorU.WithAlpha("#08FFA6", 0.75f);

        public ReadOnlyCollection<(IChatService, IChatChannel)> Channels => m_Channels.Select(x => (this as IChatService, x)).ToList().AsReadOnly();

        private List<IChatChannel> m_Channels = new List<IChatChannel>();

        public ChzzkService()
        {
        }

        public void Start()
        {

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
            return false;
        }
    }
}