using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.Class
{
    public class WaitingMassage : ErpContainerDataHelper
    {
        public static async Task Play(Channel channel, ComFreeswitchApp application, WaitingQueueModel queue)
        {
            await channel.PlayUntilCancelled(FilePathKeeper.BridgeWaitingMusic);
            var orderNum = queue.OrderNumber;
            if (MessageFileList != null && MessageFileList.Count > 0)
            {
                var constFileList = MessageFileList.Where(p =>
                    p.PubDataStoragePID == application.MessageThirdPartFileId ||
                    p.PubDataStoragePID == application.MessageFirstPartFileId).Select(p => p.Data).ToList();

                LogHelper.Log($"const file List count {constFileList.Count}");
                if (constFileList.Count >= 2)
                    await SoundPlayerHelper.PlayWaitingMessage(channel, constFileList, orderNum, application);
                else
                    PlaySysWaitingMessage(channel, application, orderNum);
            }
            else
            {
                PlaySysWaitingMessage(channel, application, orderNum);
            }

        }

        public static void PlaySysWaitingMessage(Channel channel, ComFreeswitchApp application, int orderNum)
        {
            var constFileNameList = GetSysWaitingMessageFiles();
            LogHelper.Log($"sys file list count {constFileNameList.Count}");
            if (constFileNameList.Count > 0)
            {
                SoundPlayerHelper.PlaySysWaitingMessage(channel, constFileNameList, orderNum, application);
            }
        }

        private static List<string> GetSysWaitingMessageFiles()
        {
            return new List<string>()
            {
                FilePathKeeper.YouAreThe,
                FilePathKeeper.InWaitingQueue
            };
        }
    }
}
