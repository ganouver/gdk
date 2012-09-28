using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using gdk.quality;

namespace gdk.weaktools
{
    class UnionCollection<T1, T2> : ProtectedCollection<T2>
        where T1 : class
        where T2 : class
    {
        EventReceiver _addSource, _removeSource, _resetSource;
        EventReceiver _addItem, _removeItem, _resetItems;
        IReadOnlyCollection<T1> _src;
        Expander<T1, T2> _expander;
        Dictionary<T1, IReadOnlyCollection<T2>> _sources = new Dictionary<T1, IReadOnlyCollection<T2>>();

        public UnionCollection(IReadOnlyCollection<T1> source, Expander<T1, T2> expander)
        {
            Contract.IsNotNull(source);
            Contract.IsNotNull(expander);

            _expander = expander;
            _src = source;

            _addSource = new EventReceiver<ObjectEventArgs>((x, y) => this.OnAdd((T1)y.Arg));
            _removeSource = new EventReceiver<ObjectEventArgs>((x, y) => OnRemove((T1)y.Arg));
            _resetSource = new EventReceiver((x, y) => ReloadAll());

            _addItem = new EventReceiver<ObjectEventArgs>((x, y) => this.OnAddItem((T2)y.Arg));
            _removeItem = new EventReceiver<ObjectEventArgs>((x, y) => this.OnRemoveItem((T2)y.Arg));
            _resetItems = new EventReceiver((x, y) => ResetSource((IReadOnlyCollection<T2>)x));

            _src.Added.AddReceiver(_addSource);
            _src.Removed.AddReceiver(_removeSource);
            _src.Reset.AddReceiver(_resetSource);

            _resetSource.Ping();
        }

        private void ResetSource(IReadOnlyCollection<T2> reseted)
        {
            var validValues = _sources.Values.Except(reseted.AsOne()).SelectMany(x => x);
            var values2Remove = Collection.Except(validValues).ToArray();

            foreach (var val2rem in values2Remove)
                OnRemoveItem(val2rem);

            foreach (var newval in reseted)
                OnAddItem(newval);
        }

        private void OnRemoveItem(T2 p)
        {
            if (_sources.Values.All(x => !x.Contains(p)))
                Collection.Remove(p);
        }

        private void OnAddItem(T2 p)
        {
            if (!Collection.Contains(p))
                Collection.Add(p);
        }

        private void ReloadAll()
        {
            foreach (var key in _sources.Keys.ToArray())
                OnRemove(key);

            foreach (var srcItem in _src)
                OnAdd(srcItem);
        }

        private void OnRemove(T1 p)
        {
            var source = _sources[p];
            source.Added.RemoveReceiver(_addItem);
            source.Removed.RemoveReceiver(_removeItem);
            source.Reset.RemoveReceiver(_resetItems);

            _sources.Remove(p);

            foreach (var item in source)
                OnRemoveItem(item);

        }

        private void OnAdd(T1 p)
        {
            Contract.IsNotNull(p);
            var newSource = _expander(p);
            _sources.Add(p, newSource);

            newSource.Added.AddReceiver(_addItem);
            newSource.Removed.AddReceiver(_removeItem);
            newSource.Reset.AddReceiver(_resetItems);

            foreach (var item in newSource)
                OnAddItem(item);

        }

    }
}
