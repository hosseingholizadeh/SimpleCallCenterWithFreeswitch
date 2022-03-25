using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeswitchListenerServer.FreeswitchApiClass;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.Class
{
    internal struct BrChannels
    {
        internal string ChannelId { get; set; }
        internal List<string> BrIdList { get; set; }
    }

    internal class ChannelListKeeper
    {
        internal static List<Channel> ChannelList = new List<Channel>();
        internal static List<BrChannels> BrChannelList = new List<BrChannels>();

        internal static void Add(Channel channel)
        {
            var c = GetChannel(channel.UUID);
            if (c == null)
            {
                ChannelList.Add(channel);
                LogHelper.LogBlue($"channel {channel.UUID} is added to list.");
            }
            else
            {
                LogHelper.LogDarkRed($"channel {channel.UUID} already exists.");
            }
        }

        internal static void AddBrChannel(string uuid, string brUuid)
        {
            var brChannelModel = BrChannelList.Cast<BrChannels?>().FirstOrDefault(c =>
                {
                    return c != null && c.Value.ChannelId == uuid;
                });

            var isAdded = true;
            if (brChannelModel == null)
            {
                isAdded = false;
                brChannelModel = new BrChannels()
                {
                    ChannelId = uuid
                };
            }

            var brChannelMVal = brChannelModel.Value;
            if (brChannelMVal.BrIdList == null)
                brChannelMVal.BrIdList = new List<string>();

            brChannelMVal.BrIdList.Add(brUuid);
            if (!isAdded)
                BrChannelList.Add(brChannelMVal);
        }

        internal static void RemoveChannel(string uuid)
        {
            var channel = ChannelList.FirstOrDefault(p => p.UUID == uuid);
            ChannelList.Remove(channel);
            LogHelper.Log($"channel {uuid} is removed from channel lists.");

        }

        internal static void RemoveBrChannel(string uuid, string brUuid)
        {
            BrChannelList.Where(p => p.ChannelId == uuid).Select(p => p.BrIdList).FirstOrDefault()?.Remove(brUuid);
        }

        internal static void PrintChannels()
        {
            if (ChannelList.Count == 0)
            {
                LogHelper.LogGreen("channels: count 0.");
            }
            else
            {
                LogHelper.LogGreen("channels:");
                Console.WriteLine("==========================================================");
                ChannelList.CustomeForEach((channel, index) =>
                {
                    var num = index + 1;
                    var caller = Caller.GetCallerInfo(channel);
                    Console.WriteLine($"{num})channel {channel.UUID} from {caller.CallerName} to {channel.GetDesNumber()}.");
                });
                Console.WriteLine("==========================================================");
            }
        }

        internal static void PrintChannelHeaders(Channel channel)
        {
            Console.WriteLine("==================================");
            foreach (var header in channel.Headers)
            {
                Console.WriteLine("{0}:{1}", header.Key, header.Value);
            }
            Console.WriteLine("==================================");
        }

        internal static void PrintChannelStates(Channel channel)
        {
            Console.WriteLine("==================================");
            Console.WriteLine("channel.IsAnswered:{0}", channel.IsAnswered);
            Console.WriteLine("channel.IsBridged:{0}", channel.IsBridged);
            Console.WriteLine("channel.IsPreAnswered:{0}", channel.IsPreAnswered);
            Console.WriteLine("channel.ChannelState:{0}", channel.ChannelState);
            Console.WriteLine("==================================");
        }

        internal static Channel GetChannel(string conUuid)
            => ChannelList.FirstOrDefault(p => p.UUID == conUuid);

        internal static int GetChannelIndex(Channel channel)
            => ChannelList.IndexOf(channel);

        internal static async Task<string> GetChannelIdByBrChannelId(string brUuid)
        {
            var calls = await FreeswitchApi.ShowCalls();
            var callList = FreeswitchHelper.GetRealTimeVars(calls);

            foreach (var call in callList)
            {
                if (call.Any(k => k.Value == brUuid || k.Value == "u:" + brUuid|| k.Value == "verto.rtc/" + brUuid))
                    return call.Where(p => p.Key == "uuid").Select(p => p.Value).FirstOrDefault();
            }

            return null;
            //return BrChannelList.Where(p => p.BrIdList.Any(br => br == brUuid)).Select(p => GetChannel(p.ChannelId))
            //    .FirstOrDefault();
        }

        internal static void Update(Channel channel)
        {
            var index = GetChannelIndex(channel);
            if (index != -1)
            {
                ChannelList[index] = channel;
            }
        }
    }
}
