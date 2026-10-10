using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrowRx
{
    public class SerializeReferenceListAttribute : PropertyAttribute
    {
    }

    [Serializable]
    public class SerializedReferenceList<T> : IEnumerable<T>
    {
        [SerializeReference] private List<T> items;

        public SerializedReferenceList() => items = new();
        private SerializedReferenceList(List<T> list) => items = list;

        // 묵시적 변환: SerializedReferenceList<T> → List<T>
        public static implicit operator List<T>(SerializedReferenceList<T> container) => container?.items;

        // 묵시적 변환: List<T> → SerializedReferenceList<T>
        public static implicit operator SerializedReferenceList<T>(List<T> list) => list is null ? null : new(list);

        public List<T>.Enumerator GetEnumerator() => items.GetEnumerator();

        // List의 인터페이스 경로를 사용해 빈 목록의 공유 enumerator를 활용합니다.
        // 비어 있지 않은 목록은 인터페이스 계약상 boxing이 남으므로 핫패스에서는 직접 foreach를 사용합니다.
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)items).GetEnumerator();

        // 인덱서
        public T this[int index]
        {
            get => items[index];
            set => items[index] = value;
        }

        // 기본적인 List 메서드들 위임
        public void Add(T item) => items.Add(item);
        public void Remove(T item) => items.Remove(item);
        public void Clear() => items.Clear();
        public int Count => items.Count;

        public IReadOnlyList<T> Items => items;
    }
}
