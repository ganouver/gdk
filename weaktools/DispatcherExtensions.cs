using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using gdk.quality;

namespace gdk.weaktools
{
    public static class DispatcherExtensions
    {
        [ThreadStatic]
        static bool isDelayRunning = false;

        [ThreadStatic]
        static Queue<Action> _queue;

        private static Queue<Action> Actions
        {
            get
            {
                return _queue ?? (_queue = new Queue<Action>());
            }
        }

        static void ProcessPortion(Dispatcher d)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (Actions.Count > 0 && watch.ElapsedMilliseconds < 200)
            {
                Actions.Dequeue()();
            };

            if (Actions.Count == 0)
                isDelayRunning = false;
            else
                d.BeginInvoke(DispatcherPriority.Background, new Action<Dispatcher>(ProcessPortion), d);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static void Delay(this Dispatcher d, Action a)
        {
            Contract.AllIsNotNull(d, a);

            Actions.Enqueue(a);

            if (!isDelayRunning)
            {
                isDelayRunning = true;
                d.BeginInvoke(DispatcherPriority.Background, new Action<Dispatcher>(ProcessPortion), d);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static void Delay<T>(this Dispatcher dispatcher, T argument, Action<T> action)
        {
            Contract.IsNotNull(dispatcher);
            dispatcher.BeginInvoke(DispatcherPriority.DataBind, action, argument);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static void ProcessDelayed(this Dispatcher dispatcher)
        {
            Contract.IsNotNull(dispatcher);
            dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() => { }));
        }
    }
}
