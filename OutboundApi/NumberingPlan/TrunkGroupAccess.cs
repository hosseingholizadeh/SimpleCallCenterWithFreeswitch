using System.Threading;
using System.Threading.Tasks;
using FreeswitchListenerServer.Class;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.OutboundApi.NumberingPlan
{
    internal class TrunkGroupAccess : NumberingPlanOperator
    {
        public TrunkGroupAccess(string limitNumber) : base(limitNumber){}

        /// <summary>
        /// trunk access : it can call out of the freeswitch network
        /// </summary>
        /// <returns></returns>
        public override Task ManageByNumberingPlan(Channel channel, CancellationToken ct)
        {
            var desNumber = GetExactNumber(channel.GetDesNumber());
            if (!string.IsNullOrWhiteSpace(desNumber))
            {
                UrbanLineCaller.StartCalling(desNumber, channel);
            }
            return Task.CompletedTask;
        }

        private string GetExactNumber(string desNumber)
        {
            return desNumber.Remove(0, this.LimitNumber.Length);
        }

    }
}
