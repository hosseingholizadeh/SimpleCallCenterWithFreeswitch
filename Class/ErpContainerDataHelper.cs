using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using EtraabERP.Database.Definations;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Helper;
using FreeswitchListenerServer.ViewModels;

namespace FreeswitchListenerServer.Class
{
    public class ErpContainerDataHelper
    {
        internal static Dictionary<ComFreeswitchChannel, List<vwComChannelShiftApp>> ChannelDataList;

        internal static List<vwComFreeswitchShiftOperator> ShiftOperatorList;

        internal static List<ComFreeswitchGateway> GateWayList;
        internal static List<string> VoipNumberList;
        internal static List<ComFreeswitchChannel> ChannelList;

        internal static List<ComFreeswitchApp> ApplicationList;

        internal static List<vwComFreeswitchAppQueue> AppQueueList;

        internal static List<vwComFreeswitchAppAgent> AppAgentList;

        internal static List<vwComQueueAgent> QueueAgentList;

        internal static List<ComQueue> QueueList;

        internal static List<vwComAgent> AgentList;

        internal static List<ComFreeswitchShift> ShiftList;

        internal static List<vwComOperatorSetting> OperatorSettingList;

        internal static List<PubDataStorage> MessageFileList;

        internal static List<ComFreeswitchIvrAppDetail> IvrAppDetailList;

        internal static List<vwComFreeswitchIvrApp> IvrAppList;

        internal static List<VoiceMailVm> VoiceMailSettingsList;

        internal static List<vwUserCallSetting> UserCallSettingsList;
        internal static List<vwComBlackList> BlackList;
        internal static List<ComNumberingPlan> NumebringPlanList;
        internal static List<VoipCallAccess> VoipCallAccessList;
        internal static List<AgentSettingVm> AgentSettingList;
        internal static List<ExtSettingVm> ExtSettingList;
        internal static List<ChannelGroupVm> ChannelGroupList;

        internal static async Task GetChannelShiftAppData(ErpContainer db)
        {
            //برای اینکه هربار مجبور به کوئری زدن به دیتابیس نباشیم هنگام تغییرات ایجاد شده در ERP توسط وب سوکت این تابع را فراخوانی میکنم.
            ChannelDataList = new Dictionary<ComFreeswitchChannel, List<vwComChannelShiftApp>>();
            var channelList = await db.ComFreeswitchChannel.ToListAsync();
            if (channelList.Count > 0)
            {
                var channelShiftList = db.vwComChannelShiftApp.ToList();
                channelList.ForEach(channel =>
                {
                    var channelShiftAppList = channelShiftList.Where(p => p.ComChannelId == channel.ComChannelPID)
                        .ToList();

                    ChannelDataList.Add(channel, channelShiftAppList);
                });
            }
            LogHelper.Log("Shift App data is loaded.");
        }

        internal static async Task GetMessageFileList(ErpContainer db)
        {
            var firstFileIdList = ApplicationList?.Where(p => p.MessageFirstPartFileId != null)
                .Select(p => p.MessageFirstPartFileId).ToList();

            var thirdFileIdList = ApplicationList?.Where(p => p.MessageThirdPartFileId != null)
                .Select(p => p.MessageThirdPartFileId).ToList();

            var fileIdList = firstFileIdList?.Concat(thirdFileIdList).ToList();

            if (fileIdList?.Count > 0)
            {
                MessageFileList = await db.PubDataStorage.Where(p => fileIdList.Contains(p.PubDataStoragePID)).ToListAsync();
            }
        }

        internal static ComFreeswitchIvrAppDetail GetIvrAppDetailById(Guid appId)
            => IvrAppDetailList?.FirstOrDefault(f => f.ComAppId == appId);

        internal static List<vwComFreeswitchIvrApp> GetAppListForIvrById(Guid appId)
            => IvrAppList?.Where(p => p.ComParentAppId == appId && !string.IsNullOrWhiteSpace(p.IvrNumber)).ToList();

        internal static async Task GetIvrAppList(ErpContainer db)
        {
            IvrAppList = await db.vwComFreeswitchIvrApp.ToListAsync();
            LogHelper.Log("Ivr app data is loaded.");
        }

        internal static async Task GetChannelAndGatewaypList(ErpContainer db)
        {
            GateWayList = await db.ComFreeswitchGateway.ToListAsync();
            GateWayList.RemoveAll(g => g.ComFreeswitchChannel == null || g.ComFreeswitchChannel.Count == 0);
            foreach (var gateway in GateWayList)
            {
                if (ChannelList == null)
                    ChannelList = new List<ComFreeswitchChannel>();

                ChannelList.AddRange(gateway.ComFreeswitchChannel);
            }
            LogHelper.Log("Channel and gateway data is loaded.");
        }

