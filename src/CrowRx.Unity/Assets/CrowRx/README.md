# CrowRx Unity Base UPM Package

The foundational Unity Package (UPM) for the CrowRx ecosystem, providing high-performance utilities, fluent APIs, and enhanced editor workflows.

## Prerequisites

Before installing this package, ensure the following dependencies are settled.

### 1. Install CrowRx.Core (Required)
This package depends on `CrowRx.Core`. It is highly recommended to install it via [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).

1. In the Unity Editor, go to `NuGet > Manage NuGet Packages`.
2. Search for `CrowRx.Core` and install the latest version.

### 2. External Dependencies (UPM)
The following libraries must be present in your project:

*   **[UniTask](https://github.com/Cysharp/UniTask)**: Efficient async/await integration for Unity.
*   **[R3](https://github.com/Cysharp/R3)**: The next-generation reactive extensions for Unity.
*   **[ZLinq](https://github.com/Cysharp/ZLinq)**: High-performance LINQ alternative with zero allocation.
*   **[ZString](https://github.com/Cysharp/ZString)**: Zero-allocation string formatting.

---

## Installation (Unity Package Manager)

1. Open `Window > Package Manager` in Unity.
2. Click the `+` button in the top-left and select `Add package from git URL...`.
3. Enter the following URL:
   `https://github.com/crowlib/crowrx.git?path=src/CrowRx.Unity/Assets/CrowRx`

---

## Key Features (API Reference)

### 1. Core Extensions (`GameObject`, `Component`, `Transform`)
Enhanced fluent APIs for common Unity operations.

*   **`GetOrAddComponent<T>()`**: Retrieves an existing component or adds it if missing.
*   **`SetParent(target)` / `SetActive(bool)` / `SetLayer(name)`**: Fluent versions of standard methods that return the original object for chaining.
*   **`FindChildDeep(name)`**: Recursively searches for a child by name.
*   **`ExecuteEventExceptThis(eventData, functor)`**: Uses the supplied `PointerEventData` to raycast and forward the event to the first valid hit other than the source, or its nearest eligible parent. Returns whether a handler was invoked; does not continue to subsequent hits if that target cannot handle the event. Returns false for non-pointer data or a missing current EventSystem.

**Example:**
```csharp
this.GetOrAddComponent<Rigidbody>()
    .SetParent(newParent)
    .SetLayer("Obstacle")
    .SetActive(true);

Transform weapon = transform.FindChildDeep("Sword_Handle");
```

---

### 2. Base Classes (`MonoBehaviourCrowRx`)
Optimized base classes with performance-centric caching.

*   **`gameObject`, `transform`, `rectTransform`**: Lazily caches GameObject and Transform once. `rectTransform` casts the cached Transform, returning null for an ordinary Transform without repeated component searches. One `destroyCancellationToken` registration clears references when the component is destroyed, including component-only removal.
    Components that have not reached `Awake` use an R3 Update monitor until activation or destruction, because Unity does not cancel their destruction token. Outside Play Mode, the monitor uses the Editor update instead. Destruction before activation clears references on the next update. This adds no trigger component; active components do not need monitoring. First access still allocates cancellation state, and access before `Awake` also creates the monitor.
*   **`InvalidateTransformCache()`**: Call after changing the Transform type; the next `transform` or `rectTransform` access fetches the current Transform. Disabling and re-enabling the component keeps its cache.

**Example:**
```csharp
public class PlayerController : MonoBehaviourCrowRx {
    void Update() {
        // High-performance access to transform without manual caching
        transform.position += Vector3.forward * Time.deltaTime;
    }
}
```

---

### 3. Async & Math Utilities (`Mathm`, `CrowTask`)
Asynchronous interpolation and geometric calculation helpers.

*   **`LerpAsync(...)`**: Frame-independent asynchronous Lerp using UniTask.
*   **`SpringDampen(...)`**: Frame-rate independent velocity dampening.
*   **`RunSafe(taskFunc, token)`**: Executes a task with automatic exception handling and cancellation logging.
*   **`CrowTask.WhenAny(tasks, tasksLength)`**: Consumes the first `tasksLength` inputs and reports the first completion, including failure or cancellation. Remaining inputs continue and their completion is consumed. Supply valid inputs that are not also awaited elsewhere, and await the result once. The promise is reused only after all inputs and result consumption finish; promise and continuation-state pools retain at most 64 objects each per type. Input creation and overlapping calls can still allocate.
*   **`RectIntersect(corners, p0, p1)`**: Projects exactly four perimeter-ordered corners onto XY and includes edge contact; null or a different corner count throws an argument exception.
*   **`CanvasGroupFade`**: `Fade`/`FadeInOut` consume cancellation and log failures once. Public `FadeAsync`/`FadeOutInAsync` propagate cancellation and failure to the caller.

**Example:**
```csharp
// Smoothly fade alpha over 1 second
await Mathm.LerpAsync(
    alpha => canvasGroup.alpha = alpha,
    () => canvasGroup.alpha,
    0f, 1f, 1.0f, true, false, destroyCancellationToken
);

// Fire and forget safely
CrowTask.RunSafe(token => MyAsyncMethod(token), destroyCancellationToken);
```

---

### 4. Editor Enhancements (`EditorCrowRx`, `DebugUtility`)
Streamlined custom editor development and visual debugging tools.

*   **`EditorCrowRx`**: Base class for custom inspectors with simplified lifecycle (`OnEnter`, `OnDraw`, `OnExit`).
*   **`Debug.DrawCircle`, `DrawArc`, `DrawBox`**: Extended debug drawing methods for wireframes in Scene view.
*   **`SerializeReferenceList`**: A specialized list wrapper and drawer for `[SerializeReference]` fields.
    Direct `foreach` on `SerializedReferenceList<T>` uses a struct enumerator. Nonempty enumeration through `IEnumerable<T>`/`IEnumerable` still boxes; keep the concrete type or use `Count` and the indexer in allocation-sensitive code. Enumerators are independent and retain List mutation checks.
    Converting from `List<T>` shares that list and creates only the wrapper, without creating a discarded empty list. Null conversions in either direction return null.
*   **`GetRealTypeFullName()`**: Formats nested generic names with their declaring types, including arrays and element-type suffixes. It is a display name; use `AssemblyQualifiedName` when assemblies must be distinguished. `GetRealTypeName()` shows only the leaf name and its own generic arguments.
*   **`TryGetSingleComponent<T>()`**: Returns false without logging when absent, returns the sole component when present, and reports duplicate components.

---

### 5. Task Extensions (`UniTaskExtension`)
Lifecycle and safety helpers for asynchronous tasks.

*   **`ContinueWithAnyway(action)`**: Runs an action once whether the task succeeded, failed, or was canceled. An exception from the action propagates to the caller and does not invoke another completion callback.
*   **`ForgetSafe()`**: A fire-and-forget wrapper that logs exceptions but suppresses `OperationCanceledException`.

## Requirements
- **Unity 6000.3 or newer**
- **C# 9.0+** compatible environment

## License
This project is licensed under the [MIT License](LICENSE).
