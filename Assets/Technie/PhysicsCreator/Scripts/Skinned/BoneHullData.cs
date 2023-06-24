using UnityEngine;
using System.Collections.Generic;

namespace Technie.PhysicsCreator.Skinned
{
	[System.Serializable]
	public class BoneHullData : IHull
	{
		public string Name
		{
			get { return targetBoneName; }
		}

		public float MinThreshold { get { return this.minThreshold; } }
		public float MaxThreshold { get { return this.maxThreshold; } }

		public int NumSelectedTriangles
		{
			get { return selectedFaces.Count; }
		}

		public Vector3[] CachedTriangleVertices
		{
			get { return cachedTriangleVertices.ToArray(); }
			set
			{
				cachedTriangleVertices.Clear();
				cachedTriangleVertices.AddRange(value);
			}
		}

		public string targetBoneName;
		public HullType type = HullType.Auto;
		public ColliderType colliderType = ColliderType.Convex;

		// Common properties
		public Color previewColour;
		public Mesh hullMesh; // the generated convex hull
		public PhysicMaterial material;
		public bool isTrigger;
		public int maxPlanes = 255;

		// Auto properties
		[SerializeField]
		private float minThreshold;

		[SerializeField]
		private float maxThreshold;

		// Manual properties
		[SerializeField]
		private List<int> selectedFaces = new List<int>();  // selected triangle indices
		public List<Vector3> cachedTriangleVertices = new List<Vector3>();  // TODO Implement this

		// Cache of the faces indices for triangles that are fully between the min/max thresholds
		//private List<int> thresholdSelectedFaces = new List<int>();


		// TODO: responding to IsTriangleSelected properly for Auto hulls is tricky and means we have to pass awkward extra params (Renderer+Mesh) that only Auto cares about
		// Ideally:
		//		Move TryPipetteSelection from Controller to Overlay, and do a general raycast to find which hull we hit (more consistent with threshold/weighting behaviour plus it'll be better when we
		//		eventually interpolate weights for finer control in Auto hulls)
		//
		//		Rename to something that's clear it only works for Manual hulls (IsTrianglePainted?)
		//
		public bool IsTriangleSelected(int triIndex, Renderer renderer, Mesh targetMesh)
		{
			if (type == HullType.Manual)
			{
				return selectedFaces.Contains(triIndex);
			}
			else if (type == HullType.Auto)
			{
				
				SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;
				BoneWeight[] weights = targetMesh.boneWeights;
				int[] triangles = targetMesh.triangles;

				int i0 = triangles[triIndex * 3];
				int i1 = triangles[triIndex * 3 + 1];
				int i2 = triangles[triIndex * 3 + 2];

				BoneWeight w0 = weights[i0];
				BoneWeight w1 = weights[i1];
				BoneWeight w2 = weights[i2];

				Transform bone = SkinnedColliderCreator.FindBone(skinnedRenderer, targetBoneName);
				int ownBoneIndex = Utils.FindBoneIndex(skinnedRenderer, bone);
				if (Utils.IsWeightAboveThreshold(w0, ownBoneIndex, minThreshold, maxThreshold)
					&& Utils.IsWeightAboveThreshold(w1, ownBoneIndex, minThreshold, maxThreshold)
					&& Utils.IsWeightAboveThreshold(w2, ownBoneIndex, minThreshold, maxThreshold))
				{
					return true;
				}
				
				//return thresholdSelectedFaces.Contains(triIndex);
			}
			return false;	
		}

		public int[] GetSelectedFaces()
		{
			return selectedFaces.ToArray();
		}

		public void AddToSelection(int newTriangleIndex, Mesh srcMesh)
		{
			if (selectedFaces.Contains(newTriangleIndex))
				return;

			this.selectedFaces.Add(newTriangleIndex);

			Utils.UpdateCachedVertices(this, srcMesh);
		}

		public void RemoveFromSelection(int existingTriangleIndex, Mesh srcMesh)
		{
			this.selectedFaces.Remove(existingTriangleIndex);

			Utils.UpdateCachedVertices(this, srcMesh);
		}

		public void SetMinThreshold(float newMinThreshold)
		{
			this.minThreshold = newMinThreshold;
		}

		public void SetMaxThreshold(float newMaxThreshold)
		{
			this.maxThreshold = newMaxThreshold;
		}
		
		public void SetThresholds(float newMinThreshold, float newMaxThreshold, SkinnedMeshRenderer renderer, Mesh targetMesh)
		{
			this.minThreshold = newMinThreshold;
			this.maxThreshold = newMaxThreshold;

			// TODO: Regen selected face indices
			// ..

			/*
			SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;
			BoneWeight[] weights = targetMesh.boneWeights;
			int[] triangles = targetMesh.triangles;
			int numTris = triangles.Length / 3;

			thresholdSelectedFaces.Clear();

			for (int i = 0; i< numTris; i++)
			{
				int triIndex = i;
				int i0 = triangles[triIndex * 3];
				int i1 = triangles[triIndex * 3 + 1];
				int i2 = triangles[triIndex * 3 + 2];

				BoneWeight w0 = weights[i0];
				BoneWeight w1 = weights[i1];
				BoneWeight w2 = weights[i2];

				Transform bone = SkinnedColliderCreator.FindBone(skinnedRenderer, targetBoneName);
				int ownBoneIndex = Utils.FindBoneIndex(skinnedRenderer, bone);
				if (Utils.IsWeightAboveThreshold(w0, ownBoneIndex, minThreshold, maxThreshold)
					&& Utils.IsWeightAboveThreshold(w1, ownBoneIndex, minThreshold, maxThreshold)
					&& Utils.IsWeightAboveThreshold(w2, ownBoneIndex, minThreshold, maxThreshold))
				{
					thresholdSelectedFaces.Add(triIndex);
				}
			}
			*/
		}

		public void ClearSelectedFaces()
		{
			if (type == HullType.Manual)
			{
				selectedFaces.Clear();
				cachedTriangleVertices.Clear();
			}
		}

		public void SetSelectedFaces(List<int> newSelectedFaceIndices, Mesh srcMesh)
		{
			if (type == HullType.Manual)
			{
				selectedFaces.Clear();
				selectedFaces.AddRange(newSelectedFaceIndices);

				Utils.UpdateCachedVertices(this, srcMesh);
			}
		}

		public Vector3[] GetCachedTriangleVertices()
		{
			return cachedTriangleVertices.ToArray();
		}
	}

} // namespace Technie.PhysicsCreator.Skinned
