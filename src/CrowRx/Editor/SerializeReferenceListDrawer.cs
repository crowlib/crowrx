using System;
using System.Linq;
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
        private readonly Dictionary<string, ReorderableList> _listCache = new();


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty listProperty = property.FindPropertyRelative("items");
            if (listProperty is not { isArray: true } || listProperty.propertyType != SerializedPropertyType.Generic)
            {
                EditorGUI.LabelField(position, label.text, "Invalid List<T>");
                return;
            }

            if (!_listCache.TryGetValue(property.propertyPath, out ReorderableList reorderableList))
            {
                Type listType = GetElementTypeFromGenericList(property);
                Type[] derivedTypes = TypeCache.GetTypesDerivedFrom(listType)
                    .Where(t => !t.IsAbstract && !t.IsGenericType)
                    .OrderBy(t => t.Name)
                    .ToArray();

                reorderableList = new ReorderableList(property.serializedObject, listProperty, true, true, true, true)
                {
                    drawHeaderCallback = rect => EditorGUI.LabelField(rect, label.text),

                    drawElementCallback = (rect, index, _, _) =>
                    {
                        SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
                        rect.x += 8;
                        rect.xMax -= 4;

                        GUIContent customLabel = GUIContent.none;

                        if (element.managedReferenceValue != null)
                        {
                            Type type = element.managedReferenceValue.GetType();
                            customLabel = new GUIContent(type.Name);
                        }

                        EditorGUI.PropertyField(rect, element, customLabel, true);
                    },

                    elementHeightCallback = index =>
                    {
                        SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
                        return EditorGUI.GetPropertyHeight(element, true) + 2;
                    },
                    
                    onAddDropdownCallback = (_, _) =>
                    {
                        var menu = new GenericMenu();
                        for (int i = 0; i < derivedTypes.Length; i++)
                        {
                            int captured = i;
                            menu.AddItem(new GUIContent(derivedTypes[i].Name), false, () =>
                            {
                                listProperty.serializedObject.Update();
                                listProperty.arraySize++;
                                SerializedProperty newElement = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                                newElement.managedReferenceValue = Activator.CreateInstance(derivedTypes[captured]);
                                listProperty.serializedObject.ApplyModifiedProperties();
                            });
                        }

                        menu.ShowAsContext();
                    }
                };

                _listCache[property.propertyPath] = reorderableList;
            }

            reorderableList.DoList(position);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (_listCache.TryGetValue(property.propertyPath, out ReorderableList reorderableList))
            {
                return reorderableList.GetHeight();
            }

            return EditorGUIUtility.singleLineHeight * 2;
        }

        private static Type GetElementTypeFromGenericList(SerializedProperty property)
        {
            FieldInfo field = property.GetFieldInfoFromProperty();
            Type fieldType = field?.FieldType;

            return fieldType is { IsGenericType: true } ? fieldType.GetGenericArguments()[0] : typeof(object); // fallback
        }
    }

    // Extension helper to extract FieldInfo
    public static class SerializedPropertyExtensions
    {
        public static FieldInfo GetFieldInfoFromProperty(this SerializedProperty property)
        {
            Type parentType = property.serializedObject.targetObject.GetType();
            return parentType.GetField(property.name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }
    }
}