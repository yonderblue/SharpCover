using System;

namespace Gaillard.SharpCover.Tests
{
    public interface IEvent
    {
        event EventHandler TheEvent;
    }

    public class EventUsage : IEvent
    {
        public event EventHandler TheEvent;

        public void EventMethod(object sender, EventArgs e)
        {
            var i = 0;
            i += 1;
        }

        public void RaiseEvent()
        {
            TheEvent(null, null);
        }
    }
}
