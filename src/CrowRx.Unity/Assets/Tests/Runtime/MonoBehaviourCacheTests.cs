using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using CrowRx;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class MonoBehaviourCacheTests
{
    private static readonly FieldInfo GameObjectCache = Field("_gameObject");
    private static readonly FieldInfo TransformCache = Field("_transform");
    private static readonly FieldInfo Initialized = Field("_cacheInitialized");
    private static readonly FieldInfo Registration = Field("_cacheDestroyRegistration");
    private static readonly FieldInfo Monitor = Field("_activationMonitor");

    [TestCase(false)]
    [TestCase(true)]
    public void CachesBothReferencesAndCastsRectTransformWithoutAddingTriggers(bool ui)
    {
        GameObject root = MakeObject(ui);
        try
        {
            MonoBehaviourCacheProbe probe = root.AddComponent<MonoBehaviourCacheProbe>();
            Assert.That(Initialized.GetValue(probe), Is.False);
            Assert.That(probe.rectTransform, Is.SameAs(ui ? root.transform : null));
            Assert.That(probe.gameObject, Is.SameAs(root));
            Assert.That(probe.transform, Is.SameAs(root.transform));
            Assert.That(GameObjectCache.GetValue(probe), Is.SameAs(root));
            Assert.That(TransformCache.GetValue(probe), Is.SameAs(root.transform));
            Assert.That(root.GetComponents<Component>().Length, Is.EqualTo(2));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void TransformConversionRefreshesAfterExplicitInvalidation()
    {
        GameObject root = MakeObject(false);
        try
        {
            MonoBehaviourCacheProbe probe = root.AddComponent<MonoBehaviourCacheProbe>();
            Assert.That(probe.rectTransform, Is.Null);
            var registration = (CancellationTokenRegistration)Registration.GetValue(probe);
            RectTransform converted = root.AddComponent<RectTransform>();
            Assert.That(converted, Is.Not.Null);
            probe.InvalidateTransformCache();
            Assert.That(probe.transform, Is.SameAs(converted));
            Assert.That(probe.rectTransform, Is.SameAs(converted));
            Assert.That(probe.gameObject, Is.SameAs(root));
            Assert.That((CancellationTokenRegistration)Registration.GetValue(probe), Is.EqualTo(registration));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void ComponentOnlyRemovalClearsActualReferencesAndCannotReinitialize()
    {
        GameObject root = MakeObject(true);
        try
        {
            MonoBehaviourCacheProbe probe = root.AddComponent<MonoBehaviourCacheProbe>();
            _ = probe.rectTransform;
            Object.DestroyImmediate(probe);
            Assert.That(root, Is.Not.Null);
            Assert.That(probe.OnDestroyCalled, Is.True);
            AssertCleared(probe);
            probe.InvalidateTransformCache();
            Assert.That(probe.gameObject, Is.Null);
            Assert.That(probe.transform, Is.Null);
            Assert.That(probe.rectTransform, Is.Null);
            AssertCleared(probe);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [UnityTest]
    public IEnumerator GameObjectDestructionClearsReferencesDespiteDerivedOnDestroy()
    {
        GameObject root = MakeObject(true);
        try
        {
            MonoBehaviourCacheProbe probe = root.AddComponent<MonoBehaviourCacheProbe>();
            _ = probe.gameObject;
            Object.Destroy(root);
            yield return null;
            Assert.That(probe.OnDestroyCalled, Is.True);
            AssertCleared(probe);
            Assert.That(probe.rectTransform, Is.Null);
        }
        finally { if (root) Object.DestroyImmediate(root); }
    }

    [UnityTest]
    public IEnumerator NeverActivatedObjectAlsoClearsItsCacheOnDestruction()
    {
        GameObject root = MakeObject(false);
        try
        {
            root.SetActive(false);
            MonoBehaviourCacheProbe probe = root.AddComponent<MonoBehaviourCacheProbe>();
            Assert.That(probe.gameObject, Is.SameAs(root));
            Assert.That(Monitor.GetValue(probe), Is.Not.Null);
            yield return null;
            Assert.That(probe.gameObject, Is.SameAs(root));
            Assert.That(Monitor.GetValue(probe), Is.Not.Null);
            Object.DestroyImmediate(root);
            yield return null;
            AssertCleared(probe);
        }
        finally { if (root) Object.DestroyImmediate(root); }
    }

    [UnityTest]
    public IEnumerator NeverActivatedComponentOnlyRemovalAlsoClearsItsCache()
    {
        GameObject root = MakeObject(false);
        try
        {
            root.SetActive(false);
            MonoBehaviourCacheProbe probe = root.AddComponent<MonoBehaviourCacheProbe>();
            _ = probe.gameObject;
            Object.DestroyImmediate(probe);
            yield return null;
            Assert.That(root, Is.Not.Null);
            AssertCleared(probe);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [UnityTest]
    public IEnumerator FirstActivationStopsMonitoringAndDestructionStillClearsReferences()
    {
        GameObject root = MakeObject(false);
        try
        {
            root.SetActive(false);
            MonoBehaviourCacheProbe probe = root.AddComponent<MonoBehaviourCacheProbe>();
            _ = probe.gameObject;
            Assert.That(Monitor.GetValue(probe), Is.Not.Null);
            root.SetActive(true);
            yield return null;
            Assert.That(probe.didAwake, Is.True);
            Assert.That(Monitor.GetValue(probe), Is.Null);
            Assert.That(root.GetComponents<Component>().Length, Is.EqualTo(2));
            Object.DestroyImmediate(probe);
            AssertCleared(probe);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void DisableAndReenableKeepReferencesAndRegistration()
    {
        GameObject root = MakeObject(true);
        try
        {
            MonoBehaviourCacheProbe probe = root.AddComponent<MonoBehaviourCacheProbe>();
            Transform cached = probe.transform;
            var registration = (CancellationTokenRegistration)Registration.GetValue(probe);
            probe.enabled = false;
            root.SetActive(false); root.SetActive(true);
            probe.enabled = true;
            Assert.That(probe.gameObject, Is.SameAs(root));
            Assert.That(probe.transform, Is.SameAs(cached));
            Assert.That(probe.rectTransform, Is.SameAs(cached));
            Assert.That((CancellationTokenRegistration)Registration.GetValue(probe), Is.EqualTo(registration));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void InvalidationAndDestructionBeforeFirstAccessDoNotInitialize()
    {
        GameObject root = MakeObject(false);
        try
        {
            MonoBehaviourCacheProbe probe = root.AddComponent<MonoBehaviourCacheProbe>();
            probe.InvalidateTransformCache();
            Assert.That(Initialized.GetValue(probe), Is.False);
            Object.DestroyImmediate(probe);
            Assert.That(probe.gameObject, Is.Null);
            Assert.That(probe.transform, Is.Null);
            Assert.That(probe.rectTransform, Is.Null);
            Assert.That(Initialized.GetValue(probe), Is.False);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void WarmedGettersAllocateNothingForNormalAndUiObjects(bool ui)
    {
        GameObject root = MakeObject(ui);
        try
        {
            MonoBehaviourCacheProbe probe = root.AddComponent<MonoBehaviourCacheProbe>();
            _ = probe.gameObject; _ = probe.transform; _ = probe.rectTransform;
            var options = ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.CollectOnlyOnCurrentThread;
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 1024, options);
            var calibration = new byte[1024]; GC.KeepAlive(calibration);
            int calibrationEvents = recorder.Count;
            recorder.Reset();
            GameObject owner = null; Transform transform = null; RectTransform rect = null;
            for (int i = 0; i < 100; i++) { owner = probe.gameObject; transform = probe.transform; rect = probe.rectTransform; }
            int allocationEvents = recorder.Count;
            Assert.That(recorder.Valid, Is.True);
            Assert.That(calibrationEvents, Is.GreaterThan(0));
            Assert.That(allocationEvents, Is.Zero);
            Assert.That(owner, Is.SameAs(root));
            Assert.That(transform, Is.SameAs(root.transform));
            Assert.That(rect, Is.SameAs(ui ? root.transform : null));
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static FieldInfo Field(string name) => typeof(MonoBehaviourCrowRx).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static GameObject MakeObject(bool ui) => ui ? new GameObject("UI cache test", typeof(RectTransform)) : new GameObject("normal cache test");
    private static void AssertCleared(MonoBehaviourCacheProbe probe)
    {
        Assert.That(GameObjectCache.GetValue(probe), Is.Null);
        Assert.That(TransformCache.GetValue(probe), Is.Null);
        Assert.That((CancellationTokenRegistration)Registration.GetValue(probe), Is.EqualTo(default(CancellationTokenRegistration)));
        Assert.That(Monitor.GetValue(probe), Is.Null);
    }
}

public sealed class MonoBehaviourCacheProbe : MonoBehaviourCrowRx
{
    public bool OnDestroyCalled;
    private void OnDestroy() => OnDestroyCalled = true;
}
