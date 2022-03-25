using System;
using System.Threading.Tasks;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Class;
using NEventSocket.Channels;
using System.Threading;

namespace FreeswitchListenerServer.InboundApi
{
    public abstract class CallHandler: ErpContainerDataHelper,IDisposable
    {
        public abstract Task HandleCall(Channel channel, ComFreeswitchApp application,CancellationToken ct);
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
        

        ~CallHandler() { Dispose(false); }

    }

}
