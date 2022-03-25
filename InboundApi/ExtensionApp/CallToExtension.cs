using System;
using System.Threading;
using System.Threading.Tasks;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;
using NEventSocket.FreeSwitch;

namespace FreeswitchListenerServer.InboundApi.ExtensionApp
{
    public class CallToExtension : ErpContainerDataHelper
    {
        public async Task CallAppAgent(Channel channel, ComFreeswitchApp application, string voipNumber, CancellationToken ct)
        {
            var uuid = channel.UUID;
            var caller = Caller.GetCallerInfo(channel);

            //need voip number to back the first call in the queue to the exact voipNumber
            ConnectedCallHandler.Update(uuid, voipNumber);

            var isBusy = FreeswitchWorker.ExtensionIsBusy(voipNumber);
            if (isBusy)
            {
                LogHelper.Log($"Ext {voipNumber} is busy.");
                //agent is busy
                if (application.WaittingCount > 0)
                {
                    await WaitingQueue.Add(channel, application, voipNumber, ct);
                }
                else
                {
                    channel.CallOperators(ct);
                }
            }
            else
            {
                var bridgeOptions = new BridgeOptions()
                {
                    UUID = Guid.NewGuid().ToString(),
                    ContinueOnFail = true,
                    HangupAfterBridge = false,
                    TimeoutSeconds = BridgeConstVars.TimOutSec,
                    CallerIdName = caller.CallerName,
                    CallerIdNumber = caller.CallerNumber,
                    RingBack = BridgeConstVars.RingBackMusic
                };

                await new Bridge().Start(channel, voipNumber, bridgeOptions,async ()=>
                {
                    await channel.CancelMedia();
                },async () =>
                {
                    channel.AddLogDetail(voipNumber, "NotBridged");
                    if (!ct.IsCancellationRequested && channel.IsAnswered)
                    {
                        //داخلی مورد نظر در دسترس نمیباشد
                        await channel.PlayExtIsNotAvailable();
                        await HandleByAppType.FinishedQueueTimeoutHandle(application.ComAppPID, channel, ct);
                    }
                });
            }

        }

        public async Task CallAgent(string uuid, Guid agentId)
        {
            var channel = ChannelListKeeper.GetChannel(uuid);
            if (channel != null)
            {
                var caller = Caller.GetCallerInfo(channel);
                var voipNumber = Agent.GetAgentVoipNumber(agentId);

                //need voip number to back the first call in the queue to the exact voipNumber
                ConnectedCallHandler.Update(uuid, voipNumber);
                var isBusy = FreeswitchWorker.ExtensionIsBusy(voipNumber);
                if (!isBusy)
                {
                    var fullNumber = "user/" + voipNumber;
                    //bridge call to agent

                    var bridgeOptions = new BridgeOptions()
                    {
                        UUID = Guid.NewGuid().ToString(),
                        CallerIdNumber = caller.CallerName,
                        CallerIdName = caller.CallerNumber,
                        //this is for playing any voice if the bridge is failed
                        ContinueOnFail = true,
                        HangupAfterBridge = false,
                        TimeoutSeconds = BridgeConstVars.TimOutSec,
                        RingBack = BridgeConstVars.RingBackMusic
                    };

                    //bridge call to agent
                    await channel.BridgeTo(fullNumber, bridgeOptions, (e) =>
                    {
                        LogHelper.LogGreen("Bridge Progress Ringing...");
                    });

                    if (!channel.IsBridged)
                    {

                    }
                    else
                    {
                        //cancel all playing messages after answering
                        await channel.CancelMedia();

                        LogHelper.LogGreen(
                            $"connection {uuid} from {caller.CallerNumber} is connected to {channel.GetDesNumber()}.");
                    }
                }
            }
        }

        public async Task ToExtensionDir(string desNumber, Channel channel, CancellationToken ct)
        {
            //save the last cal of the agent for the redialing
            var caller = Caller.GetCallerInfo(channel);
            LogHelper.Log("(Info)caller number : " + caller.CallerNumber);

            //need voip number to back the first call in the queue to the exact voipNumber
            ConnectedCallHandler.Update(channel.UUID, desNumber);

            var isBusy = FreeswitchWorker.ExtensionIsBusy(desNumber);
            if (isBusy)
            {
                LogHelper.Log("des number is busy");
                channel.CallOperators(ct);
            }
            else
            {
                var fullName = "user/" + desNumber;
                var bridgeOptions = new BridgeOptions()
                {
                    UUID = Guid.NewGuid().ToString(),
                    ContinueOnFail = true,
                    HangupAfterBridge = false,
                    TimeoutSeconds = BridgeConstVars.TimOutSec,
                    CallerIdName = caller.CallerName,
                    CallerIdNumber = caller.CallerNumber,
                    RingBack = BridgeConstVars.RingBackMusic
                };

                LogHelper.Log($"bridgeOptions={bridgeOptions}");

                if (!channel.IsAnswered)
                {
                    LogHelper.Log($"channel {channel.UUID} is pre-answered.");
                    await channel.PreAnswer();
                }

                if (!ct.IsCancellationRequested)
                {
                    await channel.BridgeTo(fullName, bridgeOptions, (e) =>
                    {
                        LogHelper.LogGreen("Bridge Progress Ringing...");
                    });
                }

                if (!channel.IsBridged)
                {
                    channel.CallOperators(ct);
                }
                else if(!ct.IsCancellationRequested)
                {
                    await channel.CancelMedia();
                }
            }
        }
    }
}
