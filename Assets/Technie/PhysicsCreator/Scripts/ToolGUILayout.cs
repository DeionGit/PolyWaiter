using UnityEngine;
using System.Collections.Generic;

namespace Technie.PhysicsCreator
{
	public class ToolGUILayout
	{
		public static Dictionary<string, Vector2> buttonPositions = new Dictionary<string, Vector2>();

		public static bool Button(string buttonName, ref Vector2 buttonPos)
		{
			bool res = GUILayout.Button(buttonName);
			if (Event.current.type == EventType.Repaint)
			{
				Rect lastRect = GUILayoutUtility.GetLastRect();
				buttonPos = GUIUtility.GUIToScreenPoint(lastRect.center);
			}
			return res;
		}

		public static bool Button(string buttonId, Rect rect, GUIContent content, GUIStyle style)
		{
			bool res = GUI.Button(rect, content, style);
			if (Event.current.type == EventType.Repaint)
			{
				buttonPositions[buttonId] = GUIUtility.GUIToScreenPoint(rect.center);
			}
			return res;
		}

		public static bool Button(string buttonId, GUIContent content, GUIStyle style, params GUILayoutOption[] options)
		{
			bool res = GUILayout.Button(content, style, options);
			if (Event.current.type == EventType.Repaint)
			{
				Rect lastRect = GUILayoutUtility.GetLastRect();
				buttonPositions[buttonId] = GUIUtility.GUIToScreenPoint(lastRect.center);
			}
			return res;
		}

		public static bool Button(string buttonId, string buttonName)
		{
			bool res = GUILayout.Button(buttonName);
			if (Event.current.type == EventType.Repaint)
			{
				Rect lastRect = GUILayoutUtility.GetLastRect();
				Vector2 buttonPos = GUIUtility.GUIToScreenPoint(lastRect.center);
				buttonPositions[buttonId] = buttonPos;
			}
			return res;
		}
		
		public static Vector2 GetButtonPosition(string buttonId)
		{
			return buttonPositions[buttonId];
		}
	}
	
} // namespace Technie.PhysicsCreator