using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Class;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.InboundApi.QueueApp
{
    internal abstract class AppQueueCall: ErpContainerDataHelper,IDisposable
    {
        /// <param name="channel">connected call</param>
        /// <param name="queue">connected queue in application</param>
        /// <param name="queueAgentList">all agents in the queue</param>
        public abstract Task ManageQueueCall(
            Channel channel,
            ComQueue queue,
            List<vwComQueueAgent> queueAgentList);

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Manual release of managed resources.
                }
                // Release unmanaged resources.
                _disposed = true;
            }
        }

        ~AppQueueCall() { Dispose(false); }
    }
}
