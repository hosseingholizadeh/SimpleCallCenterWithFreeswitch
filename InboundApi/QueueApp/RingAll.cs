using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;
using NEventSocket.FreeSwitch;

namespace FreeswitchListenerServer.InboundApi.QueueApp
{
    /// <summary>
    /// اعمال تماس همزمان
    /// </summary>
    internal class RingAll : AppQueueCall
    {
        /// <summary>
        /// اعمال تماس همزمان
        /// </summary>
        public override async Task ManageQueueCall(
            Channel channel,
            ComQueue queue,
            List<vwComQueueAgent> queueAgentList)
        {
            var uuid = channel.UUID;

            var caller = Caller.GetCallerInfo(channel);
            queueAgentList.RemoveNotRegisteredAgents();

            var continueCalling = true;
            var queueCall = QueueCallData.GetQueueCall(queue.ComQueuePID, uuid);
            if (queueCall != null && queueCall.Value.TimeoutIsFinished(queue.TimeoutForQueue))
            {
                //this means queue's timeout is finished => it has to go to other queue or ...
                continueCalling = false;
                await HandleNoAnswerDestination.HandleCall(uuid, queue);
            }

            if (continueCalling)
            {
                var finalEndpointStr = GetFinalEndpointAndCheckWrapTime(queueAgentList, queue.AgentWrappingTime);
                if (!string.IsNullOrWhiteSpace(finalEndpointStr))
                {

                    var bridgeOptions = new BridgeOptions()
                    {
                        CallerIdNumber = caller.CallerName,
                        CallerIdName = caller.CallerNumber,
                        HangupAfterBridge = false,
                        //agent timeout secod if he doesnot answer the call this call must go to the other call
                        TimeoutSeconds = queue.TimeoutForAgent
                    };

                    //bridge call to agent
                    await channel.BridgeTo(finalEndpointStr, bridgeOptions);
                    if (!channel.IsBridged)
                    {

                    }
                    else
                    {
                        //cancel all playing messages after answering
                        await channel.CancelMedia();

                        var desNumber = channel.GetDesNumber();
                        LogHelper.LogGreen(
                            $"connection {uuid} from {caller.CallerNumber} is connected to {desNumber}.");

                        channel.HangupCallBack = (e) =>
                        {
                            QueueAgentHandler.StartWrapTime(desNumber);
                        };


                        //calculate count of the call for the agent in this queue
                        //-----------------------------------------------------------
                        QueueAgentHandler.AddCallCount(desNumber);
                        //-----------------------------------------------------------
                    }
                   
                }
            }
        }

        private string GetFinalEndpointAndCheckWrapTime(List<vwComQueueAgent> queueAgentList,long queueAgentWrapTime)
        {
            var queueAgentListCount = queueAgentList.Count;
            var finalEndpointSb = new StringBuilder();
            //make final end point to call simultaneously
            var doBreak = false;
            queueAgentList.CustomeForEach(ref doBreak, (queueAgent, index) =>
             {
                 //check if the agent's wrapping time is finished or not
                 if (queueAgent.WrapTimeIsFinished( queueAgentWrapTime))
                 {
                     var fullNumber = "user/" + queueAgent.VoipNumber;
                     finalEndpointSb.Append(fullNumber);
                     if (index < queueAgentListCount - 1)
                         //(":_:") ==> is freeswitch syntax to make call simultaneously
                         finalEndpointSb.Append(":_:");
                 }
             });

            return finalEndpointSb.ToString();
        }
    }
}
