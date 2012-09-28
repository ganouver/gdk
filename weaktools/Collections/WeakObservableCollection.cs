using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace gdk.weaktools
{
    /// <summary>
    /// расширение класса ObservableCollection 
    /// дополнительным событием на слабых ссылках, дублирующим события 
    /// об изменении коллекции базового класса
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WeakObservableCollection<T> : ObservableCollection<T>, IEnumerable<T>
    {
        public WeakObservableCollection()
        {
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            RaisePropertyValueChanged(e);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            RaiseContentChanged(e);
        }

        WeakEvent _PropertyValueChanged;
        
        public IWeakEvent PropertyValueChanged {
        	get{
        		return _PropertyValueChanged ?? (_PropertyValueChanged = new WeakEvent(this));
        	}
        }
        
        private void RaisePropertyValueChanged(EventArgs args)
        {
        	if (_PropertyValueChanged != null)
        		_PropertyValueChanged.Raise(args);
        }
         
        WeakEvent _ContentChanged;

        public IWeakEvent ContentChanged 
        {
            get
            {
                return _ContentChanged ?? (_ContentChanged = new WeakEvent(this));
            }
        }

        private void RaiseContentChanged(EventArgs args)
        {
            if (_ContentChanged != null)
                _ContentChanged.Raise(args);
        }

        


    }
}
