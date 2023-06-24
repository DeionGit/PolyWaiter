using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
/*
namespace Technie.PhysicsCreator.Skinned
{
	public class HullScenePreview
	{
		private const string PREVIEW_ROOT_NAME = "OVERLAY ROOT (Skinned Collider Creator)";

		private Material baseMaterial;

		private GameObject previewRoot;

		private List<MeshRenderer> previewMeshes = new List<MeshRenderer>();
		private List<Material> previewMaterials = new List<Material>();

		public HullScenePreview()
		{

		}

		public void Destroy()
		{
			if (previewRoot != null)
			{
				GameObject.DestroyImmediate(previewRoot);
				previewRoot = null;
			}
		}

		public void Enable(SkinnedColliderCreator targetComponent)
		{
			FindOrCreateRoot();

			if (baseMaterial == null)
			{
				string[] assetGuids = UnityEditor.AssetDatabase.FindAssets("Hull Preview t:Material");
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuids[0]);
				baseMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Material));
			}

			UpdatePreviewMeshes(targetComponent);
		}

		public void OnTargetChanged(SkinnedColliderCreator targetComponent)
		{
			UpdatePreviewMeshes(targetComponent);
		}

		public void UpdatePreviewMeshes(SkinnedColliderCreator targetComponent)
		{
			FindOrCreateRoot();

			foreach (MeshRenderer r in this.previewMeshes)
			{
				if (r != null)
					GameObject.DestroyImmediate(r.gameObject);
			}
			previewMeshes.Clear();

			foreach (Material mat in previewMaterials)
			{
				GameObject.DestroyImmediate(mat);
			}
			previewMaterials.Clear();

			for (int i = 0; i < previewRoot.transform.childCount; i++)
			{
				GameObject childObj = previewRoot.transform.GetChild(i).gameObject;
				GameObject.DestroyImmediate(childObj);
			}

			if (targetComponent != null && targetComponent.editorData != null)
			{
				foreach (BoneHullData hullData in targetComponent.editorData.boneHullData)
				{
					Transform bone = targetComponent.FindBone(hullData);
					MeshCollider[] cols = bone.GetComponents<MeshCollider>();
					foreach (MeshCollider col in cols)
					{
						if (col.sharedMesh == hullData.hullMesh)
						{
							GameObject obj = new GameObject(bone.name);
							obj.transform.parent = previewRoot.transform;
							obj.transform.SetPositionAndRotation(col.transform.position, col.transform.rotation);

							MeshFilter filter = obj.AddComponent<MeshFilter>();
							MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

							Material material = new Material(baseMaterial);
							material.color = hullData.previewColour;

							filter.sharedMesh = col.sharedMesh;

							renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
							renderer.receiveShadows = false;
							renderer.sharedMaterial = material;

							previewMeshes.Add(renderer);
							previewMaterials.Add(material);
						}
					}
				}
			}
		}

		private void FindOrCreateRoot()
		{
			// If we don't have a valid reference, try and find an existing one in the scene
			if (previewRoot == null)
			{
				previewRoot = GameObject.Find(PREVIEW_ROOT_NAME);
			}
			// If we still don't have one, create a new root and hide it from the user
			if (previewRoot == null)
			{
				previewRoot = new GameObject(PREVIEW_ROOT_NAME);

				previewRoot.hideFlags = Console.SHOW_SHADOW_HIERARCHY ? HideFlags.None : HideFlags.HideAndDontSave;
			}
		}

		public void OnSceneGUI(SceneView sceneView, SkinnedColliderCreator target, out BoneHullData newHullSelection, out BoneHullData newHoverHullSelection)
		{
			newHullSelection = null;
			newHoverHullSelection = null;

			Ray pickRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			float closestHit = float.MaxValue;
			BoneHullData closestData = null;
			MeshRenderer closestRenderer = null;

			// Scan each of the preview meshes (MeshRenderer+MeshFilter) and try and find the associated BoneHullData by matching up the hull meshes
			foreach (MeshRenderer r in previewMeshes)
			{
				MeshFilter filter = r.GetComponent<MeshFilter>();

				BoneHullData[] hulls = target.editorData.GetBoneHullData(r.transform.name);
				foreach (BoneHullData hull in hulls)
				{
					if (hull.hullMesh == filter.sharedMesh)
					{
						MeshCollider col = FindColliderForPreview(target, r, hull);

						// Always reset this back to the default colour
						r.sharedMaterial.color = hull.previewColour;

						if (col != null && col.Raycast(pickRay, out RaycastHit hit, 1000.0f))
						{
							if (hit.distance < closestHit)
							{
								closestHit = hit.distance;
								closestData = hull;
								closestRenderer = r;
								newHoverHullSelection = hull;
							}
						}
					}
				}
			}

			if (closestRenderer != null)
			{
				closestRenderer.sharedMaterial.color = Color.white;
			}

			int controlId = GUIUtility.GetControlID(FocusType.Passive);

			if (Event.current.type == EventType.MouseDown)
			{
				if (closestRenderer != null)
				{
					newHullSelection = closestData;
					GUIUtility.hotControl = controlId;
					Event.current.Use();
				}
			}
		}


		private MeshCollider FindColliderForPreview(SkinnedColliderCreator target, MeshRenderer renderer, BoneHullData hull)
		{
			// FIXME: Ugh, need a proper explicit mapping for this

			foreach (MeshCollider col in target.targetSkinnedRenderer.rootBone.GetComponentsInChildren<MeshCollider>())
			{
				//if (col.sharedMesh == renderer.GetComponent<MeshFilter>().sharedMesh)
				if (col.sharedMesh == hull.hullMesh)
				{
					return col;
				}
			}
			return null;
		}

	}
}
*/
