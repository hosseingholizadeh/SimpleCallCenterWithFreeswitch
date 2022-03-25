using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.InboundApi.QueueApp
{
    internal class HandleCallToQueueApp : CallHandler
    {
        /// <summary>
        /// handle the connected call to application which is connected to queue
        /// </summary>
        public override Task HandleCall(Channel channel, ComFreeswitchApp application, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                LogHelper.LogRed("cancelation requested for this task on queue app.");
                return Task.CompletedTask;
            }

            var appQueueList = AppQueueList.Where(p => p.ComAppId == application.ComAppPID)
                .OrderBy(p => p.Priority)
                .ToList();

            //صف اول انتخاب می شود و برحسب نوع اعمال زنگ در تنظیمات صف اینجت های صف زنگ میخورند
            //اگر پاسخ داده شد که هیچ ولی در صورت عدم پاسخ توسط همه ایجنت های صف به هردلیلی آنگاه:
            //1-اگر در تنظیمات صف مشخص بود که در صورت عدم پاسخگویی به کجا وصل شود
            //در آن صورت به آن (صف و یا ایجنت و ...) که در تنظیمات صف مشخص است وصل خواهد شد
            //2-اگر مشخص نبود به کجا وصل شود آنگاه باید به تنظیمات برنامه مراجعه میکنیم
            //و اگر صف دیگری موجود باشد به آن صف متصل میشود و همان مراحل قبل برای آن نیز تکرار خواهد شد.
            //ولی اگر صف دیگری موجود نباشد تماس گیرنده باید به حالت انتظار برحست تنظیمات برنامه برود
            var doBreak = false;
            if (channel.IsAnswered)
            {
                //this will break from the foreach loop
                return Task.CompletedTask;
            }
            var uuid = channel.UUID;
            appQueueList.CustomeForEach(ref doBreak, async (appQueue, index) =>
            {
                await HandleCallByStrategy.HandleCall(appQueue.ComQueueId, uuid);

                //if it is the last queue in the application 
                //and call has not been answered yet call must go to waiting queue 
                if (index == appQueueList.Count - 1)
                {
                    if (application.WaittingCount > 0)
                        await WaitingQueue.Add(channel, application,"",ct);
                    else
                        channel.CallOperators(ct);
                }
            });

            return Task.CompletedTask;
        }
    }
}
