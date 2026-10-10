using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CrowRx;
using CrowRx.Helper;
using CrowRx.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class CrowRxLifecycleTests
{
    private static readonly FieldInfo FadeSource = typeof(CanvasGroupFade).GetField("_ctsFade", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo TypeSnapshot = typeof(RuntimeTypeCache).GetField("_cache", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo GatherSnapshot = typeof(RuntimeTypeCache).GetMethod("GatherTypes", BindingFlags.Static | BindingFlags.NonPublic);

    [Test]
    public void ImmediateFadeClearsItsSourceAndCanStartAgain()
    {
        GameObject root = new("fade lifetime test");
        try
        {
            CanvasGroupFade fade = root.AddComponent<CanvasGroupFade>();
            fade.Fade(0, 1, 0, true, true);
            Assert.That(FadeSource.GetValue(fade), Is.Null);
            Assert.That(fade.CanvasGroup.alpha, Is.EqualTo(1));
            fade.Fade(1, 0, 0, true, true);
            Assert.That(FadeSource.GetValue(fade), Is.Null);
            Assert.That(fade.CanvasGroup.alpha, Is.Zero);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [UnityTest]
    public IEnumerator CompletedFadeDisposesItsSourceAndCanStartAgain()
    {
        GameObject root = new("completed fade test");
        try
        {
            CanvasGroupFade fade = root.AddComponent<CanvasGroupFade>();
            fade.Fade(0, 1, 0.01f, true, true);
            var source = (CancellationTokenSource)FadeSource.GetValue(fade);
            yield return WaitUntil(() => FadeSource.GetValue(fade) is null);
            AssertDisposed(source);
            Assert.That(fade.CanvasGroup.alpha, Is.EqualTo(1));
            fade.Fade(1, 0, 0, true, true);
            Assert.That(fade.CanvasGroup.alpha, Is.Zero);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [UnityTest]
    public IEnumerator ReplacedFadeCannotClearTheNewSource()
    {
        GameObject root = new("replaced fade test");
        try
        {
            CanvasGroupFade fade = root.AddComponent<CanvasGroupFade>();
            fade.Fade(0, 1, 10, true, true);
            var previous = (CancellationTokenSource)FadeSource.GetValue(fade);
            fade.Fade(1, 0, 10, true, true);
            var current = (CancellationTokenSource)FadeSource.GetValue(fade);
            Assert.That(previous.IsCancellationRequested, Is.True);
            yield return WaitUntil(() => IsDisposed(previous));
            Assert.That(FadeSource.GetValue(fade), Is.SameAs(current));
            root.SetActive(false);
            Assert.That(FadeSource.GetValue(fade), Is.Null);
            yield return WaitUntil(() => IsDisposed(current));
            float alpha = fade.CanvasGroup.alpha;
            yield return null;
            Assert.That(fade.CanvasGroup.alpha, Is.EqualTo(alpha));
            root.SetActive(true);
            fade.Fade(0, 1, 0, true, true);
            Assert.That(fade.CanvasGroup.alpha, Is.EqualTo(1));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [UnityTest]
    public IEnumerator DestroyedFadeDisposesItsSourceAfterCancellation()
    {
        GameObject root = new("destroyed fade test");
        try
        {
            CanvasGroupFade fade = root.AddComponent<CanvasGroupFade>();
            fade.enabled = false; // OnDisable 이후 시작된 작업도 OnDestroy에서 취소해야 합니다.
            fade.Fade(0, 1, 10, true, true);
            var source = (CancellationTokenSource)FadeSource.GetValue(fade);
            Object.Destroy(root);
            yield return WaitUntil(() => IsDisposed(source));
        }
        finally { if (root) Object.DestroyImmediate(root); }
    }

    [UnityTest]
    public IEnumerator FadeInOutCompletionCanStartAnotherFade()
    {
        GameObject root = new("fade callback test");
        try
        {
            CanvasGroupFade fade = root.AddComponent<CanvasGroupFade>();
            var completed = new UnityEvent();
            int calls = 0;
            CancellationTokenSource next = null;
            completed.AddListener(() =>
            {
                calls++;
                fade.Fade(0, 1, 10, true, true);
                next = (CancellationTokenSource)FadeSource.GetValue(fade);
            });
            typeof(CanvasGroupFade).GetField("onTaskCompleted", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(fade, completed);
            fade.FadeInOut(0, 1, 0, 0, 0, true);
            yield return WaitUntil(() => calls > 0);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(FadeSource.GetValue(fade), Is.SameAs(next));
            root.SetActive(false);
            yield return WaitUntil(() => IsDisposed(next));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void FailedFadeAlsoClearsItsSource()
    {
        GameObject root = new("failed fade test");
        try
        {
            CanvasGroupFade fade = root.AddComponent<CanvasGroupFade>();
            LogAssert.Expect(LogType.Exception, new Regex("ArgumentOutOfRangeException"));
            fade.Fade(0, 1, float.NaN, true, true);
            Assert.That(FadeSource.GetValue(fade), Is.Null);
            fade.Fade(0, 1, 0, true, true);
            Assert.That(fade.CanvasGroup.alpha, Is.EqualTo(1));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ThrowingEndCallbackRunsOnlyOnce(bool generic, bool cancellationException)
    {
        int calls = 0;
        Exception error = cancellationException ? new OperationCanceledException() : new InvalidOperationException("callback failure");
        Action onEnd = () => { calls++; throw error; };
        Task observed = Observe(generic
            ? UniTask.FromResult(7).ContinueWithAnyway(onEnd)
            : UniTask.CompletedTask.ContinueWithAnyway(onEnd));
        Assert.That(calls, Is.EqualTo(1));
        Exception actual = Assert.Throws(error.GetType(), () => observed.GetAwaiter().GetResult());
        if (cancellationException) Assert.That(observed.IsCanceled, Is.True);
        else Assert.That(actual, Is.SameAs(error));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TaskOutcomeSelectsOneCallback(bool generic)
    {
        foreach (int outcome in new[] { 0, 1, 2 })
        {
            int success = 0, canceled = 0, failed = 0;
            var error = new InvalidOperationException("task failure");
            using var source = new CancellationTokenSource(); source.Cancel();
            UniTask task;
            if (generic)
            {
                UniTask<int> original = outcome == 0 ? UniTask.FromResult(7) : outcome == 1
                    ? UniTask.FromCanceled<int>(source.Token) : UniTask.FromException<int>(error);
                task = original.ContinueWith(value => { Assert.That(value, Is.EqualTo(7)); success++; }, () => canceled++, e => { Assert.That(e, Is.SameAs(error)); failed++; });
            }
            else
            {
                UniTask original = outcome == 0 ? UniTask.CompletedTask : outcome == 1
                    ? UniTask.FromCanceled(source.Token) : UniTask.FromException(error);
                task = original.ContinueWith(() => success++, () => canceled++, e => { Assert.That(e, Is.SameAs(error)); failed++; });
            }
            Observe(task).GetAwaiter().GetResult();
            Assert.That(success, Is.EqualTo(outcome == 0 ? 1 : 0));
            Assert.That(canceled, Is.EqualTo(outcome == 1 ? 1 : 0));
            Assert.That(failed, Is.EqualTo(outcome == 2 ? 1 : 0));
        }
    }

    [UnityTest]
    public IEnumerator NullSuccessCallbackStillAwaitsGenericTask()
    {
        var original = new UniTaskCompletionSource<int>();
        Task continuation = Observe(original.Task.ContinueWith(null, null, null));
        Assert.That(continuation.IsCompleted, Is.False);
        original.TrySetResult(7);
        yield return WaitUntil(() => continuation.IsCompleted);
        continuation.GetAwaiter().GetResult();
    }

    [Test]
    public void EmptyTypeSnapshotIsReadyAndRefreshPreservesPreviousViews()
    {
        object previous = TypeSnapshot.GetValue(null);
        try
        {
            var first = new TestAssembly("CrowRx.Test.First", () => new[] { typeof(CacheTypeA) });
            Gather(new[] { first });
            IReadOnlyList<Type> oldTypes = RuntimeTypeCache.Types;
            IReadOnlyDictionary<string, Type> oldNames = RuntimeTypeCache.TypesByName;
            Gather(new[] { new TestAssembly("CrowRx.Test.Second", () => new[] { typeof(CacheTypeB) }) });
            Assert.That(RuntimeTypeCache.Types, Is.EquivalentTo(new[] { typeof(CacheTypeB) }));
            Assert.That(oldTypes, Is.EquivalentTo(new[] { typeof(CacheTypeA) }));
            Assert.That(oldNames[typeof(CacheTypeA).GetRealTypeFullName()], Is.EqualTo(typeof(CacheTypeA)));
            Gather(Array.Empty<Assembly>());
            object empty = TypeSnapshot.GetValue(null);
            Assert.That(RuntimeTypeCache.Types.Count, Is.Zero);
            Assert.That(RuntimeTypeCache.TypesByName.Count, Is.Zero);
            Assert.That(TypeSnapshot.GetValue(null), Is.SameAs(empty));
        }
        finally { TypeSnapshot.SetValue(null, previous); }
    }

    [Test]
    public void TypeLoadFailureUsesValidTypesAndFiltersExcludedTypes()
    {
        object previous = TypeSnapshot.GetValue(null);
        try
        {
#pragma warning disable CS0612, CS0618 // 타입 캐시가 Obsolete 타입을 제외하는지 검사합니다.
            var partial = new TestAssembly("CrowRx.Test.Partial", () => throw new ReflectionTypeLoadException(
                new[] { typeof(CacheTypeA), null, typeof(HiddenCacheType), typeof(ObsoleteCacheType), typeof(List<>), typeof(string) }, new Exception[] { new TypeLoadException("missing") }));
#pragma warning restore CS0612, CS0618
            var excluded = new TestAssembly("Unity.Test.Excluded", () => throw new Exception("excluded assembly was scanned"));
            LogAssert.Expect(LogType.Warning, "Some types could not be loaded from CrowRx.Test.Partial.");
            Gather(new[] { partial, excluded });
            Assert.That(RuntimeTypeCache.Types, Is.EquivalentTo(new[] { typeof(CacheTypeA) }));
            Assert.That(RuntimeTypeCache.TypesByName[typeof(CacheTypeA).GetRealTypeFullName()], Is.EqualTo(typeof(CacheTypeA)));
        }
        finally { TypeSnapshot.SetValue(null, previous); }
    }

    [Test]
    public void UnexpectedTypeScanFailureRetainsBothPreviousCaches()
    {
        object previous = TypeSnapshot.GetValue(null);
        try
        {
            Gather(new[] { new TestAssembly("CrowRx.Test.Before", () => new[] { typeof(CacheTypeA) }) });
            object ready = TypeSnapshot.GetValue(null);
            var error = new InvalidOperationException("scan failure");
            TargetInvocationException actual = Assert.Throws<TargetInvocationException>(() => Gather(new Assembly[]
            {
                new TestAssembly("CrowRx.Test.Partial", () => new[] { typeof(CacheTypeB) }),
                new TestAssembly("CrowRx.Test.Failure", () => throw error)
            }));
            Assert.That(actual.InnerException, Is.SameAs(error));
            Assert.That(TypeSnapshot.GetValue(null), Is.SameAs(ready));
            Assert.That(RuntimeTypeCache.Types, Is.EquivalentTo(new[] { typeof(CacheTypeA) }));
            Assert.That(RuntimeTypeCache.TypesByName[typeof(CacheTypeA).GetRealTypeFullName()], Is.EqualTo(typeof(CacheTypeA)));
        }
        finally { TypeSnapshot.SetValue(null, previous); }
    }

    public sealed class CacheTypeA { }
    public sealed class CacheTypeB { }
    [Obsolete] public sealed class ObsoleteCacheType { }
    private sealed class HiddenCacheType { }

    private sealed class TestAssembly : Assembly
    {
        private readonly string _name;
        private readonly Func<Type[]> _getTypes;
        public TestAssembly(string name, Func<Type[]> getTypes) { _name = name; _getTypes = getTypes; }
        public override string FullName => _name;
        public override bool IsDynamic => false;
        public override Type[] GetTypes() => _getTypes();
    }

    private static void Gather(Assembly[] assemblies) => GatherSnapshot.Invoke(null, new object[] { assemblies });
    private static async Task Observe(UniTask task) => await task;
    private static void AssertDisposed(CancellationTokenSource source) => Assert.That(IsDisposed(source), Is.True);
    private static bool IsDisposed(CancellationTokenSource source)
    {
        try { _ = source.Token; return false; }
        catch (ObjectDisposedException) { return true; }
    }
    private static IEnumerator WaitUntil(Func<bool> completed)
    {
        float deadline = Time.realtimeSinceStartup + 3;
        while (!completed() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(completed(), Is.True, "operation completed before timeout");
    }
}
