using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CrowRx;
using CrowRx.Editor;
using UnityEditor;
using UnityEngine;

// 임시 EditorWindow와 ScriptableObject로 검증하며 사용자의 장면/에셋은 저장하지 않습니다.
public static class CrowRxEditorRegression
{
    public static readonly List<string> Passed = new();
    public static Exception Failure;
    public static bool GuiFinished;

    public static async Task<object> Run()
    {
        Passed.Clear(); Failure = null; GuiFinished = false;
        var first = ScriptableObject.CreateInstance<CrowRxRegressionWindow>();
        var second = ScriptableObject.CreateInstance<CrowRxRegressionWindow>();
        bool shown = false;
        try
        {
            Check(ReferenceEquals(CrowRxRegressionWindow.Instance, second), "latest window instance");
            second.Disable();
            Check(ReferenceEquals(CrowRxRegressionWindow.Instance, first), "closing latest restores previous instance");
            second.Enable();
            first.Disable();
            Check(ReferenceEquals(CrowRxRegressionWindow.Instance, second), "closing other window preserves latest instance");
            first.Enable();
            first.FailExit = true;
            try { first.Disable(); throw new Exception("Exit should throw"); }
            catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException) { }
            Check(ReferenceEquals(CrowRxRegressionWindow.Instance, second), "Exit exception preserves remaining instance");
            Check(!GetBool(first, "_sceneSubscribed") && !GetBool(first, "_hierarchySubscribed"), "Exit exception releases event subscriptions");
            first.FailExit = false;

            second.RunGuiChecks = true;
            second.titleContent = new GUIContent("CrowRx regression");
            second.position = new Rect(80, 80, 420, 280);
            second.ShowUtility();
            shown = true;
            second.Repaint();
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            while (!GuiFinished && DateTime.UtcNow < deadline) await Task.Delay(30);
            if (Failure != null) throw Failure;
            Check(GuiFinished, "real OnGUI validation completed");
            Check(!second.WantsConstantRepaint, "continuous repaint is opt-in");
            return new { passed = Passed.Count, failed = 0, checks = Passed.ToArray() };
        }
        finally
        {
            first.FailExit = second.FailExit = false;
            try { if (shown && second) second.Close(); }
            finally
            {
                if (first) UnityEngine.Object.DestroyImmediate(first);
                if (second) UnityEngine.Object.DestroyImmediate(second);
            }
        }
    }

    public static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAILED: " + name);
        Passed.Add(name);
    }

    private static bool GetBool(CrowRxRegressionWindow window, string name)
        => (bool)typeof(EditorWindowCrowRx<CrowRxRegressionWindow>).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(window);

    public static void CheckProperties()
    {
        var owner = ScriptableObject.CreateInstance<CrowRxRegressionOwner>();
        var other = ScriptableObject.CreateInstance<CrowRxRegressionOwner>();
        try
        {
            using var serialized = new SerializedObject(owner);
            using var second = new SerializedObject(other);
            CheckField(serialized, "settings.actions", typeof(CrowRxRegressionSettings));
            CheckField(serialized, "entries.Array.data[0].actions", typeof(CrowRxRegressionSettings));
            CheckField(serialized, "array.Array.data[0].actions", typeof(CrowRxRegressionSettings));
            CheckField(serialized, "inheritedActions", typeof(CrowRxRegressionOwnerBase));
            CheckField(serialized, "managed.actions", typeof(CrowRxRegressionManaged));

            var drawer = new SerializeReferenceListDrawer();
            using SerializedProperty firstProperty = serialized.FindProperty("settings.actions");
            float firstHeight = drawer.GetPropertyHeight(firstProperty, new GUIContent("Actions"));
            Check(firstHeight > EditorGUIUtility.singleLineHeight * 2, "drawer height available before first OnGUI");
            other.settings.actions.Add(new CrowRxRegressionAction());
            second.Update();
            using SerializedProperty secondProperty = second.FindProperty("settings.actions");
            float secondHeight = drawer.GetPropertyHeight(secondProperty, new GUIContent("Other"));
            Check(secondHeight > firstHeight, "drawer updates owner and element count");
            var cache = (System.Collections.IDictionary)typeof(SerializeReferenceListDrawer).GetField("_listCache", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(drawer);
            Check(cache.Count == 1, "drawer clears previous owner cache");

            MethodInfo canCreate = typeof(SerializeReferenceListDrawer).GetMethod("CanCreate", BindingFlags.NonPublic | BindingFlags.Static);
            Check((bool)canCreate.Invoke(null, new object[] { typeof(CrowRxRegressionAction) }), "serializable concrete base is creatable");
            Check(!(bool)canCreate.Invoke(null, new object[] { typeof(CrowRxRegressionNoConstructor) }), "missing default constructor rejected");
            Check(!(bool)canCreate.Invoke(null, new object[] { typeof(CrowRxRegressionNotSerializable) }), "non-serializable class rejected");
            Check(!(bool)canCreate.Invoke(null, new object[] { typeof(CrowRxRegressionManagedBase) }), "abstract class rejected");

            Type stateType = typeof(SerializeReferenceListDrawer).GetNestedType("ListState", BindingFlags.NonPublic);
            MethodInfo add = stateType.GetMethod("AddElement", BindingFlags.NonPublic | BindingFlags.Static);
            add.Invoke(null, new object[] { owner, "settings.actions.items", typeof(CrowRxRegressionAction) });
            serialized.Update();
            using SerializedProperty items = serialized.FindProperty("settings.actions.items");
            Check(items.arraySize == 1, "add callback applies managed reference to captured target");
            using SerializedProperty added = items.GetArrayElementAtIndex(0);
            Check(added.managedReferenceValue is CrowRxRegressionAction, "added managed reference has selected type");

            MethodInfo clear = typeof(SerializeReferenceListDrawer).GetMethod("ClearLists", BindingFlags.NonPublic | BindingFlags.Instance);
            clear.Invoke(drawer, null);
            Check(cache.Count == 0, "drawer releases bound property cache");
        }
        finally
        {
            Undo.ClearUndo(owner); Undo.ClearUndo(other);
            UnityEngine.Object.DestroyImmediate(owner); UnityEngine.Object.DestroyImmediate(other);
        }
    }

    private static void CheckField(SerializedObject owner, string path, Type declaringType)
    {
        using SerializedProperty property = owner.FindProperty(path);
        Check(property != null && property.GetFieldInfoFromProperty()?.DeclaringType == declaringType, "field resolver: " + path);
    }
}

