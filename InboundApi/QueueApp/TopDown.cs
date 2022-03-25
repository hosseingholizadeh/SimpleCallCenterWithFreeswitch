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
    /// برحسب اولویت
    /// </summary>
    internal class TopDown : AppQueueCall
    {
        /// <summary>
        /// اعمال تماس برحسب اولویت
        /// </summary>
        public override Task ManageQueueCall(
            Channel channel,
            ComQueue queue,
            List<vwComQueueAgent> queueAgentList)
        {
            var uuid = channel.UUID;
            var caller = Caller.GetCallerInfo(channel);
            //order queue agent list by tier position
            var orderedQueueAgentList =
                queueAgentList.OrderBy(p => p.TierPosition).ToList();

            //call must be bidged to agents by their TierLevel and TierPosition
            //and by application detail which is set in ERP 
            //we have to play waiting message to the user by the setting which set in ERP
            var orderedQueueAgentCount = orderedQueueAgentList.Count;
            var queueAgentIdList = orderedQueueAgentList.Select(p => p.ComQueueAgentPID).ToList();
            var doBreak = false;
            var doAgain = false;
            orderedQueueAgentList.CustomeForEach(ref doBreak, ref doAgain, async (queueAgent, queueAgentIndex) =>
            {
                var queueCall = QueueCallData.GetQueueCall(queue.ComQueuePID, uuid);

                //check if the queue's timeout is finished or not
                if (queueCall != null && queueCall.Value.TimeoutIsFinished(queue.TimeoutForQueue))
                {
                    doBreak = true;
                    //this means queue's timeout is finished => it has to go to other queue or ...
                    await HandleNoAnswerDestination.HandleCall(uuid, queue);
                    return;
                }

                var voipNumber = queueAgent.VoipNumber ?? 0;
                var queueAgentCall = QueueAgentHandler.GetQueueAgentCall(queueAgent.ComQueueAgentPID);

                //(queueAgentCall.CallCount == queue.MaxCallForAgent) == > if the agent call count was equal to maximum number call for any agent
                if (queueAgentCall != null && queueAgentCall.Value.CallCount == queue.MaxCallForAgent)
                {
                    if (queueAgentIndex == orderedQueueAgentCount - 1)
                    {
                        var resetAfterMaxCallForAllAgents = queue.RsetAfterMaxCallForAgent;
                        if (resetAfterMaxCallForAllAgents)
                        {
                            var relatedQueueAgentList =
                                QueueAgentHandler.GetRelatedQueueAgentCallList(queueAgentIdList);

                            var noFinishedQueueAgentCallIdList = relatedQueueAgentList
                                .Where(a => a.CallCount < queue.MaxCallForAgent).Select(p => p.QueueAgentId)
                                .ToList();

                            if (noFinishedQueueAgentCallIdList.Count > 0)
                            {
                                //we have to redirect the call to the queues which call count is not compeleted
                                orderedQueueAgentList = orderedQueueAgentList.Where(p =>
                                    noFinishedQueueAgentCallIdList.Contains(p.ComQueueAgentPID)).ToList();

                                orderedQueueAgentCount = orderedQueueAgentList.Count;
                            }
                            else
                            {
                                //we have to reset all the queue agents and set the call count to 0
                                QueueAgentHandler.ResetAllAgentCallCount(queueAgentIdList);
                            }

                            doAgain = true;
                            return;
                        }
                        else
                        {
                            //it means the queue is not active
                            doBreak = true;
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else if (queueAgentCall != null && !queueAgentCall.Value.WrapTimeIsFinished(queue.AgentWrappingTime))
                {
                    return;
                }

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

                        var fullNumber = "user/" + voipNumber;
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
                    }

                    //-----------------------------------------------------
                }

                if (queueAgentIndex == orderedQueueAgentCount - 1 &&
                    !channel.IsAnswered)
                {
                    //if it is the last agent and also the timeout for the queue is not finished
                    //call must handle again in the queueagents for loop
                    doAgain = true;
                    return;
                }
            });
            return Task.CompletedTask;
        }

    }
}
