using System;
using UnityEngine;
using UnityEditor;


// ReSharper disable CheckNamespace
namespace CrowRx.Editor
{
    public static partial class EditorCrowRxExtension
    {
        public static void Dialog(this IEditorCrowRx guiView, string title, string message, string ok) => EditorUtility.DisplayDialog(title, message, ok);

        public static bool YesNoDialog(this IEditorCrowRx guiView, string title, string message, string ok, string cancel) => EditorUtility.DisplayDialog(title, message, ok, cancel);

        public static bool Toggle(this IEditorCrowRx guiView, string name, bool value, Color? color = null, bool box = false, bool enabled = true) =>
            guiView.DrawGUIElement(
                content: () => EditorGUILayout.Toggle(name, value),
                color: color,
                box: box,
                enabled: enabled);

        public static void Button(this IEditorCrowRx guiView, string name, Action on_click, Action none_click, Color? color, bool box, bool enabled, float width) =>
            guiView.DrawGUIElement(
                content: () =>
                {
                    bool is_click = width > 0 ? GUILayout.Button(text: name, GUILayout.Width(width)) : GUILayout.Button(name);

                    if (is_click)
                    {
                        GUI.FocusControl(null);

                        on_click?.Invoke();
                    }
                    else
                        none_click?.Invoke();
                },
                color: color,
                box: box,
                enabled: enabled);

        public static void Button(this IEditorCrowRx guiView, string name, Action on_click) =>
            guiView.Button(name, on_click, null, null, false, true, -1f);

        public static void Button(this IEditorCrowRx guiView, string name, Action on_click, bool enabled) =>
            guiView.Button(name, on_click, null, null, false, enabled, -1f);

        public static void Button(this IEditorCrowRx guiView, string name, Action on_click, Color? color) =>
            guiView.Button(name, on_click, null, color, false, true, -1f);

        public static void Button(this IEditorCrowRx guiView, string name, Action on_click, Color? color, bool enabled) =>
            guiView.Button(name, on_click, null, color, false, enabled, -1f);

        public static void Button(this IEditorCrowRx guiView, string name, Action on_click, float width) =>
            guiView.Button(name, on_click, null, null, false, true, width);

        public static void Button(this IEditorCrowRx guiView, string name, Action on_click, float width, bool enabled) =>
            guiView.Button(name, on_click, null, null, false, enabled, width);

        public static void Label(this IEditorCrowRx guiView, string value, Color? color = null, bool box = false, bool enabled = true) =>
            guiView.DrawGUIElement(content: () => EditorGUILayout.LabelField(value), color: color, box: box, enabled: enabled);

        public static void Label(this IEditorCrowRx guiView, string title, string value, Color? color = null, bool box = false, bool enabled = true) =>
            guiView.DrawGUIElement(content: () => EditorGUILayout.LabelField(title, value), color: color, box: box, enabled: enabled);

        public static TObject FieldObject<TObject>(this IEditorCrowRx guiView, string name, TObject value, bool allow_scene_objects = false, Color? color = null, bool box = false, bool enabled = true)
            where TObject : UnityEngine.Object
            => guiView.DrawGUIElement(
                content: () => (TObject)EditorGUILayout.ObjectField(name, value, typeof(TObject), allow_scene_objects),
                color: color,
                box: box,
                enabled: enabled);

        public static TObject FieldObject<TObject>(this IEditorCrowRx guiView, TObject value, bool allow_scene_objects = false, Color? color = null, bool box = false, bool enabled = true)
            where TObject : UnityEngine.Object
            => guiView.DrawGUIElement(
                content: () => (TObject)EditorGUILayout.ObjectField(value, typeof(TObject), allow_scene_objects),
                color: color,
                box: box,
                enabled: enabled);

        public static string FieldText(this IEditorCrowRx guiView, string name, string value, Color? color, bool box, bool enabled) =>
            guiView.DrawGUIElement(
                content: () => EditorGUILayout.TextField(name, value),
                color: color,
                box: box,
                enabled: enabled);

        public static string FieldText(this IEditorCrowRx guiView, string name, string value) =>
            guiView.FieldText(name, value, null, false, true);

        public static string FieldText(this IEditorCrowRx guiView, string name, string value, bool enabled) =>
            guiView.FieldText(name, value, null, false, enabled);

        public static int FieldInt(this IEditorCrowRx guiView, string name, int value, Color? color = null, bool box = false, bool enabled = true) =>
            guiView.DrawGUIElement(
                content: () => EditorGUILayout.IntField(name, value),
                color: color,
                box: box,
                enabled: enabled);

