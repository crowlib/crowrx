using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CrowRx.Editor
{
    // 중첩된 GUI 상태와 layout을 예외 발생 시에도 복구하는 값 타입 scope.
    internal readonly struct EditorGUIScope : IDisposable
    {
        private readonly Color _color;
        private readonly bool _enabled;
        private readonly int _layout;

        internal EditorGUIScope(Color? color = null, bool box = false, bool enabled = true,
            string controlName = null, bool horizontal = false, bool vertical = false)
        {
            _color = GUI.color;
            _enabled = GUI.enabled;
            _layout = vertical ? 2 : horizontal || box ? 1 : 0;
            GUI.color = color ?? _color;
            GUI.enabled = _enabled && enabled;
            bool layoutOpened = false;
            try
            {
                if (_layout == 1)
                {
                    if (box) GUILayout.BeginHorizontal(GUI.skin.box, EditorLayoutOptions.Empty);
                    else GUILayout.BeginHorizontal(EditorLayoutOptions.Empty);
                    layoutOpened = true;
                }
                else if (_layout == 2)
                {
                    if (box) GUILayout.BeginVertical(GUI.skin.box, EditorLayoutOptions.Empty);
                    else GUILayout.BeginVertical(EditorLayoutOptions.Empty);
                    layoutOpened = true;
                }
                if (!string.IsNullOrEmpty(controlName)) GUI.SetNextControlName(controlName);
            }
            catch
            {
                try
                {
                    if (layoutOpened && _layout == 1) GUILayout.EndHorizontal();
                    else if (layoutOpened && _layout == 2) GUILayout.EndVertical();
                }
                finally
                {
                    GUI.color = _color;
                    GUI.enabled = _enabled;
                }
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_layout == 1) GUILayout.EndHorizontal();
                else if (_layout == 2) GUILayout.EndVertical();
            }
            finally
            {
                GUI.color = _color;
                GUI.enabled = _enabled;
            }
        }
    }

    internal static class EditorLayoutOptions
    {
        internal static readonly GUILayoutOption[] Empty = Array.Empty<GUILayoutOption>();
        internal static readonly GUILayoutOption[] Expand = { GUILayout.ExpandWidth(true) };
        internal static readonly GUILayoutOption[] Compact = { GUILayout.ExpandWidth(false) };
        private const int Capacity = 32;
        private static readonly Dictionary<float, GUILayoutOption[]> Widths = new();
        private static readonly Queue<float> WidthOrder = new();

        internal static GUILayoutOption[] Width(float width)
        {
            if (width <= 0 || float.IsNaN(width) || float.IsInfinity(width)) return Empty;
            if (Widths.TryGetValue(width, out GUILayoutOption[] options)) return options;
            if (Widths.Count == Capacity) Widths.Remove(WidthOrder.Dequeue());
            options = new[] { GUILayout.Width(width) };
            Widths.Add(width, options);
            WidthOrder.Enqueue(width);
            return options;
        }
    }

    public static partial class EditorCrowRxExtension
    {
        public static void Dialog(this IEditorCrowRx guiView, string title, string message, string ok) => EditorUtility.DisplayDialog(title, message, ok);
        public static bool YesNoDialog(this IEditorCrowRx guiView, string title, string message, string ok, string cancel) => EditorUtility.DisplayDialog(title, message, ok, cancel);

        public static bool Toggle(this IEditorCrowRx guiView, string name, bool value, Color? color = null, bool box = false, bool enabled = true)
        {
            using EditorGUIScope scope = new(color, box, enabled);
            return EditorGUILayout.Toggle(name, value, EditorLayoutOptions.Empty);
        }

        public static void Button(this IEditorCrowRx guiView, string name, Action on_click, Action none_click, Color? color, bool box, bool enabled, float width)
        {
            using EditorGUIScope scope = new(color, box, enabled);
            if (GUILayout.Button(name, EditorLayoutOptions.Width(width)))
            {
                GUI.FocusControl(null);
                on_click?.Invoke();
            }
            else none_click?.Invoke();
        }

        public static void Button(this IEditorCrowRx guiView, string name, Action on_click) => guiView.Button(name, on_click, null, null, false, true, -1f);
        public static void Button(this IEditorCrowRx guiView, string name, Action on_click, bool enabled) => guiView.Button(name, on_click, null, null, false, enabled, -1f);
        public static void Button(this IEditorCrowRx guiView, string name, Action on_click, Color? color) => guiView.Button(name, on_click, null, color, false, true, -1f);
        public static void Button(this IEditorCrowRx guiView, string name, Action on_click, Color? color, bool enabled) => guiView.Button(name, on_click, null, color, false, enabled, -1f);
        public static void Button(this IEditorCrowRx guiView, string name, Action on_click, float width) => guiView.Button(name, on_click, null, null, false, true, width);
        public static void Button(this IEditorCrowRx guiView, string name, Action on_click, float width, bool enabled) => guiView.Button(name, on_click, null, null, false, enabled, width);

        public static void Label(this IEditorCrowRx guiView, string value, Color? color = null, bool box = false, bool enabled = true)
        {
            using EditorGUIScope scope = new(color, box, enabled);
            EditorGUILayout.LabelField(value, EditorLayoutOptions.Empty);
        }

        public static void Label(this IEditorCrowRx guiView, string title, string value, Color? color = null, bool box = false, bool enabled = true)
        {
            using EditorGUIScope scope = new(color, box, enabled);
            EditorGUILayout.LabelField(title, value, EditorLayoutOptions.Empty);
        }

        public static TObject FieldObject<TObject>(this IEditorCrowRx guiView, string name, TObject value, bool allow_scene_objects = false, Color? color = null, bool box = false, bool enabled = true) where TObject : UnityEngine.Object
        {
            using EditorGUIScope scope = new(color, box, enabled);
            return (TObject)EditorGUILayout.ObjectField(name, value, typeof(TObject), allow_scene_objects, EditorLayoutOptions.Empty);
        }

        public static TObject FieldObject<TObject>(this IEditorCrowRx guiView, TObject value, bool allow_scene_objects = false, Color? color = null, bool box = false, bool enabled = true) where TObject : UnityEngine.Object
        {
            using EditorGUIScope scope = new(color, box, enabled);
            return (TObject)EditorGUILayout.ObjectField(value, typeof(TObject), allow_scene_objects, EditorLayoutOptions.Empty);
        }

        public static string FieldText(this IEditorCrowRx guiView, string name, string value, Color? color, bool box, bool enabled)
        {
            using EditorGUIScope scope = new(color, box, enabled);
            return EditorGUILayout.TextField(name, value, EditorLayoutOptions.Empty);
        }

        public static string FieldText(this IEditorCrowRx guiView, string name, string value) => guiView.FieldText(name, value, null, false, true);
        public static string FieldText(this IEditorCrowRx guiView, string name, string value, bool enabled) => guiView.FieldText(name, value, null, false, enabled);

        public static int FieldInt(this IEditorCrowRx guiView, string name, int value, Color? color = null, bool box = false, bool enabled = true)
        {
            using EditorGUIScope scope = new(color, box, enabled);
            return EditorGUILayout.IntField(name, value, EditorLayoutOptions.Empty);
        }

        public static float FieldFloat(this IEditorCrowRx guiView, string name, float value, Color? color = null, bool box = false, bool enabled = true)
        {
            using EditorGUIScope scope = new(color, box, enabled);
            return EditorGUILayout.FloatField(name, value, EditorLayoutOptions.Empty);
        }

        public static void Horizontal(this IEditorCrowRx guiView, Action content, Color? color = null, bool box = false, bool enabled = true)
        {
            using EditorGUIScope scope = new(color, box, enabled, horizontal: true);
            content?.Invoke();
        }

        public static void Vertical(this IEditorCrowRx guiView, Action content, Color? color = null, bool box = false, bool enabled = true)
        {
            using EditorGUIScope scope = new(color, box, enabled, vertical: true);
            content?.Invoke();
        }

        public static Vector2 Scroll(this IEditorCrowRx guiView, Vector2 scrollPosition, Action contents, Color? color = null, bool box = false, GUILayoutOption[] options = null)
        {
            using EditorGUIScope scope = new(color);
            Vector2 result = box
                ? GUILayout.BeginScrollView(scrollPosition, GUI.skin.box, options ?? EditorLayoutOptions.Empty)
                : GUILayout.BeginScrollView(scrollPosition, options ?? EditorLayoutOptions.Empty);
            try { contents?.Invoke(); }
            finally { GUILayout.EndScrollView(); }
            return result;
        }

        public static void BaseField(this IEditorCrowRx guiView, Action field, Color color = default, bool box = false, string front_text = "")
        {
            using EditorGUIScope scope = BeginBaseField(color, box, front_text);
            field?.Invoke();
        }

        internal static EditorGUIScope BeginBaseField(Color color, bool box, string frontText, bool enabled = true)
        {
            EditorGUIScope scope = new(color == default ? (Color?)null : color, box, enabled, horizontal: true);
            try
            {
                if (!string.IsNullOrEmpty(frontText))
                {
                    GUILayout.Label(frontText, EditorLayoutOptions.Expand);
                    GUILayout.FlexibleSpace();
                }
                return scope;
            }
            catch { scope.Dispose(); throw; }
        }

        public static TValue DrawGUIElement<TValue>(this IEditorCrowRx guiView, Func<TValue> content, Color? color, bool box, bool enabled, string control_name)
        {
            using EditorGUIScope scope = new(color, box, enabled, control_name);
            return content.Invoke();
        }

        public static TValue DrawGUIElement<TValue>(this IEditorCrowRx guiView, Func<TValue> content, Color? color, bool box, bool enabled) => guiView.DrawGUIElement(content, color, box, enabled, null);

        public static void DrawGUIElement(this IEditorCrowRx guiView, Action content, Color? color, bool box, bool enabled, string control_name)
        {
            using EditorGUIScope scope = new(color, box, enabled, control_name);
            content.Invoke();
        }

        public static void DrawGUIElement(this IEditorCrowRx guiView, Action content, Color? color, bool box, bool enabled) => guiView.DrawGUIElement(content, color, box, enabled, null);

        public static void AddDefaultPropertiesExcluding(this VisualElement root, SerializedObject serializedObject, params string[] excludes)
        {
            using SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                bool excluded = false;
                if (excludes != null)
                {
                    for (int i = 0; i < excludes.Length; i++)
                        if (excludes[i] == iterator.propertyPath) { excluded = true; break; }
                }
                if (!excluded) root.Add(new PropertyField(iterator.Copy()));
            }
        }

        public static void AddSpace(this VisualElement root, int space = 8) => root.Add(new VisualElement { style = { height = space } });
    }
}
