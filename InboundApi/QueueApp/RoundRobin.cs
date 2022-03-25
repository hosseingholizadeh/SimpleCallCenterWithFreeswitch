using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;
using NEventSocket.FreeSwitch;

namespace FreeswitchListenerServer.InboundApi.QueueApp
{
    /// <summary>
    /// اعمال تماس بر حسب اولیت(آخرین منشی در حافظه)
    /// منظور آخرین کسی است که جواب داده است نه آخرین کسی که تماسش قطع شده باشد.
    /// </summary>
    internal class RoundRobin: AppQueueCall
    {
        /// <summary>
        /// اعمال تماس بر حسب اولیت(آخرین منشی در حافظه) 
        /// </summary>
        public override Task ManageQueueCall(
            Channel channel,
            ComQueue queue,
            List<vwComQueueAgent> queueAgentList)
        {
            var uuid = channel.UUID;
            var queueAgentIdList = queueAgentList.Select(q => q.ComQueueAgentPID).ToList();
            var queueAgentCallList = QueueAgentHandler.GetQueueAgentCallListByLastCall(queueAgentIdList);

            var doBreak = false;
            queueAgentCallList.CustomeForEach(ref doBreak, async (queueAgentCall, index) =>
            {
                var queueAgent = queueAgentCall.GetQueueAgent();
                if (queueAgent != null)
                {
                    var voipNumber = queueAgent.VoipNumber ?? 0;
                    //check if the agent's wrapping time is finished or not
                    if (voipNumber > 0)
                    {
                        //we have to check if the user is busy or not
                        //-----------------------------------------------------
                        var userIsBusy = FreeswitchWorker.ExtensionIsBusy(voipNumber.ToString());
                        if (!userIsBusy)
                        {
                            //#############################################
                            //**************AGENT IS NOT BUSY**************
                            //#############################################

                            if (queueAgent.WrapTimeIsFinished(queue.AgentWrappingTime))
                            {
                                var caller = Caller.GetCallerInfo(channel);

                                var fullNumber = "user/" + voipNumber;
                                //bridge call to agent
                                var bridgeOptions = new BridgeOptions()
                                {
                                    UUID = Guid.NewGuid().ToString(),
                                    CallerIdNumber = caller.CallerName,
                                    CallerIdName = caller.CallerNumber,
                                    HangupAfterBridge = false,
                                    //agent timeout secod if he doesnot answer the call this call must go to the other call
                                    TimeoutSeconds = queue.TimeoutForAgent
                                };

                                //bridge call to agent
                                await channel.BridgeTo(fullNumber, bridgeOptions);
                                if (!channel.IsBridged)
                                {

                                }
                                else
                                {
                                    //cancel all playing messages after answering
                                    await channel.CancelMedia();

                                    LogHelper.LogGreen(
                                        $"connection {uuid} from {caller.CallerNumber} is connected to {channel.GetDesNumber()}.");

                                    channel.HangupCallBack = (e) =>
                                    {
                                        QueueAgentHandler.StartWrapTime(queueAgent.ComQueueAgentPID);
                                    };

                                    //calculate count of the call for the agent in this queue
                                    //-----------------------------------------------------------
                                    QueueAgentHandler.AddCallCount(queueAgent.ComQueueAgentPID);
                                    //-----------------------------------------------------------
                                }
                                //-----------------------------------------------------
                            }
                        }
                    }
                }
            });
            return Task.CompletedTask;
        }
    }
}
