using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator
{

	[CustomEditor(typeof(RigidColliderCreatorChild))]
	public class RigidColliderCreatorChildEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUI.BeginDisabledGroup(true);
			DrawDefaultInspector();
			EditorGUI.EndDisabledGroup();

			RigidColliderCreatorChild child = target as RigidColliderCreatorChild;

			if (child.parent != null)
			{
				Hull sourceHull = child.parent.FindSourceHull(child);
				if (sourceHull != null)
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.TextField("Source Hull", sourceHull.name);
					EditorGUI.EndDisabledGroup();
				}
				else
				{
					EditorGUILayout.LabelField("No source hull for this child");
				}
			}
			else
			{
				EditorGUILayout.LabelField("Missing parent!");
			}
		}
	}

} // namespace Technie.PhysicsCreator
