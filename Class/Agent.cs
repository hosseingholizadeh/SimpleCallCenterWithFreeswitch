using System;
using System.Collections.Generic;
using System.Linq;

namespace FreeswitchListenerServer.Class
{
    public class AgentLastCall
    {
        public string VoiNumber { get; set; }
        public string LastCalledNumber { get; set; }
    }

    public class Agent:ErpContainerDataHelper
    {
        public static List<AgentLastCall> AgentLastCallList { get; set; } = new List<AgentLastCall>();

        public static string GetAgentVoipNumber(Guid agentId)
        {
            return AgentList.Where(p => p.ComAgentPID == agentId).Select(p=>p.VoipNumber?.ToString()).FirstOrDefault();
        }

        public static void AddOrUpdateLastCall(string voipNumber, string desNumber)
        {
            var agentLastCall = AgentLastCallList.FirstOrDefault(a => a.VoiNumber == voipNumber);
            if (agentLastCall != null)
            {
                agentLastCall.LastCalledNumber = desNumber;
            }
            else
            {
                AgentLastCallList.Add(new AgentLastCall()
                {
                    VoiNumber = voipNumber,
                    LastCalledNumber = desNumber
                });
            }
        }

        public static string GetLastCall(string voipNumber)
        {
            return AgentLastCallList.Where(a => a.VoiNumber == voipNumber).Select(p => p.LastCalledNumber)
                .FirstOrDefault();
        }
    }
}
