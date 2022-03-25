using System.Threading;
using System.Threading.Tasks;
using FreeswitchListenerServer.OutboundApi.NumberingPlan.VoiceMail;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.OutboundApi.NumberingPlan
{
    internal class VoiceMailBox: NumberingPlanOperator
    {
        public VoiceMailBox(string limitNumber) : base(limitNumber)
        {

        }

        /// <summary>
        /// Play all unread voice mails for the agent
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public override async Task ManageByNumberingPlan(Channel channel, CancellationToken ct)
        {
            await channel.Play("EtIvr/WelcomeToVoiceMail.wav");
            await new VoiceMailIvr().PlayAndGetVoiceMailFileType(channel);
        }

    }
}
