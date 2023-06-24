using UnityEngine;
using UnityEditor;
using System.Collections;
using Technie.PhysicsCreator.Skinned;

namespace Technie.PhysicsCreator
{
	public class SelectionUtil
	{	
		public static ICreatorComponent FindSelectedHullPainter()
		{
			// Works for components in the scene, causes NPEs for selected prefabs in the assets dir
			if (Selection.transforms.Length == 1)
			{
				GameObject currentSelection = Selection.transforms[0].gameObject;

				ICreatorComponent comp = currentSelection.GetComponent<ICreatorComponent>();
				if (comp != null)
					return comp;
				/*
				RigidColliderCreator rigid = currentSelection.GetComponent<RigidColliderCreator>();
				if (rigid != null)
					return rigid;

				SkinnedColliderCreator skinned = currentSelection.GetComponent<SkinnedColliderCreator>();
				if (skinned != null)
					return skinned;
				*/
			}
			return null;
		}

		public static RigidColliderCreatorChild FindSelectedHullPainterChild()
		{
			// Works for components in the scene, causes NPEs for selected prefabs in the assets dir
			if (Selection.transforms.Length == 1)
			{
				GameObject currentSelection = Selection.transforms[0].gameObject;

				RigidColliderCreatorChild painter = currentSelection.GetComponent<RigidColliderCreatorChild>();
				if (painter != null)
					return painter;
			}
			return null;
		}

		public static GameObject FindSelectedGameObject()
		{
			if (Selection.transforms.Length > 0)
			{
				GameObject selectedObject = Selection.transforms[0].gameObject;
				return selectedObject;
			}
			return null;
		}

		public static MeshFilter FindSelectedMeshFilter()
		{
			if (Selection.transforms.Length == 1)
			{
				GameObject currentSelection = Selection.transforms[0].gameObject;
				return currentSelection.GetComponent<MeshFilter>();
			}
			return null;
		}

		public static Renderer FindSelectedRenderer()
		{
			if (Selection.transforms.Length == 1)
			{
				GameObject currentSelection = Selection.transforms[0].gameObject;

				// We could just use GetComponent<Renderer> but really we only want to find MeshRenderers or SkinnedMeshRenderers
				// (not line or particle renderers) so we check for each explicitly

				MeshRenderer meshRenderer = currentSelection.GetComponent<MeshRenderer>();
				if (meshRenderer != null)
					return meshRenderer;

				SkinnedMeshRenderer skinnedRenderer = currentSelection.GetComponent<SkinnedMeshRenderer>();
				return skinnedRenderer;
			}
			return null;
		}
	}

} // namespace Technie.PhysicsCreator
