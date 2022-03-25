using System;
using System.Linq;
using EtraabERP.Database.Definations;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;
using NEventSocket.FreeSwitch;

namespace FreeswitchListenerServer.OutboundApi
{
    public class UrbanLineCaller : ErpContainerDataHelper
    {
        public static void StartCalling(string desNumber, Channel channel)
        {
            var doBreak = false;
            var doAgain = false;
            var caller = Caller.GetCallerInfo(channel);

            GateWayList.CustomeForEach(ref doBreak, ref doAgain, async (gateway, index) =>
            {
                var destinationFullName = $"sofia/gateway/{gateway.GatewayName}/{desNumber}";
                var bridgeOptions = new BridgeOptions()
                {
                    UUID = Guid.NewGuid().ToString(),
                    IgnoreEarlyMedia = true,
                    CallerIdNumber = caller.CallerNumber,
                    CallerIdName = caller.CallerName,
                    HangupAfterBridge = false,
                    TimeoutSeconds = 60
                };

                await channel.BridgeTo(destinationFullName, bridgeOptions, (e) =>
                {
                    LogHelper.LogGreen("Bridge Progress Ringing...");
                });


                if (!channel.IsBridged)
                {
                    doAgain = true;
                    return;
                }
                else
                {
                    doBreak = true;
                    await channel.CancelMedia();

                    return;
                }
            });
        }


        /// <summary>
        /// Check settings in the Extension setitngs
        /// </summary>
        /// <param name="callerNumber"></param>
        /// <param name="desNumber"></param>
        /// <returns></returns>
        private static bool CanCall(string callerNumber, string desNumber)
        {
            //TODO:باید از مهندس در مورد نحوه برگشت true و false سوال بپرسم.
            var setting = GetExactSettingOfVoipNumber(callerNumber);
            var limit = setting?.Limit;

            //TODO:فرق بین خطوط شهری و بین المللی
            //TODO:if contains or equal?
            if (limit != null && limit.LimitDetailList.Any(p => p.LineTypeId != (short)EnLineType.Extension && p.Number == desNumber))
            {  //سلب محدودیت
                if (limit.LimitTypeId == (short)EnExtensionNumberingLimitType.GetLimitation)
                {
                    return true;
                }
                //ایجاد محدودیت
                else if (limit.LimitTypeId == (short)EnExtensionNumberingLimitType.SetLimitation)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CanCallWithChannelNumber(string callerNumber, string channelNumber)
        {
            var setting = GetExactSettingOfVoipNumber(callerNumber);
            var groupIdList = setting.GroupShiftList.GroupIdList;
            var channelGroupList = ChannelGroupList.Where(g => g.ChannelNumberList.Contains(channelNumber))
                .Select(g => g.GroupId).ToList();

            return groupIdList.Any(g => channelGroupList.Contains(g));
        }
    }
}
