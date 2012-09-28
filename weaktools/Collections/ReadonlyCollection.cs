using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections;
using gdk.weaktools;
using gdk.quality;

namespace gdk.weaktools
{
    /// <summary>
    /// интерфейс событий об изменении коллекций, реализованный на слабых ссылках
    /// Позволяет подписываться на события об изменении коллекций без удержания 
    /// ссылки на подписчика в объекте-источнике
    /// </summary>
    public interface IWeakCollectionEvents
    {
        /// <summary>
        /// объект добавлен в коллекцию
        /// </summary>
        IWeakEvent Added { get; }
        /// <summary>
        /// объект удален из коллекции
        /// </summary>
        IWeakEvent Removed { get; }
        /// <summary>
        /// содержимое коллекции изменено полностью
        /// </summary>
        IWeakEvent Reset { get; }
        /// <summary>
        /// содержимое коллекции было изменено. 
        /// Это дублирующее событие, генерируется после любого другого
        /// </summary>
        IWeakEvent Changed { get; }
    }


    /// <summary>
    /// интерфейс коллекции, доступно только для чтения и реализующей интерфейсы событий, 
    /// уведомляющих об изменениях в коллекции
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadOnlyCollection<T> : IEnumerable<T>, IWeakCollectionEvents, INotifyCollectionChanged, INotifyPropertyChanged
    {
        // Summary:
        //     Gets the number of elements contained in the System.Collections.Generic.ICollection<T>.
        //
        // Returns:
        //     The number of elements contained in the System.Collections.Generic.ICollection<T>.
        int Count { get; }

        //
        // Summary:
        //     Determines whether the System.Collections.Generic.ICollection<T> contains
        //     a specific value.
        //
        // Parameters:
        //   item:
        //     The object to locate in the System.Collections.Generic.ICollection<T>.
        //
        // Returns:
        //     true if item is found in the System.Collections.Generic.ICollection<T>; otherwise,
        //     false.
        bool Contains(T item);

        /// <summary>
        /// index property
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        T this[int index] { get; }
    }

    public static class Empty<T>
    {
        private static IReadOnlyCollection<T> _empty;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public static IReadOnlyCollection<T> Get()
        {
            return _empty ?? (_empty = new WeakObservableCollection<T>().ToReadOnly());
        }
    }

    /// <summary>
    /// коллекция только для чтения, являющаяся оберткой над другой коллекцией
    /// дублирует уведомления об изменении исходной коллекции
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WrapperCollection<T> : NotifyPropertyChanged, IReadOnlyCollection<T>
    {
        WeakObservableCollection<T> _coll;
        EventReceiver<NotifyCollectionChangedEventArgs> _updater;
        EventReceiver<PropertyChangedEventArgs> _propertyUpdater;
        public WrapperCollection(WeakObservableCollection<T> src)
        {
            _coll = src;
            _updater = new EventReceiver<NotifyCollectionChangedEventArgs>(
                coll_CollectionChanged);

            _propertyUpdater = new EventReceiver<PropertyChangedEventArgs>(
                coll_PropertyChanged);

            _coll.ContentChanged.AddReceiver(_updater);
            _coll.PropertyValueChanged.AddReceiver(_propertyUpdater);
        }

        void coll_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Modified(e.PropertyName);
        }