        internal static async Task GetShiftOperatorList(ErpContainer db)
        {
            //دریافت لیست اپراتورها در شیفت ها
            ShiftOperatorList = await db.vwComFreeswitchShiftOperator.ToListAsync();
            LogHelper.Log("Shift operator data is loaded.");
        }

        internal static async Task GetAppData(ErpContainer db)
        {
            ApplicationList = db.ComFreeswitchApp.ToList();
            await GetIvrAppList(db);
            await GetIvrAppDetailList(db);
            await GetMessageFileList(db);
            await GetAppAgentData(db);
            LogHelper.Log("Application data is loaded.");
        }

        internal static async Task GetSoftphoneSettings(ErpContainer db)
        {
            await GetVoiceMailSettingData(db);
            await GetUserCallSettingData(db);
            await GetBlaclList(db);
            await GetNumberingPlanData(db);
            LogHelper.Log("Softphone data is loaded.");
        }

        private static async Task GetVoiceMailSettingData(ErpContainer db)
        {
            VoiceMailSettingsList = await db.vwComVoiceMailSetting.Select(p => new VoiceMailVm()
            {
                Id = p.ComVoiceMailSettingPID,
                FileName = p.FileName,
                ExecuteTypeId = p.ExecuteTypeId,
                IsEnabled = p.Enabled,
                Max = p.MaxTimeoutMinute,
                StopKey = p.RecordStopKey,
                MaskKey = p.RecordMaskKey,
                UnMaskKey = p.RecordUnMaskKey,
                VoipNumber = (p.VoipNumber != null) ? p.VoipNumber.ToString() : string.Empty
            }).ToListAsync();
        }

        private static async Task GetUserCallSettingData(ErpContainer db)
        {
            UserCallSettingsList = await db.vwUserCallSetting.ToListAsync();
        }

        private static async Task GetNumberingPlanData(ErpContainer db)
        {
            NumebringPlanList = await db.ComNumberingPlan.ToListAsync();
        }

        private static async Task GetBlaclList(ErpContainer db)
        {
            BlackList = await db.vwComBlackList.ToListAsync();
        }

        internal static async Task GetQueueAgents(ErpContainer db)
        {
            QueueAgentList = await db.vwComQueueAgent.ToListAsync();
            LogHelper.Log("Queue agent data is loaded.");
        }


        internal static async Task GetPersonPermissionData(ErpContainer db)
        {
            var accessList = await db.vwEmpPersonPostAccess
                .Where(p => p.PubStaffReferenceTypeId == (short)EnStaffReferenceMainType.VoipCall).Select(p => new
                {
                    p.MainPubPersonId,
                    p.AccessPubPersonId
                }).ToListAsync();

            var pubPersonIdList = accessList.Select(p => p.MainPubPersonId);
            pubPersonIdList =
                pubPersonIdList.Concat(accessList.Select(p => p.AccessPubPersonId)).ToList();

            var userList = await db.UsrUser.Where(p => pubPersonIdList.Contains(p.PubPersonId.Value) && p.VoipNumber != null).Select(p => new
            {
                p.PubPersonId,
                p.VoipNumber
            }).ToListAsync();

            accessList.CustomeForEach(item =>
            {
                if (VoipCallAccessList == null)
                    VoipCallAccessList = new List<VoipCallAccess>();

                var mainVoipNumber = userList.Where(p => p.PubPersonId == item.MainPubPersonId)
                    .Select(p => p.VoipNumber.ToString()).FirstOrDefault();

                var accessVoipNumber = userList.Where(p => p.PubPersonId == item.AccessPubPersonId)
                    .Select(p => p.VoipNumber.ToString()).FirstOrDefault();

                VoipCallAccessList.Add(new VoipCallAccess()
                {
                    MainVoipNumber = mainVoipNumber,
                    AccessVoipNumber = accessVoipNumber
                });
            });

            LogHelper.Log("VoipCall permission data is loaded.");
        }

        public static async Task GetExtensionSetting(ErpContainer db)
        {
            var extSettingList = await db.ComExtensionSetting.ToListAsync();

            if (extSettingList.Count > 0)
            {
                var settingIdList = extSettingList.Select(p => p.ComExtensionSettingPID).ToList();
                await GetAgentSetting(db, extSettingList);
                await GetExtensionSettingDetail(db, settingIdList);
            }

            LogHelper.Log("Extension setting data is loaded.");
        }

