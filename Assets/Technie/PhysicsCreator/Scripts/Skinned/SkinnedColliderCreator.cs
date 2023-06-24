using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Technie.PhysicsCreator.Skinned
{
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	public class SkinnedColliderCreator : MonoBehaviour, ICreatorComponent
	{
		public SkinnedMeshRenderer targetSkinnedRenderer;
		public SkinnedColliderEditorData editorData;

		private void OnDestroy()
		{

		}

		private void OnEnable()
		{
			targetSkinnedRenderer = this.gameObject.GetComponent<SkinnedMeshRenderer>();
		}

		public GameObject GetGameObject()
		{
			return this.gameObject;
		}

		public bool HasEditorData()
		{
			return editorData != null;
		}

		public IEditorData GetEditorData()
		{
			return editorData;
		}

		public Transform FindBone(BoneData boneData)
		{
			if (boneData == null)
				return null;

			return FindBone(targetSkinnedRenderer, boneData.targetBoneName);
		}

		public Transform FindBone(BoneHullData hullData)
		{
			if (hullData == null)
				return null;

			return FindBone(targetSkinnedRenderer, hullData.targetBoneName);
		}

		//private Transform FindBone(string nameToFind)
		//{
		//	return FindBone(targetSkinnedRenderer, nameToFind);
		//}

		public static Transform FindBone(SkinnedMeshRenderer skinnedRenderer, string nameToFind)
		{
			if (skinnedRenderer == null)
				return null;
			if (nameToFind == null)
				return null;

			foreach (Transform bone in skinnedRenderer.bones)
			{
				if (bone.name == nameToFind)
					return bone;
			}
			return null;
		}
	}
}
