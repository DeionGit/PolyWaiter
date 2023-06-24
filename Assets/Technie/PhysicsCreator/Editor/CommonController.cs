using System.Collections.Generic;
using UnityEngine;

namespace Technie.PhysicsCreator
{
	public static class CommonController
	{
		public static void SelectAllFaces(IHull hull, Mesh sourceMesh)
		{
			hull.ClearSelectedFaces();

			// TODO: Collapse this down into a Hull.SetSelectedFaces so we only update cached vertices once at the end

			int numTris = sourceMesh.triangles.Length / 3;

			List<int> allFaces = new List<int>();
			for (int i = 0; i < numTris; i++)
			{
				allFaces.Add(i);
			}
			hull.SetSelectedFaces(allFaces, sourceMesh);
		}

		public static void UnpaintAllFaces(IHull hull, Mesh sourceMesh)
		{
			if (hull != null)
			{
				hull.ClearSelectedFaces();
			}
		}


		public static void PaintUnpaintedFaces(IHull hull, Mesh sourceMesh)
		{
			if (hull == null || sourceMesh == null)
				return;
			
			List<int> inverseFaces = new List<int>();

			// Check every face, and if it's not selected then add it to the inverse list
			int numTris = sourceMesh.triangles.Length / 3;
			for (int i = 0; i < numTris; i++)
			{
				if (!hull.IsTriangleSelected(i, null, null))
					inverseFaces.Add(i);
			}

			hull.SetSelectedFaces(inverseFaces, sourceMesh);
		}

		public static void PaintRemainingFaces(IHull hull, IHull[] allHulls, Mesh sourceMesh)
		{
			if (hull == null || sourceMesh == null)
				return;

			List<int> remainingFaces = new List<int>();
			int numTris = sourceMesh.triangles.Length / 3;
			for (int i = 0; i < numTris; i++)
			{
				remainingFaces.Add(i);
			}
			
			hull.ClearSelectedFaces();

			foreach (IHull h in allHulls)
			{
				int[] faces = h.GetSelectedFaces();
				for (int i = 0; i < faces.Length; i++)
				{
					remainingFaces.Remove(faces[i]);
				}
			}

			hull.SetSelectedFaces(remainingFaces, sourceMesh);
		}

		public static void GrowPaintedFaces(IHull hull, Mesh sourceMesh)
		{
			Vector3[] allVertices = sourceMesh.vertices;
			int[] allIndices = sourceMesh.triangles;
			int[] selectedFaces = hull.GetSelectedFaces();
			int prevNumSelected = selectedFaces.Length;

			// Find edges of existing selection

			Dictionary<Edge, int> selectedEdges = FindSelectedEdges(hull, sourceMesh);

			// Triangle indices of the newly-grown selection (including the original selection)
			List<int> grownSelection = new List<int>();

			for (int i = 0; i < allIndices.Length; i += 3)
			{
				// If the triangle is already selected, then keep it
				int triIndex = i / 3;
				if (hull.IsTriangleSelected(triIndex, null, null))
				{
					grownSelection.Add(triIndex);
					continue;
				}

				// Create the edges for this triangle

				Edge e0, e1, e2;
				CreateEdges(triIndex, allVertices, allIndices, out e0, out e1, out e2);

				// Count how many edges are shared with the selected faces

				int count = 0;
				if (GetCount(selectedEdges, e0) > 0)
					count++;
				if (GetCount(selectedEdges, e1) > 0)
					count++;
				if (GetCount(selectedEdges, e2) > 0)
					count++;

				// Grow the selection to include this triangle if at least one edge is shared
				if (count >= 1)
					grownSelection.Add(triIndex);
			}

			// Replace the existing selection with the newly grown selection
			hull.SetSelectedFaces(grownSelection, sourceMesh);

			Console.output.Log(string.Format("Grown selection from {0} to {1} faces", prevNumSelected, grownSelection.Count));
		}

