using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowRx.Editor
{
    public class EditorWindowCrowRx<TEditorWindow> : EditorWindow, IEditorCrowRx
        where TEditorWindow : EditorWindow
    {
        public static TEditorWindow Instance { get; private set; }
        private static readonly List<EditorWindowCrowRx<TEditorWindow>> Instances = new();
        private static readonly Dictionary<Type, EditableField[]> EditableFields = new();
        private readonly Dictionary<string, Type> _resolvedTypes = new();
        private bool _isEntered;
        private bool _sceneSubscribed;
        private bool _hierarchySubscribed;
        private GUIStyle _labelStyle;
        private GUISkin _labelSkin;
        private static readonly GUIContent HdrLabel = new("hdr");

        /// <summary>실시간 표시가 필요한 창에서만 Inspector 갱신 주기로 다시 그립니다.</summary>
        protected virtual bool ContinuousRepaint => false;

        private void OnEnable()
        {
            if (_isEntered) return;
            _isEntered = true;
            Instances.Add(this);
            Instance = this as TEditorWindow;
            if (this is IOnDuringSceneGui)
            {
                SceneView.duringSceneGui += OnSceneGUI;
                _sceneSubscribed = true;
                SceneView.RepaintAll();
            }
            if (this is IOnHierarchyChanged listener)
            {
                EditorApplication.hierarchyChanged += listener.OnHierarchyChanged;
                _hierarchySubscribed = true;
            }
            try { Enter(); }
            catch { Cleanup(); throw; }
        }

        private void OnGUI() { if (_isEntered) Draw(); }
        private void OnInspectorUpdate() { if (_isEntered && ContinuousRepaint) Repaint(); }
        private void OnDisable()
        {
            try { if (_isEntered) Exit(); }
            finally { Cleanup(); }
        }

        private void Cleanup()
        {
            if (_sceneSubscribed)
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                _sceneSubscribed = false;
            }
            if (_hierarchySubscribed && this is IOnHierarchyChanged listener)
            {
                EditorApplication.hierarchyChanged -= listener.OnHierarchyChanged;
                _hierarchySubscribed = false;
            }
            _isEntered = false;
            Instances.Remove(this);
            if (ReferenceEquals(Instance, this))
                Instance = Instances.Count > 0 ? Instances[Instances.Count - 1] as TEditorWindow : null;
            _resolvedTypes.Clear();
            _labelStyle = null;
            _labelSkin = null;
        }

        public static TEditorWindow Open()
        {
            string name = typeof(TEditorWindow).Name;
            if (name.Contains("Dialog"))
            {
                TEditorWindow result = GetWindow<TEditorWindow>(utility: true, title: name.Replace("Dialog", ""), focus: true);
                result.ShowUtility();
                return result;
            }
            return GetWindow<TEditorWindow>(name.Replace("Window", ""));
        }

        public TEditorWindow SetTitle(string title) { titleContent = new GUIContent(title); return this as TEditorWindow; }

        protected void Button(Color? color = null, string name = "", Action on_click = null, Action none_click = null)
        {
            using EditorGUIScope scope = new(color);
            if (GUILayout.Button(name, EditorLayoutOptions.Compact))
            {
                GUI.FocusControl(null);
                on_click?.Invoke();
            }
            else none_click?.Invoke();
        }

        protected string FieldText(string value, Color? color = null, bool box = false, string front_text = "", bool enabled = true, string control_name = "")
        {
            using EditorGUIScope scope = new(color, box, enabled, control_name);
            return EditorGUILayout.TextField(front_text, value, EditorLayoutOptions.Empty);
        }

        protected int FieldInt(int value, Color color = default, bool box = false, string front_text = "", float width = 0)
        {
            using EditorGUIScope scope = EditorCrowRxExtension.BeginBaseField(color, box, front_text);
            return EditorGUILayout.IntField(value, EditorLayoutOptions.Width(width));
        }

        protected float FieldFloat(float value, Color color = default, bool box = false, string front_text = "", float width = 0, bool enable = true)
        {
            using EditorGUIScope scope = EditorCrowRxExtension.BeginBaseField(color, box, front_text, enable);
            return EditorGUILayout.FloatField(value, EditorLayoutOptions.Width(width));
        }

        protected Vector3 FieldVector3(Vector3 value, Color color = default, bool box = false, string front_text = "", float width = 0)
        {
            using EditorGUIScope scope = EditorCrowRxExtension.BeginBaseField(color, box, front_text);
            return EditorGUILayout.Vector3Field("", value, EditorLayoutOptions.Width(width));
        }

        protected Color FieldColor(Color value, Color color = default, bool box = false, string front_text = "", float width = 0)
        {
            using EditorGUIScope scope = EditorCrowRxExtension.BeginBaseField(color, box, front_text);
            return EditorGUILayout.ColorField(HdrLabel, value, false, false, true, EditorLayoutOptions.Width(width));
        }

        /// <summary>이름이 있는 UnityEngine.Object 필드를 그립니다.</summary>
        protected TObject FieldObject<TObject>(string name, TObject value, bool allow_scene_objects = false, Color? color = null, bool box = false, bool enabled = true, string control_name = "")
            where TObject : UnityEngine.Object
        {
            using EditorGUIScope scope = new(color, box, enabled, control_name);
            return (TObject)EditorGUILayout.ObjectField(name, value, typeof(TObject), allow_scene_objects, EditorLayoutOptions.Empty);
        }

        /// <summary>UnityEngine.Object 필드를 그립니다.</summary>
        protected TObject FieldObject<TObject>(TObject value, bool allow_scene_objects = false, Color? color = null, bool box = false, bool enabled = true, string control_name = "", GUILayoutOption[] options = null)
            where TObject : UnityEngine.Object
        {
            using EditorGUIScope scope = new(color, box, enabled, control_name);
            return (TObject)EditorGUILayout.ObjectField(value, typeof(TObject), allow_scene_objects, options ?? EditorLayoutOptions.Empty);
        }

        protected void DrawTextWithOutline(Rect centerRect, string text, GUIStyle style, Color borderColor, Color innerColor, int borderWidth)
        {
            Color originalColor = style.normal.textColor;
            try
            {
                if (borderWidth > 0)
                {
                    style.normal.textColor = borderColor;
                    Rect rect = centerRect;
                    for (int x = -borderWidth; x <= borderWidth; x++)
                    {
                        rect.x = centerRect.x + x;
                        rect.y = centerRect.y - borderWidth;
                        GUI.Label(rect, text, style);
                        rect.y = centerRect.y + borderWidth;
                        GUI.Label(rect, text, style);
                    }
                    for (int y = -borderWidth + 1; y < borderWidth; y++)
                    {
                        rect.y = centerRect.y + y;
                        rect.x = centerRect.x - borderWidth;
                        GUI.Label(rect, text, style);
                        rect.x = centerRect.x + borderWidth;
                        GUI.Label(rect, text, style);
                    }
                }
                style.normal.textColor = innerColor;
                GUI.Label(centerRect, text, style);
            }
            finally { style.normal.textColor = originalColor; }
        }

        protected Enum Popup(Enum value, string front_text = "", float width = 0, Color color = default, bool box = false, bool enable = true)
        {
            if (value == null) return null;
            using EditorGUIScope scope = EditorCrowRxExtension.BeginBaseField(color, box, front_text, enable);
            return EditorGUILayout.EnumPopup(value, EditorLayoutOptions.Width(width));
        }

        protected int Popup(int value, string[] content, string front_text = "", float width = 0, Color color = default, bool box = false, bool enable = true)
        {
            if (content == null || content.Length == 0) return -1;
            using EditorGUIScope scope = EditorCrowRxExtension.BeginBaseField(color, box, front_text, enable);
            int selected = value >= 0 && value < content.Length ? value : -1;
            return EditorGUILayout.Popup(selected, content, EditorLayoutOptions.Width(width));
        }

        /// <summary>이름으로 Type을 해석합니다. 모호한 단순 이름은 기존 선택을 유지합니다.</summary>
        protected Type Popup(Type value, string[] content, string front_text = "", float width = 0, Color color = default, bool box = false, bool enable = true)
        {
            int index = -1;
            if (value != null && content != null)
                for (int i = 0; i < content.Length; i++)
                    if (content[i] == value.AssemblyQualifiedName || content[i] == value.FullName || content[i] == value.Name) { index = i; break; }
            int selected = Popup(index, content, front_text, width, color, box, enable);
            if (selected < 0 || selected == index) return value;
            return ResolveType(content[selected]) ?? value;
        }

        /// <summary>표시 문자열과 별도로 실제 Type 배열을 사용해 이름 충돌을 피합니다.</summary>
        protected Type Popup(Type value, Type[] types, string[] content, string front_text = "", float width = 0, Color color = default, bool box = false, bool enable = true)
        {
            if (types == null || content == null || types.Length != content.Length)
                throw new ArgumentException("Type and label arrays must have the same length.");
            int selected = Popup(Array.IndexOf(types, value), content, front_text, width, color, box, enable);
            return selected >= 0 ? types[selected] : value;
        }

        private Type ResolveType(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_resolvedTypes.TryGetValue(name, out Type resolved)) return resolved;
            resolved = Type.GetType(name, throwOnError: false);
            if (resolved == null)
            {
                foreach (Type candidate in TypeCache.GetTypesDerivedFrom<object>())
                {
                    if (candidate.FullName != name && candidate.Name != name) continue;
                    if (resolved != null) { resolved = null; break; }
                    resolved = candidate;
                }
            }
            if (_resolvedTypes.Count >= 64) _resolvedTypes.Clear();
            _resolvedTypes[name] = resolved;
            return resolved;
        }

        protected string Popup(string value, string[] content, string front_text = "", float width = 0, Color color = default, bool box = false, bool enable = true)
        {
            int selected = Popup(content == null ? -1 : Array.IndexOf(content, value), content, front_text, width, color, box, enable);
            return selected >= 0 ? content[selected] : value ?? string.Empty;
        }

        protected void Label(string value, Color? color = null, bool box = false, int width = 0, bool middle = false, int fontSize = 12)
        {
            using EditorGUIScope scope = new(color, box, horizontal: true);
            if (_labelStyle == null || _labelSkin != GUI.skin)
            {
                _labelSkin = GUI.skin;
                _labelStyle = new GUIStyle(EditorStyles.label);
            }
            _labelStyle.fontSize = fontSize;
            if (middle) GUILayout.FlexibleSpace();
            GUILayout.Label(value, _labelStyle, EditorLayoutOptions.Width(width));
            if (middle) GUILayout.FlexibleSpace();
        }

        protected bool Toggle(bool value, string front_text = "", string rear_text = "", int width = 0, Color color = default, bool box = false)
        {
            using EditorGUIScope scope = EditorCrowRxExtension.BeginBaseField(color, box, front_text);
            return GUILayout.Toggle(value, rear_text, EditorLayoutOptions.Width(width));
        }

        protected bool Foldout(bool value, Action on_view = null, string front_text = "", string rear_text = "", int width = 0, Color color = default, bool box = false)
        {
            bool expanded;
            using (EditorCrowRxExtension.BeginBaseField(color, box, front_text))
                expanded = GUILayout.Toggle(value, rear_text, EditorStyles.foldout, EditorLayoutOptions.Width(width));
            if (expanded) on_view?.Invoke();
            return expanded;
        }

        private readonly struct EditableField
        {
            internal readonly FieldInfo Field;
            internal readonly string Label;
            internal EditableField(FieldInfo field, string label) { Field = field; Label = label; }
        }

        private static EditableField[] GetEditableFields(Type type)
        {
            if (EditableFields.TryGetValue(type, out EditableField[] cached)) return cached;
            List<EditableField> fields = new();
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsInitOnly || field.IsLiteral) continue;
                    Type fieldType = field.FieldType;
                    if (fieldType != typeof(int) && fieldType != typeof(float) && fieldType != typeof(string) &&
                        fieldType != typeof(Vector3) && fieldType != typeof(Color) && fieldType != typeof(bool) && !fieldType.IsEnum) continue;
                    var attribute = field.GetCustomAttribute<EditorWindowCrowRxSerializeFieldAttribute>(true);
                    if (attribute != null) fields.Add(new EditableField(field, attribute._displayContents));
                }
            }
            cached = fields.ToArray();
            EditableFields.Add(type, cached);
            return cached;
        }

        /// <summary>속성이 지정된 필드를 편집하며 값 타입은 수정된 복사본을 반환합니다.</summary>
        protected STRUCT PropertyEditViewer<STRUCT>(STRUCT data)
        {
            object boxed = data;
            if (boxed == null) return data;
            foreach (EditableField editable in GetEditableFields(boxed.GetType()))
            {
                FieldInfo field = editable.Field;
                object value = field.GetValue(boxed);
                EditorGUI.BeginChangeCheck();
                object result;
                bool changed;
                try { result = DrawEditableField(field, editable.Label, value); }
                finally { changed = EditorGUI.EndChangeCheck(); }
                if (changed) field.SetValue(boxed, result);
            }
            return (STRUCT)boxed;
        }

        /// <summary>메타데이터가 캐시된 필드 하나를 그립니다. 변경 시 GUI.changed를 설정해야 합니다.</summary>
        protected virtual object DrawEditableField(FieldInfo field, string label, object value)
        {
            Type type = field.FieldType;
            if (type == typeof(int)) return FieldInt((int)value, front_text: label);
            if (type == typeof(float)) return FieldFloat((float)value, front_text: label);
            if (type == typeof(string)) return FieldText((string)value, front_text: label);
            if (type == typeof(Vector3)) return FieldVector3((Vector3)value, front_text: label);
            if (type == typeof(Color)) return FieldColor((Color)value, front_text: label);
            if (type == typeof(bool)) return Toggle((bool)value, rear_text: label);
            return Popup((Enum)value, front_text: label);
        }

        /// <summary>창이 활성화된 뒤 첫 그리기 전에 호출됩니다.</summary>
        protected virtual void Enter() { }
        /// <summary>활성 창의 GUI 이벤트마다 호출됩니다.</summary>
        protected virtual void Draw() { }
        /// <summary>비활성화 시 호출됩니다. 예외가 발생해도 구독은 해제됩니다.</summary>
        protected virtual void Exit() { }
        /// <summary>기본 구현은 SceneView 이벤트를 인터페이스 콜백으로 전달합니다.</summary>
        protected virtual void OnSceneGUI(SceneView sceneView)
        {
            if (this is IOnDuringSceneGui listener) listener.OnDuringSceneGui(sceneView);
        }
    }
}
