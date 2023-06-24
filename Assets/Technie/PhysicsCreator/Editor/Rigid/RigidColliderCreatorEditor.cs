
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using Technie.PhysicsCreator;
using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator.Rigid
{
	[CustomEditor(typeof(RigidColliderCreator))]
	public class RigidColliderCreatorEditor : Editor
	{

		public override void OnInspectorGUI()
		{
			// Ideally we'd use EditorWindow.HasOpenInstances rather than relying on our own singleton instance
			// However that's only available on 2019.4, so for now we have to do it the hard way

			if (RigidColliderCreatorWindow.IsOpen())
			{
				RigidColliderCreatorWindow window = RigidColliderCreatorWindow.instance;

				window.OnInspectorGUI();
			}

			RigidColliderCreator selectedPainter = SelectionUtil.FindSelectedHullPainter() as RigidColliderCreator;
			if (selectedPainter != null)
			{
				if (selectedPainter.paintingData != null
				    && selectedPainter.hullData != null)
				{
					MeshFilter filter = SelectionUtil.FindSelectedMeshFilter();

					GUI.enabled = false;
					EditorGUILayout.ObjectField("Target mesh", filter.sharedMesh, typeof(Mesh), true);
					EditorGUILayout.ObjectField("Editor data", selectedPainter.paintingData, typeof(PaintingData), true);
					EditorGUILayout.ObjectField("Runtime data", selectedPainter.hullData, typeof(HullData), true);
					GUI.enabled = true;

					if (GUILayout.Button(new GUIContent("Open Rigid Collider Creator", Icons.Active.technieIcon)))
					{
						EditorWindow.GetWindow(typeof(RigidColliderCreatorWindow));
					}
				}
				else
				{
					MeshFilter srcMeshFilter = selectedPainter.gameObject.GetComponent<MeshFilter>();
					Mesh srcMesh = srcMeshFilter != null ? srcMeshFilter.sharedMesh : null;
					if (srcMesh != null)
					{
						RigidColliderCreatorWindow.DrawGenerateOrReconnectGui(selectedPainter.gameObject, srcMesh);
					}
					else
					{
						GUILayout.Label("No mesh on current object!");
					}
				}
			}
		}


		// NB: If Gizmos are disabled then OnSceneGUI is not called
		// From 2019.1 onward the RigidColliderCreatorWindow uses OnBeforeSceneGUI so this might be redundant
		// (but still needed for 2018 etc.)
		public void OnSceneGUI ()
		{
			if (RigidColliderCreatorWindow.IsOpen())
			{
				if (Event.current.commandName == "UndoRedoPerformed")
				{
					Console.output.Log("Repaint from OnSceneGUI/UndoRedo");
					RigidColliderCreatorWindow window = RigidColliderCreatorWindow.instance;
					window.Repaint();
				}
			}
		}


	}

} // namespace Techie.PhysicsCreator

