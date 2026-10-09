using System.Collections.Generic;
using System;

namespace CrowRx
{
    /// <summary>
    /// 원본 HashSet을 복사하지 않고 참조하며 읽기 전용 조회와 순회를 제공합니다.
    /// </summary>
    /// <typeparam name="T">집합 요소의 형식입니다.</typeparam>
    /// <remarks>
    /// 원본의 변경은 이 뷰에도 반영됩니다. 불변 집합이나 스냅샷은 아닙니다.
    /// 유효한 HashSet으로 생성해야 합니다. default와 초기화되지 않은 값은 사용할 수 없습니다.
    /// 순회 중 원본을 변경하지 마세요.
    /// 여러 스레드에서 원본에 접근할 경우 호출자가 동기화해야 합니다.
    /// </remarks>
    public readonly struct ReadOnlyHashSet<T>
    {
        private readonly HashSet<T> _source;

        /// <summary>원본 집합을 복사하지 않고 읽기 전용으로 참조합니다.</summary>
        /// <param name="source">조회할 원본 집합입니다.</param>
        /// <exception cref="ArgumentNullException">source가 null입니다.</exception>
        public ReadOnlyHashSet(HashSet<T> source) => _source = source ?? throw new ArgumentNullException(nameof(source));

        /// <summary>원본 집합의 요소 수입니다.</summary>
        public int Count => _source.Count;

        /// <summary>복사나 인터페이스 boxing 없이 원본 집합을 순회하는 구조체 열거자를 반환합니다.</summary>
        public HashSet<T>.Enumerator GetEnumerator() => _source.GetEnumerator();

        /// <summary>원본의 비교 규칙으로 요소의 포함 여부를 확인합니다.</summary>
        /// <param name="item">포함 여부를 확인할 요소입니다.</param>
        public bool Contains(T item) => _source.Contains(item);
    }
}