		public static void ShrinkPaintedFaces(IHull hull, Mesh sourceMesh)
		{
			Vector3[] allVertices = sourceMesh.vertices;
			int[] allIndices = sourceMesh.triangles;
			int[] selectedFaces = hull.GetSelectedFaces();
			int prevNumSelected = selectedFaces.Length;

			// Find edges of existing selection

			Dictionary<Edge, int> selectedEdges = FindSelectedEdges(hull, sourceMesh);

			// Triangle indices of the newly-grown selection (including the original selection)
			List<int> shrinkedSelectionStrong = new List<int>(); // Keep triangles with three shared edges (ie. only internal triangle, a strong/heavy shrink)
			List<int> shrinkedSelectionWeak = new List<int>(); // Keep triangles with two or three shared edges (eg. internal and outer triangles with only a single exposed edge, a weak/light shrink)

			for (int i = 0; i < allIndices.Length; i += 3)
			{
				int triIndex = i / 3;

				// Create the edges for this triangle

				Edge e0, e1, e2;
				CreateEdges(triIndex, allVertices, allIndices, out e0, out e1, out e2);

				// Count how many edges are shared with the selected faces

				int count = 0;
				if (GetCount(selectedEdges, e0) > 1)
					count++;
				if (GetCount(selectedEdges, e1) > 1)
					count++;
				if (GetCount(selectedEdges, e2) > 1)
					count++;

				// Keep this triangle if all of the edges are shared

				if (count >= 3)
					shrinkedSelectionStrong.Add(triIndex);

				if (count >= 2)
					shrinkedSelectionWeak.Add(triIndex);
			}

			// If the weak shrink is actually smaller, apply that
			// Otherwise, apply the strong shrink
			// (two-step shrink means we can have a more gradual shrink without getting stuck in local minima)
			// Don't apply any shrink if it would leave us with no selection because we don't want to shrink to nothing

			if (shrinkedSelectionWeak.Count > 0 && shrinkedSelectionWeak.Count != prevNumSelected)
			{
				// Replace the existing selection with the newly grown selection
				hull.SetSelectedFaces(shrinkedSelectionWeak, sourceMesh);

				Console.output.Log(string.Format("Weak Shrink selection from {0} to {1} faces", prevNumSelected, shrinkedSelectionWeak.Count));
			}
			else if (shrinkedSelectionStrong.Count > 0 && shrinkedSelectionStrong.Count != prevNumSelected)
			{
				// Replace the existing selection with the newly grown selection
				hull.SetSelectedFaces(shrinkedSelectionStrong, sourceMesh);

				Console.output.Log(string.Format("Strong Shrink selection from {0} to {1} faces", prevNumSelected, shrinkedSelectionStrong.Count));
			}
			else
			{
				// Don't shrink the selection if it would result in no selection
			}
		}

		private static void CreateEdges(int triangleIndex, Vector3[] allVertices, int[] allIndices, out Edge e0, out Edge e1, out Edge e2)
		{
			int baseIndex = triangleIndex * 3;

			Vector3 v0 = allVertices[allIndices[baseIndex]];
			Vector3 v1 = allVertices[allIndices[baseIndex + 1]];
			Vector3 v2 = allVertices[allIndices[baseIndex + 2]];

			e0 = new Edge(v0, v1);
			e1 = new Edge(v1, v2);
			e2 = new Edge(v2, v0);
		}

		private static Dictionary<Edge, int> FindSelectedEdges(IHull hull, Mesh sourceMesh)
		{
			Vector3[] allVertices = sourceMesh.vertices;
			int[] allIndices = sourceMesh.triangles;
			int[] selectedFaces = hull.GetSelectedFaces();

			Dictionary<Edge, int> selectedEdges = new Dictionary<Edge, int>();
			for (int i = 0; i < selectedFaces.Length; i++)
			{
				int baseIndex = selectedFaces[i] * 3;
				Vector3 v0 = allVertices[allIndices[baseIndex]];
				Vector3 v1 = allVertices[allIndices[baseIndex + 1]];
				Vector3 v2 = allVertices[allIndices[baseIndex + 2]];

				Edge e0 = new Edge(v0, v1);
				Edge e1 = new Edge(v1, v2);
				Edge e2 = new Edge(v2, v0);

				IncEdgeCount(selectedEdges, e0);
				IncEdgeCount(selectedEdges, e1);
				IncEdgeCount(selectedEdges, e2);
			}

			return selectedEdges;
		}


		private static void IncEdgeCount(Dictionary<Edge, int> edges, Edge newEdge)
		{
			int count;
			if (edges.TryGetValue(newEdge, out count))
			{
				edges[newEdge] = count + 1;
			}
			else
			{
				edges[newEdge] = 1;
			}
		}

		private static int GetCount(Dictionary<Edge, int> edges, Edge e)
		{
			int count;
			if (edges.TryGetValue(e, out count))
			{
				return count;
			}
			else
			{
				return 0;
			}
		}

	}
	
} // namespace Technie.PhysicsCreator
