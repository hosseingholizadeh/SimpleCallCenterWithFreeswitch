using System.Threading;
using EtraabERP.Database.Definations;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.InboundApi.OperatorApp;
using System.Threading.Tasks;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.OutboundApi.NumberingPlan
{
    public class NumberingPlanHandler
    {
        public static async Task HandleByNumberingPlan(Channel channel, ComNumberingPlan numberingPlan,CancellationToken ct)
        {
            var numberPlanOperationId = numberingPlan.NumberPlanOperationId;
            if (numberPlanOperationId == (short)EnNumberingPlanOperator.Redial)
            {
                LogHelper.Log("redial numbering plan startred");
                using (NumberingPlanOperator redial = new Redial(numberingPlan.PlanNo))
                {
                    await redial.ManageByNumberingPlan(channel,ct);
                }
            }
            else if (numberPlanOperationId == (short)EnNumberingPlanOperator.OparatorCall)
            {
                LogHelper.Log("operator call numbering plan startred");
                channel.CallOperators(ct);
            }
            else if (numberPlanOperationId == (short)EnNumberingPlanOperator.TrunkGroupAccess)
            {
                LogHelper.Log("trunk access group numbering plan startred");
                using (NumberingPlanOperator trunkGrpAccess = new TrunkGroupAccess(numberingPlan.PlanNo))
                {
                    await trunkGrpAccess.ManageByNumberingPlan(channel, ct);
                }
            }
            else if (numberPlanOperationId == (short)EnNumberingPlanOperator.IdleLineAccess)
            {
                LogHelper.Log("idle line access plan startred");
                using (NumberingPlanOperator trunkGrpAccess = new TrunkGroupAccess(numberingPlan.PlanNo))
                {
                    await trunkGrpAccess.ManageByNumberingPlan(channel, ct);
                }
            }
            else if (numberPlanOperationId == (short)EnNumberingPlanOperator.VoiceMail)
            {
                LogHelper.Log("voicemail numbering plan startred");
                using (NumberingPlanOperator voiceMailBox = new VoiceMailBox(numberingPlan.PlanNo))
                {
                    await voiceMailBox.ManageByNumberingPlan(channel, ct);
                }
            }
        }
    }
}
