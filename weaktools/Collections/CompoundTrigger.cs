using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gdk.weaktools
{
    /// <summary>
    /// комбинированный триггер - срабатывает, 
    /// когда срабатывает триггер у любого из элементов коллекции
    /// </summary>
    public class CompoundWeakEvent : IWeakEvent
    {
        private IEventReceiver OnAdd;
        private IEventReceiver OnRemove;
        private EventReceiver trigger;
        private WeakEvent _evt;

        private CompoundWeakEvent()
        {
            _evt = new WeakEvent(this);
            trigger = new EventReceiver((x, y) => _evt.Raise(y));
        }

        public static CompoundWeakEvent Create<T>(IReadOnlyCollection<T> source, Func<T, IWeakEvent> getter)
        {
            var t = new CompoundWeakEvent();

            t.OnAdd = new EventReceiver<ObjectEventArgs>((x, y) => t.Advise(getter((T)y.Arg)));
            t.OnRemove = new EventReceiver<ObjectEventArgs>((x, y) => t.Unadvise(getter((T)y.Arg)));

            source.Added.AddReceiver(t.OnAdd);
            source.Removed.AddReceiver(t.OnRemove);
            foreach (var x in source)
                t.Advise(getter(x));

            return t;
        }

        private void Advise(IWeakEvent iWeakEvent)
        {
            iWeakEvent.AddReceiver(trigger);
        }

        private void Unadvise(IWeakEvent iWeakEvent)
        {
            iWeakEvent.RemoveReceiver(trigger);
        }

        #region IWeakEvent Members

        public void AddReceiver(IEventReceiver r)
        {
            _evt.AddReceiver(r);
        }

        public void RemoveReceiver(IEventReceiver r)
        {
            _evt.RemoveReceiver(r);
        }

        #endregion

    }
}
