using System;
using System.Threading.Tasks;
using FreeswitchListenerServer.Helper;

namespace FreeswitchListenerServer.Class
{
    internal class TransferVm
    {
        public string Cid { get; set; }
        public string Target { get; set; }
    }

    internal class Transfer
    {
        internal async Task Start(string brUuid, string target)
        {
            try
            {
                if (ErpContainerDataHelper.IsExtension(target))
                {
                    var uuid = await ChannelListKeeper.GetChannelIdByBrChannelId(brUuid);
                    var channel = ChannelListKeeper.GetChannel(uuid);
                    if (channel != null && channel.IsAnswered)
                    {
                        LogHelper.LogRed("channel found");
                        await channel.SetChannelVariable("hangup_after_bridge", "false");
                        await channel.Transfer(target);
                    }
                    else
                    {
                        if (channel != null)
                            Console.WriteLine($"channel.IsAnswered {channel.IsAnswered}");
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }

        }
    }
}
