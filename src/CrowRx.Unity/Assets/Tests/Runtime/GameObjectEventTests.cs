using System;
using System.Collections;
using System.Collections.Generic;
using CrowRx;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class GameObjectEventTests
{
    private GameObject _root;
    private GameObject _source;
    private EventSystem _events;
    private EventSystem _previous;
    private EventTestRaycaster _raycaster;
    private PointerEventData _data;
    private readonly List<Behaviour> _disabled = new();

    [SetUp]
    public void SetUp()
    {
        _previous = EventSystem.current;
        foreach (BaseRaycaster raycaster in Object.FindObjectsByType<BaseRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Disable(raycaster);
        foreach (EventSystem events in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Disable(events);
        _root = new GameObject("event forwarding tests");
        _events = _root.AddComponent<EventSystem>();
        _raycaster = _root.AddComponent<EventTestRaycaster>();
        _source = MakeObject("source");
        _data = new PointerEventData(_events) { position = new Vector2(125, 275), pointerId = 42, button = PointerEventData.InputButton.Right, delta = new Vector2(3, 5) };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        foreach (Behaviour item in _disabled) if (item) item.enabled = true;
        _disabled.Clear();
        if (_previous && _previous.isActiveAndEnabled) EventSystem.current = _previous;
    }

    [Test]
    public void UsesOriginalPointerDataForRaycastAndDelivery()
    {
        GameObject behind = MakeObject("behind");
        EventTestClickHandler handler = behind.AddComponent<EventTestClickHandler>();
        _raycaster.Hits.Add(_source); _raycaster.Hits.Add(behind);
        Assert.That(Forward(), Is.True);
        Assert.That(handler.Calls, Is.EqualTo(1));
        Assert.That(_raycaster.LastData, Is.SameAs(_data));
        Assert.That(handler.LastData, Is.SameAs(_data));
        Assert.That(handler.LastData.position, Is.EqualTo(new Vector2(125, 275)));
        Assert.That(handler.LastData.pointerId, Is.EqualTo(42));
        Assert.That(handler.LastData.button, Is.EqualTo(PointerEventData.InputButton.Right));
        Assert.That(handler.LastData.delta, Is.EqualTo(new Vector2(3, 5)));
    }

    [Test]
    public void DeliversToNearestEligibleParentOfFirstOtherHit()
    {
        GameObject parent = MakeObject("parent");
        GameObject child = MakeObject("hit child"); child.transform.SetParent(parent.transform);
        EventTestClickHandler parentHandler = parent.AddComponent<EventTestClickHandler>();
        EventTestClickHandler rootHandler = _root.AddComponent<EventTestClickHandler>();
        GameObject later = MakeObject("later");
        EventTestClickHandler laterHandler = later.AddComponent<EventTestClickHandler>();
        _raycaster.Hits.Add(_source); _raycaster.Hits.Add(child); _raycaster.Hits.Add(later);
        Assert.That(Forward(), Is.True);
        Assert.That(parentHandler.Calls, Is.EqualTo(1));
        Assert.That(rootHandler.Calls + laterHandler.Calls, Is.Zero);
    }

    [Test]
    public void DoesNotContinuePastFirstHitWithoutAHandler()
    {
        GameObject first = MakeObject("first without handler");
        GameObject later = MakeObject("later with handler");
        EventTestClickHandler laterHandler = later.AddComponent<EventTestClickHandler>();
        _raycaster.Hits.Add(_source); _raycaster.Hits.Add(first); _raycaster.Hits.Add(later);
        Assert.That(Forward(), Is.False);
        Assert.That(laterHandler.Calls, Is.Zero);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void EmptySelfOnlyOrInvalidHitsDoNotReportHandling(int count)
    {
        if (count > 0) _raycaster.Hits.Add(_source);
        if (count > 1) _raycaster.Hits.Add(null);
        Assert.That(Forward(), Is.False);
    }

    [Test]
    public void MissingPointerDataOrEventSystemReturnsFalseWithoutRaycasting()
    {
        Assert.That(_source.ExecuteEventExceptThis(null, ExecuteEvents.pointerClickHandler), Is.False);
        Assert.That(_source.ExecuteEventExceptThis(new BaseEventData(_events), ExecuteEvents.pointerClickHandler), Is.False);
        _events.enabled = false;
        Assert.That(EventSystem.current, Is.Null);
        Assert.That(Forward(), Is.False);
        Assert.That(_raycaster.Calls, Is.Zero);
    }

    [Test]
    public void NestedForwardingUsesIndependentListsAndOriginalData()
    {
        EventTestClickHandler sourceHandler = _source.AddComponent<EventTestClickHandler>();
        GameObject behind = MakeObject("nested source");
        EventTestClickHandler handler = behind.AddComponent<EventTestClickHandler>();
        _raycaster.Hits.Add(_source); _raycaster.Hits.Add(behind);
        handler.Callback = data =>
        {
            List<RaycastResult> outer = _raycaster.LastResults;
            Assert.That(behind.ExecuteEventExceptThis(data, ExecuteEvents.pointerClickHandler), Is.True);
            Assert.That(_raycaster.LastResults, Is.Not.SameAs(outer));
            Assert.That(outer.Count, Is.EqualTo(2));
            Assert.That(sourceHandler.LastData, Is.SameAs(_data));
        };
        Assert.That(Forward(), Is.True);
        Assert.That(sourceHandler.Calls, Is.EqualTo(1));
        Assert.That(handler.Calls, Is.EqualTo(1));
    }

    [Test]
    public void ThrowingRaycasterReturnsAndClearsTheList()
    {
        _raycaster.Hits.Add(_source);
        _raycaster.Error = new InvalidOperationException("raycast failure");
        Assert.Throws<InvalidOperationException>(() => Forward());
        List<RaycastResult> returned = _raycaster.LastResults;
        Assert.That(returned.Count, Is.Zero);
        using PooledObject<List<RaycastResult>> lease = ListPool<RaycastResult>.Get(out List<RaycastResult> reused);
        Assert.That(reused, Is.SameAs(returned));
    }

    [UnityTest]
    public IEnumerator GraphicRaycasterForwardsThroughOverlappingImages()
    {
        _raycaster.enabled = false;
        GameObject canvasObject = new("forwarding canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(_root.transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        GameObject back = MakeImage(canvasObject.transform, "back image");
        GameObject front = MakeImage(canvasObject.transform, "front image");
        EventTestClickHandler backHandler = back.AddComponent<EventTestClickHandler>();
        EventTestClickHandler frontHandler = front.AddComponent<EventTestClickHandler>();
        yield return null;
        Canvas.ForceUpdateCanvases();
        _data.position = RectTransformUtility.WorldToScreenPoint(null, front.transform.position);
        Assert.That(front.ExecuteEventExceptThis(_data, ExecuteEvents.pointerClickHandler), Is.True);
        Assert.That(frontHandler.Calls, Is.Zero);
        Assert.That(backHandler.Calls, Is.EqualTo(1));
        Assert.That(backHandler.LastData, Is.SameAs(_data));
    }

    [Test]
    public void WarmedForwardingDoesNotAllocateManagedObjects()
    {
        GameObject behind = MakeObject("allocation target");
        EventTestClickHandler handler = behind.AddComponent<EventTestClickHandler>();
        _raycaster.Hits.Add(_source); _raycaster.Hits.Add(behind);
        for (int i = 0; i < 20; i++) Forward();
        var options = ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.CollectOnlyOnCurrentThread;
        using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 1024, options);
        var calibration = new byte[1024]; GC.KeepAlive(calibration);
        int calibrationEvents = recorder.Count;
        recorder.Reset();
        int handled = 0;
        for (int i = 0; i < 100; i++) if (Forward()) handled++;
        int allocationEvents = recorder.Count;
        Assert.That(recorder.Valid, Is.True);
        Assert.That(calibrationEvents, Is.GreaterThan(0));
        Assert.That(handled, Is.EqualTo(100));
        Assert.That(handler.Calls, Is.EqualTo(120));
        Assert.That(allocationEvents, Is.Zero);
    }

    private bool Forward() => _source.ExecuteEventExceptThis(_data, ExecuteEvents.pointerClickHandler);
    private void Disable(Behaviour item) { if (item.enabled) { _disabled.Add(item); item.enabled = false; } }
    private GameObject MakeObject(string name) { GameObject result = new(name); result.transform.SetParent(_root.transform); return result; }
    private static GameObject MakeImage(Transform parent, string name)
    {
        GameObject result = new(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var rect = (RectTransform)result.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(100, 100);
        return result;
    }
}

public sealed class EventTestClickHandler : MonoBehaviour, IPointerClickHandler
{
    public int Calls;
    public PointerEventData LastData;
    public Action<PointerEventData> Callback;
    public void OnPointerClick(PointerEventData data) { Calls++; LastData = data; Callback?.Invoke(data); }
}

public sealed class EventTestRaycaster : BaseRaycaster
{
    public readonly List<GameObject> Hits = new();
    public PointerEventData LastData;
    public List<RaycastResult> LastResults;
    public Exception Error;
    public int Calls;
    public override Camera eventCamera => null;
    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        Calls++; LastData = eventData; LastResults = resultAppendList;
        for (int i = 0; i < Hits.Count; i++) resultAppendList.Add(new RaycastResult { gameObject = Hits[i], module = this, distance = i + 1, index = resultAppendList.Count });
        if (Error != null) throw Error;
    }
}
