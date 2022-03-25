using System;
using System.Threading;
using System.Threading.Tasks;
using NEventSocket.Channels;

namespace FreeswitchListenerServer.OutboundApi.NumberingPlan
{
    public abstract class NumberingPlanOperator : IDisposable
    {
        internal NumberingPlanOperator(string limitNumber)
        {
            this.LimitNumber = limitNumber;
        }

        public string LimitNumber { get; set; }

        public abstract Task ManageByNumberingPlan(Channel channel,CancellationToken ct);

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

        ~NumberingPlanOperator() { Dispose(false); }
    }
}
