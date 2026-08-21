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
        /// <param name="value">제거된 요소</param>
        /// <returns>성공 여부</returns>
        public static bool TryPop<T>(this HashSet<T> set, out T value)
        {
            HashSet<T>.Enumerator e = set.GetEnumerator();

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