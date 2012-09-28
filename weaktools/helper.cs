using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gdk.weaktools
{
    static class helper 
    {
        public static IEnumerable<T> AsOne<T>(this T x)
        {
            return new T[] { x };
        }
    }
}
