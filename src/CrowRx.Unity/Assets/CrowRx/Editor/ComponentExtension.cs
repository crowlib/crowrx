using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace CrowRx.Editor
{
    public enum VariableType { Field, Property }

    public static class ComponentExtension
    {
        private static readonly Dictionary<(Type, string, VariableType), MemberInfo> Members = new();

        internal static MemberInfo FindMember(Type type, string name, VariableType variableType)
        {
            if (type == null || string.IsNullOrEmpty(name)) return null;
            var key = (type, name, variableType);
            if (Members.TryGetValue(key, out MemberInfo cached)) return cached;
            MemberInfo member = null;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;
            for (Type current = type; current != null && member == null; current = current.BaseType)
            {
                if (variableType == VariableType.Field) member = current.GetField(name, flags);
                else if (variableType == VariableType.Property)
                {
                    PropertyInfo property = current.GetProperty(name, flags);
                    if (property != null)
                    {
                        if (property.GetIndexParameters().Length == 0 && property.GetGetMethod(true) != null) member = property;
                        break;
                    }
                }
            }
            if (Members.Count >= 256) Members.Clear();
            Members.Add(key, member);
            return member;
        }

        /// <summary>지정 타입과 기반 타입의 인스턴스 필드 또는 속성 값을 읽습니다.</summary>
        public static TValueType GetValue<TValueType>(this Component self, Type componentType, string fieldName, VariableType variableType)
        {
            if (!self) return default;
            MemberInfo member = FindMember(componentType, fieldName, variableType);
            if (member is FieldInfo field) return (TValueType)field.GetValue(self);
            if (member is PropertyInfo property)
                return (TValueType)property.GetValue(self);
            return default;
        }

        /// <summary>컴포넌트의 지정 필드 또는 속성 값을 읽습니다.</summary>
        public static TValueType GetValue<TComponent, TValueType>(this TComponent self, string field_name, VariableType variable_type)
            where TComponent : Component
            => self.GetValue<TValueType>(typeof(TComponent), field_name, variable_type);
    }
}
