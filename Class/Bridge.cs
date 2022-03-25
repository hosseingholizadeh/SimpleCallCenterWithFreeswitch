using System;
using System.Threading.Tasks;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;
using NEventSocket.FreeSwitch;

namespace FreeswitchListenerServer.Class
{
    internal class Bridge
    {
        internal async Task Start(Channel channel,string destination,BridgeOptions options,Action success,Action fail)
        {
            var fullNumber = "user/" + destination;
            LogHelper.Log($"bridgeOptions={options}");
            if (!channel.IsAnswered)
            {
                LogHelper.Log($"channel {channel.UUID} is pre-answered.");
                await channel.PreAnswer();
            }

            await channel.BridgeTo(fullNumber, options, (e) =>
            {
                LogHelper.LogGreen("Bridge Progress Ringing...");
            });

            if (!channel.IsBridged)
            {
                fail();
            }
            else
            {
                success();
            }
        }

    }
}
