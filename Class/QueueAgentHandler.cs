using System;
using System.Collections.Generic;
using System.Linq;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Helper;

namespace FreeswitchListenerServer.Class
{
    public struct QueueAgentCall
    {
        public Guid QueueAgentId;
        public long StartTimeTicks
        {
            get => StartTimeTicks;
            set => StartTimeTicks = value;
        }

        /// <summary>
        /// تعداد تماس برقرار شده به این ایجنت در این صف تا الان
        /// </summary>
        public int CallCount
        {
            get => CallCount;
            set => CallCount = value;
        }

        public double LastConnectionTimeTick { get; set; }
    }

    public class QueueAgentHandler : ErpContainerDataHelper
    {
        public static List<QueueAgentCall> QueueAgentCallList = new List<QueueAgentCall>();

        public static void Add(Guid queueAgentId)
        {
            if (QueueAgentCallList.All(q => q.QueueAgentId != queueAgentId))
            {
                QueueAgentCallList.Add(new QueueAgentCall()
                {
                    QueueAgentId = queueAgentId,
                    CallCount = 1,
                    LastConnectionTimeTick = DateTime.Now.Ticks,
                    StartTimeTicks = 0
                });
            }
        }

        public static void AddCallCount(string voipNumber)
        {
            var queueAgentId = QueueAgentList.Where(p => p.VoipNumber?.ToString() == voipNumber)
                .Select(a => a.ComQueueAgentPID).FirstOrDefault();
            if (queueAgentId.HasGuidValue())
            {
                AddCallCount(queueAgentId);
            }
        }

        public static void AddCallCount(Guid queueAgentId)
        {
            var queueAgentCall = GetQueueAgentCall(queueAgentId);
            if (queueAgentCall != null)
            {
                var queueAgentCallVal = queueAgentCall.Value;
                queueAgentCallVal.CallCount += 1;
            }
            else
            {
                Add(queueAgentId);
            }
        }

        public static void ResetQueueAgentCallCount(Guid queueAgentId)
        {
            var queueAgentCall = GetQueueAgentCall(queueAgentId);
            if (queueAgentCall != null)
            {
                var queueAgentCallVal = queueAgentCall.Value;
                queueAgentCallVal.CallCount = 1;
            }
            else
            {
                Add(queueAgentId);
            }
        }

        public static void ResetAllAgentCallCount(List<Guid> queueAgentIdList)
        {
            var relatedQueueAgentList = GetRelatedQueueAgentCallList(queueAgentIdList);
            var doBreak = false;
            relatedQueueAgentList.CustomeForEach(ref doBreak, (rQueueAgent, index) =>
             {
                 rQueueAgent.CallCount = 0;
             });
        }

        public static void Remove(Guid queueAgentId)
        {
            var queueAgentCall = QueueAgentCallList.FirstOrDefault(q => q.QueueAgentId == queueAgentId);
            QueueAgentCallList.Remove(queueAgentCall);
        }

        public static QueueAgentCall? GetQueueAgentCall(Guid queueAgentId)
        {
            return QueueAgentCallList.Cast<QueueAgentCall?>()
                .FirstOrDefault(p => p != null && p.Value.QueueAgentId == queueAgentId);
        }

        public static void StartWrapTime(string voipNumber)
        {
            var queueAgentId = QueueAgentList.Where(p => p.VoipNumber?.ToString() == voipNumber)
                .Select(a => a.ComQueueAgentPID).FirstOrDefault();
            if (queueAgentId.HasGuidValue())
            {
                StartWrapTime(queueAgentId);
            }
        }

        public static void StartWrapTime(Guid queueAgentId)
        {
            var agentCall = GetQueueAgentCall(queueAgentId);
            if (agentCall != null)
            {
                var agentCallVal = agentCall.Value;
                //شروع زمان استراحت برای ایجنت تا تماس بعدی
                agentCallVal.StartTimeTicks = DateTime.Now.Ticks;
            }
            else
            {
                Add(queueAgentId);
            }
        }

