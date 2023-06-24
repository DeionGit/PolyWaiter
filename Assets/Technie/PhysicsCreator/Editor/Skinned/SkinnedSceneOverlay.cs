using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using Technie.PhysicsCreator;

namespace Technie.PhysicsCreator.Skinned
{
	public enum CoordSpace
	{
		World,
		Model,
		Bone
	}

	public class SkinnedSceneOverlay : ISceneOverlay
	{
		private const string PREVIEW_ROOT_NAME = "OVERLAY ROOT (Skinned Collider Creator)";

		private SkinnedColliderCreatorWindow parentWindow;

		private Material baseMaterial;

		private GameObject previewRoot;

		private List<MeshRenderer> previewMeshes = new List<MeshRenderer>();
		private List<Material> previewMaterials = new List<Material>();
		private Dictionary<BoneHullData, Material> previewHullMapping = new Dictionary<BoneHullData, Material>();

		private MeshCache cache = new MeshCache();

		private int lastOverlayFrame = -1;

		public SkinnedSceneOverlay(SkinnedColliderCreatorWindow parentWindow)
		{
			this.parentWindow = parentWindow;

#if UNITY_2017_4_OR_NEWER
			EditorApplication.playModeStateChanged += LogPlayModeState;
#endif
		}
		
#if UNITY_2017_4_OR_NEWER
		private static void LogPlayModeState(PlayModeStateChange state)
		{
			Console.output.Log("PlayMode State now:"+state+"    |    isPlayingOrWillChange: "+ EditorApplication.isPlayingOrWillChangePlaymode);
		}
#endif

		public void Destroy()
		{
			if (previewRoot != null)
			{
				GameObject.DestroyImmediate(previewRoot);
				previewRoot = null;
			}
			previewMeshes.Clear();
			previewMaterials.Clear();
			previewHullMapping.Clear();

			lastOverlayFrame = -1;
		}

		public void Disable()
		{
			if (previewRoot != null)
			{
				previewRoot.gameObject.SetActive(false);
			}
		}

		public void FindOrCreateOverlay()
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
			
