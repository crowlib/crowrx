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
        [SerializeReference] private List<T> items = new();

        // 묵시적 변환: SerializedReferenceList<T> → List<T>
        public static implicit operator List<T>(SerializedReferenceList<T> container) => container.items;

        // 묵시적 변환: List<T> → SerializedReferenceList<T>
        public static implicit operator SerializedReferenceList<T>(List<T> list) => new SerializedReferenceList<T> { items = list };

        // IEnumerable<T> 지원
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();

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