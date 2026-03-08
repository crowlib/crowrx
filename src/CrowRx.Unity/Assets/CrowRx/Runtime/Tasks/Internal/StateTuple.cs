using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;


namespace CrowRx.Tasks.Internal
{
    internal static class StateTuple
    {
        public static StateTuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) => StatePool<T1, T2, T3>.Create(item1, item2, item3);
    }

    internal class StateTuple<T1, T2, T3> : IDisposable
    {
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
        private static readonly ConcurrentQueue<StateTuple<T1, T2, T3>> s_queue = new();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StateTuple<T1, T2, T3> Create(T1 item1, T2 item2, T3 item3)
        {
            if (s_queue.TryDequeue(out StateTuple<T1, T2, T3> value))
            {
                value.Item1 = item1;
                value.Item2 = item2;
                value.Item3 = item3;

                return value;
            }

            return new StateTuple<T1, T2, T3> { Item1 = item1, Item2 = item2, Item3 = item3 };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(StateTuple<T1, T2, T3> tuple)
        {
            tuple.Item1 = default;
            tuple.Item2 = default;
            tuple.Item3 = default;

            s_queue.Enqueue(tuple);
        }
    }
}