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

*   **`gameObject`, `transform`, `rectTransform`**: Cached properties that minimize internal Unity C++ to C# calls. Automatically cleared on destruction via R3.

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

---

### 5. Task Extensions (`UniTaskExtension`)
Lifecycle and safety helpers for asynchronous tasks.

*   **`ContinueWithAnyway(action)`**: Ensures an action runs regardless of whether the task succeeded, failed, or was canceled.
*   **`ForgetSafe()`**: A fire-and-forget wrapper that logs exceptions but suppresses `OperationCanceledException`.

## Requirements
- **Unity 6000.3 or newer**
- **C# 9.0+** compatible environment

## License
This project is licensed under the [MIT License](LICENSE).
