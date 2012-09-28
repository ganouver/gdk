using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace testProject
{

    /// <summary>
    /// вспомогательные методы для тестирования
    /// </summary>
    public class TestTools
    {
        /// <summary>
        /// выполняет указанное действие и ловит (и игнорирует) исключения только указанного типа
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        public static void Catch<T>(Action a) where T : System.Exception
        {
            try
            {
                a();
                Assert.Fail("Require exception of type " + typeof(T).FullName);
            }
            catch (T)
            {
                ;
            }

        }

        public static void Catch<T>(Action a, Predicate<T> test) where T : System.Exception
        {
            try
            {
                a();
                Assert.Fail("Require exception of type " + typeof(T).FullName);
            }
            catch (T x)
            {
                if (!test(x))
                    throw;
            }
        }

        public static void CompareCollectionsContent<T>(IEnumerable<T> coll1, IEnumerable<T> coll2)
        {
            int n = coll1.Intersect(coll2).Count();

            Assert.AreEqual(coll1.Count(), n);
            Assert.AreEqual(coll2.Count(), n);
        }

        public static void CompareCollectionsContent<T>(IEnumerable<T> coll1, IEnumerable<T> coll2, IEqualityComparer<T> comparer)
        {
            int n = coll1.Intersect(coll2, comparer).Count();

            Assert.AreEqual(coll1.Count(), n);
            Assert.AreEqual(coll2.Count(), n);
        }

        public static void CompareCollectionsEquality<T>(IEnumerable<T> coll1, IEnumerable<T> coll2)
        {
            var a1 = coll1.ToList();
            var a2 = coll2.ToList();
            Assert.AreEqual(a1.Count, a2.Count);
            for (int index = 0; index < a1.Count; index++)
            {
                Assert.AreEqual(a1[index], a2[index]);
            }
        }

    }
}
