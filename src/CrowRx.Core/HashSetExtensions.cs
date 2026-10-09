using System;
using System.Collections.Generic;

namespace CrowRx
{
    public static class HashSetExtensions
    {
        /// <summary>
        /// <see cref="HashSet{T}"/>에서 임의의 요소를 제거하고 반환합니다.
        /// </summary>
        /// <typeparam name="T">요소의 형식</typeparam>
        /// <param name="set">제거할 요소가 있는 HashSet</param>
        /// <param name="value">제거된 요소입니다. 집합이 비어 있으면 기본값입니다.</param>
        /// <returns>요소를 제거하면 true이며 집합이 비어 있으면 false입니다.</returns>
        /// <remarks>제거 순서는 보장하지 않습니다. 여러 스레드에서 공유하면 호출자가 동기화해야 합니다.</remarks>
        /// <exception cref="ArgumentNullException">set이 null입니다.</exception>
        public static bool TryPop<T>(this HashSet<T> set, out T value)
        {
            if (set is null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            using HashSet<T>.Enumerator e = set.GetEnumerator();

            if (e.MoveNext())
            {
                value = e.Current;
                set.Remove(value);
                return true;
            }

            value = default!;
            return false;
        }
    }
}
