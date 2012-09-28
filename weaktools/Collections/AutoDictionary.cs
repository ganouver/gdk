using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using gdk.quality;

namespace gdk.weaktools
{
    /// <summary>
    /// автоиндексатор коллекции
    /// для каждого узла коллекции определяется ключ и формируется словарь, в котором
    /// в качестве значения используется коллекция (список) значений из исходной коллекции
    /// </summary>
    /// <typeparam name="TK"></typeparam>
    /// <typeparam name="TV"></typeparam>
    /// <typeparam name="TS"></typeparam>
    public class AutoIndex<TK, TV>
    {
        IReadOnlyCollection<TV> _src;
        IReadOnlyCollection<TK> _keys;
        Func<TV, TK> _getkey;
        Dictionary<TK, WeakObservableCollection<TV>> _index = new Dictionary<TK, WeakObservableCollection<TV>>();
        EventReceiver<ObjectEventArgs> _adder, _remover;
        EventReceiver _updater;

        EventReceiver<ObjectEventArgs> _KAdder, _KRemover;
        EventReceiver _KUpdater;

        internal AutoIndex(IReadOnlyCollection<TV> src, Func<TV, TK> GetKeyMethod, IReadOnlyCollection<TK> KeysSource)
        {
            Contract.AllIsNotNull(src, GetKeyMethod);
            _src = src;
            _keys = KeysSource;
            _getkey = GetKeyMethod;

            _adder = new EventReceiver<ObjectEventArgs>((x, y) => this.OnAddElement((TV)y.Arg));
            _remover = new EventReceiver<ObjectEventArgs>((x, y) => this.OnRemoveElement((TV)y.Arg));
            _updater = new EventReceiver(RebuildIndex);

            _KAdder = new EventReceiver<ObjectEventArgs>((x, y) => this.OnAddKey((TK)y.Arg));
            _KRemover = new EventReceiver<ObjectEventArgs>((x, y) => this.OnRemoveKey((TK)y.Arg));
            _KUpdater = new EventReceiver(RebuildKeys);

            if (_keys != null)
                RebuildKeys();
            else
                BuildIndex();
            AdviseEvents();
        }

        public IReadOnlyCollection<TV> this[TK key]
        {
            get
            {
                WeakObservableCollection<TV> result;
                if (_index.TryGetValue(key, out result))
                    return result.ToReadOnly();

                return Empty<TV>.Get();
            }
        }

        public int Count
        {
            get
            {
                return _index.Count;
            }
        }

        private void AdviseEvents()
        {
            _src.Added.AddReceiver(_adder);
            _src.Removed.AddReceiver(_remover);
            _src.Reset.AddReceiver(_updater);

            if (_keys != null)
            {
                _keys.Added.AddReceiver(_KAdder);
                _keys.Removed.AddReceiver(_KRemover);
                _keys.Reset.AddReceiver(_KUpdater);
            }

        }

        private void RebuildKeys()
        {
            Verify.IsTrue(_keys != null);
            _index.Clear();

            foreach (var k in _keys)
                OnAddKey(k);

            BuildIndex();
        }

        private void OnAddKey(TK k)
        {
            Verify.IsTrue(_keys != null);
            if (!_index.ContainsKey(k))
                _index.Add(k, new WeakObservableCollection<TV>());
        }

        private void OnRemoveKey(TK k)
        {
            Verify.IsTrue(_keys != null);
            if (_index.ContainsKey(k))
            {
                FireRemovedKey(k);
                _index.Remove(k);
            }
        }

        private void FireRemovedKey(TK k)
        {
            if (KeyRemoved != null)
                KeyRemoved(this, new KeyRemovedEventArgs<TK, TV> { Key = k, Values = _index[k] });
        }

        private void RebuildIndex()
        {
            if (_keys == null)
            {
                _index.Clear();
            }
            else
            {
                foreach (var c in _index.Values)
                    c.Clear();
            }

            BuildIndex();
        }


        private void BuildIndex()
        {
            foreach (var item in _src)
                OnAddElement(item);
        }

        private void OnAddElement(TV item)
        {
            var k = _getkey(item);
            if (!_index.ContainsKey(k))
            {
                _index.Add(k, new WeakObservableCollection<TV>());
            }

            _index[k].Add(item);
        }

        private void OnRemoveElement(TV item)
        {
            var k = _getkey(item);
            if (_index.ContainsKey(k))
            {
                var x = _index[k];
                //если ключи управляются автоматически 
                //и это единственный оставшийся элемент то удаляем ключ целиком
                if (_keys == null && x.Count == 1 && x[0].Equals(item))
                {
                    FireRemovedKey(k); 
                    _index.Remove(k);
                }
                else //если ключ остается - то из него удаляем элемент
                    x.Remove(item);
            }
        }

        public event EventHandler<KeyRemovedEventArgs<TK, TV>> KeyRemoved;

        /// <summary>
        /// перечисление всех значений индекса
        /// </summary>
        public IEnumerable<TV> Values
        {
            get
            {
                return _index.Values.SelectMany(x => x).Distinct();
            }
        }
    }

    public class KeyRemovedEventArgs<TK, TV> : EventArgs
    {
        public TK Key { get; internal set; }
        public WeakObservableCollection<TV> Values { get; internal set; }
    }
}
