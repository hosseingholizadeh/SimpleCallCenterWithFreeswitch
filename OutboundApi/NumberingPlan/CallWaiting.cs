using System;
using System.Threading;
using System.Threading.Tasks;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.OutboundApi.NumberingPlan
{
    public class CallWaiting:NumberingPlanOperator
    {
        public CallWaiting(string limitNumber) : base(limitNumber)
        {
        }

        public override Task ManageByNumberingPlan(Channel channel, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