        void coll_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            if (CollectionChanged != null)
                CollectionChanged(this, e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (_added != null)
                        foreach (var x in e.NewItems)
                            _added.Raise(x);
                    if (_changed != null)
                        _changed.Raise();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (_removed != null)
                        foreach (var x in e.OldItems)
                            _removed.Raise(x);
                    if (_changed != null)
                        _changed.Raise();
                    break;
                case NotifyCollectionChangedAction.Reset:
                    if (_reset != null)
                        _reset.Raise();
                    if (_changed != null)
                        _changed.Raise();
                    break;
            }
        }

        #region ICollection<T> Members

        public bool Contains(T item)
        {
            return _coll.Contains(item);
        }

        public int Count
        {
            get { return _coll.Count; }
        }

        #endregion

        #region IEnumerable<NavigationItem> Members

        public IEnumerator<T> GetEnumerator()
        {
            return _coll.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion    }

        #region IReadonlyCollection<T> Members

        WeakEvent _added;
        WeakEvent _removed;
        WeakEvent _reset;
        WeakEvent _changed;

        public IWeakEvent Added
        {
            get { return _added ?? (_added = new WeakEvent(this)); }
        }

        public IWeakEvent Removed
        {
            get { return _removed ?? (_removed = new WeakEvent(this)); }
        }

        public IWeakEvent Reset
        {
            get { return _reset ?? (_reset = new WeakEvent(this)); }
        }

        public IWeakEvent Changed
        {
            get { return _changed ?? (_changed = new WeakEvent(this)); }
        }

        public virtual T this[int index]
        {
            get
            {
                return _coll[index];
            }
        }

        #endregion

    }

    /// <summary>
    /// коллекция, доступная на изменение только из наследных классов, 
    /// остальные могут с ней общаться только на чтение
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ProtectedCollection<T> : WrapperCollection<T>
    {
        WeakObservableCollection<T> _myColl;

        protected ProtectedCollection()
            : this(new WeakObservableCollection<T>())
        {

        }

        private ProtectedCollection(WeakObservableCollection<T> myColl)
            : base(myColl)
        {
            _myColl = myColl;
        }

        protected WeakObservableCollection<T> Collection
        {
            get
            {
                return _myColl;
            }
        }
    }

    /// <summary>
    /// коллекция, которая автоматически создает новую коллекцию узлов на основе существующей
    /// используя указанные правила фильтрации узлов, 
    /// и автоматически отсылая уведомления об изменении коллекции
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class AutoCollection<T> : ProtectedCollection<T>
    {
        Predicate<T> _f;
        Comparison<T> _comparer;

        bool _IsDelaySort;
        bool _IsDelaySortRunning;

        IReadOnlyCollection<T> _c;
        List<Func<T, IWeakEvent>> _triggers = new List<Func<T, IWeakEvent>>();

        EventReceiver _collReset;
        EventReceiver _collAdd;
        EventReceiver _collRemove;

        EventReceiver _updateOneElm;
        EventReceiver _updateAllCollection;

        internal AutoCollection(IReadOnlyCollection<T> srcColl)
        {
            Contract.IsNotNull(srcColl);

            _IsDelaySort = _IsDelaySortRunning = false;

            _collReset = new EventReceiver((x, y) => RefillColl());
            _collAdd = new EventReceiver<ObjectEventArgs>((x, y) => AddElm((T)(y as ObjectEventArgs).Arg, true));
            _collRemove = new EventReceiver((x, y) => RemoveElm((T)(y as ObjectEventArgs).Arg));

            _updateOneElm = new EventReceiver((x, y) => { if (x is T) Refilter((T)x); else Refresh(); });
            _updateAllCollection = new EventReceiver((x, y) => Refresh());

            _c = srcColl;
            _f = x => true;

            _c.Added.AddReceiver(_collAdd);
            _c.Removed.AddReceiver(_collRemove);
            _c.Reset.AddReceiver(_collReset);

            RefillColl();

        }

        private void Refilter(T x)
        {
            bool f = _f(x);
            if (f && !Collection.Contains(x))
                Collection.Add(x);
            else if (!f && Collection.Contains(x))
                Collection.Remove(x);
            Resort();
        }

        private void RemoveElm(T x)
        {
            foreach (var trig in _triggers.Select(t => t(x)).Where(t => t != null))
                trig.RemoveReceiver(_updateOneElm);

            if (Collection.Contains(x))
                Collection.Remove(x);
        }

        private void AddElm(T x, bool callSort)
        {
            foreach (var trig in _triggers.Select(t => t(x)).Where(t => t != null))
                trig.AddReceiver(_updateOneElm);

            if (_f(x))
                Collection.Add(x);

            if (callSort)
                Resort();
        }

        private void RefillColl()
        {
            foreach (var z in Collection.ToArray())
                RemoveElm(z);
            foreach (var x in _c)
                AddElm(x, false);

            Resort();
        }

        public void Refresh()
        {
            foreach (var x in _c)
            {
                Refilter(x);
            }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public AutoCollection<T> UpdateOn(Func<T, IWeakEvent> trigger)
        {
            Contract.IsNotNull(trigger);
            _triggers.Add(trigger);
            foreach (var x in _c)
            {
                var t = trigger(x);
                if (t != null)
                    t.AddReceiver(_updateOneElm);
            }
            return this;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public AutoCollection<T> UpdateOn(IWeakEvent trigger)
        {
            Contract.IsNotNull(trigger);
            trigger.AddReceiver(_updateAllCollection);
            return this;
        }

        public AutoCollection<T> FilteredBy(Predicate<T> predicate)
        {
            _f = predicate;
            Refresh();

            return this;
        }

        public AutoCollection<T> SortedOn(Comparison<T> comparer)
        {
            if (_IsDelaySortRunning)
                throw new InvalidOperationException();

            _comparer = comparer;
            _IsDelaySort = false;

            Resort();

            return this;
        }

        public AutoCollection<T> DelaySortedOn(Comparison<T> comparer)
        {
            if (_IsDelaySortRunning)
                throw new InvalidOperationException();

            _comparer = comparer;
            _IsDelaySort = true;

            Resort();

            return this;
        }

        /// <summary>
        /// выполняет пересортировку внутренней коллекции
        /// </summary>
        private void Resort()
        {
            if (_comparer == null)
                return;

            if (!_IsDelaySort)
                Collection.SortInPlace(_comparer);
            else if (!_IsDelaySortRunning)
            {
                _IsDelaySortRunning = true;
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Delay(() =>
                    {
                        Collection.SortInPlace(_comparer);
                        _IsDelaySortRunning = false;
                    });
            }
        }

    }

}
