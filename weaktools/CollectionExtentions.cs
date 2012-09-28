using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using gdk.quality;

namespace gdk.weaktools
{
    /// <summary>
    /// Класс методов расширения для работы с коллекциями
    /// </summary>
    public static class CollectionExtentions
    {
        /// <summary>
        /// Добавить к коллекции перечисление
        /// </summary>
        public static void AddRange<T>(this ICollection<T> targetCollection, IEnumerable<T> items)
        {
            Contract.AllIsNotNull(targetCollection, items);
            foreach (var x in items)
                targetCollection.Add(x);
        }

        /// <summary>
        /// Сортирует коллекцию по месту
        /// </summary>
        public static void SortInPlace<T>(this ObservableCollection<T> collection, Comparison<T> comparer)
        {
            Contract.AllIsNotNull(collection, comparer);

            //если в коллекции только 1 элемент или меньще, то она автоматически отсортирована
            if (collection.Count < 2)
                return;

            //инвариант цикла : все что от 0 до sortedBound - уже отсортировано
            for (int sortedBound = 0; sortedBound < collection.Count - 1; sortedBound++)
            {
                T currentMax = collection[sortedBound];
                T testVal = collection[sortedBound + 1];
                int compareResult = comparer(currentMax, testVal);
                //действие цикла
                //взять элемент [sortedBound - 1] и сравнить с [sortedBound]
                //если порядок сохранен - переходим дальше
                if (compareResult <= 0) //i.e. currentMax <= testVal
                    continue;
                //если порядок нарушен - то найти ему место и передвинуть

                int newIndex = FindIndex(collection, 0, sortedBound, collection[sortedBound + 1], comparer);

                collection.Move(sortedBound + 1, newIndex);
            }

        }

        /// <summary>
        /// Ищет подходящее место в отсортированной коллекции в пределах указанного диапазона для
        /// вставки указанного элемента используя переданную функцию сравнения
        /// </summary>
        private static int FindIndex<T>(ObservableCollection<T> collection, int minIndex, int maxIndex, T arg, Comparison<T> comparer)
        {
            if (minIndex == maxIndex)
                return minIndex;

            int nTestIndex = (minIndex + maxIndex) / 2;
            int testVal = comparer(collection[nTestIndex], arg);
            if (testVal == 0)
                return nTestIndex;
            else if (testVal < 0)
                return FindIndex(collection, Math.Min(nTestIndex + 1, maxIndex), maxIndex, arg, comparer);
            else
                return FindIndex(collection, minIndex, nTestIndex, arg, comparer);
        }

    } 
    /// class CollectionExtentions
    /// 
}
