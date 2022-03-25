using System;
using System.Collections.Generic;
namespace FreeswitchListenerServer.ViewModels
{
    public class ExtSettingVm
    {
        public Guid SettingId { get; set; }
        public List<ExtLimitSettingVm> LimitList { get; set; }
        public List<GroupShiftVm> GroupShiftList { get; set; }
    }

    public class ExtShiftSettingVm
    {
        public ExtLimitSettingVm Limit { get; set; }
        public GroupShiftVm GroupShiftList { get; set; }
    }

    public class ExtLimitSettingVm
    {
        public Guid ShiftId { get; set; }
        public short LimitTypeId { get; set; }
        public List<ExtLimitDetailVm> LimitDetailList { get; set; }
    }

    public struct ExtLimitDetailVm
    {
        public short LineTypeId { get; set; }
        public string Number { get; set; }
    }

    public class AgentSettingVm
    {
        public string VoipNumber { get; set; }
        public Guid SettingId { get; set; }
    }

    public struct GroupShiftVm
    {
        public Guid ShiftId { get; set; }
        public List<Guid> GroupIdList { get; set; }
    }

    public struct ChannelGroupVm
    {
        public Guid GroupId { get; set; }
        public List<string> ChannelNumberList { get; set; }
    }
}
