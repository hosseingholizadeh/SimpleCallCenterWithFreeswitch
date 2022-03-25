using System;
using System.Configuration;
using System.Threading.Tasks;
using NEventSocket;
using NEventSocket.FreeSwitch;

namespace FreeswitchListenerServer.FreeswitchApiClass
{
    public static class FreeswitchApi
    {
        private static InboundSocket _client;

        private static string inboundSocketIp = "127.0.0.1";

        private static readonly string InbooundSocketPass = ConfigurationManager.AppSettings["EventSocketPass"];

        private static int port = 8021;

        /// <summary>
        /// Freeswitch Command : ReloadXml
        /// </summary>
        /// <returns></returns>
        public static async Task ReloadXml()
        {
            _client = await InboundSocket.Connect(inboundSocketIp,port,InbooundSocketPass);
            await _client.SendApi("reloadxml");
            await _client.Exit();
        }

        /// <summary>
        /// Freeswitch Command : ReloadAcl
        /// </summary>
        /// <returns></returns>
        public static async Task ReloadAcl()
        {
            _client = await InboundSocket.Connect(inboundSocketIp,port,InbooundSocketPass);
            await _client.SendApi("reloadacl");
            await _client.Exit();
        }

        /// <summary>
        /// Freeswitch Command : reload mod_sofia
        /// </summary>
        /// <returns></returns>
        public static async Task ReloadModSofia()
        {
            _client = await InboundSocket.Connect(inboundSocketIp,port,InbooundSocketPass);
            await _client.SendApi("reload mod_sofia");
            await _client.Exit();
        }
        
        /// <summary>
        /// Freeswitch Command : show channels
        /// </summary>
        /// <returns></returns>
        public static async Task<string> ShowChannels()
        {
            _client = await InboundSocket.Connect(inboundSocketIp,port,InbooundSocketPass);
            var result = await _client.SendApi("show channels");
            await _client.Exit();
            return result.BodyText;
        }
        
        /// <summary>
        /// Freeswitch Command : show channels
        /// </summary>
        /// <returns></returns>
        public static async Task<string> ShowCalls()
        {
            _client = await InboundSocket.Connect(inboundSocketIp,port,InbooundSocketPass);
            var result = await _client.SendApi("show calls");
            Console.WriteLine(result);
            await _client.Exit();
            return result.BodyText;
        }
        
        /// <summary>
        /// Freeswitch Command : show channels count
        /// </summary>
        /// <returns></returns>
        public static async Task<string> ShowChannelsCount()
        {
            _client = await InboundSocket.Connect(inboundSocketIp,port,InbooundSocketPass);
            var result = await _client.SendApi("show channels count");
            await _client.Exit();
            return result.BodyText;
        }

        /// <summary>
        /// Freeswitch Command : show registrations
        /// </summary>
        /// <returns></returns>
        public static async Task<string> ShowRegistrations()
        {
            _client = await InboundSocket.Connect(inboundSocketIp,port,InbooundSocketPass);
            var result = await _client.SendApi("show registrations");
            await _client.Exit();
            return result.BodyText;
        }

        /// <summary>
        /// Freeswitch Command : show registrations
        /// </summary>
        /// <returns></returns>
        public static async Task<string> UserIsBusy()
        {
            _client = await InboundSocket.Connect(inboundSocketIp,port,InbooundSocketPass);
            var result = await _client.SendApi("limit_usage(db time_spent in_bed)");
            await _client.Exit();
            return result.BodyText;
        }
    }
}
