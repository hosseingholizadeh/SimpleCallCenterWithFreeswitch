using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeswitchListenerServer.Helper;

namespace FreeswitchListenerServer.InboundApi.QueueApp
{

    public struct QueueCall
    {
        public Guid QueueId;
        public string Uuid;
        public long StartTimeTicks;

    }

    internal class QueueCallData
    {
        public static List<QueueCall> QueueCallList;

        public static QueueCall? GetQueueCall(Guid? queueId, string uuid)
        {
            var queueCall = QueueCallList?.Cast<QueueCall?>()
                .FirstOrDefault(p => p != null && p.Value.Uuid == uuid);
            return queueCall;
        }

        public static void AddOrUpdate(string uuid, Guid queueId)
        {
            if (QueueCallList == null)
                QueueCallList = new List<QueueCall>();

            var queueCall = QueueCallList.FirstOrDefault(p => p.Uuid == uuid && p.QueueId == queueId);
            if (queueCall.Equals(null))
            {
                QueueCallList.Add(new QueueCall()
                {
                    Uuid = uuid,
                    QueueId = queueId,
                    StartTimeTicks = DateTime.Now.Ticks
                });
            }
            else
            {
                queueCall.StartTimeTicks = DateTime.Now.Ticks;
            }
        }

        public static void Remove(string uuid, Guid queueId)
        {
            QueueCallList?.RemoveAll(p => p.Uuid == uuid && p.QueueId == queueId);
        }

        public static void RemoveAll(string uuid)
        {
            QueueCallList?.RemoveAll(p => p.Uuid == uuid);
        }

    }

    public static class QueueCallExt
    {
        public static bool TimeoutIsFinished(this QueueCall queueCall, int timeout)
        {
            return TimeHelper.IsFinished(queueCall.StartTimeTicks, DateTime.Now.Ticks, timeout);
        }
    }
}
