using System;
using System.Collections.Generic;
using System.Linq;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Helper;

namespace FreeswitchListenerServer.Class
{
    public class ShiftOperatorHandler : ErpContainerDataHelper
    {
        public static List<vwComOperatorSetting> GetRelatedShiftOpertors()
        {
            var shiftId = GetShiftIdFoNow();
            if (!shiftId.HasGuidValue())
                return OperatorSettingList;

            var shiftOperatorIdList = ShiftOperatorList.Where(p => p.ComShiftPID == shiftId)
                .Select(p => p.ComOperatorSettingPID).ToList();

            return OperatorSettingList
                .Where(p => shiftOperatorIdList.Contains(p.ComOperatorSettingPID)).ToList();
        }

        private static Guid GetShiftIdFoNow()
        {
            var dayOfWeek = (short)DateTime.Now.DayOfWeek;
            return ShiftList.Where(p => (p.StartTime <= DateTime.Now.TimeOfDay &&
                                         p.EndTime >= DateTime.Now.TimeOfDay &&
                                         (bool)p.IsEnabled)).Where(p =>
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
    }
}
