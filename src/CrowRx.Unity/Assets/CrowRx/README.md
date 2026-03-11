# CrowRx Base UPM Package

The foundational Unity Package (UPM) for the CrowRx ecosystem, providing high-performance utilities, fluent APIs, and enhanced editor workflows.

## Dependencies

- **UniTask**: Async/Await integration.
- **R3**: Reactive extensions for Unity.
- **ZLinq**: High-performance LINQ alternative.
- **ZString**: Zero-allocation string formatting.

## API Reference

### 1. Core Extensions (`GameObject`, `Component`, `Transform`)
Enhanced fluent APIs for common Unity operations.

*   **`GetOrAddComponent<T>()`**: Retrieves an existing component or adds it if missing.
*   **`SetParent(target)` / `SetActive(bool)` / `SetLayer(name)`**: Fluent versions of standard methods that return the original object for chaining.
*   **`FindChildDeep(name)`**: Recursively searches for a child by name.

**Example:**
```csharp
// Fluent chaining
this.GetOrAddComponent<Rigidbody>()
    .SetParent(newParent)
    .SetLayer("Obstacle")
    .SetActive(true);

Transform weapon = transform.FindChildDeep("Sword_Handle");
```

---

### 2. Base Classes (`MonoBehaviourCrowRx`)
Optimized base classes with performance-centric caching.

*   **`gameObject`, `transform`, `rectTransform`**: Cached properties that minimize internal Unity C++ to C# calls. Automatically cleared on destruction via R3.

**Example:**
```csharp
public class PlayerController : MonoBehaviourCrowRx {
    void Update() {
        // High-performance access to transform without caching manually
        transform.position += Vector3.forward * Time.deltaTime;
    }
}
```

---

### 3. Async & Math Utilities (`Mathm`, `CrowTask`)
Asynchronous interpolation and geometric calculation helpers.

*   **`LerpAsync(setter, getter, start, end, duration, ...)`**: Frame-independent asynchronous Lerp using UniTask.
*   **`SpringDampen(ref velocity, strength, deltaTime)`**: Frame-rate independent velocity dampening.
*   **`RunSafe(taskFunc, token)`**: Executes a task with automatic exception handling and cancellation logging.

**Example:**
```csharp
// Smoothly fade alpha over 1 second
await Mathm.LerpAsync(
    alpha => canvasGroup.alpha = alpha,
    () => canvasGroup.alpha,
    0f, 1f, 1.0f, true, false, destroyCancellationToken
);

// Fire and forget safely
CrowTask.RunSafe(() => MyAsyncMethod(), destroyCancellationToken);
```

---

### 4. Editor Enhancements (`EditorCrowRx`, `DebugUtility`)
Streamlined custom editor development and visual debugging tools.

*   **`EditorCrowRx`**: Base class for custom inspectors with simplified lifecycle (`OnEnter`, `OnDraw`, `OnExit`).
*   **`Debug.DrawCircle`, `DrawArc`, `DrawBox`**: Extended debug drawing methods for wireframes in Scene view.
*   **`SerializeReferenceList`**: A specialized list wrapper and drawer for `[SerializeReference]` fields.

**Example:**
```csharp
// In an Editor script
Debug.DrawCircle(pos, rot, 5f, Color.green, 32);
GizmosUtility.DrawWireCapsule(p1, p2, 0.5f);
```

---

### 5. Task Extensions (`UniTaskExtension`)
Lifecycle and safety helpers for asynchronous tasks.

*   **`ContinueWithAnyway(action)`**: Ensures an action runs regardless of whether the task succeeded, failed, or was canceled.
*   **`ForgetSafe()`**: A fire-and-forget wrapper that logs exceptions but suppresses `OperationCanceledException`.

**Example:**
```csharp
LoadAssetsAsync()
    .ContinueWithAnyway(() => HideLoadingUI())
    .ForgetSafe();
```

## Requirements
- **Unity 6000.3 or newer**
- **C# 9.0+** compatible environment

## License
This project is licensed under the [MIT License](LICENSE).