        public static List<QueueAgentCall> OrderByCallCountAsc(List<QueueAgentCall> queueAgentCallList)
        {
            return queueAgentCallList.OrderBy(p => p.CallCount).ToList();
        }

        public static void OrderByCallCount(ref List<QueueAgentCall> queueAgentCallList)
        {
            queueAgentCallList = queueAgentCallList.OrderBy(p => p.CallCount).ToList();
        }

        public static void OrderByCallCountDes(ref List<QueueAgentCall> queueAgentCallList)
        {
            queueAgentCallList = queueAgentCallList.OrderByDescending(p => p.CallCount).ToList();
        }

        public static List<QueueAgentCall> GetRelatedQueueAgentCallList(List<Guid> queueAgentIdList)
        {
            return OrderByCallCountAsc(QueueAgentCallList.Where(a => queueAgentIdList.Contains(a.QueueAgentId)).ToList());
        }

        public static vwComQueueAgent GetQueueAgent(QueueAgentCall queueAgentCall)
        {
            return QueueAgentList.FirstOrDefault(p => p.ComQueueAgentPID == queueAgentCall.QueueAgentId);
        }

        public static List<QueueAgentCall> GetQueueAgentCallListByLastCall(List<Guid> queueAgentIdList)
        {
            var relatedQueueAgentCallList = GetRelatedQueueAgentCallList(queueAgentIdList);
            return relatedQueueAgentCallList.OrderByDescending(p => p.LastConnectionTimeTick).ToList();
        }

    }

    public static class QueueCallExt
    {
        public static bool WrapTimeIsFinished(this QueueAgentCall queueAgentCall, long wrapTime)
        {
            if (!TimeHelper.IsFinished(queueAgentCall.StartTimeTicks, DateTime.Now.Ticks,
                wrapTime))
            {
                return true;
            }

            return false;
        }

        public static bool WrapTimeIsFinished(this vwComQueueAgent queueAgent, long wrapTime)
        {
            var queueAgentCall = QueueAgentHandler.GetQueueAgentCall(queueAgent.ComQueueAgentPID);
            if (queueAgentCall != null)
            {
                if (!TimeHelper.IsFinished(queueAgentCall.Value.StartTimeTicks, DateTime.Now.Ticks,
                    wrapTime))
                {
                    return true;
                }
            }

            return false;
        }

        public static List<QueueAgentCall> OrderByCallCount(this List<vwComQueueAgent> queueAgentList)
        {
            var queueAgentIdList = queueAgentList.Select(p => p.ComQueueAgentPID).ToList();
            var queueAgentCallList = QueueAgentHandler.GetRelatedQueueAgentCallList(queueAgentIdList);
            QueueAgentHandler.OrderByCallCount(ref queueAgentCallList);
            return queueAgentCallList;
        }

        public static List<QueueAgentCall> OrderByCallCountDes(this List<vwComQueueAgent> queueAgentList)
        {
            var queueAgentIdList = queueAgentList.Select(p => p.ComQueueAgentPID).ToList();
            var queueAgentCallList = QueueAgentHandler.GetRelatedQueueAgentCallList(queueAgentIdList);
            QueueAgentHandler.OrderByCallCountDes(ref queueAgentCallList);
            return queueAgentCallList;
        }

        /// <summary>
        /// remove all agents which are not registered in FreeSWICTH(he is OFF)
        /// </summary>
        /// <param name="queueAgentList"></param>
        public static async void RemoveNotRegisteredAgents(this List<vwComQueueAgent> queueAgentList)
        {
            var registeredVoipNumbers = await FreeswitchWorker.GetRegisteredUserList();
            queueAgentList.RemoveAll(p => !(registeredVoipNumbers.Contains(p.VoipNumber?.ToString())));
        }

        public static vwComQueueAgent GetQueueAgent(this QueueAgentCall queueAgentCall)
        {
            return QueueAgentHandler.GetQueueAgent(queueAgentCall);
        }
    }
}
