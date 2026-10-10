using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using CrowRx.Tasks;
using CrowRx.Utility;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class MathmTests
{
    [Test]
    public void UserFixesSkipCanceledFactoriesAndReturnMatrixResult()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        int calls = 0;
        CrowTask.RunSafe(token => { calls++; return UniTask.CompletedTask; }, canceled.Token);
        CrowTask.RunSafe<int>(token => { calls++; return UniTask.FromResult(1); }, canceled.Token);
        Assert.That(calls, Is.Zero);
        using var active = new CancellationTokenSource();
        CancellationToken received = default, receivedGeneric = default;
        CrowTask.RunSafe(token => { received = token; calls++; return UniTask.CompletedTask; }, active.Token);
        CrowTask.RunSafe<int>(token => { receivedGeneric = token; calls++; return UniTask.FromResult(1); }, active.Token);
        Assert.That(calls, Is.EqualTo(2));
        Assert.That(received, Is.EqualTo(active.Token)); Assert.That(receivedGeneric, Is.EqualTo(active.Token));

        Matrix4x4 original = Matrix4x4.identity;
        Matrix4x4 target = Matrix4x4.Scale(new Vector3(2, 3, 4));
        Matrix4x4 result = original.Lerp(target, 0.5f);
        Assert.That(original, Is.EqualTo(Matrix4x4.identity));
        Assert.That(result.m00, Is.EqualTo(1.5f));
        Assert.That(result.m11, Is.EqualTo(2f));
        Assert.That(result.m22, Is.EqualTo(2.5f));
        Assert.That(original.Lerp(target, 0f), Is.EqualTo(original));
        Assert.That(original.Lerp(target, 1f), Is.EqualTo(target));
    }

    [TestCase(0f, 0f, 1f, 0f, 2f, 0f, 3f, 0f, false, 0f, 0f)]
    [TestCase(0f, 0f, 1f, 0f, 0f, 1f, 1f, 1f, false, 0f, 0f)]
    [TestCase(0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, true, 0f, 0f)]
    [TestCase(0f, 0f, 1f, 0f, 1f, 0f, 2f, 0f, true, 1f, 0f)]
    [TestCase(0f, 0f, 1f, 1f, 0f, 1f, 1f, 0f, true, 0.5f, 0.5f)]
    [TestCase(0f, 0f, 3f, 0f, 1f, 0f, 2f, 0f, true, 1f, 0f)]
    [TestCase(3f, 0f, 0f, 0f, 1f, 0f, 2f, 0f, true, 2f, 0f)]
    [TestCase(0f, 0f, 1f, 0f, -1f, 0f, 2f, 0f, true, 0f, 0f)]
    [TestCase(0.5f, 0f, 0.5f, 0f, 0f, 0f, 1f, 0f, true, 0.5f, 0f)]
    [TestCase(0f, 0f, 1f, 0f, 0.5f, 0f, 0.5f, 0f, true, 0.5f, 0f)]
    [TestCase(2f, 0f, 2f, 0f, 0f, 0f, 1f, 0f, false, 0f, 0f)]
    [TestCase(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, true, 0f, 0f)]
    [TestCase(0f, 0f, 0f, 0f, 1f, 0f, 1f, 0f, false, 0f, 0f)]
    [TestCase(0f, 0f, 1f, 0.000001f, 0f, 0.000001f, 1f, 0f, true, 0.5f, 0.0000005f)]
    [TestCase(1000000f, 1000000f, 1000002f, 1000002f, 1000000f, 1000002f, 1000002f, 1000000f, true, 1000001f, 1000001f)]
    public void SegmentIntersectionsIncludeEndpointsAndDegenerateSegments(float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy, bool expected, float px, float py)
    {
        Assert.That(Mathm.GetIntersectPosition(new Vector3(ax, ay, 10), new Vector3(bx, by, 20),
            new Vector3(cx, cy, 30), new Vector3(dx, dy, 40), out Vector3 point), Is.EqualTo(expected));
        Assert.That(point.x, Is.EqualTo(px).Within(0.00001f));
        Assert.That(point.y, Is.EqualTo(py).Within(0.00001f));
        Assert.That(point.z, Is.Zero);
    }

    [Test]
    public void RectangleIntersectionsRespectEdgesAndSeparatedCollinearSegments()
    {
        Rect rect = new(0, 0, 1, 1);
        Assert.That(rect.Intersect(new Vector2(-1, 0), new Vector2(2, 0)), Is.True);
        Assert.That(rect.Intersect(new Vector2(1, 1), new Vector2(2, 2)), Is.True);
        Assert.That(rect.Intersect(new Vector2(2, 0), new Vector2(3, 0)), Is.False);
        Assert.That(rect.Intersect(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)), Is.True);
        Assert.That(rect.Intersect(new Vector2(-1, 0.5f), new Vector2(2, 0.5f)), Is.True);
        Vector3[] corners = { new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0) };
        Assert.That(Mathm.RectIntersect(corners, new Vector2(-1, 0), new Vector2(2, 0)), Is.True);
        Assert.That(Mathm.RectIntersect(corners, new Vector2(2, 0), new Vector2(3, 0)), Is.False);
    }

    [Test]
    public void ColliderContainsUsesActualShapeAndRejectsDisabledTargets()
    {
        var root = new GameObject("CrowRx containment regression");
        Mesh mesh = null;
        try
        {
            root.transform.position = new Vector3(10, 2, -3);
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.size = Vector3.one * 2;
            Physics.SyncTransforms();
            Assert.That(box.Contains(new Vector3(12, 2, -3), false), Is.False);
            Assert.That(box.Contains(root.transform.position), Is.True);
            Assert.That(box.Contains(new Vector3(11, 2, -3)), Is.True);
            root.transform.rotation = Quaternion.Euler(15, 30, 10);
            root.transform.localScale = new Vector3(2, 1, 0.5f);
            box.center = new Vector3(0.25f, 0, 0);
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.centerOfMass = new Vector3(100, 100, 100);
            Physics.SyncTransforms();
            Vector3 inside = root.transform.TransformPoint(box.center);
            Vector3 outside = root.transform.TransformPoint(box.center + new Vector3(1.1f, 0, 0));
            Assert.That(box.Contains(inside, true), Is.True);
            Assert.That(box.Contains(inside, false), Is.True);
            Assert.That(box.Contains(outside, true), Is.False);
            Assert.That(box.Contains(outside, false), Is.False);
            box.enabled = false;
            Assert.That(box.Contains(inside), Is.False);

            SphereCollider sphere = root.AddComponent<SphereCollider>();
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 0;
            capsule.height = 4;
            capsule.radius = 0.5f;
            Physics.SyncTransforms();
            Assert.That(sphere.Contains(root.transform.position), Is.True);
            Assert.That(sphere.Contains(root.transform.position + Vector3.one * 20), Is.False);
            Assert.That(capsule.Contains(root.transform.position), Is.True);
            Assert.That(capsule.Contains(root.transform.position + Vector3.one * 20), Is.False);

            MeshCollider convex = root.AddComponent<MeshCollider>();
            Assert.That(convex.Contains(root.transform.position), Is.False);
            mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up, Vector3.forward },
                triangles = new[] { 0, 2, 1, 0, 1, 3, 0, 3, 2, 1, 2, 3 } };
            convex.sharedMesh = mesh;
            convex.convex = true;
            Physics.SyncTransforms();
            Assert.That(convex.Contains(root.transform.TransformPoint(Vector3.one * 0.1f)), Is.True);
            Assert.That(convex.Contains(root.transform.TransformPoint(Vector3.one * 2)), Is.False);
            convex.convex = false;
            Assert.That(convex.Contains(root.transform.TransformPoint(Vector3.one * 0.1f)), Is.False);
            root.SetActive(false);
            Assert.That(sphere.Contains(inside), Is.False);
            Assert.That(Mathm.Contains(null, inside), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (mesh) Object.DestroyImmediate(mesh);
        }
    }

    [TestCase(0f, 0.016f)]
    [TestCase(-1f, 0.016f)]
    [TestCase(0.00000001f, 0.016f)]
    [TestCase(9f, 0f)]
    [TestCase(9f, -1f)]
    [TestCase(9f, 2f)]
    [TestCase(1000f, 0.016f)]
    [TestCase(1500f, 0.016f)]
    public void DampingBoundaryInputsStayFiniteAndOverloadsAgree(float strength, float time)
    {
        float scalar = 2f;
        Vector2 v2 = new(2, -4);
        Vector3 v3 = new(2, -4, 6);
        float displacement = Mathm.SpringDampen(ref scalar, strength, time);
        Vector2 d2 = Mathm.SpringDampen(ref v2, strength, time);
        Vector3 d3 = Mathm.SpringDampen(ref v3, strength, time);
        Assert.That(float.IsNaN(scalar) || float.IsInfinity(scalar) || float.IsNaN(displacement) || float.IsInfinity(displacement), Is.False);
        Assert.That(v2.x, Is.EqualTo(scalar)); Assert.That(v3.x, Is.EqualTo(scalar));
        Assert.That(d2.x, Is.EqualTo(displacement)); Assert.That(d3.x, Is.EqualTo(displacement));
        Assert.That(d2.y, Is.EqualTo(-2 * displacement).Within(0.00001f));
        Assert.That(d3.z, Is.EqualTo(3 * displacement).Within(0.00001f));
        if (strength <= 0)
        {
            Assert.That(scalar, Is.EqualTo(2));
            Assert.That(displacement, Is.EqualTo(1.92f).Within(0.00001f));
        }
        if (time <= 0) { Assert.That(scalar, Is.EqualTo(2)); Assert.That(displacement, Is.Zero); }
        if (strength >= 1000) { Assert.That(scalar, Is.Zero); Assert.That(displacement, Is.Zero); }
    }

    [Test]
    public void DampingRetainsNormalScaleAndHandlesTinyStrength()
    {
        float original = 2f;
        float factor = 1f - 9f * 0.001f;
        float attenuation = Mathf.Pow(factor, 16);
        float legacy = original * (attenuation - 1f) / Mathf.Log(factor) * 0.06f;
        float velocity = original;
        float displacement = Mathm.SpringDampen(ref velocity, 9f, 0.016f);
        Assert.That(velocity, Is.EqualTo(original * attenuation).Within(0.00001f));
        Assert.That(displacement, Is.EqualTo(legacy).Within(0.0001f));
        velocity = 2f;
        Assert.That(Mathm.SpringDampen(ref velocity, float.Epsilon, 0.016f), Is.EqualTo(1.92f).Within(0.00001f));
        Assert.That(velocity, Is.EqualTo(2f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Mathm.SpringDampen(ref velocity, float.NaN, 0.016f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Mathm.SpringDampen(ref velocity, 9f, float.PositiveInfinity));
    }

    [Test]
    public void PreCanceledLerpNeverCallsGetterOrSetter()
    {
        using var source = new CancellationTokenSource(); source.Cancel();
        foreach (float duration in new[] { 0f, 1f })
        foreach (bool reset in new[] { false, true })
        foreach (bool sameValue in new[] { false, true })
        {
            int writes = 0, reads = 0;
            Assert.That(Observe(Mathm.LerpAsync(_ => writes++, () => { reads++; return 0f; }, 0f, sameValue ? 0f : 1f, duration, reset, true, source.Token)).IsCanceled, Is.True);
            Assert.That(Observe(Mathm.LerpAsync(_ => writes++, () => { reads++; return Vector3.zero; }, Vector3.zero, sameValue ? Vector3.zero : Vector3.one, duration, reset, true, source.Token)).IsCanceled, Is.True);
            Assert.That(Observe(Mathm.LerpAsync(AnimationCurve.Linear(0, 0, 1, 1), _ => writes++, () => { reads++; return Vector3.zero; }, Vector3.zero, sameValue ? Vector3.zero : Vector3.one, duration, reset, true, source.Token)).IsCanceled, Is.True);
            Assert.That(writes + reads, Is.Zero);
        }
    }

    [UnityTest]
    public IEnumerator CancellationInsideCallbacksIsObservedAtNextYield()
    {
        foreach (bool immediate in new[] { false, true })
        {
            using var scalar = new CancellationTokenSource();
            Task scalarTask = Observe(Mathm.LerpAsync(_ => scalar.Cancel(), () => 0f, 0f, 1f, immediate ? 0f : 1f, true, true, scalar.Token));
            yield return WaitForCompletion(scalarTask);
            Assert.That(scalarTask.IsCanceled, Is.EqualTo(!immediate));
            if (immediate) scalarTask.GetAwaiter().GetResult();
            using var vector = new CancellationTokenSource();
            Task vectorTask = Observe(Mathm.LerpAsync(_ => vector.Cancel(), () => Vector3.zero, Vector3.zero, Vector3.one, immediate ? 0f : 1f, true, true, vector.Token));
            yield return WaitForCompletion(vectorTask);
            Assert.That(vectorTask.IsCanceled, Is.EqualTo(!immediate));
            if (immediate) vectorTask.GetAwaiter().GetResult();
        }
        using var getter = new CancellationTokenSource();
        int writes = 0;
        Task task = Observe(Mathm.LerpAsync(_ => writes++, () => { getter.Cancel(); return 0f; }, 0f, 1f, 1f, false, true, getter.Token));
        yield return WaitForCompletion(task);
        Assert.That(task.IsCanceled, Is.True); Assert.That(writes, Is.Zero);
    }

    [UnityTest]
    public IEnumerator CancellationInFinalSetterKeepsCompletedResult()
    {
        using var scalar = new CancellationTokenSource();
        using var vector = new CancellationTokenSource();
        Task scalarTask = Observe(Mathm.LerpAsync(value => { if (value == 1f) scalar.Cancel(); }, () => 0f, 0f, 1f, 0.000001f, true, true, scalar.Token));
        Task vectorTask = Observe(Mathm.LerpAsync(value => { if (value == Vector3.one) vector.Cancel(); }, () => Vector3.zero, Vector3.zero, Vector3.one, 0.000001f, true, true, vector.Token));
        yield return WaitForCompletion(scalarTask); yield return WaitForCompletion(vectorTask);
        scalarTask.GetAwaiter().GetResult(); vectorTask.GetAwaiter().GetResult();
        Assert.That(scalar.IsCancellationRequested, Is.True); Assert.That(vector.IsCancellationRequested, Is.True);
    }

    [Test]
    public void InvalidDurationsFailBeforeWritingValues()
    {
        int writes = 0;
        Task scalar = Observe(Mathm.LerpAsync(_ => writes++, () => 0f, 0f, 1f, float.NaN, true, true, CancellationToken.None));
        Task vector = Observe(Mathm.LerpAsync(_ => writes++, () => Vector3.zero, Vector3.zero, Vector3.one, float.PositiveInfinity, true, true, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => scalar.GetAwaiter().GetResult());
        Assert.Throws<ArgumentOutOfRangeException>(() => vector.GetAwaiter().GetResult());
        Assert.That(writes, Is.Zero);
    }

    [UnityTest]
    public IEnumerator NormalLerpAndCurveReachExactEndpoint()
    {
        float scalar = -1;
        Vector3 vector = Vector3.zero;
        int scalarReads = 0;
        Task scalarTask = Observe(Mathm.LerpAsync(value => scalar = value, () => { scalarReads++; return 0.5f; }, 0f, 1f, 0.02f, false, true, CancellationToken.None));
        Task vectorTask = Observe(Mathm.LerpAsync(AnimationCurve.Linear(0, 0, 1, 0), value => vector = value, () => Vector3.zero, Vector3.zero, Vector3.one, 0.02f, true, true, CancellationToken.None));
        yield return WaitForCompletion(scalarTask);
        yield return WaitForCompletion(vectorTask);
        scalarTask.GetAwaiter().GetResult(); vectorTask.GetAwaiter().GetResult();
        Assert.That(scalar, Is.EqualTo(1)); Assert.That(vector, Is.EqualTo(Vector3.one)); Assert.That(scalarReads, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator CancellationDuringYieldStopsFurtherWrites()
    {
        using var source = new CancellationTokenSource();
        int writes = 0;
        Task task = Observe(Mathm.LerpAsync(_ => writes++, () => 0f, 0f, 1f, 10f, true, true, source.Token));
        yield return null;
        source.Cancel();
        int before = writes;
        yield return WaitForCompletion(task);
        Assert.That(task.IsCanceled, Is.True); Assert.That(writes, Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator LerpRespectsScaledAndUnscaledClocks()
    {
        float previousScale = Time.timeScale;
        using var source = new CancellationTokenSource();
        try
        {
            Time.timeScale = 0f;
            Task scaled = Observe(Mathm.LerpAsync(_ => { }, () => 0f, 0f, 1f, 0.02f, true, false, source.Token));
            Task unscaled = Observe(Mathm.LerpAsync(_ => { }, () => 0f, 0f, 1f, 0.02f, true, true, CancellationToken.None));
            yield return WaitForCompletion(unscaled);
            unscaled.GetAwaiter().GetResult();
            Assert.That(scaled.IsCompleted, Is.False);
            source.Cancel(); yield return WaitForCompletion(scaled);
            Assert.That(scaled.IsCanceled, Is.True);
        }
        finally { source.Cancel(); Time.timeScale = previousScale; }
    }

    // 설치된 UniTask.AsTask는 취소를 Faulted로 바꿉니다. 실제 await의 취소 의미를 검사합니다.
    private static async Task Observe(UniTask task) => await task;

    private static IEnumerator WaitForCompletion(Task task)
    {
        float deadline = Time.realtimeSinceStartup + 3f;
        while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(task.IsCompleted, Is.True, "async operation completed before timeout");
    }
}
