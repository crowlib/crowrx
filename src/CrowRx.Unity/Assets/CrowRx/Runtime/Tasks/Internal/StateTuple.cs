using System;
using System.Runtime.CompilerServices;

namespace CrowRx.Tasks.Internal
{
    internal static class StateTuple
    {
        public static StateTuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) => StatePool<T1, T2, T3>.Create(item1, item2, item3);
    }

    internal class StateTuple<T1, T2, T3> : IDisposable
    {
        internal StateTuple<T1, T2, T3> Next;
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3)
        {
            item1 = Item1;
            item2 = Item2;
            item3 = Item3;
        }

        public void Dispose() => StatePool<T1, T2, T3>.Return(this);
    }

    internal static class StatePool<T1, T2, T3>
    {
        private const int MaxCached = 64;

        private static readonly object s_gate = new();
        private static int _count;

        private static StateTuple<T1, T2, T3> _head;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StateTuple<T1, T2, T3> Create(T1 item1, T2 item2, T3 item3)
        {
            lock (s_gate)
            {
                if (_head != null)
                {
                    StateTuple<T1, T2, T3> value = _head;
                    _head = value.Next;
                    value.Next = null;
                    _count--;
                    value.Item1 = item1;
                    value.Item2 = item2;
                    value.Item3 = item3;
                    return value;
                }
            }

            return new StateTuple<T1, T2, T3> { Item1 = item1, Item2 = item2, Item3 = item3 };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(StateTuple<T1, T2, T3> tuple)
        {
            tuple.Item1 = default;
            tuple.Item2 = default;
            tuple.Item3 = default;

            lock (s_gate)
            {
                if (_count >= MaxCached)
                {
                    return;
                }

                tuple.Next = _head;
                _head = tuple;
                _count++;
            }
        }
    }
}