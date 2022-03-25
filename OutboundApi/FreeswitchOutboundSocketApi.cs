using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using NEventSocket;
using System;
using System.Configuration;
using System.Threading;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.OutboundApi
{
    internal class FreeswitchOutboundSocketApi
    {
        private static readonly int ListenerPort = int.Parse(ConfigurationManager.AppSettings["FreeswitchEventSocketPortOutbound"]);
        private static readonly OutboundListener Listener = new OutboundListener(ListenerPort);
        public static bool ListenerIsStarted;

        public static void Run()
        {
            try
            {
                if (!ListenerIsStarted)
                {
                    Listener.Channels.Subscribe(
                        async channel =>
                        {
                            try
                            {
                                var ctx = new CancellationTokenSource();
                                var uuid = channel.UUID;
                                LogHelper.LogBlue($"destination:{channel.GetDesNumber()} from channel {uuid}.");
                                var caller = Caller.GetCallerInfo(channel);
                                var callerNumber = caller.CallerNumber;

                                channel.HangupCallBack = async (e) =>
                                {
                                    //if the call in the queue is hang up we have to remove it from the queue
                                    ChannelListKeeper.RemoveChannel(uuid);
                                    ConnectedCallHandler.Remove(uuid);

                                    LogHelper.LogRed(
                                        $"channel {uuid} exit.[{caller.CallerNumber} -> {channel.GetDesNumber()}]");

                                    ctx.Cancel();
                                    await channel.Exit();
                                    channel.Dispose();
                                    LogHelper.LogMagenta($"channe {uuid} is disposed.");
                                    if (ErpContainerDataHelper.IsExtension(caller.CallerNumber))
                                    {
                                        BusyLine.Remove(callerNumber);
                                        await WaitingQueue.WorkOnQueueCalls(caller.CallerNumber);
                                    }
                                };

                                channel.BridgedChannels.Subscribe(bridgedChannel =>
                                {
                                    LogHelper.Log($"bridgedChannel answer state is ({bridgedChannel.IsAnswered})");
                                    ChannelListKeeper.AddBrChannel(uuid, bridgedChannel.UUID);
                                    var brCaller = bridgedChannel.Headers["variable_outbound_caller_id_name"].ToString();
                                    if (ErpContainerDataHelper.IsExtension(brCaller))
                                        BusyLine.Add(brCaller);

                                    bridgedChannel.HangupCallBack = async (e) =>
                                    {
                                        ChannelListKeeper.RemoveBrChannel(uuid, bridgedChannel.UUID);
                                        LogHelper.Log("bridged channel exit.");

                                        if (ErpContainerDataHelper.IsExtension(brCaller))
                                        {
                                            BusyLine.Remove(brCaller);
                                            await WaitingQueue.WorkOnQueueCalls(brCaller);
                                        }
                                    };
                                });

                                ChannelListKeeper.Add(channel);
                                if (ErpContainerDataHelper.IsExtension(callerNumber))
                                    BusyLine.Add(callerNumber);
                                CallToExtensionSoftphone.StartCalling(uuid, ctx.Token);
                            }
                            catch (Exception e)
                            {
                                LogHelper.WriteExceptionLog(e);
                            }
                        });

                    Listener.Start();
                    ListenerIsStarted = true;
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        public static void PrintAll(Channel c)
        {
            foreach (var cHeader in c.Headers)
            {
                Console.WriteLine("{0}:{1}", cHeader.Key, cHeader.Value);
            }
        }
    }
}
