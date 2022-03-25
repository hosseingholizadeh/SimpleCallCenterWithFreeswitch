using System;
using System.Threading;
using System.Threading.Tasks;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.OutboundApi.NumberingPlan
{
    internal class IdleLineAccess : NumberingPlanOperator
    {
        public IdleLineAccess(string limitNumber) : base(limitNumber)
        {
        }

        public override Task ManageByNumberingPlan(Channel channel, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
