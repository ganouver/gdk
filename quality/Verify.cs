using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Permissions;
using System.Runtime.Serialization;

namespace gdk.quality
{
    /// <summary>
    /// исключение возникает при нарушении внутренних инвариантов приложения или класса
    /// Конструируется только при вызове проверок класса Verify
    /// </summary>
    [Serializable]
    public sealed class VerifyException : Exception
    {
        public VerifyException() { }

        private VerifyException(SerializationInfo si, StreamingContext ctx) : base(si, ctx) { }

        public VerifyException(string x) : base(x) { }
        public VerifyException(string x, Exception inner) : base(x, inner) { }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

    }

    /// <summary>
    /// статический класс, содержит методы для вставки в текст программы утверждений, 
    /// проверяющих выполнение некоторых инвариантов, невыполнение которых означает 
    /// грубую ошибку в алгоритмах программы и приводит к немедленному завершению работы приложения
    /// 
    /// каждое утверждение содержит данные для проверки выполняемости какого-то условия и 
    /// если данное условие не выполняется, то генерируется VerificationException
    /// 
    /// При возникновении VerificationException приложение должно выдать сообщение и тихо закрыться
    /// </summary>
    public static class Verify
    {
        /// <summary>
        /// проверяет истинность утверждения
        /// </summary>
        /// <param name="val"></param>
        /// <param name="ef"></param>
        public static void IsTrue(bool val)
        {
            if (!val)
                throw new VerifyException();
        }

        /// <summary>
        /// генерирует VerificationException с указанным сообщением
        /// </summary>
        /// <param name="p"></param>
        public static void Fail(string p)
        {
            throw new VerifyException(p);
        }

        /// <summary>
        /// проверяет, что указанная строка непуста 
        /// </summary>
        /// <param name="p"></param>
        public static void IsNullOrEmpty(string p)
        {
            if (!String.IsNullOrEmpty(p))
                throw new VerifyException();
        }

        /// <summary>
        /// проверяет, что указанная ссылка не нулевая
        /// </summary>
        /// <param name="testedValue"></param>
        public static void IsNotNull(object testedValue)
        {
            if (testedValue == null)
                throw new VerifyException();
        }

        /// <summary>
        /// проверяет, что указанная ссылка нулевая
        /// </summary>
        /// <param name="Source"></param>
        public static void IsNull(object testedValue)
        {
            if (testedValue != null)
                throw new VerifyException();
        }
    }

    /// <summary>
    /// класс, описывающий контракт на ограничение значений аргументов, поступающих в функцию 
    /// 
    /// при невыполнении ожидаемых условий генерирует исключение ArgumentException или его производные классы
    /// 
    /// декларация класса такова, чтобы записанные условия легко читались и воспринимались 
    /// как часть спецификации на функцию
    /// 
    /// пример:
    /// void f(int x)
    /// {
    ///     Contract.InRange(x, 0, 10);
    /// }
    /// 
    /// </summary>
    public static class Contract
    {
        public static void IsNull(object x)
        {
            if (x != null)
                throw new ArgumentException("Object is not null");
        }

        public static void IsNotNull(object x)
        {
            if (x == null)
                throw new ArgumentNullException("x", "Object is null");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static void AllIsNotNull(params object[] x)
        {
            Contract.IsNotNull(x);
            foreach (var o in x)
                Contract.IsNotNull(o);
        }

        public static void InRange<T>(T testValue, T min, T max) where T : IComparable
        {
            Verify.IsTrue(min.CompareTo(max) <= 0);

            if (testValue.CompareTo(min) < 0 || testValue.CompareTo(max) > 0)
                throw new ArgumentOutOfRangeException("testValue", "Value out of range");
        }


        public static void IsNotEmpty(string Value)
        {
            if (String.IsNullOrEmpty(Value))
                throw new ArgumentException("String is empty");
        }

        public static void AreNotEqual(object arg1, object arg2)
        {
            if (arg1 == null)
                Contract.IsNotNull(arg2);
            else
            {
                if (arg2 != null && arg1.Equals(arg2))
                    throw new ArgumentException("Object are equal");
            }
        }

        public static void AreEqual(object arg1, object arg2)
        {
            if (arg1 == null)
                Contract.IsNull(arg2);
            else
            {
                Contract.IsNotNull(arg2);
                if (!arg1.Equals(arg2))
                    throw new ArgumentException("Values not equal");
            }
        }

        public static void IsTrue(bool b)
        {
            if (!b)
                throw new ArgumentException("Value is false");
        }

        public static void IsFalse(bool b)
        {
            if (b)
                throw new ArgumentException("Value is true");
        }

        public static void IsEmpty(string Value)
        {
            if (!string.IsNullOrEmpty(Value))
                throw new ArgumentException("String is not empty");
        }
    }


}