			if (baseMaterial == null)
			{
				string[] assetGuids = UnityEditor.AssetDatabase.FindAssets("Skinned Hull Preview t:Material");
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuids[0]);
				baseMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Material));
			}
		}

		public void SyncParentChain(GameObject srcLeafObj)
		{

		}

		public void SyncOverlay(ICreatorComponent currentComponent, Mesh inputMesh)
		{
			SyncMeshes(currentComponent, inputMesh);

			SyncHighlight(currentComponent, inputMesh);
		}

		private void SyncMeshes(ICreatorComponent currentComponent, Mesh inputMesh)
		{
			SkinnedColliderCreator targetComponent = currentComponent as SkinnedColliderCreator;

			if (targetComponent != null && targetComponent.editorData != null && targetComponent.editorData.GetLastModifiedFrame() == this.lastOverlayFrame)
			{
				return;
			}
			this.lastOverlayFrame = (targetComponent != null && targetComponent.editorData != null) ? targetComponent.editorData.GetLastModifiedFrame() : 0;

			UnpackedMesh unpackedMesh = UnpackedMesh.Create(targetComponent != null ? targetComponent.targetSkinnedRenderer : null);

			//Console.output.Log("Regenerate overlay");

			foreach (MeshRenderer r in this.previewMeshes)
			{
				if (r != null)
				{
					GameObject.DestroyImmediate(r.gameObject);
				}
			}
			previewMeshes.Clear();

			foreach (Material mat in previewMaterials)
			{
				if (mat != null)
				{
					GameObject.DestroyImmediate(mat);
				}
			}
			previewMaterials.Clear();

			previewHullMapping.Clear();

			for (int i = 0; i < previewRoot.transform.childCount; i++)
			{
				GameObject childObj = previewRoot.transform.GetChild(i).gameObject;

				if (childObj != null)
				{
					GameObject.DestroyImmediate(childObj);
				}
			}

			if (targetComponent != null && targetComponent.editorData != null)
			{
				foreach (BoneHullData hullData in targetComponent.editorData.boneHullData)
				{
					Transform bone = targetComponent.FindBone(hullData);

					Transform modelSpace = targetComponent.targetSkinnedRenderer.rootBone.parent;

					GameObject obj = new GameObject(bone.name+" overlay");
					obj.transform.parent = previewRoot.transform;
					obj.transform.SetPositionAndRotation(modelSpace.position, modelSpace.rotation);
					obj.transform.localScale = modelSpace.lossyScale;

					MeshFilter filter = obj.AddComponent<MeshFilter>();
					MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

					Material material = new Material(baseMaterial);
					material.name = hullData.targetBoneName;
					material.color = hullData.previewColour;

					filter.sharedMesh = FindOrCreateOverlayMesh(unpackedMesh, targetComponent.targetSkinnedRenderer, bone, hullData);

					renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
					renderer.receiveShadows = false;
					renderer.sharedMaterial = material;

					previewMeshes.Add(renderer);
					previewMaterials.Add(material);
					previewHullMapping.Add(hullData, material);
				}
			}

			previewRoot.SetActive(true);
		}

		private void SyncHighlight(ICreatorComponent currentComponent, Mesh inputMesh)
		{
			SkinnedColliderCreator targetComponent = currentComponent as SkinnedColliderCreator;
			if (targetComponent == null || targetComponent.editorData == null)
				return;

			//Console.output.Log(string.Format("SkinnedSceneOverlay.SyncHighlight with {0} entries and first is {1}", previewMaterials.Count, (previewMaterials.Count > 0 ? previewMaterials[0].ToString() : "none")));

			BoneHullData selectedHull = targetComponent.editorData.GetSelectedHull();

			float baseAlpha = parentWindow != null ? parentWindow.GetGlobalHullAlpha() : 0.5f;
			bool shouldDim = parentWindow != null && parentWindow.ShouldDimInactiveHulls();
			float dimFactor = parentWindow != null ? parentWindow.GetInactiveHullDimFactor() : 0.7f;

			foreach (KeyValuePair<BoneHullData, Material> entry in previewHullMapping)
			{
				if (entry.Key == null || entry.Value == null)
					continue;

				if (shouldDim && selectedHull != null)
				{
					if (entry.Key == selectedHull)
					{
						SetMaterialAlpha(entry.Value, baseAlpha);
					}
					else
					{
						SetMaterialAlpha(entry.Value, baseAlpha * (1.0f - dimFactor));
					}
				}
				else
				{
					SetMaterialAlpha(entry.Value, baseAlpha);
				}
			}
		}

		private static void SetMaterialAlpha(Material mat, float alpha)
		{
			Color col = mat.color;
			col.a = alpha;
			mat.color = col;
		}

		private Mesh FindOrCreateOverlayMesh(UnpackedMesh unpackedMesh, SkinnedMeshRenderer skinnedRenderer, Transform bone, BoneHullData hull)
		{
			Hash160 hash = cache.CalcHash(skinnedRenderer, bone, hull);
			Mesh result;
			if (cache.FindExisting(hash, out result))
			{
				return result;
			}
			else
			{
				Mesh mesh = CreateOverlayMesh(skinnedRenderer, unpackedMesh, bone, hull);
				cache.Add(hash, mesh);
				return mesh;
			}
		}

		public static Mesh CreateOverlayMesh(SkinnedMeshRenderer skinnedRenderer, UnpackedMesh unpackedMesh, Transform bone, BoneHullData hull)
		{
			if (hull.type == HullType.Auto)
			{
				/*
				if (false)
				{
					// Overlay mesh from approximation of what faces are within the min/max threshold
					int[] selectedFaces = FindAutoFaces(skinnedRenderer, bone, hull);
					return CreateMeshFromFaces(selectedFaces, skinnedRenderer);
				}
				else
				*/
				{
					// Overlay mesh by calculating the convex hull of thresholded vertices
					return SkinnedColliderCreatorWindow.CreateHullMesh(skinnedRenderer, unpackedMesh, bone, hull, CoordSpace.Model);
				}
			}
			else if (hull.type == HullType.Manual)
			{
				// Overlay mesh from painted faces
				int[] selectedFaces = hull.GetSelectedFaces();
				return CreateMeshFromFaces(selectedFaces, skinnedRenderer, unpackedMesh);
			}

			return null;
		}


		private static Mesh CreateMeshFromFaces(int[] selectedFaces, SkinnedMeshRenderer skinnedRenderer, UnpackedMesh unpackedMesh)
		{
			Vector3[] vertices = unpackedMesh.ModelSpaceVertices;
			int[] indices = unpackedMesh.Indices;

			List<int> overlayIndices = new List<int>();
			foreach (int face in selectedFaces)
			{
				int baseIndex = face * 3;
				overlayIndices.Add(indices[baseIndex]);
				overlayIndices.Add(indices[baseIndex + 1]);
				overlayIndices.Add(indices[baseIndex + 2]);
			}

			Mesh overlayMesh = new Mesh();
			overlayMesh.vertices = vertices;
			overlayMesh.triangles = overlayIndices.ToArray();
			overlayMesh.RecalculateNormals();

			return overlayMesh;
		}

		// This is less than ideal because the min/max weight thresholds operate on vertices, but here we're converting that into a face
		// so we can show the face as a preview.
		// Ideally we'd operate on the vertices and re-triangulate into a more accurate form for overlay display
		// Maybe interpolating the vertex edges and adding mid points?
		// Alternately, run QHull whenever the weights change and cache the result, then use that as the actual display overlay
		private static int[] FindAutoFaces(SkinnedMeshRenderer skinnedRenderer, Transform bone, BoneHullData hull)
		{
			List<int> selectedFaces = new List<int>();

			Mesh targetMesh = skinnedRenderer.sharedMesh; // TODO: What about sub-meshes?
			//Vector3[] vertices = targetMesh.vertices;
			BoneWeight[] weights = targetMesh.boneWeights;
			//Transform[] bones = skinnedRenderer.bones;
			int[] indices = targetMesh.triangles;

			int ownBoneIndex = Utils.FindBoneIndex(skinnedRenderer, bone);

			int numFaces = indices.Length / 3;
			for (int i=0; i<numFaces; i++)
			{
				int baseIndex = i * 3;
				
				BoneWeight w0 = weights[indices[baseIndex]];
				BoneWeight w1 = weights[indices[baseIndex+1]];
				BoneWeight w2 = weights[indices[baseIndex+2]];

				if (Utils.IsWeightAboveThreshold(w0, ownBoneIndex, hull.MinThreshold, hull.MaxThreshold)
					&& Utils.IsWeightAboveThreshold(w1, ownBoneIndex, hull.MinThreshold, hull.MaxThreshold)
					&& Utils.IsWeightAboveThreshold(w2, ownBoneIndex, hull.MinThreshold, hull.MaxThreshold))
				{
					selectedFaces.Add(i) ;
				}
			}

			return selectedFaces.ToArray();
		}




	}
	
} // namespace Technie.PhysicsCreator.Skinned
