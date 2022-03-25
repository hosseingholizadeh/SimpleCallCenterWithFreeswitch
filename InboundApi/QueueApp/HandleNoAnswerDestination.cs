using System.Threading.Tasks;
using EtraabERP.Database.Definations;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.InboundApi.ExtensionApp;

namespace FreeswitchListenerServer.InboundApi.QueueApp
{
    internal class HandleNoAnswerDestination
    {
        public static async Task HandleCall(string uuid, ComQueue queue)
        {
            //if queue has setting for NoAnswerDesTimeGuid call must go to that destination
            var noAnswerDesTimeGuid = queue.NoAnswerDesTimeGuid;
            if (noAnswerDesTimeGuid != null)
            {
                var noAnswerDesTimeTypeId = queue.NoAnswerDesTimeTypeId;
                if (noAnswerDesTimeTypeId == (short) EnFreeswitchQueueNoAnswerDesType.Extension)
                {
                    await new CallToExtension().CallAgent(uuid, noAnswerDesTimeGuid.Value);
                }
                else if (noAnswerDesTimeTypeId == (short) EnFreeswitchQueueNoAnswerDesType.Operator)
                {
                    
                }
                else if (noAnswerDesTimeTypeId == (short) EnFreeswitchQueueNoAnswerDesType.Queue)
                {
                    await HandleCallByStrategy.HandleCall(noAnswerDesTimeGuid.Value, uuid);
                }
            }
        }
    }
}
