using System;
using System.Threading;
using System.Threading.Tasks;
using EtraabERP.Database.Definations;
using FreeswitchListenerServer.InboundApi.ExtensionApp;
using FreeswitchListenerServer.InboundApi.IvrApp;
using FreeswitchListenerServer.InboundApi.QueueApp;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.InboundApi
{
    public class HandleByAppType : ErpContainerDataHelper
    {
        public static async void StartCalling(string uuid,CancellationToken ct)
        {
            var tsc = new TaskCompletionSource<Task>();
            ct.Register((() =>
            {
                tsc.TrySetCanceled(ct);
            }));

            var channel = ChannelListKeeper.GetChannel(uuid);
            if (channel != null)
            {
                var desNumber = channel.GetDesNumber();
                if (IsExtension(desNumber))
                {
                    await new CallToExtension().ToExtensionDir(desNumber, channel, ct);
                }
                else
                {
                    //دریافت تمام اطلاعات مربوط به شیفت ها و برنامه های کانالی که با آن تماس گرفته شده است. 
                    var app = GetSpecificShiftAppForChannel(desNumber);
                    if (app != null)
                    {
                        ManageByAppType(app.AppId, uuid,ct);
                    }
                    else
                    {
                        LogHelper.LogRed("There is no application for this channel.");
                        channel.CallOperators(ct);
                    }
                }
            }
        }

        public static async void ManageByAppType(Guid appId, string uuid, CancellationToken ct)
        {
            var channel = ChannelListKeeper.GetChannel(uuid);
            if (channel != null)
            {
                //main application of this shiftApp for get more details of the application
                var application = GetAppById(appId);
                if (application != null)
                {
                    ConnectedCallHandler.Add(appId, uuid);
                    var appTypeId = application.AppTypeId;
                    LogHelper.Log($"{(EnFreeswitchAppType)appTypeId} application is started.");
                    if (appTypeId == (short)EnFreeswitchAppType.Queue)
                    {
                        using (CallHandler callHandler = new HandleCallToQueueApp())
                        {
                            await callHandler.HandleCall(channel, application, ct);
                        }
                    }
                    else if (appTypeId == (short)EnFreeswitchAppType.Extension)
                    {
                        using (CallHandler callHandler = new HandleCallToExtensionApp())
                        {
                            await callHandler.HandleCall(channel, application, ct);
                        }
                    }
                    else if (appTypeId == (short)EnFreeswitchAppType.IVR)
                    {
                        using (CallHandler callHandler = new HandleCallToIvrApp())
                        {
                            await callHandler.HandleCall(channel, application, ct);
                        }
                    }
                    else if (appTypeId == (short)EnFreeswitchAppType.Operator)
                    {
                        channel.CallOperators(ct);
                    }
                }

            }

        }

        public static async Task FinishedQueueTimeoutHandle(Guid appId, Channel channel, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }
            //main application of this shiftApp for get more details of the application
            var application = GetAppById(appId);
            if (application != null)
            {
                var noAnswerAppId = application.WaitingTimeFinishedNextAppId;
                if (noAnswerAppId != null && noAnswerAppId.HasGuidValue())
                {
                    ManageByAppType(noAnswerAppId.Value, channel.UUID,ct);
                }
                else
                {
                    var finalNoAnswerFileName = application.FinalNoAnswerFileName;
                    await SoundPlayerHelper.SimplePlayFile(channel, finalNoAnswerFileName);
                    channel.CallOperators(ct);
                }
            }
            else
            {
                channel.CallOperators(ct);
            }
        }
    }
}
