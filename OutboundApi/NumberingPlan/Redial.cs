using System.Threading;
using System.Threading.Tasks;
using FreeswitchListenerServer.Class;
using NEventSocket;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.OutboundApi.NumberingPlan
{
    internal class Redial : NumberingPlanOperator
    {
        public Redial(string limitNumber) : base(limitNumber)
        {
        }

        /// <summary>
        /// redial the last call for the caller(agent)
        /// </summary>
        /// <returns></returns>
        public override async Task ManageByNumberingPlan(Channel channel,CancellationToken ct)
        {
            var caller = Caller.GetCallerInfo(channel);
            var lastCalledNumber = Agent.GetLastCall(caller.CallerNumber);
            if (string.IsNullOrWhiteSpace(lastCalledNumber))
            {
                //get last call from the logs saved in the db
                lastCalledNumber = CallLog.GetLastCallFromLog(caller.CallerNumber);
                if (string.IsNullOrWhiteSpace(lastCalledNumber))
                {
                    await channel.Hangup();
                }
            }

            if (!string.IsNullOrWhiteSpace(lastCalledNumber))
                await CallToExtensionSoftphone.CallToExtension(lastCalledNumber, channel, ct);
        }
    }
}
