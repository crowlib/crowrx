using System;
using System.Reflection;

using UnityEngine;

// ReSharper disable CheckNamespace
namespace CrowRx.Editor
{
	public enum VariableType
	{
		Field,
		Property,
	}

	public static class ComponentExtension
	{
		/// <summary>
		/// TComponent self 안의 "TValueType name" 변수의 값을 받아온다.
		/// </summary>
		/// <typeparam name="TValueType"> 변수 타입 </typeparam>
		/// <param name="self"> 컴포넌트 인스턴스 </param>
		/// <param name="componentType"> 변수가 있는 컴포넌트 타입 </param>
		/// <param name="fieldName"> 변수의 이름 </param>
		/// <param name="variableType"> 선언 형식 </param>
		/// <returns> 변수의 값을 리턴 </returns>
		public static TValueType GetValue<TValueType>(this Component self, Type componentType, string fieldName, VariableType variableType)
		{
			TValueType result = default(TValueType);
			if (componentType != null)
			{
				if (VariableType.Field.Equals(variableType))
				{
					FieldInfo fieldInfo = componentType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					if (fieldInfo == null)
					{
						result = self.GetValue<TValueType>(componentType.BaseType, fieldName, variableType);
					}
					else
					{
						result = (TValueType)fieldInfo.GetValue(self);
					}
				}
				else
				if (VariableType.Property.Equals(variableType))
				{
					PropertyInfo propertyInfo = componentType.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					if (propertyInfo == null)
					{
						result = self.GetValue<TValueType>(componentType.BaseType, fieldName, variableType);
					}
					else
					{
						result = (TValueType)propertyInfo.GetValue(self);
					}
					//result = (TValueType)component_type.GetProperty(field_name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(self);
				}
			}
			return result;
		}

		/// <summary>
		/// TComponent self 안의 "TValueType name" 변수의 값을 받아온다.
		/// </summary>
		/// <typeparam name="TComponent"> 변수가 있는 컴포넌트 타입 </typeparam>
		/// <typeparam name="TValueType"> 변수 타입 </typeparam>
		/// <param name="self"> 컴포넌트 인스턴스 </param>
		/// <param name="field_name"> 변수의 이름 </param>
		/// <param name="variable_type"> 선언 형식 </param>
		/// <returns> 변수의 값을 리턴 </returns>
		public static TValueType GetValue<TComponent, TValueType>(this TComponent self, string field_name, VariableType variable_type)
			where TComponent : Component
		{
			return self.GetValue<TValueType>(typeof(TComponent), field_name, variable_type);
		}
	}
}