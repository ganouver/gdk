using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using gdk.weaktools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace testProject
{
    [TestClass]
    public class TestCollections
    {
        [TestMethod]
        public void TestMethod1()
        {
        }

        [TestMethod]
        public void test_WrapperCollection()
        {
            NotifyCollectionChangedAction lastAction = NotifyCollectionChangedAction.Reset;
            object lastAdd = null;
            object lastRemove = null;
            int changeCounter = 0;
            EventReceiver onAdd = new EventReceiver((x, y) => lastAdd = ((ObjectEventArgs)y).Arg),
                onRemove = new EventReceiver((x, y) => lastRemove = ((ObjectEventArgs)y).Arg),
                onChange = new EventReceiver((x, y) => ++changeCounter);

            var targetColl = new WeakObservableCollection<string>();
            var wcoll = new WrapperCollection<string>(targetColl);

            wcoll.Added.AddReceiver(onAdd);
            wcoll.Removed.AddReceiver(onRemove);
            wcoll.Changed.AddReceiver(onChange);
            wcoll.CollectionChanged += (x, y) => { lastAction = y.Action; };

            string t1 = "aaa", t2 = "bbb";

            CompareWrap(targetColl, wcoll, t1, t2);
            Assert.AreEqual(0, changeCounter);

            targetColl.Add(t1);
            CompareWrap(targetColl, wcoll, t1, t2);
            Assert.AreEqual(1, changeCounter);
            Assert.AreEqual(t1, lastAdd);
            Assert.AreEqual(NotifyCollectionChangedAction.Add, lastAction);

            targetColl.Add(t2);
            CompareWrap(targetColl, wcoll, t1, t2);
            Assert.AreEqual(2, changeCounter);
            Assert.AreEqual(t2, lastAdd);
            Assert.AreEqual(NotifyCollectionChangedAction.Add, lastAction);

            targetColl.Remove(t1);
            CompareWrap(targetColl, wcoll, t1, t2);
            Assert.AreEqual(3, changeCounter);
            Assert.AreEqual(t1, lastRemove);
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, lastAction);

            Assert.AreEqual(targetColl.Count, wcoll.Count);
        }

        private static void CompareWrap(ObservableCollection<string> targetColl, IReadOnlyCollection<string> wcoll, string t1, string t2)
        {
            Assert.AreEqual(targetColl.Count, wcoll.Count);
            Assert.AreEqual(targetColl.Contains(t1), wcoll.Contains(t1));
            Assert.AreEqual(targetColl.Contains(t2), wcoll.Contains(t2));
            for (int i = 0; i < targetColl.Count; i++)
            {
                Assert.AreEqual(targetColl[i], wcoll[i]);
            }
        }

        class IInt
        {
            int _x;
            public IInt(int x)
            {
                _x = x;
            }

            public override string ToString()
            {
                return _x.ToString();
            }
        }

        [TestMethod]
        public void test_ConvertCollection()
        {
            var t1 = new IInt(11);
            var t2 = new IInt(33);
            var src = new WeakObservableCollection<IInt>() { t1, t2};
            var target = src.ToReadOnly().Convert(x => x.ToString(), false);
            Assert.AreEqual(2, target.Count);
            src.Clear();

            for (int i = 0; i < 2; i++)
            {
                src.Add(t1);
                Assert.AreEqual(src.Count, target.Count);
                Assert.AreEqual(t1.ToString(), target[0]);
                src.Add(t2);
                Assert.AreEqual(src.Count, target.Count);
                Assert.AreEqual(t2.ToString(), target.Single(x => x == t2.ToString()));
                src.Remove(t1);
                Assert.AreEqual(t2.ToString(), target.Single());

                src.Remove(t2);
                Assert.IsFalse(target.Any());
                Assert.AreEqual(0, target.Count);
            }

            var target1 = target.Convert(x => x.Length.ToString(), true);
            src.Add(t1);
            src.Add(t2);
            Assert.AreEqual(2, target.Count);
            Assert.AreEqual(1, target1.Count);

            src.Remove(t1);

            Assert.AreEqual(1, target.Count);
            Assert.AreEqual(1, target1.Count);

            src.Clear();

            Assert.AreEqual(0, target.Count);
            Assert.AreEqual(0, target1.Count);
        }

        [TestMethod]
        public void test_UnionCollectionDistinct()
        {
            WeakObservableCollection<string> src1 = new WeakObservableCollection<string>();
            WeakObservableCollection<string> src2 = new WeakObservableCollection<string>();

            WeakObservableCollection<IReadOnlyCollection<string>> collections =
                new WeakObservableCollection<IReadOnlyCollection<string>> { 
                    src1.ToReadOnly(), 
                    src2.ToReadOnly() };

            var union = collections.ToReadOnly().UnionAll(x => x);
            Assert.AreEqual(0, union.Count);

            src1.Add("a");
            src2.Add("a");
            Assert.AreEqual(1, union.Count);
            src2.Add("b");
            src2.Remove("a");
            Assert.AreEqual(2, union.Count);
            src1.Remove("a");
            Assert.AreEqual(1, union.Count);
        }

        [TestMethod]
        public void test_UnionCollection()
        {
            string t11 = "11", t12 = "12", t21 = "21", t22 = "22";

            WeakObservableCollection<string> src1 = new WeakObservableCollection<string>() { t11, t12 };
            WeakObservableCollection<string> src2 = new WeakObservableCollection<string>() { t21, t22 };

            WeakObservableCollection<IReadOnlyCollection<string>> collections =
                new WeakObservableCollection<IReadOnlyCollection<string>> { src1.ToReadOnly() };

            var union = collections.ToReadOnly().UnionAll(x => x);

            Assert.AreEqual(2, union.Count);
            Assert.IsTrue(union.Contains(t11));
            Assert.IsTrue(union.Contains(t12));
            Assert.IsFalse(union.Contains(t21));
            Assert.IsFalse(union.Contains(t22));

            collections.Add(src2.ToReadOnly());

            Assert.AreEqual(4, union.Count);
            Assert.IsTrue(union.Contains(t11));
            Assert.IsTrue(union.Contains(t12));
            Assert.IsTrue(union.Contains(t21));
            Assert.IsTrue(union.Contains(t22));

            src1.Remove(t11);

            Assert.AreEqual(3, union.Count);
            Assert.IsFalse(union.Contains(t11));
            Assert.IsTrue(union.Contains(t12));
            Assert.IsTrue(union.Contains(t21));
            Assert.IsTrue(union.Contains(t22));

            src1.Clear();
            src2.Clear();

            Assert.AreEqual(0, union.Count);

            src2.Add(t21);
            Assert.AreEqual(1, union.Count);
            Assert.IsTrue(union.Contains(t21));
            Assert.IsFalse(union.Contains(t12));

            src1.Add(t11);
            Assert.AreEqual(2, union.Count);

            collections.Remove(collections[0]);
            Assert.AreEqual(1, union.Count);

            collections.Clear();
            Assert.AreEqual(0, union.Count);

        }

        class FilterItem : NotifyPropertyChanged
        {
            private bool _Property;

            /// <summary>
            /// tested property
            /// </summary>
            public bool Property
            {
                get { return _Property; }
                set
                {
                    if (_Property != value)
                    {
                        _Property = value;
                        Modified("Property");
                    }
                }
            }


            /// <summary>
            /// Property Changed Trigger
            /// </summary>
            public IWeakEvent PropertyTrigger
            {
                get { return Triggers["Property"]; }
            }
        }

        [TestMethod]
        public void test_FilterCollection()
        {
            WeakObservableCollection<FilterItem> items = new WeakObservableCollection<FilterItem>() {
                new FilterItem() {Property = false}};

            var manualTrigger = new WeakEvent(items);


            var filtered = items.ToAutoProcessed().FilteredBy(x => x.Property)
                .UpdateOn(x => x.PropertyTrigger)
                .UpdateOn(x => null);

            var filteredM = items.ToAutoProcessed().FilteredBy(x => x.Property).UpdateOn(manualTrigger);

            Assert.AreEqual(0, filtered.Count);
            items[0].Property = true;
            Assert.AreEqual(1, filtered.Count);

            Assert.AreEqual(0, filteredM.Count);
            manualTrigger.Raise();
            Assert.AreEqual(filtered.Count, filteredM.Count);


            items[0].Property = false;
            Assert.AreEqual(0, filtered.Count);

            items.Add(new FilterItem() { Property = true });
            Assert.AreEqual(1, filtered.Count);

            items.Add(new FilterItem() { Property = false });
            Assert.AreEqual(1, filtered.Count);

            items.Add(new FilterItem() { Property = true });
            Assert.AreEqual(2, filtered.Count);
            items.Remove(items.First(x => x.Property));
            Assert.AreEqual(1, filtered.Count);
            Assert.AreSame(items.Single(x => x.Property), filtered[0]);
            items.Remove(items.First(x => !x.Property));
            Assert.AreEqual(1, filtered.Count);

            items.Clear();
            Assert.AreEqual(0, filtered.Count);

            //тестируем работу сортировки
            var rawColl = new WeakObservableCollection<string>();

            //сортируем по первым символам
            var sorted = rawColl.ToAutoProcessed().SortedOn((x, y) => x.Substring(0, 1).CompareTo(y.Substring(0, 1)));
            rawColl.Add("5ф");
            rawColl.Add("2ф");
            rawColl.Add("4ф");
            rawColl.Add("3ф");
            rawColl.Add("1с");
            rawColl.Add("5с");
            rawColl.Add("2с");
            rawColl.Add("4с");
            rawColl.Add("3с");
            rawColl.Add("1с");

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual((i + 1).ToString(), sorted[2 * i].Substring(0, 1));
                Assert.AreEqual((i + 1).ToString(), sorted[2 * i + 1].Substring(0, 1));
            }
        }

        [TestMethod]
        public void Test_Comparator()
        {
            Comparator<int> compar = new Comparator<int>((x, y) => x - y);
            Assert.AreEqual(-7, compar.Compare(10, 17));
        }

        [TestMethod]
        public void TestIndex1()
        {
            WeakObservableCollection<String> tc = new WeakObservableCollection<String> { "aa", "ab", "bc" };

            var ix = tc.ToReadOnly().ToIndex(x => x[0]);

            Assert.AreEqual(2, ix.Count);
            TestTools.CompareCollectionsContent(ix['a'], new string[] { "ab", "aa" });
            TestTools.CompareCollectionsContent(ix['b'], new string[] { "bc" });

            tc.Add("ce");
            Assert.AreEqual(3, ix.Count);

            tc.Remove("aa");
            Assert.AreEqual(3, ix.Count);

            tc.Remove("ab");
            Assert.AreEqual(2, ix.Count);
        }

        [TestMethod]
        public void TestIndex2()
        {
            WeakObservableCollection<Char> keys = new WeakObservableCollection<Char> { 'a', 'b', 'c', 'd' };
            WeakObservableCollection<String> tc = new WeakObservableCollection<String> { "aa", "ab", "bc" };

            var ix = tc.ToReadOnly().ToIndex(keys.ToReadOnly(), x => x[0]);

            Assert.AreEqual(4, ix.Count);
            TestTools.CompareCollectionsContent(ix['a'], new string[] { "ab", "aa" });
            TestTools.CompareCollectionsContent(ix['b'], new string[] { "bc" });

            var ac = ix['a'];
            Assert.AreEqual(2, ac.Count);
            tc.Add("ce");
            Assert.AreEqual(4, ix.Count);

            tc.Remove("aa");
            Assert.AreEqual(4, ix.Count);
            Assert.AreEqual(1, ac.Count);

            tc.Remove("ab");
            Assert.AreEqual(4, ix.Count);
            Assert.AreEqual(0, ac.Count);

            keys.Remove('d');
            Assert.AreEqual(3, ix.Count);
            tc.Add("as");
            Assert.AreEqual(1, ac.Count);

        }

    }
}