        public static async Task GetExtensionSettingDetail(ErpContainer db, List<Guid> settingIdList)
        {
            var extShiftLimit = await db.ComExtensionShiftLimit
                .Where(p => settingIdList.Contains(p.ComExtensionSettingId)).ToListAsync();

            var extShiftIdList = extShiftLimit.Select(p => p.ComExtensionShiftLimitPID).ToList();
            var limitDetailList = await db.ComExtensionShiftLimitDetail
                .Where(p => extShiftIdList.Contains(p.ExtensionShiftLimitId))
                .ToListAsync();

            var groupShiftList = await db.vwComGroupShiftSetting.Where(p => settingIdList.Contains(p.ComExtensionSettingId))
                .ToListAsync();

            var groupIdList = groupShiftList.Select(p => p.ComGroupId).ToList();
            var groupList = await db.ComChannelGroup.Where(p => groupIdList.Contains(p.ComGroupId))
                .ToListAsync();

            ChannelGroupList = GetChannelGroupList(groupList).ToList();

            foreach (var settingId in settingIdList)
            {
                var relatedExtShiftLimit = extShiftLimit.Where(p => p.ComExtensionSettingId == settingId).ToList();
                var relatedExtShiftIdList = extShiftLimit.Select(p => p.ComExtensionShiftLimitPID).ToList();
                var relatedlimitDetailList = limitDetailList.Where(p => relatedExtShiftIdList.Contains(p.ExtensionShiftLimitId)).ToList();

                var extLimitSettingList = GetLimitDetailList(relatedExtShiftLimit, relatedlimitDetailList).ToList();

                var relatedGroupShiftList = groupShiftList.Where(p => p.ComExtensionSettingId == settingId).ToList();

                if (ExtSettingList == null)
                    ExtSettingList = new List<ExtSettingVm>();

                ExtSettingList.Add(new ExtSettingVm()
                {
                    SettingId = settingId,
                    LimitList = extLimitSettingList,
                    GroupShiftList = GetGroupShiftList(relatedGroupShiftList).ToList()
                });
            }
        }

        public static IEnumerable<ChannelGroupVm> GetChannelGroupList(List<ComChannelGroup> groupList)
        {
            var channelGroupList = groupList.GroupBy(p => p.ComGroupId).ToList();
            foreach (var group in channelGroupList)
            {
                var channelNumberList = GetChannelNumberListPerGroup(group).ToList();
                yield return new ChannelGroupVm()
                {
                    GroupId = group.Key,
                    ChannelNumberList = channelNumberList
                };
            }
        }

        public static IEnumerable<GroupShiftVm> GetGroupShiftList(List<vwComGroupShiftSetting> relatedGroupShiftList)
        {
            var groupShiftListPerShift = relatedGroupShiftList.GroupBy(p => p.ComShiftId).ToList();
            foreach (var group in groupShiftListPerShift)
            {
                var shiftId = group.Key;
                yield return new GroupShiftVm()
                {
                    ShiftId = shiftId,
                    GroupIdList = group.Select(p => p.ComGroupId).ToList()
                };
            }
        }

        public static IEnumerable<ChannelGroupVm> GetGroupListPerShift(IGrouping<Guid, vwComGroupShiftSetting> group,
            List<ChannelGroupVm> relatedChannelGroupList)
        {
            foreach (var groupShiftSetting in group)
            {
                var groupId = groupShiftSetting.ComGroupId;
                var channelNumberList = relatedChannelGroupList.Where(p => p.GroupId == groupId).Select(p => p.ChannelNumberList)
                    .FirstOrDefault();

                if (channelNumberList != null)
                {
                    yield return new ChannelGroupVm()
                    {
                        GroupId = groupId,
                        ChannelNumberList = channelNumberList
                    };
                }
            }
        }

        public static IEnumerable<string> GetChannelNumberListPerGroup(IGrouping<Guid, ComChannelGroup> group)
        {
            foreach (var channelGroup in group)
            {
                yield return channelGroup.ComFreeswitchChannel.ChannelNumber;
            }
        }