        public static float FieldFloat(this IEditorCrowRx guiView, string name, float value, Color? color = null, bool box = false, bool enabled = true) =>
            guiView.DrawGUIElement(
                content: () => EditorGUILayout.FloatField(name, value),
                color: color,
                box: box,
                enabled: enabled);

        public static void Horizontal(this IEditorCrowRx guiView, Action content, Color? color = null, bool box = false, bool enabled = true)
        {
            GUI.color = color.HasValue ? color.Value : Color.white;

            if (box)
                GUILayout.BeginHorizontal("box");
            else
                GUILayout.BeginHorizontal();

            GUI.enabled = enabled;
            content?.Invoke();
            GUI.enabled = true;

            GUILayout.EndHorizontal();

            GUI.color = Color.white;
        }

        public static void Vertical(this IEditorCrowRx guiView, Action content, Color? color = null, bool box = false, bool enabled = true)
        {
            GUI.color = color.HasValue ? color.Value : Color.white;

            if (box)
                GUILayout.BeginVertical("box");
            else
                GUILayout.BeginVertical();

            GUI.enabled = enabled;
            content?.Invoke();
            GUI.enabled = true;

            GUILayout.EndVertical();

            GUI.color = Color.white;
        }

        public static Vector2 Scroll(this IEditorCrowRx guiView, Vector2 scrollPosition, Action contents, Color? color = null, bool box = false, GUILayoutOption[] options = null)
        {
            Vector2 result = scrollPosition;

            GUI.color = color.HasValue ? color.Value : Color.white;

            if (box)
                result = GUILayout.BeginScrollView(result, "Box", options ?? Array.Empty<GUILayoutOption>());
            else
                result = GUILayout.BeginScrollView(result);

            GUI.color = Color.white;

            contents?.Invoke();

            GUILayout.EndScrollView();

            return result;
        }

        public static void BaseField(this IEditorCrowRx guiView, Action field, Color color = default(Color), bool box = false, string front_text = "") =>
            Horizontal(
                guiView: null,
                content: () =>
                {
                    if (!string.IsNullOrEmpty(front_text))
                    {
                        GUILayout.Label(front_text, GUILayout.ExpandWidth(true));
                        GUILayout.FlexibleSpace();
                    }

                    field?.Invoke();
                },
                color: color,
                box: box);

        public static TValue DrawGUIElement<TValue>(this IEditorCrowRx guiView, Func<TValue> content, Color? color, bool box, bool enabled, string control_name)
        {
            GUI.color = color ?? Color.white;

            if (box)
                GUILayout.BeginHorizontal("box");

            GUI.enabled = enabled;

            if (string.IsNullOrEmpty(control_name) == false)
            {
                GUI.color = Color.white;

                GUI.SetNextControlName(control_name);

                GUI.color = color ?? Color.white;
            }

            TValue result = content.Invoke();

            GUI.enabled = true;

            if (box)
                GUILayout.EndHorizontal();

            GUI.color = Color.white;

            return result;
        }

        public static TValue DrawGUIElement<TValue>(this IEditorCrowRx guiView, Func<TValue> content, Color? color, bool box, bool enabled) => guiView.DrawGUIElement(content, color, box, enabled, null);

        public static void DrawGUIElement(this IEditorCrowRx guiView, Action content, Color? color, bool box, bool enabled, string control_name)
        {
            GUI.color = color ?? Color.white;

            if (box)
                GUILayout.BeginHorizontal("box");

            GUI.enabled = enabled;

            if (string.IsNullOrEmpty(control_name) == false)
            {
                GUI.color = Color.white;

                GUI.SetNextControlName(control_name);

                GUI.color = color ?? Color.white;
            }

            content.Invoke();

            GUI.enabled = true;

            if (box)
                GUILayout.EndHorizontal();

            GUI.color = Color.white;
        }

        public static void DrawGUIElement(this IEditorCrowRx guiView, Action content, Color? color, bool box, bool enabled) => guiView.DrawGUIElement(content, color, box, enabled, null);
    }
}

namespace CrowRx.Editor
{
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;
    using ZLinq;


    public static partial class EditorCrowRxExtension
    {
        public static void AddDefaultPropertiesExcluding(this VisualElement root, SerializedObject serializedObject, params string[] excludes)
        {
            SerializedProperty iterator = serializedObject.GetIterator();

            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (excludes?.AsValueEnumerable().Contains(iterator.propertyPath) == true)
                {
                    continue;
                }

                root.Add(new PropertyField(iterator.Copy()));
            }
        }

        public static void AddSpace(this VisualElement root, int space = 8) => root.Add(new VisualElement { style = { height = space } });
    }
}