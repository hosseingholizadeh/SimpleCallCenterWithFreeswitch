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
    /// اعمال تماس تصادفی
    /// </summary>
    internal class RingRandom : AppQueueCall
    {
        /// <summary>
        /// اعمال تماس تصادفی
        /// </summary>
        public override async Task ManageQueueCall(
            Channel channel,
            ComQueue queue,
            List<vwComQueueAgent> queueAgentList)
        {
            var uuid = channel.UUID;
            var caller = Caller.GetCallerInfo(channel);
            var queueAgentListCount = queueAgentList.Count;
            var queueAgentIdList = queueAgentList.Select(p => p.ComQueueAgentPID).ToList();

            while (true)
            {
                var queueCall = QueueCallData.GetQueueCall(queue.ComQueuePID, uuid);
                if (queueCall != null && queueCall.Value.TimeoutIsFinished(queue.TimeoutForQueue))
                {
                    //this means queue's timeout is finished => it has to go to other queue or ...
                    await HandleNoAnswerDestination.HandleCall(uuid, queue);
                    break;
                }

                var randomIndex = RandomHelper.GenerateRandomNumber(0, queueAgentListCount - 1);
                var queueAgent = queueAgentList[randomIndex];
                var voipNumber = queueAgent.VoipNumber ?? 0;

                var queueAgentCall = QueueAgentHandler.GetQueueAgentCall(queueAgent.ComQueueAgentPID);

                //(queueAgentCall.CallCount == queue.MaxCallForAgent) == > if the agent call count was equal to maximum number call for any agent
                if (queueAgentCall != null && queueAgentCall.Value.CallCount == queue.MaxCallForAgent)
                {
                    var relatedQueueAgentList = QueueAgentHandler.GetRelatedQueueAgentCallList(queueAgentIdList);

                    //queue agents which call count is not finished
                    var noFinishedQueueAgentCallIdList = relatedQueueAgentList
                        .Where(a => a.CallCount < queue.MaxCallForAgent).Select(p => p.QueueAgentId)
                        .ToList();

                    if (noFinishedQueueAgentCallIdList.Count > 0)
                    {
                        continue;
                    }
                    else
                    {
                        var resetAfterMaxCallForAllAgents = queue.RsetAfterMaxCallForAgent;
                        if (resetAfterMaxCallForAllAgents)
                        {
                            //we have to reset all the queue agents and set the call count to 0
                            QueueAgentHandler.ResetAllAgentCallCount(queueAgentIdList);
                            continue;
                        }
                        else
                        {
                            //it means the queue is not active
                            break;
                        }
                    }

                }
                else if (queueAgentCall != null)
                {
                    //check if the user's wrapping time is finished or not
                    if (!queueAgentCall.Value.WrapTimeIsFinished(queue.AgentWrappingTime))
                    {
                        //this means user is in his wrapping time so call must go to other user or anything else
                        continue;
                    }
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
            }
        }
    }
}
