using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using FreeswitchListenerServer.InboundApi.QueueApp;
using NEventSocket;
using NEventSocket.FreeSwitch;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.InboundApi
{
    internal class FreeswitchInboundSocketApi
    {
        private static readonly int ListenerPort = int.Parse(ConfigurationManager.AppSettings["FreeswitchEventSocketPort"]);
        private static readonly OutboundListener Listener = new OutboundListener(ListenerPort);
        internal static bool ListenerIsStarted = false;

        internal static void Run()
        {
            try
            {
                if (!ListenerIsStarted)
                {
                    Listener.Channels.Subscribe(
                        async channel =>
                        {
                            var ctx = new CancellationTokenSource();

                            //------------------Logs--------------------
                            channel.SaveCallLog_SQL();
                            channel.AddLog();
                            //------------------END Logs--------------------

                            var desNumber = channel.GetDesNumber();
                            LogHelper.LogConnectedChannel(channel);
                            LogHelper.Log("(Info)destination number:" + desNumber);


                            channel.Events.Where(p => p.EventName != EventName.ChannelHangupComplete).Subscribe(
                                async e =>
                                {
                                    LogHelper.Log($"eventName:{e.EventName}");
                                    if (e.EventName == EventName.ChannelAnswer ||
                                        e.EventName == EventName.ChannelBridge)
                                    {
                                        var channelVm = new FreeswitchChannelVm()
                                        {
                                            ChannelNumber = desNumber,
                                            ChannelState = (short?)e.ChannelState,
                                            AnswerState = (short?)e.AnswerState
                                        };

                                        //send data by web socket to ChaannleIndex View in ERP 
                                        //-------------------------------------------------------
                                        var channelStr = JsonConvert.SerializeObject(channelVm);
                                        await SignalrClient.SendMessage(
                                            FresswitchConstVariables.CallRealTimeData + "#" + channelStr);
                                        //-------------------------------------------------------
                                    }
                                });

                            channel.HangupCallBack = async (e) =>
                            {
                                await ChannelHangupEvent(channel, ctx);
                            };


                            channel.BridgedChannels.Subscribe(async bridgedChannel =>
                            {
                                await BridgedChannelConnectedEvent(channel, bridgedChannel);
                            });

                            await channel.Answer();
                            ChannelListKeeper.Add(channel);
                            HandleByAppType.StartCalling(channel.UUID, ctx.Token);
                        });

                    Listener.Start();
                    ListenerIsStarted = true;
                    LogHelper.LogListenerStarted();
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        private static async Task ChannelHangupEvent(Channel channel, CancellationTokenSource ctx)
        {
            var uuid = channel.UUID;
            ctx.Cancel();
            ChannelListKeeper.RemoveChannel(uuid);
            channel.SetEndToCallLog();
            channel.UpdateEndCall_SQL();

            var channelVm = new FreeswitchChannelVm()
            {
                ChannelNumber = channel.GetDesNumber(),
                ChannelState = null,
                AnswerState = null
            };

            //send data by web socket to ChannleIndex View in ERP 
            //-------------------------------------------------------
            var channelStr = JsonConvert.SerializeObject(channelVm);
            await SignalrClient.SendMessage(FresswitchConstVariables.CallRealTimeData + "#" + channelStr);
            //-------------------------------------------------------

            LogHelper.LogMagenta($"channel {uuid} exit.");
            QueueCallData.RemoveAll(uuid);

            ConnectedCallHandler.Remove(uuid);

            var caller = Caller.GetCallerInfo(channel);
            await channel.Exit();
            channel.Dispose();

            var isExt = ErpContainerDataHelper.IsExtension(caller.CallerNumber);
            if (isExt)
                await WaitingQueue.WorkOnQueueCalls(caller.CallerNumber);
        }

        private static async Task BridgedChannelConnectedEvent(Channel channel, BridgedChannel bridgedChannel)
        {
            //record all bridged call sounds in {FS_Sound_Directory}/rec/...
            var recordingPath = $"rec/{bridgedChannel.UUID}.wav";
            await channel.StartRecording(recordingPath);

            channel.MakeCallBridged();
            LogHelper.Log($"bridgedChannel answer state is ({bridgedChannel.IsAnswered})");
            ChannelListKeeper.AddBrChannel(channel.UUID, bridgedChannel.UUID);
            var brCaller = bridgedChannel.Headers["variable_outbound_caller_id_number"];
            if (ErpContainerDataHelper.IsExtension(brCaller))
                BusyLine.Add(brCaller);

            channel.UpdateCallLog(isBridged: true);
            channel.AddLogDetail(brCaller, "Bridged", bridgedChannel.UUID);
            SignalrClient.SendNewCallConnectedMessage(new RokhCallCenterMessageVm()
            {
                TelephoneNumber = Caller.GetCallerInfo(channel).CallerNumber,
                AgentNumber = brCaller
            });

            bridgedChannel.HangupCallBack = async (e) =>
            {
                await BrChannelHangupEvent(channel, bridgedChannel, brCaller);
            };
        }

        private static async Task BrChannelHangupEvent(Channel channel, BridgedChannel bridgedChannel, string brCaller)
        {
            await channel.StopRecording();
            ChannelListKeeper.RemoveBrChannel(channel.UUID, bridgedChannel.UUID);
            CallLog.SaveRecFileDataInDb(channel.UUID, bridgedChannel.UUID);
            await bridgedChannel.Exit();
            bridgedChannel.Dispose();

            if (ErpContainerDataHelper.IsExtension(brCaller))
            {
                BusyLine.Remove(brCaller);
                await WaitingQueue.WorkOnQueueCalls(brCaller);
            }
        }
    }
}
