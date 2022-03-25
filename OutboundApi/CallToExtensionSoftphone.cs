using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using EtraabERP.Database.Definations;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using FreeswitchListenerServer.OutboundApi.NumberingPlan;
using NEventSocket;
using NEventSocket.Channels;
using NEventSocket.FreeSwitch;

namespace FreeswitchListenerServer.OutboundApi
{
    internal class CallToExtensionSoftphone : ErpContainerDataHelper
    {
        public static async void StartCalling(string uuid, CancellationToken ct)
        {
            try
            {
                var channel = ChannelListKeeper.GetChannel(uuid);
                if (channel != null)
                {
                    var desNumber = channel.GetDesNumber();
                    if (IsExtension(desNumber))
                    {
                        LogHelper.Log("started call to extension.");
                        await CallToExtension(desNumber, channel, ct);
                    }
                    else
                    {
                        var numberingPlan = NumebringPlanList.FirstOrDefault(n => desNumber.StartsWith(n.PlanNo));
                        if (numberingPlan != null)
                        {
                            LogHelper.Log("handling numbering plan.");
                            if (!channel.IsAnswered)
                                await channel.Answer();

                            await NumberingPlanHandler.HandleByNumberingPlan(channel, numberingPlan, ct);
                        }
                        else
                        {
                            LogHelper.Log("there is no numbering plan set to this number.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        public static async Task CallToExtension(string desNumber, Channel channel, CancellationToken ct)
        {
            //save the last cal of the agent for the redialing
            var caller = Caller.GetCallerInfo(channel);
            LogHelper.Log("(Info)caller number : " + caller.CallerNumber);

            //چک کردن دسترسی داخلی ها به همدیگر
            //check limits in the ext setting
            if (CanCallExt(caller.CallerNumber, desNumber))
            {
                LogHelper.Log("caller access is granted to the desnumber.");
                //check if it is an agent or not 
                if (VoipNumberList.Contains(caller.CallerNumber))
                    Agent.AddOrUpdateLastCall(caller.CallerNumber, desNumber);

                LogHelper.Log("started to handle by user call setting.");
                await UserCallSettingHandler.HandleByCallSetting(channel, desNumber, ct);

                var voiceMail = VoiceMailPlayer.GetVoiceMail(desNumber);

                var isBusy = FreeswitchWorker.ExtensionIsBusy(desNumber);
                if (isBusy)
                {
                    LogHelper.Log("des number is busy.");
                    if (voiceMail != null && voiceMail.IsEnabled &&
                        voiceMail.ExecuteTypeId == (short)EnVoiceMailExcuteType.WhenHasCaller)
                    {
                        LogHelper.Log("started to record voicemail.");
                        await channel.Answer();
                        await channel.PlayVoiceMail(desNumber, voiceMail);
                    }
                    else
                    {
                        await channel.PlayExtIsNotAvailable();
                        await channel.Hangup();
                    }
                }
                else
                {
                    var bridgeOptions = new BridgeOptions()
                    {
                        UUID = Guid.NewGuid().ToString(),
                        //IgnoreEarlyMedia = true,
                        ContinueOnFail = true,
                        HangupAfterBridge = false,
                        TimeoutSeconds = 20,
                        CallerIdName = caller.CallerName,
                        CallerIdNumber = caller.CallerNumber
                    };

                    await new Bridge().Start(channel, desNumber, bridgeOptions, async () =>
                    {
                        await channel.CancelMedia();
                        LogHelper.Log("all medias are canceled.");

                        // for just brideged channel
                        channel.Socket.Events.Where(x => x.Headers[ChannelVar.UniqueID] == bridgeOptions.UUID && x.EventName == EventName.Dtmf)
                            .Subscribe(
                                async e =>
                                {
                                    var digits = e.Headers[HeaderNames.DtmfDigit];

                                    var numberingPlan =
                                        NumebringPlanList.FirstOrDefault(n => desNumber.StartsWith(digits));
                                    if (numberingPlan != null)
                                    {
                                        LogHelper.LogGreen("started numbering plan handling.");
                                        await NumberingPlanHandler.HandleByNumberingPlan(channel,
                                            numberingPlan, ct);
                                    }
                                });

                        //if (IsExtension(caller.CallerNumber))
                        //{
                        //    // for main channel = caller
                        //    channel.Socket.Events.Where(x => x.Headers[ChannelVar.CallerUniqueID] == channel.UUID && x.EventName == EventName.Dtmf)
                        //        .Subscribe(
                        //            async e =>
                        //            {
                        //                var digits = e.Headers[HeaderNames.DtmfDigit];
                        //                var numberingPlan = NumebringPlanList.FirstOrDefault(n => desNumber.StartsWith(digits));
                        //                if (numberingPlan != null)
                        //                {
                        //                    LogHelper.LogGreen("startrted numbering plan handling.");
                        //                    await NumberingPlanHandler.HandleByNumberingPlan(channel, numberingPlan);
                        //                }

                        //            });
                        //}
                    }, async () =>
                    {
                        if (channel.OtherLeg?.HangupCause == HangupCause.NoUserResponse)
                        {
                            if (voiceMail != null && voiceMail.IsEnabled && voiceMail.ExecuteTypeId ==
                                (short)EnVoiceMailExcuteType.AfterTimeoutNoAnswer)
                            {
                                LogHelper.Log("started to record voicemail");
                                await channel.PlayVoiceMail(desNumber, voiceMail);
                            }
                            else
                            {
                                await channel.Hangup();
                            }
                        }
                        else if (channel.OtherLeg?.HangupCause == HangupCause.UserNotRegistered)
                        {
                            LogHelper.Log("des number is not registered in the FreeSWITCH");
                            if (voiceMail != null && voiceMail.IsEnabled &&
                                voiceMail.ExecuteTypeId == (short)EnVoiceMailExcuteType.WhenLogedOut)
                            {
                                LogHelper.Log("started to record voicemail");
                                await channel.Answer();
                                await channel.PlayVoiceMail(desNumber, voiceMail);
                            }
                            else
                            {
                                await channel.Hangup();
                            }
                        }
                        else
                        {
                            await channel.Hangup();
                        }
                    });
                }
            }
            else
            {
                LogHelper.Log("caller access is denied to the desnumber");
            }
        }

        private static bool HasPermissionAccess(string callerNumber, string desNumber)
            => VoipCallAccessList?.Any(p => p.MainVoipNumber == callerNumber && p.AccessVoipNumber == desNumber) ?? true;

        /// <summary>
        /// Check settings in the Extension setitngs
        /// </summary>
        /// <param name="callerNumber"></param>
        /// <param name="desNumber"></param>
        /// <returns></returns>
        private static bool CanCallExt(string callerNumber, string desNumber)
        {
            if (BlackList.Any(p => p.VoipNumber != null && (p.BlockecNumber == callerNumber && p.VoipNumber.ToString() == desNumber)))
                return false;

            var setting = GetExactSettingOfVoipNumber(callerNumber);
            var limit = setting?.Limit;
            if (limit != null && limit.LimitDetailList.Any(p => p.LineTypeId == (short)EnLineType.Extension && desNumber.StartsWith(p.Number)))
            {  //سلب محدودیت
                if (limit.LimitTypeId == (short)EnExtensionNumberingLimitType.GetLimitation)
                {
                    if (HasPermissionAccess(callerNumber, desNumber))
                        return true;
                    return false;
                }
                //ایجاد محدودیت
                else if (limit.LimitTypeId == (short)EnExtensionNumberingLimitType.SetLimitation)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
