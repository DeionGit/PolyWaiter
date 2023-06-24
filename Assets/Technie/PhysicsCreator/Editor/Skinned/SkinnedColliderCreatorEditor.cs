using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Technie.PhysicsCreator.Skinned
{
	[CustomEditor(typeof(SkinnedColliderCreator))]
	public class SkinnedColliderCreatorEditor : Editor
	{
		//private Icons icons;

		public override void OnInspectorGUI()
		{
			// Ideally we'd use EditorWindow.HasOpenInstances rather than relying on our own singleton instance
			// However that's only available on 2019.4, so for now we have to do it the hard way

			if (SkinnedColliderCreatorWindow.IsOpen())
			{
				SkinnedColliderCreatorWindow window = SkinnedColliderCreatorWindow.instance;

				window.OnInspectorGUI();
			}

			SkinnedColliderCreator component = (SkinnedColliderCreator)target;
			if (component != null)
			{
				if (component.targetSkinnedRenderer != null && component.editorData != null)
				{
					GUI.enabled = false;
					EditorGUILayout.ObjectField("Target renderer", component.targetSkinnedRenderer, typeof(SkinnedMeshRenderer), true);
					EditorGUILayout.ObjectField("Editor data", component.editorData, typeof(SkinnedColliderEditorData), true);
					EditorGUILayout.ObjectField("Runtime data", component.editorData.runtimeData, typeof(SkinnedColliderRuntimeData), true);
					GUI.enabled = true;

					if (GUILayout.Button(new GUIContent("Open Skinned Collider Creator", Icons.Active.technieIcon)))
					{
						SkinnedColliderCreatorWindow.ShowWindow();
					}
				}
				else if (component.targetSkinnedRenderer == null)
				{
					GUILayout.Label("Component missing! Needs a SkinnedMeshRenderer", EditorStyles.boldLabel);
					GUILayout.Label("Put this component on the same object as your target SkinnedMeshRenderer");

					// TODO: Or allow the user to drag in a target skinned mesh renderer?
				}
				else if (component.editorData == null)
				{
					GUI.enabled = false;
					EditorGUILayout.ObjectField("Target renderer", component.targetSkinnedRenderer, typeof(SkinnedMeshRenderer), true);
					GUI.enabled = true;

					if (GUILayout.Button("Generate asset data"))
					{
						SkinnedColliderCreatorWindow.GenerateAsset(component.gameObject, component.targetSkinnedRenderer);
					}
				}
			}
		}
	}
}