public class CrowRxRegressionWindow : EditorWindowCrowRx<CrowRxRegressionWindow>, IOnDuringSceneGui, IOnHierarchyChanged
{
    public bool FailExit;
    public bool RunGuiChecks;
    public bool WantsConstantRepaint => ContinuousRepaint;
    public void Enable() => typeof(EditorWindowCrowRx<CrowRxRegressionWindow>).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, null);
    public void Disable() => typeof(EditorWindowCrowRx<CrowRxRegressionWindow>).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, null);
    protected override void Exit() { if (FailExit) throw new InvalidOperationException("expected Exit failure"); }
    public void OnDuringSceneGui(SceneView view) { }
    public void OnHierarchyChanged() { }

    protected override object DrawEditableField(FieldInfo field, string label, object value)
    {
        GUI.changed = true;
        return field.FieldType.IsEnum ? (object)CrowRxRegressionEnum.Second : 42;
    }

    protected override void Draw()
    {
        if (!RunGuiChecks || CrowRxEditorRegression.GuiFinished || Event.current.type != EventType.Layout) return;
        Color color = GUI.color;
        bool enabled = GUI.enabled;
        try
        {
            GUI.color = Color.yellow;
            GUI.enabled = false;
            this.DrawGUIElement(() =>
            {
                CrowRxEditorRegression.Check(!GUI.enabled, "nested helper preserves disabled parent");
                return 0;
            }, null, false, true);
            CrowRxEditorRegression.Check(GUI.color == Color.yellow && !GUI.enabled, "helper restores parent GUI state");
            try
            {
                this.Horizontal(() => { throw new InvalidOperationException("expected GUI failure"); }, Color.red, true, true);
            }
            catch (InvalidOperationException) { }
            CrowRxEditorRegression.Check(GUI.color == Color.yellow && !GUI.enabled, "throwing layout callback restores state and closes group");
            GUI.enabled = true;
            var data = PropertyEditViewer(new CrowRxRegressionData());
            CrowRxEditorRegression.Check(data.Number == 42, "struct edits returned to caller");
            CrowRxEditorRegression.Check(data.Mode == CrowRxRegressionEnum.Second, "concrete enum field is editable");
            var inherited = PropertyEditViewer(new CrowRxRegressionDerivedData());
            CrowRxEditorRegression.Check(inherited.Value == 42, "inherited private editable field updated");
            CrowRxEditorRegression.Check(Popup("keep", Array.Empty<string>()) == "keep", "empty string popup keeps value");
            CrowRxEditorRegression.Check(Popup(typeof(int), Array.Empty<string>()) == typeof(int), "empty type popup keeps value");
            CrowRxEditorRegression.Check(Popup(0, Array.Empty<string>()) == -1, "empty index popup has no selection");
            CrowRxEditorRegression.Check(Popup(null, Array.Empty<Type>(), Array.Empty<string>()) == null, "typed popup supports empty selection");
            CrowRxEditorRegression.CheckProperties();
        }
        catch (Exception exception) { CrowRxEditorRegression.Failure = exception; }
        finally
        {
            GUI.color = color; GUI.enabled = enabled;
            CrowRxEditorRegression.GuiFinished = true;
        }
    }
}

