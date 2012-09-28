using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using gdk.quality;

namespace gdk.weaktools
{
    /// <summary>
    /// методы расширения для коллекций только для чтения
    /// Позволяет генерировать другие коллекции, которые автоматически 
    /// синхронизируют свое содержимое при изменении исходной коллекции
    /// </summary>
    public static class ReadOnlyCollectionExtentions
    {
        /// <summary>
        /// обертывает исходную коллекцию в оболочку, реализующую интерфейс IReadOnlyCollection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceCollection"></param>
        /// <returns></returns>
        public static IReadOnlyCollection<T> ToReadOnly<T>(this WeakObservableCollection<T> sourceCollection)
        {
            return new WrapperCollection<T>(sourceCollection);
        }
        /// <summary>
        /// Конвертация коллекции. 
        /// Формирует новую коллекцию, 
        /// которая представляет собой новую коллекцию других элементов, 
        /// формирующихся на основе исходной коллекции с использованием функции преобразования.
        /// Количество элементов результирующей коллекции всегда совпадает с исходной коллекцией, при изменении (удаление-добавление) 
        /// элементов исходной коллекции происходит автоматическое изменение результирующей коллекции
        /// 
        /// аналог Linq.Select
        /// </summary>
        /// <typeparam name="T1">Тип элементов исходной коллекции</typeparam>
        /// <typeparam name="T2">Тип элементов результирующей коллекции</typeparam>
        /// <param name="source">Исходная коллекция</param>
        /// <param name="converter">Результирующая коллекция</param>
        /// <param name="distinct">Признак необходимости отслеживать уникальность элементов результирующей коллекции</param>
        /// <returns>Автоматически синхронизируемая с исходной преобразованная коллекция</returns>
        public static IReadOnlyCollection<T2> Convert<T1,T2>(
            this IReadOnlyCollection<T1> source, Func<T1, T2> converter, bool distinct) 
            where T1:class 
            where T2:class
        {
            return new AutoConvertCollection<T1,T2>(source, converter, distinct);
        }


        /// <summary>
        /// формирует коллекцию, 
        /// которая является объединением коллекций получающихся из каждого элемента исходной коллекции.
        /// 
        /// аналог Linq.SelectMany
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="source"></param>
        /// <param name="expander"></param>
        /// <returns></returns>
        public static IReadOnlyCollection<T2> UnionAll<T1,T2>(this IReadOnlyCollection<T1> source, Expander<T1, T2> expander) 
            where T1:class 
            where T2:class
        {
            return new UnionCollection<T1, T2>(source, expander);
        }

        public static AutoCollection<T> ToAutoProcessed<T>(this IReadOnlyCollection<T> source) 
            where T : class
        {
            return new AutoCollection<T>(source);
        }

        public static AutoCollection<T> ToAutoProcessed<T>(this WeakObservableCollection<T> source)
            where T : class
        {
            return source.ToReadOnly().ToAutoProcessed();
        }

        public static AutoIndex<TK, TV> ToIndex<TK, TV>(this IReadOnlyCollection<TV> source, Func<TV, TK> keyExtractor)
        {
            return new AutoIndex<TK, TV>(source, keyExtractor, null);
        }

        public static AutoIndex<TK, TV> ToIndex<TK, TV>(this IReadOnlyCollection<TV> source, IReadOnlyCollection<TK> allkeys, Func<TV, TK> keyExtractor)
        {
            return new AutoIndex<TK, TV>(source, keyExtractor, allkeys);
        }
    }

    /// <summary>
    /// делегат, который из элемент получает коллекцию других элементов 
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="argument"></param>
    /// <returns></returns>
    public delegate IReadOnlyCollection<T2> Expander<T1, T2>(T1 argument);


    /// <summary>
    /// реализация интерфейса сравнения с использованием делегата
    /// </summary>
    public class Comparator<T> : IComparer<T>
    {
        Comparison<T> _comparer;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="comparer">Делегат сравнения</param>
        public Comparator(Comparison<T> comparer)
        {
            Contract.IsNotNull(comparer);
            _comparer = comparer;
        }

        #region IComparer<T> Members

        /// <summary>
        /// Сравнить 2 объекта
        /// </summary>
        /// <returns>Результат сравнения входных объектов</returns>
        public int Compare(T x, T y)
        {
            return _comparer(x, y);
        }

        #endregion
    }
}
