using System;
using System.Collections.Generic;
using System.ComponentModel;
using gdk.quality;

namespace gdk.weaktools
{
    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
 
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// отсылает уведомление об изменении свойства подписчикам
        /// </summary>
        /// <param name="propertyName">Название измененного свойства</param>
        protected virtual void Modified(string propertyName)
        {
            if (_triggers != null)
                _triggers.Raise(propertyName);

            if (!String.IsNullOrEmpty(propertyName))
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

            }
        }

        TriggersCollection _triggers;

        public IPropertyChangedTriggers Triggers
        {
            get
            {
                return _triggers ?? (_triggers = new TriggersCollection(this));
            }
        }

        class TriggersCollection : IPropertyChangedTriggers
        {
            object _owner;
            WeakEvent _anyChange;
            Dictionary<string, WeakEvent> _propertyEvents = new Dictionary<string, WeakEvent>();

            public TriggersCollection(object owner)
            {
                _owner = owner;
            }

            public void Raise(string PropertyName)
            {
                WeakEvent evt;
                if (!string.IsNullOrEmpty(PropertyName) && 
                    _propertyEvents.TryGetValue(PropertyName, out evt))
                    evt.Raise();

                if (_anyChange != null)
                    _anyChange.Raise(new PropertyChangedEventArgs(PropertyName));
            }

            #region IPropertyChangedTriggers Members

            public IWeakEvent anyChange
            {
                get
                {
                    return _anyChange ?? (_anyChange = new WeakEvent(_owner));
                }
            }

            public IWeakEvent this[string PropertyName]
            {
                get
                {
                    WeakEvent evt;
                    if (_propertyEvents.TryGetValue(PropertyName, out evt))
                        return evt;

                    evt = new WeakEvent(_owner);
                    _propertyEvents.Add(PropertyName, evt);
                    return evt;
                }

            }

            #endregion
        }


        /// <summary>
        /// регистрирует объект как подчиненный для дублирования уведомлений о его изменениях
        /// </summary>
        /// <param name="child"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        protected void RegisterChild(INotifyPropertyChanged child)
        {
            Contract.IsNotNull(child);
            child.PropertyChanged += new PropertyChangedEventHandler(child_PropertyChanged);
        }

        /// <summary>
        /// снимает подписку об уведомлениях об изменениях подчиненного объекта
        /// </summary>
        /// <param name="child"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        protected void UnregisterChild(INotifyPropertyChanged child)
        {
            Contract.IsNotNull(child);
            child.PropertyChanged -= new PropertyChangedEventHandler(child_PropertyChanged);
        }
         
        void child_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_triggers != null)
                _triggers.Raise(string.Empty); //notify any change
        }

/*
        protected Func<T>  CreatePropertyValue<T>(string name, BindSource<T> valueSource)
        {
            var f = new PropertyValue<T>(this, name, valueSource);

            return new Func<T>(() => f.Value);
        }

        class PropertyValue<T>
        {
            string _name;
            NotifyPropertyChanged _owner;
            BindSource<T> _source;

            public PropertyValue(NotifyPropertyChanged owner, string Name, BindSource<T> source)
            {
                Contract.IsNotNull(owner);
                Contract.IsNotNull(source);
                Contract.IsNotEmpty(Name);

                _owner = owner;
                _name = Name;
                _source = source;
                source.Changed += new EventHandler(source_Changed);
            }

            void source_Changed(object sender, EventArgs e)
            {
                _owner.Modified(_name);
            }

            public T Value
            {
                get
                {
                    return _source.Value;
                }
            }
        }

*/
    }

    /// <summary>
    /// внешний интерфейс коллекции бессылочных триггеров на изменение значений свойств объекта
    /// </summary>
    public interface IPropertyChangedTriggers
    {
        IWeakEvent this[string PropertyName] { get; }
        IWeakEvent anyChange { get; }
    }

}