public enum CrowRxRegressionEnum { First, Second }
public struct CrowRxRegressionData
{
    [EditorWindowCrowRxSerializeField("Number")] public int Number;
    [EditorWindowCrowRxSerializeField("Mode")] public CrowRxRegressionEnum Mode;
}
public class CrowRxRegressionBaseData
{
    [EditorWindowCrowRxSerializeField("Private")] private int number = 0;
    public int Value => number;
}
public class CrowRxRegressionDerivedData : CrowRxRegressionBaseData { }
[Serializable] public class CrowRxRegressionAction { public int number; }
[Serializable] public class CrowRxRegressionNoConstructor { public CrowRxRegressionNoConstructor(int value) { } }
public class CrowRxRegressionNotSerializable { }
[Serializable] public abstract class CrowRxRegressionManagedBase { }
[Serializable] public class CrowRxRegressionManaged : CrowRxRegressionManagedBase { public SerializedReferenceList<CrowRxRegressionAction> actions = new(); }
[Serializable] public class CrowRxRegressionSettings { public SerializedReferenceList<CrowRxRegressionAction> actions = new(); }
public class CrowRxRegressionOwnerBase : ScriptableObject
{
    [SerializeField] private SerializedReferenceList<CrowRxRegressionAction> inheritedActions = new();
}
public class CrowRxRegressionOwner : CrowRxRegressionOwnerBase
{
    public CrowRxRegressionSettings settings = new();
    public List<CrowRxRegressionSettings> entries = new() { new() };
    public CrowRxRegressionSettings[] array = { new() };
    [SerializeReference] public CrowRxRegressionManagedBase managed = new CrowRxRegressionManaged();
}
 
public class CrowRxEditorRegressionTests
{
    [NUnit.Framework.Test]
    public void ReflectionAndLayoutMetadataAreReused()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        MethodInfo editable = typeof(EditorWindowCrowRx<CrowRxRegressionWindow>).GetMethod("GetEditableFields", flags);
        object first = editable.Invoke(null, new object[] { typeof(CrowRxRegressionData) });
        object second = editable.Invoke(null, new object[] { typeof(CrowRxRegressionData) });
        NUnit.Framework.Assert.That(second, NUnit.Framework.Is.SameAs(first));

        Type options = typeof(EditorCrowRx).Assembly.GetType("CrowRx.Editor.EditorLayoutOptions");
        MethodInfo width = options.GetMethod("Width", flags);
        first = width.Invoke(null, new object[] { 100f });
        second = width.Invoke(null, new object[] { 100f });
        NUnit.Framework.Assert.That(second, NUnit.Framework.Is.SameAs(first));
        for (int i = 1; i <= 40; i++) width.Invoke(null, new object[] { (float)i });
        var widths = (System.Collections.IDictionary)options.GetField("Widths", flags).GetValue(null);
        NUnit.Framework.Assert.That(widths.Count, NUnit.Framework.Is.LessThanOrEqualTo(32));

