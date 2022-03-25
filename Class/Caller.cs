using FreeswitchListenerServer.Helper;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.Class
{
    public struct CallerModel
    {
        public string CallerName { get; set; }
        public string CallerNumber { get; set; }
    }

    public class Caller
    {
        public static CallerModel GetCallerInfo(Channel channel)
        {
            return new CallerModel()
            {
                CallerName = channel.Headers[ChannelVar.CallerCallerIDNumber],
                CallerNumber = channel.Headers[ChannelVar.CallerCallerIDNumber],
            };
        }
    }

    public static class CallerStatic
    {
        public static CallerModel GetCallerInfo(this Channel channel)
        {
            return new CallerModel()
            {
                CallerName = channel.Headers[ChannelVar.CallerCallerIDNumber],
                CallerNumber = channel.Headers[ChannelVar.CallerCallerIDNumber],
            };
        }
    }
}
