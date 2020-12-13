using System.Collections.Generic;

namespace PaletteExtractor
{
    static class EnumerableExtensions
    {
        public static IEnumerable<T> Top<T>(this IEnumerable<T> enumerable, int x)
        {
            var top = new List<T>();
            var i = 0;
            foreach (var value in enumerable)
            {
                top.Add(value);
                i++;
                if (i >= x)
                    break;
            }
            return top;
        }
    }
}
