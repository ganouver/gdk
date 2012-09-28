using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using gdk.quality;

namespace gdk.weaktools
{
    /// <summary>
    /// пример класса, инициирующего событие
    /// </summary>
    public class sampleEventSource
    {
        WeakEvent _event;
        public IWeakEvent MyEventSample
        {
            get
            {
                return _event ?? (_event = new WeakEvent(this));
            }
        }

        public void Tick()
        {
            if (_event != null)
                _event.Raise();
        }

        public IEnumerable<IEventReceiver> Receivers
        {
            get
            {
                if (_event != null)
                    return _event.Receivers;

                return new IEventReceiver[0];
            }
        }
    }

    /// <summary>
    /// пример класса, обрабатывающего событие
    /// </summary>
    public class sampleListener
    {
        EventReceiver _sampleEventReceiver;
        public sampleListener()
        {
            _sampleEventReceiver = new EventReceiver(OnSampleEvent);
        }

        /// <summary>
        /// счетчик количества полученных событий
        /// </summary>
        public int EventsReceived
        {
            get; private set;
        }

        /// <summary>
        /// сброс счетчика количества полученных событий
        /// </summary>
        public void Reset()
        {
            EventsReceived = 0;
        }

        private void OnSampleEvent(object sender, EventArgs args)
        {
            EventsReceived++;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public void AdviseTo(sampleEventSource src)
        {
            Contract.IsNotNull(src);
            src.MyEventSample.AddReceiver(_sampleEventReceiver);
        }
    }

    /// <summary>
    /// интерфейс объекта события, реализующего управления подписчиками с использованием слабых ссылок
    /// </summary>
    public interface IWeakEvent
    {
        /// <summary>
        /// добавляет получателя события
        /// </summary>
        /// <param name="r"></param>
        void AddReceiver(IEventReceiver r);

        /// <summary>
        /// удаляет получателя события
        /// </summary>
        /// <param name="r"></param>
        void RemoveReceiver(IEventReceiver r);

    }

    /// <summary>
    /// интерфейс получается уведомления о наступлении события,
    /// на которое былу установлена подписка с помощью IWeakEvent.AddReceiver
    /// </summary>
    public interface IEventReceiver
    {
        void OnEvent(object sender, EventArgs args);
    }

    public class ObjectEventArgs : EventArgs
    {
        public Object Arg { get; private set; }

        public ObjectEventArgs(object oarg)
        {
            Arg = oarg;
        }
    }

    /// <summary>
    /// реализация объекта-события, уведомляющего по слабым ссылкам
    /// </summary>
    public class WeakEvent : IWeakEvent
    {
        List<WeakReference> _targets = new List<WeakReference>();
        object _source;

        public WeakEvent(object source)
        {
            _source = source;
        }
#pragma warning disable 

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        public void Raise()
        {
            Raise(EventArgs.Empty);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        public void Raise(object arg)
        {
            Raise(new ObjectEventArgs(arg));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        public void Raise(EventArgs args)
        {
            bool needClean = false;
            foreach (var w in _targets.ToList())
            {
                if (w.IsAlive)
                {
                    ((IEventReceiver)w.Target).OnEvent(_source, args);
                }
                else
                    needClean = true;
            }
            if (needClean)
            {
                foreach (var w in _targets.Where(x => !x.IsAlive).ToList())
                {
                    _targets.Remove(w);
                }
            }
        }

        public IEnumerable<IEventReceiver> Receivers
        {
            get
            {
                return (from t in _targets where t.IsAlive select (IEventReceiver)t.Target).ToList();
            }
        }

        #region IWeakEvent Members

        public void AddReceiver(IEventReceiver r)
        {
            Contract.IsNotNull(r);
 //           if(!_targets.Any(wr => wr.IsAlive && wr.Target.Equals(r)))
                _targets.Add(new WeakReference(r));

        }

        public void RemoveReceiver(IEventReceiver r)
        {
            var w = _targets.FirstOrDefault(x => x.IsAlive && x.Target.Equals(r));
            if (w != null)
                _targets.Remove(w);
        }

        #endregion
    }

    /// <summary>
    /// реализация объекта-приемника события, который при возникновении события вызывает
    /// правильный делегат
    /// </summary>
    public class EventReceiver : IEventReceiver
    {
        EventHandler _handler;
        public EventReceiver(EventHandler targetCall)
        {
            _handler = targetCall;
        }

        public EventReceiver(Action targetCall)
        {
            _handler = new EventHandler((x,y) => targetCall());
        }
        /// <summary>
        /// имитация вызова события с примитивными параметрами
        /// </summary>
        public void Ping()
        {
            OnEvent(null, EventArgs.Empty);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public void AdviseTo(params IWeakEvent[] args)
        {
            Contract.IsNotNull(args);
            foreach (var evt in args)
                evt.AddReceiver(this);
        }

        #region IEventReceiver Members

        public void OnEvent(object sender, EventArgs args)
        {
            _handler(sender, args);
        }

        #endregion
    }

    public class EventReceiver<T> : EventReceiver where T:EventArgs
    {
        public EventReceiver(EventHandler<T> targetCall)
            :base((x , y) => targetCall(x, (T)y))
        {

        }
    }
}
