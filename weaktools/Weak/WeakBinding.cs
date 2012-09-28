using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using gdk.quality;

namespace gdk.weaktools
{
    /// <summary>
    /// реализует автоподдерживаемую функциональную связь между свойствами объектов
    /// хранит явные ссылки на источники данных (подписывается на события изменения), 
    /// а также (через делегат установки значения) на получателя значения.
    /// однако источники значения могут быть уничтожены. 
    /// В этом случае объект перестает что либо делать.
    /// </summary>
    public class Setter<T>
    {
        Action<T> _setter;
        DispatcherOperation _currentOperation;
        Function<T> _src;
        bool _Disabled = false;
        Dispatcher _myDispatcher;


        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="setter">процедура установки значения</param>
        /// <param name="sources">источник значения</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        public Setter(Action<T> setter, Function<T> src)
        {
            Contract.AllIsNotNull(setter, src);
            src.Changed += new EventHandler(src_Changed);
            _src = src;
            _setter = setter;
            _myDispatcher = Dispatcher.CurrentDispatcher;

            Recalculate();
        }

        void src_Changed(object sender, EventArgs e)
        {
            if (!_Disabled && _currentOperation == null)
            {
                _currentOperation = _myDispatcher.BeginInvoke(DispatcherPriority.DataBind,
                    new Action(Recalculate));

                _currentOperation.Completed += new EventHandler(_currentOperation_Completed);
            }
            else
            {
                ;
            }
        }

        void _currentOperation_Completed(object sender, EventArgs e)
        {
            _currentOperation = null;
        }

        void Recalculate()
        {
            try
            {
                _setter(_src.Value);
            }
            catch (InvalidOperationException)
            {
                _Disabled = true;
            }
        }

        public Function<T> Source
        {
            get
            {
                return _src;
            }
        }
    }

    /// <summary>
    /// источник данных, отслеживающий изменение данных на той стороне
    /// хранит слабую ссылку на объект-источник и слушает событие изменения.
    /// при необходимости обращается к функции за значением.
    /// </summary>
    public class Function<TV>
    {
        EventReceiver _receiver;
        WeakReference _target;
        Func<object, TV> _getter;
        WeakEvent _changed;


        /// <summary>
        /// конструктор объекта
        /// </summary>
        /// <param name="changeTriggers">Триггеры изменения отслеживаемого свойства</param>
        /// <param name="target">объект-владелец свойства</param>
        /// <param name="getter">функция-получатель значения свойства</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public Function(IWeakEvent[] changeTriggers, object target, Func<object, TV> getter)
        {
            Contract.AllIsNotNull(changeTriggers, target, getter);
            _receiver = new EventReceiver(OnTrigger);
            foreach (var trig in changeTriggers)
                trig.AddReceiver(_receiver);

            _target = new WeakReference(target, false);
            _getter = getter;
        }

        public Function<TV> AddEvent(IWeakEvent trigger)
        {
            trigger.AddReceiver(_receiver);
            return this;
        }

        public TV Value
        {
            get
            {
                if (_target.IsAlive)
                    return _getter(_target.Target);

                throw new InvalidOperationException();
            }
        }

        public event EventHandler Changed;

        public IWeakEvent ChangeTrigger
        {
            get
            {
                return _changed ?? (_changed = new WeakEvent(this));
            }
        }

        /// <summary>
        /// при генерации любого триггера генерируем событие
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnTrigger(object sender, EventArgs args)
        {
            if (Changed != null)
                Changed(this, EventArgs.Empty);

            if (_changed != null)
                _changed.Raise(args);
        }
    }

    public static class BindTools
    {
        public static Function<T2> CreateFunction<T1, T2>(this T1 target, Func<T1, T2> getter)
        {
            return new Function<T2>(new IWeakEvent[0], target, x => getter((T1)x));
        }

        public static Setter<T> CreateSetter<T>(this Function<T> func, Action<T> setter)
        {
            return new Setter<T>(setter, func);
        }

        public static Function<T2> CreateFunction<T1, T2>(IWeakEvent trigger, T1 target, Func<T1, T2> getter)
        {
            return new Function<T2>(trigger.AsOne().ToArray(), target, x => getter((T1)x));
        }
        /* пример использования 

    

                //модель представления человека, включающая вычисляемое поле
                class DisplayPerson
                {
                       //конструктор
                    public DisplayPerson(Person p)
                    {
                        Person = p;

                        //конструируем установщик вычисленного значения
                        _displayNameSetter = Person.CreateFunction(x => String.Format("{0} {1}.{2}.",
                            x.LastName, x.FirsName.Substring(0, 1), x.Patronymic.Substring(0, 1)))
                            .AddEvent(Person.FirstNameChanged)
                            .AddEvent(Person.LastNameChanged)
                            .AddEvent(Person.PatronymicChanged)
                            .CreateSetter(x => DisplayName = x);
                    }            
           
                    //ссылка на исходные данные о человеке
                    public Person Person { get; private set; }

                    //вычисленное значение свойства имени
                    public string DisplayName { get; private set; }

                    //установщик значения 
                    private Setter<string> _displayNameSetter;


                }
 
             //описание человека
                class Person
                {
                    //фамилия
                    public string LastName { get; set; }

                    //имя
                    public string FirsName { get; set; }

                    //отчество
                    public string Patronymic { get; set; }

                    //событие изменения фамилии
                    public IWeakEvent LastNameChanged { get; }

                    //событие изменения имени
                    public IWeakEvent FirstNameChanged { get; }

                    //событие изменения отчества
                    public IWeakEvent PatronymicChanged { get; }
                }*/
    }


    public class sampleSource
    {
        int _x;
        WeakEvent _changed;
        public int X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
                if (_changed != null)
                    _changed.Raise();
            }
        }

        public IWeakEvent XChanged
        {
            get
            {
                return _changed ?? (_changed = new WeakEvent(this));
            }
        }
    }
}
