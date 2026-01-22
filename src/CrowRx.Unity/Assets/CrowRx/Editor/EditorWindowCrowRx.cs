using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

// ReSharper disable CheckNamespace
namespace CrowRx.Editor
{
	public class EditorWindowCrowRx<TEditorWindow> : EditorWindow, IEditorCrowRx
		where TEditorWindow : EditorWindow
	{
		public static TEditorWindow Instance { get; private set; }
		private static bool s_IsEnter;

		#region Unity Method

		private void OnValidate()
		{
			Instance = this as TEditorWindow;
		}

		private void OnEnable()
		{
			OnValidate();

			s_IsEnter = false;

			if (!s_IsEnter)
			{
				s_IsEnter = true;

				if (Instance is IOnDuringSceneGui)
				{
					SceneView.duringSceneGui += OnSceneGUI;
					SceneView.RepaintAll();
				}

				Enter();
			}
		}

		private void OnGUI()
		{
			if (s_IsEnter)
				Draw();

			Repaint();
		}

		private void OnDisable()
		{
			Exit();

			if (Instance is IOnDuringSceneGui)
			{
				SceneView.duringSceneGui -= OnSceneGUI;
			}

			Instance = null;
		}
		#endregion Unity Method

		public static TEditorWindow Open()
		{
			TEditorWindow result = default(TEditorWindow);
			if (typeof(TEditorWindow).Name.Contains("Dialog"))
			{
				result = GetWindow<TEditorWindow>(utility: true, title: typeof(TEditorWindow).Name.Replace("Dialog", ""), focus: true);
				result?.ShowUtility();
			}
			else
			if (typeof(TEditorWindow).Name.Contains("Window"))
			{
				result = GetWindow<TEditorWindow>(typeof(TEditorWindow).Name.Replace("Window", ""));
			}
			return result;
		}

		public TEditorWindow SetTitle(string title)
		{
			titleContent = new GUIContent(title);

			return this as TEditorWindow;
		}

		protected void Button(Color? color = null, string name = "", Action on_click = null, Action none_click = null)
		{
			GUI.color = color.HasValue ? color.Value : Color.white;

			bool is_click = false;

			is_click = GUILayout.Button(name, GUILayout.ExpandWidth(false));

			GUI.color = Color.white;

			if (is_click)
			{
				GUI.FocusControl(null);
				on_click?.Invoke();
			}
			else
			{
				none_click?.Invoke();
			}
		}

		#region Fields
		protected string FieldText(string value, Color? color = null, bool box = false, string front_text = "", bool enabled = true, string control_name = "")
		{
			this.DrawGUIElement(content: () =>
			{
				value = EditorGUILayout.TextField(front_text, value);
			}, color: color, box: box, enabled: enabled, control_name: control_name);
			return value;
		}

		protected int FieldInt(int value, Color color = default(Color), bool box = false, string front_text = "", float width = 0)
		{
			int result = 0;

			this.BaseField(() =>
			{
				if (width == 0)
				{
					result = EditorGUILayout.IntField(value);
				}
				else
				{
					result = EditorGUILayout.IntField(value, GUILayout.Width(width));
				}
			}, color, box, front_text);

			return result;
		}

		protected float FieldFloat(float value, Color color = default(Color), bool box = false, string front_text = "", float width = 0, bool enable = true)
		{
			float result = 0f;

			this.BaseField(() =>
			{
				GUI.enabled = enable;
				if (width == 0)
				{
					result = EditorGUILayout.FloatField(value);
				}
				else
				{
					result = EditorGUILayout.FloatField(value, GUILayout.Width(width));
				}
				GUI.enabled = true;
			}, color, box, front_text);

			return result;
		}

		protected Vector3 FieldVector3(Vector3 value, Color color = default(Color), bool box = false, string front_text = "", float width = 0)
		{
			Vector3 result = Vector3.zero;

			this.BaseField(() =>
			{
				if (width == 0)
				{
					result = EditorGUILayout.Vector3Field("", value);
				}
				else
				{
					result = EditorGUILayout.Vector3Field("", value, GUILayout.Width(width));
				}
			}, color, box, front_text);

			return result;
		}

		protected Color FieldColor(Color value, Color color = default(Color), bool box = false, string front_text = "", float width = 0)
		{
			Color result = Color.white;

			this.BaseField(() =>
			{
				if (width == 0)
				{
					result = EditorGUILayout.ColorField(new GUIContent("hdr"), value, false, false, true);
				}
				else
				{
					result = EditorGUILayout.ColorField(value, GUILayout.Width(width));
				}
			}, color, box, front_text);

			return result;
		}

		/// <summary>
		/// UnityEngine.Object 필드
		/// </summary>
		protected TObject FieldObject<TObject>(string name, TObject value, bool allow_scene_objects = false, Color? color = null, bool box = false, bool enabled = true, string control_name = "")
			where TObject : UnityEngine.Object
			=> this.DrawGUIElement<TObject>(
				content: () => (TObject)EditorGUILayout.ObjectField(name, value, typeof(TObject), allow_scene_objects),
				color: color,
				box: box,
				enabled: enabled,
				control_name: control_name);

		/// <summary>
		/// UnityEngine.Object 필드
		/// </summary>
		protected TObject FieldObject<TObject>(TObject value, bool allow_scene_objects = false, Color? color = null, bool box = false, bool enabled = true, string control_name = "", GUILayoutOption[] options = null)
			where TObject : UnityEngine.Object
			=> this.DrawGUIElement<TObject>(
				content: () => (TObject)EditorGUILayout.ObjectField(value, typeof(TObject), allow_scene_objects, options ?? Array.Empty<GUILayoutOption>()),
				color: color,
				box: box,
				enabled: enabled,
				control_name: control_name);

		protected void DrawTextWithOutline(Rect centerRect, string text, GUIStyle style, Color borderColor, Color innerColor, int borderWidth)
		{
			// assign the border color
			style.normal.textColor = borderColor;

			// draw an outline color copy to the left and up from original
			Rect modRect = centerRect;
			modRect.x -= borderWidth;
			modRect.y -= borderWidth;
			GUI.Label(modRect, text, style);


			// stamp copies from the top left corner to the top right corner
			while (modRect.x <= centerRect.x + borderWidth)
			{
				modRect.x++;
				GUI.Label(modRect, text, style);
			}

			// stamp copies from the top right corner to the bottom right corner
			while (modRect.y <= centerRect.y + borderWidth)
			{
				modRect.y++;
				GUI.Label(modRect, text, style);
			}

			// stamp copies from the bottom right corner to the bottom left corner
			while (modRect.x >= centerRect.x - borderWidth)
			{
				modRect.x--;
				GUI.Label(modRect, text, style);
			}

			// stamp copies from the bottom left corner to the top left corner
			while (modRect.y >= centerRect.y - borderWidth)
			{
				modRect.y--;
				GUI.Label(modRect, text, style);
			}

			// draw the inner color version in the center
			style.normal.textColor = innerColor;
			GUI.Label(centerRect, text, style);
		}

		#endregion

		protected Enum Popup(Enum value, string front_text = "", float width = 0, Color color = default(Color), bool box = false, bool enable = true)
		{
			Enum result = value;

			this.BaseField(() =>
			{
				GUI.enabled = enable;
				if (width == 0)
				{
					result = EditorGUILayout.EnumPopup(result);
				}
				else
				{
					result = EditorGUILayout.EnumPopup(result, GUILayout.Width(width));
				}
				GUI.enabled = true;
			}, color, box, front_text);

			return result;
		}

		protected int Popup(int value, string[] content, string front_text = "", float width = 0, Color color = default(Color), bool box = false, bool enable = true)
		{
			int result = value;

			this.BaseField(() =>
			{
				GUI.enabled = enable;
				if (width == 0)
				{
					result = EditorGUILayout.Popup(result, content);
				}
				else
				{
					result = EditorGUILayout.Popup(result, content, GUILayout.Width(width));
				}
				GUI.enabled = true;
			}, color, box, front_text);

			return result;
		}

		protected Type Popup(Type value, string[] content, string front_text = "", float width = 0, Color color = default(Color), bool box = false, bool enable = true)
		{
			int result = ArrayIndexOf(value.Name, content);

			this.BaseField(() =>
			{
				GUI.enabled = enable;
				if (width == 0)
				{
					result = EditorGUILayout.Popup(result, content);
				}
				else
				{
					result = EditorGUILayout.Popup(result, content, GUILayout.Width(width));
				}
				GUI.enabled = true;
			}, color, box, front_text);

			return Type.GetType(content[result]);
		}

		protected string Popup(string value, string[] content, string front_text = "", float width = 0, Color color = default(Color), bool box = false, bool enable = true)
		{
			int result = ArrayIndexOf(value, content);

			this.BaseField(() =>
			{
				GUI.enabled = enable;
				if (width == 0)
				{
					result = EditorGUILayout.Popup(result, content);
				}
				else
				{
					result = EditorGUILayout.Popup(result, content, GUILayout.Width(width));
				}
				GUI.enabled = true;
			}, color, box, front_text);


			if (content.Length > result)
			{
				return content[result];
			}
			else
			{
				return string.Empty;
			}
		}

		private int ArrayIndexOf(string value, string[] content)
		{
			for (int index = 0; index < content.Length; ++index)
			{
				if (content[index].Equals(value))
				{
					return index;
				}
			}
			return 0;
		}

		protected void Label(string value, Color? color = null, bool box = false, int width = 0, bool middle = false, int fontSize = 12)
		{
			this.Horizontal(content: () =>
			{
				// guiStyle = new GUIStyle(); //create a new variable
				//.fontSize = fontSize;
				//.normal.textColor = Color.white;

				if (middle)
				{
					GUILayout.FlexibleSpace();
					GUILayout.Label(value);
					GUILayout.FlexibleSpace();
				}
				else if (width == 0)
				{
					GUILayout.Label(value);
				}
				else
				{
					GUILayout.Label(value, GUILayout.Width(width));
				}
			}, color: color, box: box);
		}

		protected bool Toggle(bool value, string front_text = "", string rear_text = "", int width = 0, Color color = default(Color), bool box = false)
		{
			bool result = value;

			this.BaseField(() =>
			{
				if (width == 0)
				{
					result = GUILayout.Toggle(value, rear_text);
				}
				else
				{
					result = GUILayout.Toggle(value, rear_text, GUILayout.Width(width));
				}
			}, color, box, front_text);

			return result;
		}

		protected bool Foldout(bool value, Action on_view = null, string front_text = "", string rear_text = "", int width = 0, Color color = default(Color), bool box = false)
		{
			bool result = value;

			this.BaseField(() =>
			{
				if (width == 0)
				{
					result = GUILayout.Toggle(value, rear_text, "Foldout");
				}
				else
				{
					result = GUILayout.Toggle(value, rear_text, "Foldout", GUILayout.Width(width));
				}
			}, color, box, front_text);

			if (result)
			{
				on_view?.Invoke();
			}

			return result;
		}

		private readonly Type WINDOW_SERIALIZE_FIELD = typeof(EditorWindowCrowRxSerializeFieldAttribute);
		private readonly Type INT = typeof(int);
		private readonly Type FLOAT = typeof(float);
		private readonly Type STRING = typeof(string);
		private readonly Type VECTOR3 = typeof(Vector3);
		private readonly Type COLOR = typeof(Color);
		private readonly Type BOOL = typeof(bool);
		private readonly Type ENUM = typeof(Enum);

		protected STRUCT PropertyEditViewer<STRUCT>(STRUCT data)
		{
			IEnumerable<FieldInfo> fieldInfos = data.GetType()
				.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(field => field.IsDefined(WINDOW_SERIALIZE_FIELD, true));

			foreach (FieldInfo fieldInfo in fieldInfos)
			{
				// 필드 타입
				Type fieldType = fieldInfo.FieldType;
				// 필드 값
				dynamic value = Convert.ChangeType(fieldInfo.GetValue(data), fieldType);
				// 화면에 보여지는 필드 설명
				string displayContents = fieldInfo.GetCustomAttributes<EditorWindowCrowRxSerializeFieldAttribute>(true).First()._displayContents;

				// 각 타입별 속성 에디터 뷰어
				if (fieldType.Equals(INT))
				{
					fieldInfo.SetValue(data, FieldInt(value, front_text: displayContents));
				}
				else if (fieldType.Equals(FLOAT))
				{
					fieldInfo.SetValue(data, FieldFloat(value, front_text: displayContents));
				}
				else if (fieldType.Equals(STRING))
				{
					fieldInfo.SetValue(data, FieldText(value, front_text: displayContents));
				}
				else if (fieldType.Equals(VECTOR3))
				{
					fieldInfo.SetValue(data, FieldVector3(value, front_text: displayContents));
				}
				else if (fieldType.Equals(COLOR))
				{
					fieldInfo.SetValue(data, FieldColor(value, front_text: displayContents));
				}
				else if (fieldType.Equals(BOOL))
				{
					fieldInfo.SetValue(data, Toggle(value, rear_text: displayContents));
				}
				else if (fieldType.Equals(ENUM))
				{
					fieldInfo.SetValue(data, Popup(value, front_text: displayContents));
				}
			}

			return data;
		}

		#region Custom editor window life cycle.
		/// <summary>
		/// 윈도우 창 활성화 OnEnable 하위 Draw 프레임 돌아가기 전에들어온다
		/// </summary>
		protected virtual void Enter() { }
		/// <summary>
		/// Enter 이후 OnGUI의 틱당 들어옴.
		/// </summary>
		protected virtual void Draw() { }
		/// <summary>
		/// 윈도우 창 비활성화 될때 (OnDisable)
		/// </summary>
		protected virtual void Exit() { }
		/// <summary>
		/// SceneView 이벤트.
		/// </summary>
		/// <param name="sceneView"></param>
		protected virtual void OnSceneGUI(SceneView sceneView) { }
		#endregion
	}
}