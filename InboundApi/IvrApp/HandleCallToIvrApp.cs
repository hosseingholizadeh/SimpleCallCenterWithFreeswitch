using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using FreeswitchListenerServer.InboundApi.ExtensionApp;
using NEventSocket.Channels;
using NEventSocket.FreeSwitch;

namespace FreeswitchListenerServer.InboundApi.IvrApp
{
    public class HandleCallToIvrApp : CallHandler
    {
        public override async Task HandleCall(Channel channel, ComFreeswitchApp application, CancellationToken ct)
        {
            var tsc = new TaskCompletionSource<Task>();
            ct.Register(() =>
            {
                tsc.TrySetCanceled(ct);
            });

            if (ct.IsCancellationRequested)
            {
                return;
            }

            var appId = application.ComAppPID;
            var appIvrDetail = GetIvrAppDetailById(appId);
            var appListForIvr = GetAppListForIvrById(appId);

            if (appIvrDetail != null)
            {
                var desNumber = channel.GetDesNumber();
                LogHelper.LogGreen($"IVR app is started on channel {desNumber}.");
                var enteredDigits = new List<string>();
                var playGetDigitsResult = await channel.PlayGetDigits(
                    new PlayGetDigitsOptions()
                    {
                        MinDigits = appIvrDetail.MinDigits,
                        //to get extensions number too
                        MaxDigits = appIvrDetail.MaxDigits,
                        MaxTries = appIvrDetail.MaxTries,
                        TimeoutMs = appIvrDetail.TimeoutForResponse * 1000,
                        DigitTimeoutMs = appIvrDetail.DigitTimeout * 1000,
                        ValidDigits = appIvrDetail.ValidDigits,
                        PromptAudioFile = appIvrDetail.PromptAudioFileFullName,
                        BadInputAudioFile = appIvrDetail.BadInputAudioFileFullName
                    });

                if (playGetDigitsResult.Success)
                {
                    enteredDigits.Add(playGetDigitsResult.Digits);
                    channel.Events.Where(x => x.EventName == EventName.Dtmf)
                        .Subscribe(
                            e =>
                            {
                                var digits = e.Headers[HeaderNames.DtmfDigit];
                                enteredDigits.Add(digits);
                            });

                    //TODO:we can set this in ivr app for if want to call to extensions directly or not
                    //تایمر رو برای گرفتن شماره داخلی ست کرده ام
                    WaitingQueueTimer.SetTimer(4, async () =>
                    {
                        var finalDigits = StringHelper.AppendStrings(enteredDigits);
                        channel.AddLogIvr(appId,finalDigits);
                        LogHelper.LogMagenta($"finalDigits:{finalDigits}.");
                        if (!string.IsNullOrWhiteSpace(finalDigits))
                        {
                            var ivrApp = appListForIvr.FirstOrDefault(p => p.IvrNumber == finalDigits);
                            if (ivrApp != null)
                            {
                                var subAppId = ivrApp.ComAppId;
                                HandleByAppType.ManageByAppType(subAppId, channel.UUID, ct);
                            }
                            else
                            {
                                if (IsExtension(finalDigits) && StringHelper.IsDigitsOnly(finalDigits))
                                {
                                    var voipNumber = long.Parse(finalDigits);
                                    var app = GetAppByVoipNumber(voipNumber);

                                    //برای اینکه بتونیم از قابلیت صف برای ایجنت استفاده کنیم
                                    //باید برای او یک برنامه به داخلی خاص ایجاد نماییم.
                                    if (app?.WaittingCount > 0)
                                    {
                                        await new CallToExtension().CallAppAgent(channel, app, finalDigits, ct);
                                    }
                                    else
                                    {
                                        await new CallToExtension().ToExtensionDir(finalDigits, channel, ct);
                                    }
                                }
                                else
                                {
                                    LogHelper.LogRed("for this digit there is no ivr.");
                                    await HandleCall(channel, application, ct);
                                }
                            }
                        }
                        else
                        {
                            if (!ct.IsCancellationRequested)
                            {
                                LogHelper.LogRed("no digits entered.");

                                //call must go to operators
                                channel.CallOperators(ct);
                            }
                        }
                    });

                }
                else
                {
                    channel.AddLogIvr(appId,"",false);
                    if (!ct.IsCancellationRequested)
                    {
                        LogHelper.LogRed("IVR not successfull.");

                        //call must go to operators
                        channel.CallOperators(ct);
                    }
                }
            }
            else
            {
                LogHelper.LogRed("Ivr app is not set.");
            }

        }

    }
}
