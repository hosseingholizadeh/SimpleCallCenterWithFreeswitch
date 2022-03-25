using System.Threading;
using System.Threading.Tasks;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.InboundApi.ExtensionApp
{
    public class HandleCallToExtensionApp :  CallHandler
    {
        public override async Task HandleCall(Channel channel, ComFreeswitchApp application, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                LogHelper.LogRed("cancelation requested for this task on ext app.");
                return;
            }

            var appAgent = GetAppAgentByAppId(application.ComAppPID);
            if (appAgent?.VoipNumber != null)
            {
                var voipNumber = appAgent.VoipNumber.ToString();
                await new CallToExtension().CallAppAgent(channel, application, voipNumber, ct);
            }
        }
        
    }
}
