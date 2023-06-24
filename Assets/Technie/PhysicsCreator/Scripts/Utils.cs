using UnityEngine;

using System.Collections;
using System.Collections.Generic;

namespace Technie.PhysicsCreator
{
	public static class Utils
	{
		/** Creates a Matrix4x4 which represents the actual transform, taking into account all of it's parents
		 *
		 *  Roughly equivilent to Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale),
		 *  except this version will correctly handle skews introduced by weird hierarchys with non-uniform scales and rotations
		 */
		public static Matrix4x4 CreateSkewableTRS(Transform target)
		{
			// Recurse upwards through the transform hierarchy, and combine the individual matrices (from root to leaf) into the final result

			if (target.parent == null)
				return Matrix4x4.TRS(target.localPosition, target.localRotation, target.localScale);

			Matrix4x4 lower = CreateSkewableTRS(target.parent);
			Matrix4x4 self = Matrix4x4.TRS(target.localPosition, target.localRotation, target.localScale);
			return lower * self;
		}

		public static void Inflate(Vector3 point, ref Vector3 min, ref Vector3 max)
		{
			min.x = Mathf.Min(min.x, point.x);
			min.y = Mathf.Min(min.y, point.y);
			min.z = Mathf.Min(min.z, point.z);
			
			max.x = Mathf.Max(max.x, point.x);
			max.y = Mathf.Max(max.y, point.y);
			max.z = Mathf.Max(max.z, point.z);
		}

		public static Plane[] ConvertToPlanes(Mesh convexMesh, bool show)
		{
			List<Plane> planes = new List<Plane>();

			Vector3[] vertices = convexMesh.vertices;
			int[] indices = convexMesh.triangles;

			for (int i = 0; i < indices.Length; i += 3)
			{
				Vector3 p0 = vertices[indices[i]];
				Vector3 p1 = vertices[indices[i + 1]];
				Vector3 p2 = vertices[indices[i + 2]];

				Vector3 e0 = (p1 - p0).normalized;
				Vector3 e1 = (p2 - p0).normalized;

				Vector3 normal = Vector3.Cross(e0, e1).normalized;

				if (normal.magnitude > 0.01f)
				{
					Plane plane = new Plane(normal, p0);
					if (!Contains(plane, planes))
					{
						planes.Add(plane);

						if (show)
						{
							GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
							obj.name = string.Format("{0} : {1} / {2} / {3}", i, indices[i], indices[i + 1], indices[i + 2]);
							obj.transform.SetPositionAndRotation(p0, Quaternion.LookRotation(normal));
						}
					}
				}
			}

			return planes.ToArray();
		}

		public static bool Contains(Plane toTest, List<Plane> planes)
		{
			foreach (Plane existing in planes)
			{
				if (Mathf.Abs(toTest.distance - existing.distance) < 0.01f
					&& Vector3.Angle(toTest.normal, existing.normal) < 0.01f)
				{
					return true;
				}
			}
			return false;
		}

		public static Mesh Clip(Mesh boundingMesh, Mesh inputMesh)
		{
			if (boundingMesh == null || boundingMesh.triangles.Length == 0)
				return null;
			if (inputMesh == null || inputMesh.triangles.Length == 0)
				return null;

			CuttableMesh inProgress = new CuttableMesh(inputMesh);

			MeshCutter cutter = new MeshCutter();

			// Convert the bounding mesh to planes that we'll clip against
			Plane[] boundingPlanes = Utils.ConvertToPlanes(boundingMesh, false);

			// Clip the input mesh against the planes one at a time
			// Note that after each clip we must recreate the convex hull to fill the hole created in the cut plane
			// If we don't do that then further clipping can give inconsistant results based on the triangulation

			for (int i = 0; i < boundingPlanes.Length; i++)
			{
				Plane plane = boundingPlanes[i];

				cutter.Cut(inProgress, plane);

				Mesh cutOutput = cutter.GetBackOutput().CreateMesh();
				Mesh newHull = QHullUtil.FindConvexHull("", cutOutput, false);
				inProgress = new CuttableMesh(newHull);
			}

			Mesh resultMesh = inProgress.CreateMesh();
			if (resultMesh.triangles.Length > 0)
			{
				return resultMesh;
			}
			else
				return null;
		}

