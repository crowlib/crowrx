using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CrowRx;
using CrowRx.Tasks;
using CrowRx.Helper;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class CrowRxRemainingTests
{
    public static readonly Dictionary<string, int> AllocationMetrics = new();

    [Test]
    public void TypeNamesPreserveDeclaringTypesAndDistributeGenericArguments()
    {
        Assert.That(typeof(NameOuter<int>.First<string>).GetRealTypeFullName(), Is.EqualTo("NameOuter<System.Int32>.First<System.String>"));
        Assert.That(typeof(NameOuter<int>.Second<string>).GetRealTypeFullName(), Is.EqualTo("NameOuter<System.Int32>.Second<System.String>"));
        Assert.That(typeof(NameOuter<int>.Plain).GetRealTypeFullName(), Is.EqualTo("NameOuter<System.Int32>.Plain"));
        Assert.That(typeof(NameOuter<int>.Plain).GetRealTypeName(), Is.EqualTo("Plain"));
        Assert.That(typeof(NameOuter<int>.First<string>).GetRealTypeName(), Is.EqualTo("First<String>"));
        Assert.That(typeof(NameOuter<>.First<>).GetRealTypeFullName(), Is.EqualTo("NameOuter<T>.First<U>"));
        Assert.That(typeof(Dictionary<string, List<int[]>>).GetRealTypeFullName(), Is.EqualTo("System.Collections.Generic.Dictionary<System.String,System.Collections.Generic.List<System.Int32[]>>"));
        Assert.That(typeof(NameOuter<int>.First<string>[,]).GetRealTypeFullName(), Is.EqualTo("NameOuter<System.Int32>.First<System.String>[,]"));
        Assert.That(typeof(int).MakeArrayType(1).GetRealTypeFullName(), Is.EqualTo("System.Int32[*]"));
        Assert.That(typeof(int).MakeByRefType().GetRealTypeFullName(), Is.EqualTo("System.Int32&"));
        Assert.That(typeof(int).MakePointerType().GetRealTypeFullName(), Is.EqualTo("System.Int32*"));
        Assert.Throws<ArgumentNullException>(() => TypeExtension.GetRealTypeName(null));
        Assert.Throws<ArgumentNullException>(() => TypeExtension.GetRealTypeFullName(null));
    }

    [Test]
    public void InheritsFromExcludesOnlyTheExactType()
    {
        Assert.That(typeof(string).InheritsFrom(typeof(string), false), Is.False);
        Assert.That(typeof(string).InheritsFrom(typeof(string)), Is.True);
        Assert.That(typeof(string).InheritsFrom(typeof(object), false), Is.True);
        Assert.That(typeof(List<int>).InheritsFrom(typeof(IEnumerable<int>), false), Is.True);
        Assert.That(typeof(object).InheritsFrom(typeof(string)), Is.False);
        Assert.That(TypeExtension.InheritsFrom(null, null, false), Is.False);
    }

    [Test]
    public void HierarchyTraversalPreservesDirectMatchPriorityAndPrunesSubtrees()
    {
        var root = new GameObject("root");
        try
        {
            var branch = new GameObject("branch");
            branch.transform.SetParent(root.transform);
            var nested = new GameObject("match");
            nested.transform.SetParent(branch.transform);
            var direct = new GameObject("match");
            direct.transform.SetParent(root.transform);
            Assert.That(root.transform.FindChildDeep("match"), Is.SameAs(direct.transform));
            Assert.That(root.transform.FindChildDeep("branch/match"), Is.SameAs(nested.transform));
            var visited = new List<GameObject>();
            root.ChildCirculateCallback(node =>
            {
                visited.Add(node);
                return node != branch;
            });
            Assert.That(visited, Is.EqualTo(new[] { root, branch, direct }));
            root.SetLayerRecursively(2);
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true)) Assert.That(node.gameObject.layer, Is.EqualTo(2));
            Transform leaf = branch.transform;
            for (int i = 0; i < 128; i++)
            {
                var child = new GameObject(i == 127 ? "deep target" : "deep");
                child.transform.SetParent(leaf);
                leaf = child.transform;
            }

            Assert.That(root.transform.FindChildDeep("deep target"), Is.SameAs(leaf));
            int count = 0;
            root.ChildCirculateCallback(_ =>
            {
                count++;
                return true;
            });
            Assert.That(count, Is.EqualTo(132));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase("a", "A")]
    [TestCase("hello", "Hello")]
    [TestCase("Already", "Already")]
    [TestCase("한글", "한글")]
    public void UpperFirstPreservesText(string text, string expected) => Assert.That(text.ToUpperFirst(), Is.EqualTo(expected));

    [Test]
    public void HierarchyPathsPreserveRootAndInvalidAncestorContracts()
    {
        var root = new GameObject("root");
        var unrelated = new GameObject("other");
        try
        {
            var child = new GameObject("child");
            child.transform.SetParent(root.transform);
            var leaf = new GameObject("leaf");
            leaf.transform.SetParent(child.transform);
            Assert.That(leaf.GetHierarchyPath(), Is.EqualTo("root/child/leaf"));
            Assert.That(leaf.GetHierarchyPath(root), Is.EqualTo("child/leaf"));
            Assert.That(leaf.GetHierarchyPath(child), Is.EqualTo("leaf"));
            Assert.That(root.GetHierarchyPath(root), Is.Empty);
            Assert.That(leaf.GetHierarchyPath(unrelated), Is.Empty);
            Assert.That(root.GetHierarchyPath(leaf), Is.Empty);
            Assert.That(GameObjectExtension.GetHierarchyPath(null), Is.Empty);
            child.name = "";
            Assert.That(leaf.GetHierarchyPath(root), Is.EqualTo("/leaf"));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(unrelated);
        }
    }

    [Test]
    public void MissingSingleComponentIsSilentButDuplicatesAreStillErrors()
    {
        var root = new GameObject("single component test");
        try
        {
            Assert.That(root.TryGetSingleComponent<BoxCollider>(out var missing), Is.False);
            Assert.That(missing, Is.Null);
            Assert.That(root.transform.TryGetSingleComponent<BoxCollider>(out missing), Is.False);
            LogAssert.NoUnexpectedReceived();
            var first = root.AddComponent<BoxCollider>();
            Assert.That(root.TryGetSingleComponent<BoxCollider>(out var found), Is.True);
            Assert.That(found, Is.SameAs(first));
            root.AddComponent<BoxCollider>();
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: <single component test> requires at most one [BoxCollider] component.");
            Assert.That(root.TryGetSingleComponent<BoxCollider>(out found), Is.False);
            Assert.That(found, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void RectangleRequiresFourCornersAndSupportsRotatedRectangles()
    {
        Assert.Throws<ArgumentNullException>(() => Mathm.RectIntersect(null, default, default));
        Assert.Throws<ArgumentException>(() => Mathm.RectIntersect(new Vector3[3], default, default));
        Assert.Throws<ArgumentException>(() => Mathm.RectIntersect(new Vector3[5], default, default));
        Vector3[] corners = { new(0, 1, 0), new(1, 0, 0), new(0, -1, 0), new(-1, 0, 0) };
        Assert.That(Mathm.RectIntersect(corners, new(-2, 0), new(2, 0)), Is.True);
        Assert.That(Mathm.RectIntersect(corners, new(0, 0), new(0.1f, 0)), Is.True);
        Assert.That(Mathm.RectIntersect(corners, new(0.8f, 0.8f), new(0.9f, 0.9f)), Is.False);
        Assert.That(Mathm.RectIntersect(corners, new(0, 1), new(0, 2)), Is.True);
        Array.Reverse(corners);
        Assert.That(Mathm.RectIntersect(corners, new(-2, 0), new(2, 0)), Is.True);
    }

    [Test]
    public void ArrivalAgreesWithNormalizedReferenceForOrdinaryAndTinyDistances()
    {
        Vector3[] distances = { Vector3.zero, new(0.000001f, 0, 0), new(0.00002f, 0, 0), new(2, 3, 4), new(float.MaxValue, 0, 0) };
        foreach (var distance in distances)
        {
            Assert.That(Mathm.IsArrival(-Vector3.one, Vector3.zero, distance),
                Is.EqualTo(Vector3.Dot(-Vector3.one, distance.normalized) < 0));
        }

        var tinyMovement = new Vector3(-float.Epsilon, 0, 0);
        var smallDistance = new Vector3(0.001f, 0, 0);
        Assert.That(Mathm.IsArrival(tinyMovement, Vector3.zero, smallDistance),
            Is.EqualTo(Vector3.Dot(tinyMovement, smallDistance.normalized) < 0));
    }

    [Test]
    public void WhenAnyConsumesSynchronousSuccessesAndLoserFailures()
    {
        var first = new CountedSource();
        var second = new CountedSource();
        first.Succeed();
        second.Fail(new InvalidOperationException("loser"));
        Assert.That(CrowTask.WhenAny(new[] { first.Task, second.Task }, 2).GetAwaiter().GetResult(), Is.Zero);
        Assert.That(first.Consumed, Is.EqualTo(1));
        Assert.That(second.Consumed, Is.EqualTo(1));
    }

    [Test]
    public void WhenAnyRetainsPromiseUntilLateLoserCompletes()
    {
        var winner = new CountedSource();
        var loser = new CountedSource();
        var old = CrowTask.WhenAny(new[] { winner.Task, loser.Task }, 2);
        var oldSource = Source(old);
        winner.Succeed();
        Assert.That(old.GetAwaiter().GetResult(), Is.Zero);
        var next = CrowTask.WhenAny(new[] { UniTask.CompletedTask }, 1);
        Assert.That(Source(next), Is.Not.SameAs(oldSource));
        Assert.That(next.GetAwaiter().GetResult(), Is.Zero);
        Assert.That(loser.Consumed, Is.Zero);
        loser.Fail(new InvalidOperationException("late loser"));
        Assert.That(loser.Consumed, Is.EqualTo(1));
        var reused = CrowTask.WhenAny(new[] { UniTask.CompletedTask }, 1);
        Assert.That(Source(reused), Is.SameAs(oldSource));
        Assert.That(reused.GetAwaiter().GetResult(), Is.Zero);
        Assert.Throws<InvalidOperationException>(() => old.GetAwaiter().GetResult());
    }

    [Test]
    public void WhenAnyPropagatesFirstFaultAndCancellation()
    {
        var failure = new InvalidOperationException("winner failed");
        var first = new CountedSource();
        var second = new CountedSource();
        var fault = CrowTask.WhenAny(new[] { first.Task, second.Task }, 2);
        second.Fail(failure);
        Assert.That(Assert.Throws<InvalidOperationException>(() => fault.GetAwaiter().GetResult()), Is.SameAs(failure));
        first.Succeed();
        Assert.That(first.Consumed, Is.EqualTo(1));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var cancellation = CrowTask.WhenAny(new[] { UniTask.FromCanceled(canceled.Token), UniTask.CompletedTask }, 2);
        Assert.That(cancellation.Status, Is.EqualTo(UniTaskStatus.Canceled));
        Assert.Throws<OperationCanceledException>(() => cancellation.GetAwaiter().GetResult());
    }

    [Test]
    public void WhenAnyPoolIsBoundedAndRejectsInvalidLengths()
    {
        Assert.Throws<ArgumentNullException>(() => CrowTask.WhenAny(null, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CrowTask.WhenAny(new UniTask[1], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CrowTask.WhenAny(new UniTask[1], 2));
        var tasks = new UniTask<int>[80];
        for (int i = 0; i < tasks.Length; i++) tasks[i] = CrowTask.WhenAny(new[] { UniTask.CompletedTask }, 1);
        foreach (var task in tasks) Assert.That(task.GetAwaiter().GetResult(), Is.Zero);
        var count = Source(tasks[0]).GetType().GetField("_poolCount", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That((int)count.GetValue(null), Is.LessThanOrEqualTo(64));
    }

    [Test]
    public void WhenAnyHandlesConcurrentCompletionAndReentrantConsumption()
    {
        for (int round = 0; round < 40; round++)
        {
            var sources = new[] { new CountedSource(), new CountedSource(), new CountedSource(), new CountedSource() };
            var input = new[] { sources[0].Task, sources[1].Task, sources[2].Task, sources[3].Task };
            Task<int> observed = Observe(CrowTask.WhenAny(input, input.Length));
            Task[] workers = new Task[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                workers[i] = System.Threading.Tasks.Task.Run(source.Succeed);
            }

            System.Threading.Tasks.Task.WaitAll(workers);
            Assert.That(observed.Result, Is.InRange(0, sources.Length - 1));
            foreach (var source in sources) Assert.That(source.Consumed, Is.EqualTo(1));
        }
    }

    [Test]
    public void NestedEnumerationsRemainIndependentAndDetectListMutation()
    {
        SerializedReferenceList<int> values = new List<int> { 1, 2 };
        int pairs = 0;
        foreach (int outer in values)
        foreach (int inner in values)
            pairs += outer * inner;
        Assert.That(pairs, Is.EqualTo(9));
        using var enumerator = ((IEnumerable<int>)values).GetEnumerator();
        Assert.That(enumerator.MoveNext(), Is.True);
        values.Add(3);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Test]
    public void WarmedValueOperationsAndWhenAnyDoNotAllocate()
    {
        var input = new[] { UniTask.CompletedTask, UniTask.CompletedTask };
        SerializedReferenceList<int> values = new List<int> { 1, 2 };
        Vector3[] corners = { new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0) };
        for (int i = 0; i < 10; i++) CrowTask.WhenAny(input, 2).GetAwaiter().GetResult();
        _ = "Already".ToUpperFirst();
        _ = Mathm.RectIntersect(corners, new(-1, 0), new(2, 0));
        var options = ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.CollectOnlyOnCurrentThread;
        using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 2048, options);
        var calibration = new byte[1024];
        GC.KeepAlive(calibration);
        int calibrationCount = recorder.Count;
        recorder.Reset();
        int sum = 0;
        bool intersects = false;
        for (int i = 0; i < 100; i++)
        {
            sum += CrowTask.WhenAny(input, 2).GetAwaiter().GetResult();
            foreach (int value in values) sum += value;
            intersects |= Mathm.RectIntersect(corners, new(-1, 0), new(2, 0));
            _ = "Already".ToUpperFirst();
        }

        int allocations = recorder.Count;
        Assert.That(recorder.Valid, Is.True);
        Assert.That(calibrationCount, Is.GreaterThan(0));
        Assert.That(sum, Is.EqualTo(300));
        Assert.That(intersects, Is.True);
        Assert.That(allocations, Is.Zero);
    }

    [Test]
    public void WarmedAsynchronousWhenAnyAlsoReusesItsContinuationState()
    {
        var first = new CountedSource();
        var second = new CountedSource();
        var inputs = new UniTask[2];
        Action run = () =>
        {
            first.Reset();
            second.Reset();
            inputs[0] = first.Task;
            inputs[1] = second.Task;
            var winner = CrowTask.WhenAny(inputs, 2);
            first.Succeed();
            winner.GetAwaiter().GetResult();
            second.Succeed();
        };
        for (int i = 0; i < 10; i++) run();
        var options = ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.CollectOnlyOnCurrentThread;
        using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 2048, options);
        var calibration = new byte[1024];
        GC.KeepAlive(calibration);
        int calibrationCount = recorder.Count;
        recorder.Reset();
        for (int i = 0; i < 100; i++) run();
        int allocations = recorder.Count;
        Assert.That(recorder.Valid, Is.True);
        Assert.That(calibrationCount, Is.GreaterThan(0));
        Assert.That(allocations, Is.Zero);
        Assert.That(first.Consumed, Is.EqualTo(1));
        Assert.That(second.Consumed, Is.EqualTo(1));
    }

    [Test]
    public void ReleaseDisposesSourceEvenWhenCancellationCallbackThrows()
    {
        var source = new CancellationTokenSource();
        using var registration = source.Token.Register(() => throw new InvalidOperationException("callback"));
        Assert.Throws<AggregateException>(() => source.Release());
        Assert.Throws<ObjectDisposedException>(() => { _ = source.Token; });
        CancellationTokenSourceExtension.Release(null);
    }

    [Test]
    public void FadeInOutReportsFailureOnceAndPublicAsyncPropagatesCancellation()
    {
        var root = new GameObject("fade reporting test");
        try
        {
            var fade = root.AddComponent<CanvasGroupFade>();
            using var source = new CancellationTokenSource();
            source.Cancel();
            var canceled = fade.FadeOutInAsync(0, 1, 0, 0, 0, true, source.Token);
            Assert.That(canceled.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.Throws<OperationCanceledException>(() => canceled.GetAwaiter().GetResult());
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("ArgumentException:.*TimeSpan"));
            fade.FadeInOut(0, 1, 0, 0, float.NaN, true);
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [UnityTest]
    public IEnumerator CompiledAllocationMeasurementsIncludeReferenceAndStringControls()
    {
        bool wasEnabled = UnityEngine.Profiling.Profiler.enabled;
        bool memoryEnabled = UnityEngine.Profiling.Profiler.GetAreaEnabled(UnityEngine.Profiling.ProfilerArea.Memory);
        try
        {
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.Memory, true);
            UnityEngine.Profiling.Profiler.enabled = true;
            yield return null;
            _ = "already".ToUpperFirst();
            var options = ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.CollectOnlyOnCurrentThread;
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 4096, options);
            var calibration = new byte[1024];
            GC.KeepAlive(calibration);
            int arrayControl = recorder.Count;
            recorder.Reset();
            GC.KeepAlive(new CountedSource());
            int referenceControl = recorder.Count;
            recorder.Reset();
            GC.KeepAlive(new string('x', 64));
            int stringControl = recorder.Count;
            recorder.Reset();
            for (int i = 0; i < 100; i++)
            {
                var builder = new System.Text.StringBuilder();
                builder.Append(char.ToUpperInvariant("already"[0]));
                builder.Append("already"[1..]);
                GC.KeepAlive(builder.ToString());
            }

            int oldUpper = recorder.Count;
            recorder.Reset();
            for (int i = 0; i < 100; i++) GC.KeepAlive("already".ToUpperFirst());
            int newUpper = recorder.Count;
            var input = new[] { UniTask.CompletedTask, UniTask.CompletedTask };
            for (int i = 0; i < 10; i++) CrowTask.WhenAny(input, 2).GetAwaiter().GetResult();
            recorder.Reset();
            for (int i = 0; i < 100; i++) CrowTask.WhenAny(input, 2).GetAwaiter().GetResult();
            int synchronousWhenAny = recorder.Count;
            var first = new CountedSource();
            var second = new CountedSource();
            var asynchronousInputs = new UniTask[2];
            Action run = () =>
            {
                first.Reset();
                second.Reset();
                asynchronousInputs[0] = first.Task;
                asynchronousInputs[1] = second.Task;
                var winner = CrowTask.WhenAny(asynchronousInputs, 2);
                first.Succeed();
                winner.GetAwaiter().GetResult();
                second.Succeed();
            };
            for (int i = 0; i < 10; i++) run();
            recorder.Reset();
            for (int i = 0; i < 100; i++) run();
            int asynchronousWhenAny = recorder.Count;
            AllocationMetrics.Clear();
            AllocationMetrics.Add("arrayControl", arrayControl);
            AllocationMetrics.Add("referenceControl", referenceControl);
            AllocationMetrics.Add("stringControl", stringControl);
            AllocationMetrics.Add("legacyUpper100", oldUpper);
            AllocationMetrics.Add("newUpper100", newUpper);
            AllocationMetrics.Add("synchronousWhenAny100", synchronousWhenAny);
            AllocationMetrics.Add("asynchronousWhenAny100", asynchronousWhenAny);
            UnityEngine.Debug.Log($"CrowRx GC comparison: array={arrayControl} reference={referenceControl} string={stringControl} legacyUpper100={oldUpper} newUpper100={newUpper} synchronousWhenAny100={synchronousWhenAny} asynchronousWhenAny100={asynchronousWhenAny}");
            Assert.That(recorder.Valid, Is.True);
            Assert.That(arrayControl, Is.GreaterThan(0));
            if (stringControl > 0 && oldUpper > 0) Assert.That(newUpper, Is.LessThan(oldUpper));
            if (referenceControl > 0)
            {
                Assert.That(synchronousWhenAny, Is.Zero);
                Assert.That(asynchronousWhenAny, Is.Zero);
            }
        }
        finally
        {
            UnityEngine.Profiling.Profiler.enabled = wasEnabled;
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.Memory, memoryEnabled);
        }
    }

    [Test]
    public void ListConversionsShareTheInputAndPreserveNull()
    {
        var list = new List<int> { 1 };
        SerializedReferenceList<int> wrapper = list;
        Assert.That(wrapper.Items, Is.SameAs(list));
        wrapper.Add(2);
        Assert.That(list.Count, Is.EqualTo(2));
        Assert.That((List<int>)wrapper, Is.SameAs(list));
        Assert.That((SerializedReferenceList<int>)(List<int>)null, Is.Null);
        Assert.That((List<int>)(SerializedReferenceList<int>)null, Is.Null);
        Assert.That(new SerializedReferenceList<int>().Count, Is.Zero);
    }

    private static object Source(UniTask<int> task) => typeof(UniTask<int>).GetField("source", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(task);
    private static async Task<int> Observe(UniTask<int> task) => await task;

    private sealed class CountedSource : IUniTaskSource
    {
        private UniTaskCompletionSourceCore<AsyncUnit> _core;
        public int Consumed;
        public UniTask Task => new(this, _core.Version);
        public void Succeed() => _core.TrySetResult(AsyncUnit.Default);
        public void Fail(Exception exception) => _core.TrySetException(exception);

        public void Reset()
        {
            _core.Reset();
            Consumed = 0;
        }

        public void GetResult(short token)
        {
            Interlocked.Increment(ref Consumed);
            _core.GetResult(token);
        }

        public UniTaskStatus GetStatus(short token) => _core.GetStatus(token);
        public UniTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();
        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);
    }
}

public class NameOuter<T>
{
    public class First<U>
    {
    }

    public class Second<U>
    {
    }

    public class Plain
    {
    }
}