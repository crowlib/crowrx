using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CrowRx.Editor
{
    [CustomPropertyDrawer(typeof(SerializeReferenceListAttribute), useForChildren: false)]
    public class SerializeReferenceListDrawer : PropertyDrawer
    {
        private const int CacheCapacity = 32;
        private readonly Dictionary<string, ListState> _listCache = new();
        private static readonly Dictionary<Type, Type[]> CandidateTypes = new();
        private static readonly Dictionary<Type, GUIContent> TypeLabels = new();
        private SerializedObject _owner;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ListState state = GetList(property, label);
            if (state == null)
            {
                EditorGUI.LabelField(position, label.text, "Invalid SerializedReferenceList<T>");
                return;
            }
            EditorGUI.BeginProperty(position, label, property);
            try { state.List.DoList(position); }
            finally { EditorGUI.EndProperty(); }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => GetList(property, label)?.List.GetHeight() ?? EditorGUIUtility.singleLineHeight;

        private ListState GetList(SerializedProperty property, GUIContent label)
        {
            if (!ReferenceEquals(_owner, property.serializedObject))
            {
                ClearLists();
                _owner = property.serializedObject;
            }

            SerializedProperty listProperty = property.FindPropertyRelative("items");
            if (listProperty == null) return null;
            if (!listProperty.isArray || listProperty.propertyType != SerializedPropertyType.Generic)
            {
                listProperty.Dispose();
                return null;
            }

            if (!_listCache.TryGetValue(property.propertyPath, out ListState state))
            {
                Type elementType = GetElementType(fieldInfo?.FieldType) ??
                                   GetElementType(property.GetFieldInfoFromProperty()?.FieldType);
                if (elementType == null)
                {
                    listProperty.Dispose();
                    return null;
                }
                if (_listCache.Count == CacheCapacity) ClearLists();
                state = new ListState(property.serializedObject, listProperty, GetCandidates(elementType));
                _listCache.Add(property.propertyPath, state);
            }
            else
            {
                SerializedProperty previous = state.List.serializedProperty;
                state.List.serializedProperty = listProperty;
                previous?.Dispose();
            }
            state.Header.text = label.text;
            state.Header.tooltip = label.tooltip;
            state.Header.image = label.image;
            return state;
        }

        private void ClearLists()
        {
            foreach (ListState state in _listCache.Values) state.List.serializedProperty?.Dispose();
            _listCache.Clear();
        }

        private static Type GetElementType(Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(SerializedReferenceList<>))
                    return current.GetGenericArguments()[0];
            return null;
        }

        private static bool CanCreate(Type type)
            => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters &&
               !typeof(UnityEngine.Object).IsAssignableFrom(type) &&
               type.IsDefined(typeof(SerializableAttribute), false) &&
               type.GetConstructor(Type.EmptyTypes) != null;

        private static Type[] GetCandidates(Type baseType)
        {
            if (CandidateTypes.TryGetValue(baseType, out Type[] cached)) return cached;
            List<Type> types = new();
            if (CanCreate(baseType)) types.Add(baseType);
            foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType))
                if (CanCreate(type)) types.Add(type);
            types.Sort((a, b) =>
            {
                int name = string.Compare(a.FullName, b.FullName, StringComparison.Ordinal);
                return name != 0 ? name : string.Compare(a.AssemblyQualifiedName, b.AssemblyQualifiedName, StringComparison.Ordinal);
            });
            cached = types.ToArray();
            CandidateTypes.Add(baseType, cached);
            return cached;
        }

        private static GUIContent GetTypeLabel(Type type)
        {
            if (!TypeLabels.TryGetValue(type, out GUIContent label))
            {
                label = new GUIContent(type.FullName?.Replace('+', '.') ?? type.Name, type.AssemblyQualifiedName);
                TypeLabels.Add(type, label);
            }
            return label;
        }

        private sealed class ListState
        {
            internal readonly ReorderableList List;
            internal readonly GUIContent Header = new();
            private readonly Type[] _types;

            internal ListState(SerializedObject owner, SerializedProperty property, Type[] types)
            {
                _types = types;
                List = new ReorderableList(owner, property, true, true, true, true);
                List.drawHeaderCallback = rect => EditorGUI.LabelField(rect, Header);
                List.drawElementCallback = DrawElement;
                List.elementHeightCallback = ElementHeight;
                List.onAddDropdownCallback = ShowAddMenu;
            }

            private void DrawElement(Rect rect, int index, bool active, bool focused)
            {
                using SerializedProperty element = List.serializedProperty.GetArrayElementAtIndex(index);
                rect.x += 8;
                rect.xMax -= 4;
                object value = element.managedReferenceValue;
                EditorGUI.PropertyField(rect, element, value == null ? GUIContent.none : GetTypeLabel(value.GetType()), true);
            }

            private float ElementHeight(int index)
            {
                using SerializedProperty element = List.serializedProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + 2;
            }

            private void ShowAddMenu(Rect rect, ReorderableList list)
            {
                var menu = new GenericMenu();
                SerializedObject owner = List.serializedProperty.serializedObject;
                if (owner.isEditingMultipleObjects)
                    menu.AddDisabledItem(new GUIContent("Add requires a single target"));
                else if (_types.Length == 0)
                    menu.AddDisabledItem(new GUIContent("No serializable types with a public default constructor"));
                else
                {
                    UnityEngine.Object target = owner.targetObject;
                    string path = List.serializedProperty.propertyPath;
                    foreach (Type type in _types)
                    {
                        Type selectedType = type;
                        menu.AddItem(GetTypeLabel(type), false, () => AddElement(target, path, selectedType));
                    }
                }
                menu.DropDown(rect);
            }

            private static void AddElement(UnityEngine.Object target, string path, Type type)
            {
                if (!target) return;
                try
                {
                    // 생성자 실패로 빈 항목이 남지 않도록 직렬화 수정 전에 인스턴스를 만듭니다.
                    object instance = Activator.CreateInstance(type);
                    using var owner = new SerializedObject(target);
                    owner.Update();
                    using SerializedProperty property = owner.FindProperty(path);
                    if (property == null || !property.isArray) return;
                    int count = property.arraySize;
                    try
                    {
                        property.arraySize = count + 1;
                        using SerializedProperty element = property.GetArrayElementAtIndex(count);
                        element.managedReferenceValue = instance;
                        owner.ApplyModifiedProperties();
                    }
                    catch { owner.Update(); throw; }
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception is TargetInvocationException { InnerException: { } inner } ? inner : exception, target);
                }
            }
        }
    }

    public static class SerializedPropertyExtensions
    {
        /// <summary>중첩 필드, 배열/List 항목, 상속된 private 필드의 실제 선언을 찾습니다.</summary>
        public static FieldInfo GetFieldInfoFromProperty(this SerializedProperty property)
        {
            if (property?.serializedObject.targetObject == null) return null;
            return ResolveFieldInfo(property.serializedObject.targetObject.GetType(), property.propertyPath, property.serializedObject);
        }

        internal static FieldInfo ResolveFieldInfo(Type rootType, string propertyPath, SerializedObject owner = null)
        {
            if (rootType == null || string.IsNullOrEmpty(propertyPath)) return null;
            string[] parts = propertyPath.Split('.');
            Type current = rootType;
            FieldInfo field = null;
            string prefix = "";
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                prefix = prefix.Length == 0 ? part : prefix + "." + part;
                if (part == "Array" && i + 1 < parts.Length && parts[i + 1].StartsWith("data[", StringComparison.Ordinal))
                {
                    current = GetCollectionElementType(current);
                    prefix += "." + parts[++i];
                }
                else
                {
                    field = FindField(current, part);
                    if (field == null) return null;
                    current = field.FieldType;
                }
                if (current == null) return null;
                if (owner != null)
                {
                    using SerializedProperty parent = owner.FindProperty(prefix);
                    if (parent?.propertyType == SerializedPropertyType.ManagedReference && parent.managedReferenceValue != null)
                        current = parent.managedReferenceValue.GetType();
                }
            }
            return field;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            for (; type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            return null;
        }

        private static Type GetCollectionElementType(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            for (Type current = type; current != null; current = current.BaseType)
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(List<>))
                    return current.GetGenericArguments()[0];
            return null;
        }
    }
}