        public static IEnumerable<ExtLimitSettingVm> GetLimitDetailList(List<ComExtensionShiftLimit> extShiftLimit,
            List<ComExtensionShiftLimitDetail> limitDetailList)
        {
            var shiftLimitList = extShiftLimit.Select(p => new
            {
                p.ComExtensionShiftLimitPID,
                p.ComShiftId,
                p.LimitTypeId
            }).ToList();

            var limitDetailGroupList = limitDetailList.GroupBy(p => p.ExtensionShiftLimitId).ToList();
            foreach (var group in limitDetailGroupList)
            {
                var limit = shiftLimitList.FirstOrDefault(p => p.ComExtensionShiftLimitPID == group.Key);
                if (limit != null)
                {
                    yield return new ExtLimitSettingVm()
                    {
                        LimitDetailList = GetLimitDetailListByGroup(group).ToList(),
                        ShiftId = limit.ComShiftId,
                        LimitTypeId = limit.LimitTypeId
                    };
                }
            }
        }

        public static IEnumerable<ExtLimitDetailVm> GetLimitDetailListByGroup(IGrouping<Guid, ComExtensionShiftLimitDetail> group)
        {
            foreach (var limitDetail in group)
            {
                yield return new ExtLimitDetailVm()
                {
                    LineTypeId = limitDetail.LineTypeId,
                    Number = limitDetail.Number,
                };
            }
        }

        public static async Task GetAgentSetting(ErpContainer db, List<ComExtensionSetting> extSettingList)
        {
            var extSettingIdList = extSettingList.Select(p => p.ComExtensionSettingPID).ToList();
            try
            {
                AgentSettingList = await db.vwComAgentSetting
                    .Where(p => p.SettingId != null && extSettingIdList.Contains(p.SettingId.Value))
                    .Select(p => new AgentSettingVm()
                    {
                        SettingId = p.SettingId.Value,
                        VoipNumber = p.VoipNumber.Value.ToString()
                    }).ToListAsync();
                LogHelper.Log("Agent setting data is loaded.");
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }

        }

        public static Guid GetEnabledShiftOfNow()
        {
            var now = DateTime.Now;
            var dayOfWeek = (short)DateTime.Now.DayOfWeek;
            var enabledShiftList = ShiftList.Where(p => p.StartTime <= now.TimeOfDay &&
                                                                     p.EndTime >= now.TimeOfDay &&
                                                                     p.IsEnabled).ToList();


            return enabledShiftList.Where(p =>
            {
                //اعمال شرط کدام روز هفته
                var dayListStr = p.WeekDayListStr;
                if (!(string.IsNullOrWhiteSpace(dayListStr)))
                {
                    var dayList = dayListStr.Split(',');
                    if (dayList.Any(d => d == dayOfWeek.ToString()))
                    {
                        return true;
                    }
                }

                return false;
            }).Select(p => p.ComShiftPID).FirstOrDefault();
        }

        public static ExtShiftSettingVm GetExactSettingOfVoipNumber(string voipNumber)
        {
            var extSettingId = AgentSettingList?
                .Where(p => p.VoipNumber != null && p.VoipNumber.ToString() == voipNumber).Select(p => p.SettingId)
                .FirstOrDefault();


            if (extSettingId != null && extSettingId.HasGuidValue())
            {
                var shiftId = GetEnabledShiftOfNow();
                var setting = ExtSettingList.FirstOrDefault(p => p.SettingId == extSettingId);
                if (setting != null)
                {
                    var groupShift = setting.GroupShiftList.FirstOrDefault(p => p.ShiftId == shiftId);
                    var limitShift = setting.LimitList.FirstOrDefault(p => p.ShiftId == shiftId);

                    return new ExtShiftSettingVm()
                    {
                        GroupShiftList = groupShift,
                        Limit = limitShift
                    };
                }
            }

            return null;
        }

        internal static async Task GetIvrAppDetailList(ErpContainer db)
        {
            IvrAppDetailList = await db.ComFreeswitchIvrAppDetail.ToListAsync();
            LogHelper.Log("Ivr app detail data is loaded.");
        }

        internal static async Task GetQueueData(ErpContainer db)
        {
            QueueList = await db.ComQueue.ToListAsync();
            LogHelper.Log("Queue data is loaded.");
        }

        internal static async Task GetAgentData(ErpContainer db)
        {
            AgentList = await db.vwComAgent.ToListAsync();
            await GetVoipNumberList(db);
            LogHelper.Log("Agent data is loaded.");
        }

        internal static async Task GetShiftData(ErpContainer db)
        {
            ShiftList = await db.ComFreeswitchShift.ToListAsync();
            LogHelper.Log("Shift data is loaded.");
        }

