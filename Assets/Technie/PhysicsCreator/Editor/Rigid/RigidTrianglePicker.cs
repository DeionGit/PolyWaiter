using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Technie.PhysicsCreator
{
	public class RigidTrianglePicker : ITrianglePicker
	{
		//private MeshFilter selectedMeshFilter;

		private Mesh sourceMesh;
		private Vector3[] targetVertices;
		private int[] targetTriangles;

		private Matrix4x4 modelToWorld;

		public RigidTrianglePicker()
		{

		}

		public void Destroy()
		{

		}

		public void Disable()
		{

		}

		public void FindOrCreatePickClone()
		{

		}

		public bool HasValidTarget()
		{
			//return selectedMeshFilter != null;
			return sourceMesh != null;
		}

		public int NumPickableTris()
		{
			if (sourceMesh == null)
				return 0;

			int numTris = sourceMesh.triangles.Length / 3;
			return numTris;
		}

		/** Vertices for the whole pickable mesh (in mesh local space)  */
		public Vector3[] GetTargetVertices()
		{
			return targetVertices;
		}

		/** Triangle indices for the whole pickable mesh */
		public int[] GetTargetTriangles()
		{
			return targetTriangles;
		}

		public Mesh GetInputMesh()
		{
			return sourceMesh;
		}

		public void SyncPickClone(Renderer selectedRenderer)
		{
			MeshRenderer meshRenderer = selectedRenderer as MeshRenderer;
			SkinnedMeshRenderer skinnedRenderer = selectedRenderer as SkinnedMeshRenderer;
			if (meshRenderer != null)
			{
				MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
				sourceMesh = filter != null ? filter.sharedMesh : null;
			}
			else
			{
				sourceMesh = skinnedRenderer.sharedMesh;
			}

			if (sourceMesh != null)
			{
				targetVertices = sourceMesh.vertices;
				targetTriangles = sourceMesh.triangles;
			}

			// TODO: Use UnpackedMesh.ModelSpaceTransform to hide this ugly distinction
			if (skinnedRenderer != null && skinnedRenderer.rootBone != null)
			{
				modelToWorld = Utils.CreateSkewableTRS(skinnedRenderer.rootBone.parent);
			}
			else
			{
				modelToWorld = Utils.CreateSkewableTRS(selectedRenderer.transform);
			}
		}

		public bool Raycast(Ray pickRay, UnpackedMesh unpackedMesh, out int hitTriIndex)
		{
			//if (selectedMeshFilter == null)
			if (sourceMesh == null)
			{
				hitTriIndex = -1;
				return false;
			}

			if (unpackedMesh == null)
			{
				Console.output.LogError(Console.Technie, "No unpacked mesh to raycast against");
				hitTriIndex = -1;
				return false;
			}

			//return RaycastUtil.Raycast(localToWorld, targetVertices, targetTriangles, pickRay, out hitTriIndex, 10000.0f);
			return RaycastUtil.Raycast(modelToWorld, unpackedMesh.ModelSpaceVertices, targetTriangles, pickRay, out hitTriIndex, 10000.0f);
		}
	}

} // namespace Technie.PhysicsCreator
