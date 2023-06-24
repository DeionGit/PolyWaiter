using System.Collections.Generic;
using UnityEngine;

using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator
{
	public class RigidSceneOverlay : ISceneOverlay
	{
		private const string PREVIEW_ROOT_NAME = "OVERLAY ROOT (Rigid Collider Creator)";

		private RigidColliderCreatorWindow parentWindow; // We only really need this for access to some global settings, so maybe need a Settings class later

		// Hidden scene object for all overlay objects
		private GameObject overlayRoot;

		// Hull overlay drawn via a single mesh with vertex colours
		private GameObject overlayObject;
		private MeshFilter overlayFilter;
		private MeshRenderer overlayRenderer;
		private Material overlayMaterial;

		// Skew-tolerant matrix for local->world for the selected mesh
		private Matrix4x4 localToWorld;

		public RigidSceneOverlay(RigidColliderCreatorWindow parentWindow)
		{
			this.parentWindow = parentWindow;
		}

		public void Destroy()
		{
			if (overlayObject != null)
			{
				GameObject.DestroyImmediate(overlayObject);
				overlayObject = null;
			}

			if (overlayRoot != null)
			{
				GameObject.DestroyImmediate(overlayRoot);
				overlayRoot = null;
			}
		}

		public void Disable()
		{
			if (overlayObject != null)
			{
				overlayRenderer.enabled = false;
			}
		}

		public void FindOrCreateOverlay()
		{
			if (overlayObject == null)
			{
				overlayObject = GameObject.Find(PREVIEW_ROOT_NAME);
			}

			if (overlayObject != null)
			{
				//Console.output.Log ("Use existing overlay");

				overlayFilter = overlayObject.GetComponent<MeshFilter>();
				overlayRenderer = overlayObject.GetComponent<MeshRenderer>();
			}
			else
			{
				Console.output.Log ("Create new overlay from scratch");

				overlayObject = new GameObject(PREVIEW_ROOT_NAME);
				overlayObject.transform.localPosition = Vector3.zero;
				overlayObject.transform.localRotation = Quaternion.identity;
				overlayObject.transform.localScale = Vector3.one;

				overlayObject.hideFlags = Console.SHOW_SHADOW_HIERARCHY ? HideFlags.None : HideFlags.HideAndDontSave;
			}

			if (overlayFilter == null)
			{
				overlayFilter = overlayObject.AddComponent<MeshFilter>();
			}

			if (overlayFilter.sharedMesh == null)
			{
				overlayFilter.sharedMesh = new Mesh();
			}

			if (overlayRenderer == null)
			{
				overlayRenderer = overlayObject.AddComponent<MeshRenderer>();
				overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				overlayRenderer.receiveShadows = false;
			}

			if (overlayMaterial == null)
			{
				Console.output.Log("Recreate overlay material");

				string[] assetGuids = UnityEditor.AssetDatabase.FindAssets("Rigid Hull Preview t:Material");
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuids[0]);
				overlayMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Material));
			}

			overlayRenderer.sharedMaterial = overlayMaterial;

			overlayObject.transform.SetParent(overlayRoot.transform, false);
		}

		public void SyncOverlay(ICreatorComponent creatorComponent, Mesh inputMesh)
		{
			RigidColliderCreator hullPainter = creatorComponent as RigidColliderCreator;
			//Console.output.Log ("SyncOverlay - overlayObject: " + overlayObject);

			if (hullPainter != null && hullPainter.paintingData != null)
			{
				int totalFaces = 0;
				for (int i = 0; i < hullPainter.paintingData.hulls.Count; i++)
				{
					Hull hull = hullPainter.paintingData.hulls[i];
					if (hull.isVisible)
					{
						totalFaces += hull.NumSelectedTriangles;
					}
				}

				//Console.output.Log("Overlay has "+totalFaces+" faces");

				Vector3[] vertices = new Vector3[totalFaces * 3];
				Color[] colours = new Color[totalFaces * 3];
				int[] indices = new int[totalFaces * 3];

				Vector3[] targetVertices = inputMesh.vertices;
				int[] targetTriangles = inputMesh.triangles;

				// Rebuild vertex buffers

				int nextIndex = 0;

				for (int i = 0; i < hullPainter.paintingData.hulls.Count; i++)
				{
					Hull hull = hullPainter.paintingData.hulls[i];

					if (!hull.isVisible)
						continue;

					bool shouldDim = parentWindow != null && parentWindow.ShouldDimInactiveHulls();
					float dimFactor = parentWindow != null ? parentWindow.GetInactiveHullDimFactor() : 0.7f;
					float baseAlpha = parentWindow != null ? parentWindow.GetGlobalHullAlpha() : 0.6f;

					Color baseColour = hull.colour;
					baseColour.a = baseAlpha;
					if (shouldDim && hullPainter.paintingData.HasActiveHull())
					{
						if (hullPainter.paintingData.GetActiveHull() != hull)
						{
							baseColour.a = baseAlpha * (1.0f - dimFactor);
						}
					}

					for (int j = 0; j < hull.NumSelectedTriangles; j++)
					{
						int faceIndex = hull.GetSelectedFaceIndex(j);

						int i0 = faceIndex * 3;
						int i1 = faceIndex * 3 + 1;
						int i2 = faceIndex * 3 + 2;

						if (i0 < targetTriangles.Length
							&& i1 < targetTriangles.Length
							&& i2 < targetTriangles.Length)
						{
							int t0 = targetTriangles[i0];
							int t1 = targetTriangles[i1];
							int t2 = targetTriangles[i2];

							if (t0 < targetVertices.Length && t1 < targetVertices.Length && t2 < targetVertices.Length)
							{
								Vector3 p0 = targetVertices[t0];
								Vector3 p1 = targetVertices[t1];
								Vector3 p2 = targetVertices[t2];

								// Convert from model local space to world space
								p0 = localToWorld.MultiplyPoint(p0);
								p1 = localToWorld.MultiplyPoint(p1);
								p2 = localToWorld.MultiplyPoint(p2);

								colours[nextIndex] = baseColour;
								colours[nextIndex + 1] = baseColour;
								colours[nextIndex + 2] = baseColour;

								vertices[nextIndex] = p0;
								vertices[nextIndex + 1] = p1;
								vertices[nextIndex + 2] = p2;

								nextIndex += 3;
							}
							else
							{
								Console.output.LogWarning(Console.Technie, "Skipping face because vertex index out of bounds");
							}
						}
						else
						{
							Console.output.LogWarning(Console.Technie, "Skipping face because triangle index out of bounds: " + faceIndex);

							// TODO: Should we advance nextIndex or not?
							// Maybe we should have a validate step rather than trying to solve this problem now?
						}
					}
				}

				// Generate the indices
				for (int i = 0; i < indices.Length; i++)
					indices[i] = i;

				Mesh overlayMesh = overlayFilter.sharedMesh;
				if (overlayMesh != null)
				{
					overlayMesh.triangles = new int[0];
					overlayMesh.vertices = vertices;
					overlayMesh.colors = colours;
					overlayMesh.triangles = indices;
				}
				else
				{
					Console.output.LogError(Console.Technie, "Couldn't update overlay mesh!");
				}

				overlayFilter.sharedMesh = overlayMesh;

				overlayRenderer.enabled = true;
			}
			else
			{
				// No hull painter selected, clear everything

				overlayFilter.sharedMesh.Clear();
				overlayRenderer.enabled = false;
			}
		}

		public void SyncParentChain(GameObject srcLeafObj)
		{
			localToWorld = Utils.CreateSkewableTRS(srcLeafObj.transform);

			if (overlayRoot == null)
			{
				overlayRoot = GameObject.Find(PREVIEW_ROOT_NAME);
			}

			if (overlayRoot == null)
			{
				// Not found, create a new one from scratch
				overlayRoot = new GameObject(PREVIEW_ROOT_NAME);

				overlayRoot.hideFlags = Console.SHOW_SHADOW_HIERARCHY ? HideFlags.None : HideFlags.HideAndDontSave;
			}

			// Ensure the overaly is in the same scene as the object we're manipulating
			// (this also ensures the overlay is moved into the prefab editing scene when that is used)
			if (overlayRoot.scene != srcLeafObj.scene)
			{
				UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(overlayRoot, srcLeafObj.scene);
			}
		}
	}

} // namespace Technie.PhysicsCreator