        internal static async Task GetOperatorSettingData(ErpContainer db)
        {
            OperatorSettingList = await db.vwComOperatorSetting.ToListAsync();
            LogHelper.Log("Operator setting data is loaded.");
        }

        internal static async Task GetAppAgentData(ErpContainer db)
        {
            AppAgentList = await db.vwComFreeswitchAppAgent.ToListAsync();
            LogHelper.Log("App Agent data is loaded.");
        }

        internal static vwComFreeswitchAppAgent GetAppAgentByAppId(Guid appId)
            => AppAgentList.FirstOrDefault(p => p.ComAppId == appId);

        internal static ComFreeswitchApp GetAppByVoipNumber(long voipNumber)
        {
            return ApplicationList.FirstOrDefault(a =>
                a.ComAppPID == AppAgentList.Where(p => p.VoipNumber == voipNumber).Select(p => p.ComAppId)
                    .FirstOrDefault());
        } 

        internal static async Task GetAppQueueData(ErpContainer db)
        {
            AppQueueList = await db.vwComFreeswitchAppQueue.ToListAsync();
            LogHelper.Log("Application queue data is loaded.");
        }

        internal static async Task GetVoipNumberList(ErpContainer db)
        {
            VoipNumberList = await db.UsrUser.Where(u => u.VoipNumber > 0).Select(p => p.VoipNumber.ToString())
                .ToListAsync();
        }

        internal static ComFreeswitchApp GetAppById(Guid id)
            => ApplicationList.FirstOrDefault(p => p.ComAppPID == id);

        internal static AppVm GetSpecificShiftAppForChannel(string channelNumber)
        {
            var channelShiftAppData = ChannelDataList?.Where(p => p.Key.ChannelNumber == channelNumber)
                .Select(p => p.Value).FirstOrDefault();

            if (channelShiftAppData != null && channelShiftAppData.Count > 0)
            {
                //دریافت شیفت و برنامه مربوط برای همان تایم روز اگر فعال باشد.
                var withShift = (channelShiftAppData[0].ComShiftId != null);
                if (withShift)
                {
                    //با شیفت 
                    var dayOfWeek = (short)DateTime.Now.DayOfWeek;
                    var enabledShiftAppList = channelShiftAppData.Where(p => p.IsEnabled != null &&
                                                                             (p.StartTime <= DateTime.Now.TimeOfDay &&
                                                                              p.EndTime >= DateTime.Now.TimeOfDay &&
                                                                              (bool)p.IsEnabled)).ToList();

                    return enabledShiftAppList.Where(p =>
                    {
                        //اعمال شرط کدام روز هفته
                        var dayListStr = p.WeekDayListStr;
                        if (!(string.IsNullOrWhiteSpace(dayListStr)))
                        {
                            var dayList = dayListStr.Split(',');
                            if (dayList.Any(d => d == dayOfWeek.ToString()))
                            {
                                return true;
                            }
                        }

                        return false;
                    }).Select(p => new AppVm()
                    {
                        AppId = p.ComAppId,
                        AppTypeId = p.AppTypeId
                    }).FirstOrDefault();
                }
                else
                {
                    //بدون شیفت
                    return channelShiftAppData.Where(p => p.AppIsEnable != null && p.AppIsEnable.Value)
                        .Select(p => new AppVm()
                        {
                            AppId = p.ComAppId,
                            AppTypeId = p.AppTypeId
                        }).FirstOrDefault();
                }


            }
            return null;
        }

        public static bool IsExtension(string desNumber)
        {
            return VoipNumberList.Contains(desNumber);
        }

        public static async Task ReloadAllData()
        {
            LogHelper.LogMagenta("data is loading ...");
            try
            {
                using (var db = new ErpContainer())
                {
                    db.Configuration.AutoDetectChangesEnabled = false;
                    db.Configuration.ProxyCreationEnabled = false;
                    db.Configuration.LazyLoadingEnabled = false;

                    await GetChannelShiftAppData(db);
                    await GetShiftOperatorList(db);
                    await GetChannelAndGatewaypList(db);
                    await GetQueueData(db);
                    await GetAppData(db);
                    await GetQueueAgents(db);
                    await GetAgentData(db);
                    await GetShiftData(db);
                    await GetSoftphoneSettings(db);
                    await GetOperatorSettingData(db);
                    await GetAppQueueData(db);
                    await GetPersonPermissionData(db);
                    await GetExtensionSetting(db);
                    LogHelper.LogGreen("data is loaded");
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }
    }
}
