using System;
using System.Linq;
using System.Threading.Tasks;
using EtraabERP.Database.Definations;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.InboundApi.QueueApp
{
    internal class HandleCallByStrategy : ErpContainerDataHelper
    {
        public static async Task HandleCall(Guid queueId, string uuid)
        {
            var channel = ChannelListKeeper.GetChannel(uuid);
            if (channel != null)
            {
                var queue = QueueList.FirstOrDefault(a => a.ComQueuePID == queueId);
                var canHaveInboundCalls = queue.CanHaveInboundCalls;

                //canHaveInboundCalls = میتواند تماس از بیرون دریافت کند
                if (queueId.HasGuidValue() && canHaveInboundCalls)
                {
                    await channel.CancelMedia();

                    //get the start time for the queue (for calculating the timeout for the queue)
                    //add connection to QueueCallList
                    QueueCallData.AddOrUpdate(uuid, queueId);

                    var strategyId = queue.StrategyId;
                    //get agents of the queue and order by tierLevel
                    var queueAgentList =
                        QueueAgentList
                            .Where(p => p.ComQueueId == queueId).OrderBy(p => p.TierLevel)
                            .ToList();

                    //if queue has agent
                    if (queueAgentList.Count > 0)
                    {
                        var mohFileName = queue.MohFileName;
                        if (!string.IsNullOrWhiteSpace(mohFileName))
                        {
                            //پخش پیام انتظار
                            await channel.PlayUntilCancelled(mohFileName);
                        }


                        //برحسب نحوه اعمال زنگ در تنظیمات صف
                        switch (strategyId)
                        {
                            // همزمان
                            case (short)EnFreeswitchQueueStrategy.RingAll:
                                using (AppQueueCall iQueueCall = new RingAll())
                                {
                                    await iQueueCall.ManageQueueCall(channel, queue, queueAgentList);
                                }
                                break;

                            // کمترین تماس
                            case (short)EnFreeswitchQueueStrategy.AgentWithFewestCalls:
                                using (AppQueueCall iQueueCall = new AgentWithFewestCalls())
                                {
                                    await iQueueCall.ManageQueueCall(channel, queue, queueAgentList);
                                }
                                break;

                            // بیشترین تماس
                            case (short)EnFreeswitchQueueStrategy.AgentWithLeastTalkTime:
                                using (AppQueueCall iQueueCall = new AgentWithLeastTalkTime())
                                {
                                    await iQueueCall.ManageQueueCall(channel, queue, queueAgentList);
                                }
                                break;

                            // برحسب اولویت
                            case (short)EnFreeswitchQueueStrategy.TopDown:
                                using (AppQueueCall iQueueCall = new TopDown())
                                {
                                    await iQueueCall.ManageQueueCall(channel, queue, queueAgentList);
                                }
                                break;

                            // به ترتیب
                            case (short)EnFreeswitchQueueStrategy.SequentiallyByAgentOrder:
                                using (AppQueueCall iQueueCall = new SequentiallyByAgentOrder())
                                {
                                    await iQueueCall.ManageQueueCall(channel, queue, queueAgentList);
                                }
                                break;

                            // تصادفی
                            case (short)EnFreeswitchQueueStrategy.Random:
                                using (AppQueueCall iQueueCall = new RingRandom())
                                {
                                    await iQueueCall.ManageQueueCall(channel, queue, queueAgentList);
                                }
                                break;

                            // برحسب بیکار بودن منشی
                            case (short)EnFreeswitchQueueStrategy.LongestIdleAgent:
                                using (AppQueueCall iQueueCall = new LongestIdleAgent())
                                {
                                    await iQueueCall.ManageQueueCall(channel, queue, queueAgentList);
                                }
                                break;

                            // بر حسب اولیت(آخرین منشی در حافظه(
                            case (short)EnFreeswitchQueueStrategy.RoundRobin:
                                using (AppQueueCall iQueueCall = new RoundRobin())
                                {
                                    await iQueueCall.ManageQueueCall(channel, queue, queueAgentList);
                                }
                                break;
                        }

                    }
                }
            }
        }
    }
}
