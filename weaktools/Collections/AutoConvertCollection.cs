using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using gdk.quality;

namespace gdk.weaktools
{
    /// <summary>
    /// автоматически конвертируемая коллекция
    /// </summary>
    class AutoConvertCollection<T1, T2> : ProtectedCollection<T2> 
        where T1 : class
        where T2 : class
    {
        Func<T1, T2> _converter;
        IReadOnlyCollection<T1> _src;
        Dictionary<T1, T2> _addedValues = new Dictionary<T1, T2>();
        bool _IsDistinct;

        EventReceiver _adder, _remover, _reset;

        public AutoConvertCollection(IReadOnlyCollection<T1> source, Func<T1, T2> converter, bool IsDistinct)
        {
            Contract.IsNotNull(source);
            Contract.IsNotNull(converter);
            _converter = converter;
            _src = source;
            _IsDistinct = IsDistinct;

            ReloadAll();

            _adder = new EventReceiver<ObjectEventArgs>((x, y) => this.Add((T1)y.Arg));
            _remover = new EventReceiver<ObjectEventArgs>((x, y) => this.Remove((T1)y.Arg));
            _reset = new EventReceiver((x, y) => this.ReloadAll());

            _src.Added.AddReceiver(_adder);
            _src.Removed.AddReceiver(_remover);
            _src.Reset.AddReceiver(_reset);
        }



        private void ReloadAll()
        {
            Collection.Clear();
            _addedValues.Clear();

            foreach (var item in _src)
                Add(item);
        }

        private void Remove(T1 item)
        {
            Contract.IsNotNull(item);
            T2 value;
            if (_addedValues.TryGetValue(item, out value))
            {
                _addedValues.Remove(item);
                if (!_IsDistinct || !_addedValues.Values.Contains(value))
                    Collection.Remove(value);

            }
        }

        private void Add(T1 item)
        {
            Contract.IsNotNull(item);

            if (!_addedValues.ContainsKey(item))
            {
                var value = _converter(item);
                _addedValues.Add(item, value);
                if (!_IsDistinct || !Collection.Contains(value))
                    Collection.Add(value);
            }
        }
    }
}