        MethodInfo member = typeof(CrowRx.Editor.ComponentExtension).GetMethod("FindMember", flags);
        var field = (FieldInfo)member.Invoke(null, new object[] { typeof(CrowRxRegressionDerivedData), "number", VariableType.Field });
        NUnit.Framework.Assert.That(field.DeclaringType, NUnit.Framework.Is.EqualTo(typeof(CrowRxRegressionBaseData)));
        var property = (PropertyInfo)member.Invoke(null, new object[] { typeof(CrowRxRegressionDerivedData), "Value", VariableType.Property });
        NUnit.Framework.Assert.That(property.GetValue(new CrowRxRegressionDerivedData()), NUnit.Framework.Is.EqualTo(0));
        NUnit.Framework.Assert.That(member.Invoke(null, new object[] { typeof(CrowRxRegressionDerivedData), "missing", VariableType.Field }), NUnit.Framework.Is.Null);
        NUnit.Framework.Assert.That(member.Invoke(null, new object[] { typeof(CrowRxRegressionDerivedData), "missing", VariableType.Field }), NUnit.Framework.Is.Null);

        var gameObject = new GameObject("CrowRx component regression");
        try
        {
            Vector3 position = new(2, 3, 4);
            gameObject.transform.localPosition = position;
            NUnit.Framework.Assert.That(CrowRx.Editor.ComponentExtension.GetValue<Transform, Vector3>(gameObject.transform, "localPosition", VariableType.Property), NUnit.Framework.Is.EqualTo(position));
            NUnit.Framework.Assert.That(CrowRx.Editor.ComponentExtension.GetValue<Transform, int>(gameObject.transform, "missing", VariableType.Field), NUnit.Framework.Is.EqualTo(0));
        }
        finally { UnityEngine.Object.DestroyImmediate(gameObject); }
    }

    [NUnit.Framework.Test]
    public void PrefabSnapshotDistinguishesSceneAssetAndInstance()
    {
        string path = "Assets/Tests/Editor/CrowRxRegression_" + Guid.NewGuid().ToString("N") + ".prefab";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        GameObject source = null;
        try
        {
            source = new GameObject("CrowRx prefab regression");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(source, scene);
            var plain = new PrefabInformation(source);
            NUnit.Framework.Assert.That(plain.isSceneObject, NUnit.Framework.Is.True);
            NUnit.Framework.Assert.That(plain.isPartOfAnyPrefab, NUnit.Framework.Is.False);
            NUnit.Framework.Assert.That(plain.prefabAssetPath, NUnit.Framework.Is.Null);

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(source, path);
            NUnit.Framework.Assert.That(asset, NUnit.Framework.Is.Not.Null);
            var assetInfo = new PrefabInformation(asset);
            NUnit.Framework.Assert.That(assetInfo.isPrefabAssetRoot && assetInfo.isPartOfPrefabAsset, NUnit.Framework.Is.True);
            NUnit.Framework.Assert.That(assetInfo.isSceneObject, NUnit.Framework.Is.False);
            NUnit.Framework.Assert.That(assetInfo.prefabAssetPath, NUnit.Framework.Is.EqualTo(path));

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            var instanceInfo = new PrefabInformation(instance);
            NUnit.Framework.Assert.That(instanceInfo.isPrefabInstanceRoot && instanceInfo.isSceneObject, NUnit.Framework.Is.True);
            NUnit.Framework.Assert.That(instanceInfo.nearestInstanceRoot, NUnit.Framework.Is.EqualTo(instance));
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instanceInfo.Refresh();
            NUnit.Framework.Assert.That(instanceInfo.isPartOfAnyPrefab, NUnit.Framework.Is.False);
            NUnit.Framework.Assert.That(instanceInfo.prefabAssetPath, NUnit.Framework.Is.Null);

            UnityEngine.Object.DestroyImmediate(source);
            plain.Refresh();
            NUnit.Framework.Assert.That(plain.isSceneObject || plain.isPartOfAnyPrefab, NUnit.Framework.Is.False);
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            if (AssetDatabase.LoadMainAssetAtPath(path)) AssetDatabase.DeleteAsset(path);
        }
    }

    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator EditorHelpersAndSerializationRegression()
    {
        System.Threading.Tasks.Task<object> task = CrowRxEditorRegression.Run();
        while (!task.IsCompleted) yield return null;
        if (task.IsFaulted) throw task.Exception.InnerException;
        NUnit.Framework.Assert.That(CrowRxEditorRegression.Failure, NUnit.Framework.Is.Null);
        NUnit.Framework.Assert.That(CrowRxEditorRegression.Passed.Count, NUnit.Framework.Is.GreaterThanOrEqualTo(30));
        UnityEngine.Debug.Log("CrowRx editor regression checks passed: " + CrowRxEditorRegression.Passed.Count);
    }
}

