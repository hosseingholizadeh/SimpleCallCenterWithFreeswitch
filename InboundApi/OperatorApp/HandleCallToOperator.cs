using System;
using System.Threading;
using System.Threading.Tasks;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;
using NEventSocket.FreeSwitch;

namespace FreeswitchListenerServer.InboundApi.OperatorApp
{
    public class HandleCallToOperator : IDisposable
    {
        public async Task HandleCall(Channel channel, CancellationToken ct)
        {
            var tsc = new TaskCompletionSource<Task>();
            ct.Register((() =>
            {
                tsc.TrySetCanceled(ct);
            }));

            if (ct.IsCancellationRequested)
            {
                return;
            }

            var caller = Caller.GetCallerInfo(channel);
            var relatedOperatorList = ShiftOperatorHandler.GetRelatedShiftOpertors();
            var relatedOperatorCount = relatedOperatorList.Count;
            if (relatedOperatorCount == 0)
            {
                LogHelper.LogRed("there was no operator created for call center.");
                await channel.NoOneIsAvailable();
            }
            else
            {
                LogHelper.Log($"started trying to {relatedOperatorCount} operator.");
            }
            
            for (int index = 0; index < relatedOperatorList.Count; index++)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                var rOperator = relatedOperatorList[index];
                var voipNumber = rOperator.VoipNumber ?? 0;

                //we have to check if the user is busy or not
                //-----------------------------------------------------
                var userIsBusy = FreeswitchWorker.ExtensionIsBusy(voipNumber.ToString());
                if (!userIsBusy)
                {
                    //#############################################
                    //**************operator IS NOT BUSY**************
                    //#############################################

                    var fullNumber = "user/" + voipNumber;
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
                    if (!channel.IsAnswered && ct.IsCancellationRequested)
                    {
                        LogHelper.Log($"channel {channel.UUID} is pre-answered.");
                        await channel.PreAnswer();
                    }

                    await channel.BridgeTo(fullNumber, bridgeOptions, (e) =>
                    {
                        LogHelper.LogGreen("Bridge Progress Ringing...");
                    });

                    if (!channel.IsBridged)
                    {
                        channel.AddLogDetail(voipNumber.ToString(), "NotBridged");
                        LogHelper.Log($"cannot bridge to {fullNumber}");
                    }
                    else
                    {
                        //cancel all playing messages after answering
                        await channel.CancelMedia();
                        channel.HangupCallBack = (e) => { };

                        break;
                    }
                }

                //-----------------------------------------------------
                if (index == relatedOperatorCount - 1 && !channel.IsBridged && channel.IsAnswered)
                {
                    if(!channel.IsAnswered)
                        await channel.Answer();

                    await channel.NoOneIsAvailable();
                }
            }
        }


        public void Dispose()
        {

        }
    }
}
