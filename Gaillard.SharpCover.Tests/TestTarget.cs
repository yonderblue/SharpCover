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

    public sealed class TestTarget
    {
        public sealed class Nested
        {
            public void Covered()
            {
                var i = 0;
                ++i;
            }
        }

        public static void UncoveredIf()
        {
            var i = 0;
            if (i == 0)
                ++i;
            else
                --i;
        }

        public static void UncoveredLeave()
        {
            var i = 0;
            ++i;
            try {
                if (i == 1)
                    throw new Exception();
                //miss a leave from this try here.
            } catch (Exception) {
                var j = 1;
                --j;
            }
        }

        public static void OffsetExcludes()
        {
            var i = 0;
            if (i == 1)
                ++i;
        }

        public static void LineExcludes()
        {
            var i = 0;
            if (i == 1)
                ++i;

            try {
                --i;
            } catch (Exception) {
                var b = false; b = !b;//will never get here
            }
        }

        //different bits c# syntax to exercise different instructions and jumps etc
        public void Covered()
        {
            var i = 0;
            ++i;

            try {
                --i;
            } finally {
                i += 2;
            }

            int j;
            goto There;
            There:
            j = 1234;
            ++j;

            foreach (var k in new [] { "boo", "foo" }) {
                k.EndsWith("oo");
            }

            for (var k = 0; k < 2; ++k)
                k += 2;

            {
                var k = 2;
                while (k != 0)
                    --k;
            }

            for (var k = 0; k < 2; ++k) {
                var b = k % 2 == 0 ? true : false;
                b &= b;
            }

            for (var k = 0; k < 4; ++k) {
                switch (k) {
                case 0:
                    break;
                case 1:
                case 2:
                    break;
                }
            }

            Func<bool> func = () => true;

            func();

            {
                var b = (object)false;
                b = (bool)b;
            }

            using (var disposable = new Disposable()) {
                var b = true;
                b = !b;
            }
        }

        private sealed class Disposable : IDisposable
        {
            public void Dispose() { }
        }

        private sealed class Constrained
        {
            public string ToString<T>(T value) where T : struct
            {
                return value.ToString();
            }
        }

        public static void Main(string[] args)
        {
            new TestTarget().Covered();
            new Nested().Covered();

            UncoveredIf();
            UncoveredLeave();
            OffsetExcludes();
            LineExcludes();

            var eventUsage = new Gaillard.SharpCover.Tests.EventUsage();
            eventUsage.TheEvent += eventUsage.EventMethod;
            eventUsage.RaiseEvent();
            eventUsage.TheEvent -= eventUsage.EventMethod;

            new Constrained().ToString(5);
        }
    }
}
