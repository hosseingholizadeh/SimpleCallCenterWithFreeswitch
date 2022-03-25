using FreeswitchListenerServer.FreeswitchApiClass;
using FreeswitchListenerServer.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FreeswitchListenerServer.Class
{
    internal class FreeswitchWorker
    {
        internal static string FsSoundDirectory = @"C:\Program Files\FreeSWITCH\sounds\en\us\callie\";
        /// <summary>
        /// this only works for sip registrations
        /// </summary>
        /// <returns></returns>
        public static async Task<List<string>> GetRegisteredUserList()
        {
            var registeredUserListStr = await FreeswitchApi.ShowRegistrations();
            var registeredUserList = FreeswitchHelper.GetRealTimeVars(registeredUserListStr);

            var registeredVoipNumbers = new List<string>();
            //get registered users voip number
            var doBreak = false;
            registeredUserList.CustomeForEach(ref doBreak,(regUser,index) =>
            {
                var voipNumber = regUser.Where(k => k.Key == "reg_user").Select(p => p.Value)
                    .FirstOrDefault();

                if (voipNumber != null)
                {
                    registeredVoipNumbers.Add(voipNumber);
                }
            });

            return registeredVoipNumbers;
        }

        /// <summary>
        /// if the extension has any active call
        /// </summary>
        /// <param name="voipNumber"></param>
        /// <returns></returns>
        public static bool ExtensionIsBusy(string voipNumber)
        {
            // var channelListStr = await FreeswitchApi.ShowCalls();

            //var channelList = FreeswitchHelper.GetRealTimeVars(channelListStr);
            //channelList.RemoveAll(p =>
            //    p.Where(c => c.Key == "callstate").Select(c => c.Value).Any(c => c != "ACTIVE"));

            //return channelList.Select(t => t.Where(k => k.Key == "initial_dest" ||k.Key == "callee_num" || k.Key == "initial_cid_num" || k.Key == "cid_num")
            //        .Select(p => p.Value)
            //        .ToList())
            //    .Any(numberList => numberList.Any(p => p == voipNumber));
            return BusyLine.IsExists(voipNumber);
        }

        /// <summary>
        /// will be used just for sip accounts not verto 
        /// </summary>
        /// <param name="voipNumber"></param>
        /// <returns></returns>
        public static async Task<bool> ExtensionIsRegistered(string voipNumber)
        {
            var registeredUserList = await GetRegisteredUserList();
            return registeredUserList.Any(p=>p == voipNumber);
        }
    }
}
