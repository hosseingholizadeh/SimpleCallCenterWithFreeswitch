using System;
using System.Collections.Generic;
using System.Linq;
using FreeswitchListenerServer.Helper;

namespace FreeswitchListenerServer.Class
{
    public class ConnectedCall
    {
        public string Uuid { get; set; }

        public Guid AppId { get; set; }

        public string ExactDesNumber { get; set; }
    }

    public class ConnectedCallHandler : ErpContainerDataHelper
    {
        public static List<ConnectedCall> ConnectedChannelList;

        public static void Add(Guid appId, string uuid)
        {
            if (ConnectedChannelList == null)
                ConnectedChannelList = new List<ConnectedCall>();

            var connectedChannel = ConnectedChannelList.FirstOrDefault(p => p.Uuid == uuid);
            if (connectedChannel == null)
            {
                ConnectedChannelList.Add(new ConnectedCall()
                {
                    AppId = appId,
                    Uuid = uuid
                });
            }
            else
            {
                connectedChannel.AppId = appId;
            }
        }

        public static void Update(string uuid, string desNumber = "")
        {
            var connectedChannel = ConnectedChannelList.FirstOrDefault(p => p.Uuid == uuid);
            if (connectedChannel != null)
            {
                connectedChannel.ExactDesNumber = desNumber;
            }
        }

        public static void Remove(string uuid)
        {
            var connectedCall = ConnectedChannelList?.FirstOrDefault(p => p.Uuid == uuid);
            if (connectedCall != null)
            {
                ConnectedChannelList.Remove(connectedCall);
                LogHelper.Log($"channel {connectedCall.Uuid} is removed from connected channel list too.");
            }
        }

        public static short GetConnectedCallAppTypeId(string uuid)
        {
            if (ConnectedChannelList != null)
            {
                var appId = ConnectedChannelList.Where(p => p.Uuid == uuid).Select(p => p.AppId).FirstOrDefault();
                var app = GetAppById(appId);
                if (app != null)
                    return app.AppTypeId;
            }
            return -1;
        }

        public static ConnectedCall GetConnectedCall(string uuid)
        {
            return ConnectedChannelList.FirstOrDefault(p => p.Uuid == uuid);
        }
    }
}
