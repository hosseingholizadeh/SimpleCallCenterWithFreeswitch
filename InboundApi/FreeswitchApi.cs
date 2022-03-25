using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using NEventSocket;
using NEventSocket.FreeSwitch;

namespace EtraabERP.Freeswitch.ApiClass
{
    public static class FreeswitchApi
    {
        private static InboundSocket client;
        private static string inboundSocketIp = "127.0.0.1", inbooundSocketPass = "hx4"; 
        private static int port = 8021;

        /// <summary>
        /// Freeswitch Command : ReloadXml
        /// </summary>
        /// <returns></returns>
        public static async Task ReloadXml()
        {
            client = await InboundSocket.Connect(inboundSocketIp,port,inbooundSocketPass);
            await client.SendApi("reloadxml");
            await client.Exit();
        }

        /// <summary>
        /// Freeswitch Command : ReloadAcl
        /// </summary>
        /// <returns></returns>
        public static async Task ReloadAcl()
        {
            client = await InboundSocket.Connect(inboundSocketIp,port,inbooundSocketPass);
            await client.SendApi("reloadacl");
            await client.Exit();
        }

        /// <summary>
        /// Freeswitch Command : reload mod_sofia
        /// </summary>
        /// <returns></returns>
        public static async Task ReloadModSofia()
        {
            client = await InboundSocket.Connect(inboundSocketIp,port,inbooundSocketPass);
            await client.SendApi("reload mod_sofia");
            await client.Exit();
        }
        
        /// <summary>
        /// Freeswitch Command : show channels
        /// </summary>
        /// <returns></returns>
        public static async Task<string> ShowChannels()
        {
            client = await InboundSocket.Connect(inboundSocketIp,port,inbooundSocketPass);
            var result = await client.SendApi("show channels");
            await client.Exit();
            return result.BodyText;
        }
        
        /// <summary>
        /// Freeswitch Command : show channels count
        /// </summary>
        /// <returns></returns>
        public static async Task<string> ShowChannelsCount()
        {
            client = await InboundSocket.Connect(inboundSocketIp,port,inbooundSocketPass);
            var result = await client.SendApi("show channels count");
            await client.Exit();
            return result.BodyText;
        }

        /// <summary>
        /// Freeswitch Command : show registrations
        /// </summary>
        /// <returns></returns>
        public static async Task<string> ShowRegistrations()
        {
            client = await InboundSocket.Connect(inboundSocketIp,port,inbooundSocketPass);
            var result = await client.SendApi("show registrations");
            await client.Exit();
            return result.BodyText;
        }
    }
}
