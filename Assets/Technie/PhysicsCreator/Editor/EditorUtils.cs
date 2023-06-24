using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator
{
	
	public static class EditorUtils
	{
		private const float EXPONENTIAL_SCALE = 5.0f;

		public static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
		{
			float inRange = inMax - inMin;
			float normalised = (value - inMin) / inRange;

			float outRange = outMax - outMin;
			float result = (normalised * outRange) + outMin;

			return result;
		}

		// To/From exponential conversion
		// The 'power' factor in ToExponential becomes the inverse in FromExponential
		// eg. x^2 and sqrt(x)
		// Larger power factors create a steeper graph (and therefore provide more weight towards the lower numbers)
		public static float ToExponential(float input)
		{
			return Mathf.Pow(input, 1.0f / EXPONENTIAL_SCALE);
		}

		public static float FromExponential(float input)
		{
			return Mathf.Pow(input, EXPONENTIAL_SCALE);
		}

		public static float RoundToTwoDecimalPlaces(float input)
		{
			float output = Mathf.Round(input * 100.0f) / 100.0f;
			return output;
		}



		public static bool UpgradeStoredData(ICreatorComponent currentRigidCreator)
		{
			if (currentRigidCreator == null || !currentRigidCreator.HasEditorData())
				return false; // nothing to upgrade

			IEditorData editorData = currentRigidCreator.GetEditorData();

			if (editorData.HasCachedData)
			{
				// Hash current src mesh

				Hash160 newHash = HashUtil.CalcHash(editorData.SourceMesh);
				Hash160 oldHash = editorData.CachedHash;

				// Compare new hash with stored hash
				if (newHash != oldHash)
				{
					Console.output.Log("Source mesh - detected modifications (from hash changed)");

					// Remap old data onto new mesh indices

					Mesh srcMesh = editorData.SourceMesh;
					Vector3[] vertices = srcMesh.vertices;
					int[] triangleIndices = srcMesh.triangles;

					int numPrevPaintedTris = 0;
					int numNewPaintedTris = 0;

					foreach (IHull hull in editorData.Hulls)
					{
						Vector3[] cachedVertices = hull.CachedTriangleVertices;
						int prevNumSelectedIndices = hull.NumSelectedTriangles;
						
						List<int> newSelectedIndices = ReprojectFaces2(vertices, triangleIndices, cachedVertices, ref numPrevPaintedTris, ref numNewPaintedTris);

						// Update the selected face indices and re-cache the triangle vertices

						hull.SetSelectedFaces(newSelectedIndices, srcMesh);

						numPrevPaintedTris += prevNumSelectedIndices;
						numNewPaintedTris += newSelectedIndices.Count;
						Console.output.Log(string.Format("Remapped hull '{0}' - previously {1} painted triangles mapped to {2} triangles", hull.Name, prevNumSelectedIndices, newSelectedIndices.Count));
					}

					editorData.CachedHash = newHash;
					editorData.SetAssetDirty();

					// Show a dialog if we're allowed to
					if (!editorData.HasSuppressMeshModificationWarning)
					{
						string msg = string.Format("Mesh data difference detected - this means previous painting data is no longer valid. " +
													"The model file, model import setting, or Unity version has changed.\n\n" +
														"Old data has been projected onto the new mesh, please review painted hulls and update as needed.\n\n" +
														"    {0} previously painted triangles\n" +
														"    {1} remapped triangles",
														numPrevPaintedTris, numNewPaintedTris);

						EditorUtility.DisplayDialog("Technie Collider Creator", msg, "Ok");
					}
				}

				return true;
			}
			else
			{
				// No cached data (upgrading from old data format)
				// Calculate the hash and store it
				// Also cache the selected triangle vertices

				Console.output.Log("Auto upgrade old data");

				Mesh srcMesh = editorData.SourceMesh;

				// Update the hash

				Hash160 newHash = HashUtil.CalcHash(srcMesh);
				editorData.CachedHash = newHash;

				// Update the cached vertices
				
				foreach (IHull hull in editorData.Hulls)
				{
					Utils.UpdateCachedVertices(hull, srcMesh);
				}

				// Mark the painting data as dirty
				editorData.SetAssetDirty();

				return false; // cached data updated, but painted faces unchanged
			}
		}

		// "Smart" algorithm that tries to generate multiple samples on the current triangles, and project them onto the cached skin, and update the painted indices accordingly.
		// Works some of the time - perhaps too smart. Fails to cleanly upgrade the 'chair' and 'mug' models in the example scene.
		private static List<int> ReprojectFaces(Hull hull, Mesh srcMesh, Vector3[] vertices, int[] triangleIndices, Vector3[] cachedVertices, ref int numPrevPaintedTris, ref int numNewPaintedTris)
		{
			List<int> newSelectedIndices = new List<int>();

			// Check all of the new triangles
			for (int i = 0; i < triangleIndices.Length; i += 3)
			{
				Vector3 newV0 = vertices[triangleIndices[i]];
				Vector3 newV1 = vertices[triangleIndices[i + 1]];
				Vector3 newV2 = vertices[triangleIndices[i + 2]];

				// TODO: Generate multiple sample points per new triangle here
				Vector3 center = (newV0 + newV1 + newV2) / 3.0f;
				Vector3 normal = Vector3.Cross((newV2 - newV0).normalized, (newV1 - newV0).normalized);

				// Back-project samples onto cached triangles
				if (IsSampleOnSkin(center, normal, cachedVertices))
				{
					// Keep triangle index (and it's cached vertex positions) if it's back-project has a hit
					newSelectedIndices.Add(i / 3); // divide by 3 to get triangle index (not vertex index)
				}
			}

			return newSelectedIndices;	
		}

		// Simpler algorithm that looks for 'exact' triangle matches between the cached and new vertices
		// Won't catch topography changes, but should be more robust when the model changes due to re-indexing, unity version or mesh optimisation
		// 'exact' matching allows for vertices to be specified in a different order (eg. 0-1-2 or 1-2-0) but only with the same winding direction
		private static List<int> ReprojectFaces2(Vector3[] vertices, int[] triangleIndices, Vector3[] cachedVertices, ref int numPrevPaintedTris, ref int numNewPaintedTris)
		{
			// Optimisation - if no cached vertices (eg. if an Auto skinned hull) then just bail
			if (cachedVertices.Length == 0)
				return new List<int>();

			List<int> newSelectedIndices = new List<int>();
			
			// Check all of the new triangles
			for (int i = 0; i < triangleIndices.Length; i += 3)
			{
				Vector3 newV0 = vertices[triangleIndices[i]];
				Vector3 newV1 = vertices[triangleIndices[i + 1]];
				Vector3 newV2 = vertices[triangleIndices[i + 2]];

				// Does this triangle exist in the cached triangle list for this hull?
				if (ContainsTriangle(newV0, newV1, newV2, cachedVertices))
				{
					// Keep triangle index (and it's cached vertex positions)
					newSelectedIndices.Add(i / 3); // divide by 3 to get triangle index (not vertex index)
				}
			}

			return newSelectedIndices;
		}

		private static bool IsSampleOnSkin(Vector3 center, Vector3 normal, Vector3[] skinVertices)
		{
			float rayLength = 0.05f; // TODO: How do we tune this?
			Ray ray = new Ray(center - normal * (rayLength / 2.0f), normal);
			
			bool hit = false;

			for (int i=0; i<skinVertices.Length; i+=3)
			{
				Vector3 v0 = skinVertices[i];
				Vector3 v1 = skinVertices[i + 1];
				Vector3 v2 = skinVertices[i + 2];

				float intersectDist;
				if (RaycastUtil.Intersect(v0, v1, v2, ray, false, false, out intersectDist))
				{
					hit = true;
					break;
				}
			}
			
			return hit;
		}

		private static bool ContainsTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3[] skinVertices)
		{
			for (int i = 0; i < skinVertices.Length; i += 3)
			{
				Vector3 s0 = skinVertices[i];
				Vector3 s1 = skinVertices[i + 1];
				Vector3 s2 = skinVertices[i + 2];

				if (IsMatchedTriangle(v0, v1, v2, s0, s1, s2))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsMatchedTriangle(Vector3 a0, Vector3 a1, Vector3 a2, Vector3 b0, Vector3 b1, Vector3 b2)
		{
			return IsEqualTriangle(a0, a1, a2, b0, b1, b2)
				|| IsEqualTriangle(a0, a1, a2, b1, b2, b0)
				|| IsEqualTriangle(a0, a1, a2, b2, b0, b1);
		}

		private static bool IsEqualTriangle(Vector3 a0, Vector3 a1, Vector3 a2, Vector3 b0, Vector3 b1, Vector3 b2)
		{
			float epsilon = 0.001f;
			return EpsilonEquals(a0, b0, epsilon) && EpsilonEquals(a1, b1, epsilon) && EpsilonEquals(a2, b2, epsilon);
		}

		private static bool EpsilonEquals(Vector3 lhs, Vector3 rhs, float epsilon)
		{
			float dist = Vector3.Distance(lhs, rhs);
			return dist < epsilon;
		}

		public static readonly Color DIVIDER_COLOUR = new Color(116.0f / 255.0f, 116.0f / 255.0f, 116.0f / 255.0f);

		public static void DrawUiDivider()
		{
			DrawUiDivider(DIVIDER_COLOUR);
		}

		public static void DrawUiDivider(Color color, int thickness = 1, int padding = 10)
		{
			Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
			r.height = thickness;
			r.y += padding / 2;
			r.x -= 2;
			r.width += 6;
			EditorGUI.DrawRect(r, color);
		}
	}
	
} // namespace Technie.PhysicsCreator
