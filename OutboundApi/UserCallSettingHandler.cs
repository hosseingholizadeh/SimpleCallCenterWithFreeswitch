using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EtraabERP.Database.Definations;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.OutboundApi
{
    internal class DndSetting
    {
        public bool IsTrunk { get; set; }
        public bool IsBlackList { get; set; }
        public bool IsExt { get; set; }
    }

    internal class UserCallSettingHandler : ErpContainerDataHelper
    {
        private static vwUserCallSetting GetSetting(string voipNumber)
        {
            return UserCallSettingsList.FirstOrDefault(p => p.VoipNumber != null && p.VoipNumber.Value.ToString() == voipNumber);
        }

        private static bool MatchWithSetting(string desNumber, string callerNumber, DndSetting setting)
        {
            if (setting.IsExt && VoipNumberList.Any(p => p == callerNumber))
                return true;

            if (setting.IsBlackList)
            {
                var blackList = BlackList.Where(p => p.VoipNumber.ToString() == desNumber).ToList();
                if (blackList.Any(p => p.BlockecNumber == callerNumber))
                    return true;
            }
            
            if (setting.IsTrunk)
            {
                return true;
            }

            return false;
        }

        public static async Task HandleByCallSetting(Channel channel, string desNumber, CancellationToken ct)
        {
            var userCallSetting = GetSetting(desNumber);
            if (userCallSetting != null)
            {
                var dndEnabled = (userCallSetting.DndEnabled != null && userCallSetting.DndEnabled.Value);
                if (dndEnabled)
                {
                    await ExecuteDnd(desNumber, channel, userCallSetting);
                }
                else
                {
                    var fwdEnabled = (userCallSetting.FwdEnabled != null && userCallSetting.FwdEnabled.Value);
                    if (fwdEnabled)
                    {
                        ExecuteFwd(channel, userCallSetting,ct);
                    }
                }
            }
            else
            {
                LogHelper.Log($"no user setting is enabled for {desNumber}");
            }
        }

        private static void ExecuteFwd(Channel channel, vwUserCallSetting setting,CancellationToken ct)
        {
            var fwdNumber = setting.FwdNumber;
            var fwdTypeId = setting.FwdCallTypeId;
            if (!string.IsNullOrWhiteSpace(fwdNumber))
            {
                //مقصد به داخلی دیگری وصل شود
                if (fwdTypeId == (short)EnVoipCallLineType.Ext_Calls)
                {
                    CallToExtensionSoftphone.StartCalling(channel.UUID, ct);
                }
                else
                {
                    UrbanLineCaller.StartCalling(fwdNumber, channel);
                }
            }
        }

        private static async Task ExecuteDnd(string desNumber, Channel channel, vwUserCallSetting setting)
        {
            var caller = Caller.GetCallerInfo(channel);
            var obj = new DndSetting()
            {
                IsTrunk = (setting.DndIsForTrunk != null && setting.DndIsForTrunk.Value),
                IsBlackList = (setting.DndIsForDndList != null && setting.DndIsForDndList.Value),
                IsExt = (setting.DndIsForExtensions != null && setting.DndIsForExtensions.Value)
            };

            if (MatchWithSetting(desNumber, caller.CallerNumber, obj))
            {
                await SoundPlayerHelper.SimplePlayFile(channel, setting.DndFileName);
            }

        }

    }
}