		public static float CalcTriangleArea(Vector3 p0, Vector3 p1, Vector3 p2)
		{
			Vector3 d0 = p1 - p0;
			Vector3 d1 = p2 - p0;

			float area = 0.5f * Vector3.Cross(d0, d1).magnitude;
			return area;
		}

		public static float TimeProgression(float elapsedTime, float maxTime)
		{
			float normalizedTime = elapsedTime / maxTime;

			float result = -((-normalizedTime) / (normalizedTime + (1.0f / 2.0f))); // 1/4 - reaches 80% of maxTime in maxTime seconds
																					// 1/2 - reaches 80% of maxTime in maxTime*2 seconds
			return result;
		}

		public static float AsymtopicProgression(float inputProgress, float maxProgression, float rate)
		{
			/* Asymtopic progression curve
			 *               b(-x)
			 * f(x) =  -1 * ------- 
			 *              (x + a)
			 * 
			 * Where:
			 *	x - input progress (time)
			 *	b - value to approach (but never reach)
			 *	a - rate of approach (higher values quickly reach near to max value before slowing down, lower values are smoother
			 *	
			 */
			float result = -((maxProgression * (-inputProgress)) / (inputProgress + rate));
			return result;
		}

		public static int FindBoneIndex(SkinnedMeshRenderer skinnedRenderer, Transform bone)
		{
			Transform[] bones = skinnedRenderer.bones;
			for (int i = 0; i < bones.Length; i++)
			{
				if (bones[i] == bone)
				{
					return i;
				}
			}
			return -1;
		}

		public static bool IsWeightAboveThreshold(BoneWeight weights, int ownBoneIndex, float minThreshold, float maxThreshold)
		{
			return IsWeightAboveThreshold(weights.boneIndex0, weights.weight0, ownBoneIndex, minThreshold, maxThreshold)
				|| IsWeightAboveThreshold(weights.boneIndex1, weights.weight1, ownBoneIndex, minThreshold, maxThreshold)
				|| IsWeightAboveThreshold(weights.boneIndex2, weights.weight2, ownBoneIndex, minThreshold, maxThreshold)
				|| IsWeightAboveThreshold(weights.boneIndex3, weights.weight3, ownBoneIndex, minThreshold, maxThreshold);
		}

		public static bool IsWeightAboveThreshold(int boneIndex, float boneWeight, int ourIndex, float minThreshold, float maxThreshold)
		{
			return boneIndex == ourIndex && boneWeight >= minThreshold && boneWeight <= maxThreshold;
		}

		public static int NumVerticesForBone(UnpackedMesh mesh, Transform bone, float minThreshold, float maxThreshold)
		{
			int count = 0;

			int boneIndex = Utils.FindBoneIndex(mesh.SkinnedRenderer, bone);
			for (int i = 0; i < mesh.NumVertices; i++)
			{
				BoneWeight w = mesh.BoneWeights[i];
				if (Utils.IsWeightAboveThreshold(w, boneIndex, minThreshold, maxThreshold))
				{
					count++;
				}
			}
			return count;
		}

		public static void UpdateCachedVertices(IHull hull, Mesh srcMesh)
		{
			Vector3[] vertices = srcMesh.vertices;
			int[] triangleIndices = srcMesh.triangles;

			List<Vector3> newCachedVertices = new List<Vector3>();

			int[] selectedFaces = hull.GetSelectedFaces();

			for (int i = 0; i < hull.NumSelectedTriangles; i++)
			{
				int faceIndex = selectedFaces[i];

				int i0 = (faceIndex * 3);
				int i1 = (faceIndex * 3) + 1;
				int i2 = (faceIndex * 3) + 2;

				Vector3 v0 = vertices[triangleIndices[i0]];
				Vector3 v1 = vertices[triangleIndices[i1]];
				Vector3 v2 = vertices[triangleIndices[i2]];

				newCachedVertices.Add(v0);
				newCachedVertices.Add(v1);
				newCachedVertices.Add(v2);
			}

			hull.CachedTriangleVertices = newCachedVertices.ToArray();

			//Console.output.Log(string.Format("Hull '{0}' Cached {1} triangles ({2} vertices)", this.name, newCachedVertices.Count / 3, newCachedVertices.Count));
		}

	} // Utils

} // namespace Techie.PhysicsCreator
