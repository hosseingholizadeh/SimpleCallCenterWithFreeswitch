using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.FreeswitchApiClass;
using FreeswitchListenerServer.Helper;
using FreeswitchListenerServer.InboundApi;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.Class
{
    public struct WaitingQueueModel
    {
        public Guid AppId { get; set; }
        public int OrderNumber { get; set; }
        public string DesNumber { get; set; }
        public string Uuid { get; set; }
        public DateTime ConnectedDateTime { get; set; }
        public System.Timers.Timer QueueTimer { get; set; }
        public HTimer QueueTimerForPlay { get; set; }
        public CancellationToken Ctoken { get; set; }
    }

    public class WaitingQueue : ErpContainerDataHelper
    {
        public static List<WaitingQueueModel> QueueCalls = new List<WaitingQueueModel>();

        public static async Task Add(Channel channel, ComFreeswitchApp application, string desNumber,
            CancellationToken ct)
        {
            //if call's count in the queue is not reached it's max number
            var queueCallCountForApp = GetQueueCallCountForApp(application.ComAppPID);
            var max = application.WaittingCount;
            if (queueCallCountForApp < max)
            {
                LogHelper.Log("call forwarded to agent queue.");
                var slaTimeOut = (double) (application.SlaTimeout ?? 60);
                var orderNum = queueCallCountForApp + 1;
                var queue = new WaitingQueueModel()
                {
                    Uuid = channel.UUID,
                    AppId = application.ComAppPID,
                    OrderNumber = orderNum,
                    DesNumber = desNumber,
                    ConnectedDateTime = DateTime.Now,
                    Ctoken = ct,
                    QueueTimer =
                        WaitingQueueTimer.SetTimer(slaTimeOut, () => { HandleCallWhenTimeoutFinished(channel, ct); }),
                    QueueTimerForPlay = new HTimer()
                };
                QueueCalls.Add(queue);

                await WaitingMassage.Play(channel, application, queue);
            }
            else
            {
                LogHelper.LogRed("new channel could not go to the queue because queue is full.");
                CallWaitIsFull(channel, ct);
            }
        }

        private static async void CallWaitIsFull(Channel channel, CancellationToken ct)
        {
            //صف انتظار پر شده است .
            await channel.Play(FilePathKeeper.AgentQueueIsFilled);
            //برای تماس با اپراتور منتظر بمانید
            await channel.Play(FilePathKeeper.WaitToCallOperator);
            channel.CallOperators(ct);
        }

        private static int GetQueueCallCountForApp(Guid appId)
        {
            return QueueCalls.Count(p => p.AppId == appId);
        }

        public static void Remove(string uuid)
        {
            var queueCall = GetQueueCall(uuid);
            if (queueCall != null)
            {
                var value = queueCall.Value;
                Remove(value);
                LogHelper.Log($"{value.Uuid} is removed from the {value.DesNumber} queue.");
                UpdateQueueCalls(value.DesNumber);
            }
        }

        private static void UpdateQueueCalls(string desNumber)
        {
            QueueCalls.Where(q => q.DesNumber == desNumber).CustomeForEach(async item =>
            {
                item.QueueTimerForPlay?.Dispose();
                item.QueueTimerForPlay = new HTimer();
                item.OrderNumber -= 1;
                var channel = ChannelListKeeper.GetChannel(item.Uuid);
                if (channel != null && !channel.IsBridged)
                {
                    await channel.CancelMedia();
                    var application = GetAppById(item.AppId);
                    if (application != null)
                        await WaitingMassage.Play(channel, application, item);
                }
            });
            LogHelper.Log($"{QueueCalls.Count} call is remained in {desNumber} queue.");
        }

        public static void Remove(WaitingQueueModel queueCall)
        {
            queueCall.QueueTimer?.Dispose();
            queueCall.QueueTimerForPlay?.Dispose();
            QueueCalls.Remove(queueCall);
        }

        public static List<WaitingQueueModel?> OrderQueueCallList(string desNumber)
        {
            return QueueCalls.Where(q => q.DesNumber == desNumber)
                .OrderBy(c => c.ConnectedDateTime).Cast<WaitingQueueModel?>()
                .ToList();
        }

        public static WaitingQueueModel? OrderAndGetFirstCall(string desNumber)
        {
            return OrderQueueCallList(desNumber).ToList().FirstOrDefault();
        }

        public static WaitingQueueModel? GetQueueCall(string uuid)
            => QueueCalls.Cast<WaitingQueueModel?>().FirstOrDefault(p => p != null && p.Value.Uuid == uuid);

        /// <summary>
        /// وقتی حداکثر زمان مجاز برای تماس گیرنده در صف انتظار به پایان برسد این تابع اجرا میشود.
        /// </summary>
        public static async void HandleCallWhenTimeoutFinished(Channel channel, CancellationToken ct)
        {
            var queueCall = GetQueueCall(channel.UUID);
            if (queueCall != null)
            {
                var queueCallVal = queueCall.Value;
                Remove(queueCallVal);
                await HandleByAppType.FinishedQueueTimeoutHandle(queueCallVal.AppId, channel, ct);
            }
        }

        public static void PrintAll()
        {
            foreach (var queueCall in QueueCalls)
            {
                LogHelper.LogGreen(
                    $"queuCall:{queueCall.Uuid} , {queueCall.OrderNumber} , {queueCall.QueueTimerForPlay}");
            }
        }

        public static async Task WorkOnQueueCalls(string voipNumber)
        {
            //get the first call which is added to the queue before all of them
            await Task.Delay(1500);
            var firstChannel = OrderAndGetFirstCall(voipNumber);
            if (firstChannel != null)
            {
                var uuid = firstChannel.Value.Uuid;
                var nextChannel = ChannelListKeeper.GetChannel(uuid);
                if (nextChannel != null)
                {
                    LogHelper.Log($"first channel in {voipNumber} queue is {uuid}.");
                    //remove call from the queue
                    Remove(uuid);
                    await nextChannel.CancelMedia();
                    HandleByAppType.ManageByAppType(firstChannel.Value.AppId, uuid,
                        firstChannel.Value.Ctoken);
                }
                else
                {
                    LogHelper.LogRed("could not find next channel in queue.");
                }
            }
        }
    }
}
