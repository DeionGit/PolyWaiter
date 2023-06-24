using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Technie.PhysicsCreator
{
	public class NgonHull
	{
		public List<Face> faces = new List<Face>();

		public static NgonHull FromBounds(Bounds bounds)
		{
			Vector3[] v = new Vector3[8];
			// bottom
			v[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
			v[1] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
			v[2] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
			v[3] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
			// top
			v[4] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
			v[5] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
			v[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);
			v[7] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);

			NgonHull hull = new NgonHull();
			hull.faces.Add(new Face(v[0], v[1], v[2], v[3])); // bottom
			hull.faces.Add(new Face(v[7], v[6], v[5], v[4])); // top

			hull.faces.Add(new Face(v[5], v[6], v[2], v[1])); // x+
			hull.faces.Add(new Face(v[7], v[4], v[0], v[3])); // x-

			hull.faces.Add(new Face(v[6], v[7], v[3], v[2])); // z+
			hull.faces.Add(new Face(v[4], v[5], v[1], v[0])); // z-

			return hull;
		}

		public Mesh ToMesh()
		{
			Mesh mesh = new Mesh();

			List<Vector3> vertices = new List<Vector3>();
			List<Vector3> normals = new List<Vector3>();
			List<int> indices = new List<int>();

			int baseVertexIndex = 0;

			for (int i = 0; i < faces.Count; i++)
			{
				Face f = faces[i];
				Vector3 faceNormal = f.CalcNormal();

				vertices.Add(f.CalcCenter());
				normals.Add(faceNormal);

				for (int j = 0; j < f.vertices.Count; j++)
				{
					vertices.Add(f.vertices[j]);
					normals.Add(faceNormal);
				}

				for (int j = 0; j < f.vertices.Count; j++)
				{
					indices.Add(baseVertexIndex);
					indices.Add(baseVertexIndex + j + 1);
					indices.Add(baseVertexIndex + ((j + 1) % f.vertices.Count) + 1);
				}

				baseVertexIndex += f.vertices.Count + 1;
			}

			mesh.vertices = vertices.ToArray();
			mesh.normals = normals.ToArray();
			mesh.triangles = indices.ToArray();

			return mesh;
		}
	}

	public class Face
	{
		public List<Vector3> vertices = new List<Vector3>();

		public static GameObject lastDebugObj;

		public Face() { }

		public Face(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
		{
			vertices.Add(p0);
			vertices.Add(p1);
			vertices.Add(p2);
			vertices.Add(p3);
		}

		public Vector3 CalcCenter()
		{
			Vector3 center = Vector3.zero;

			foreach (Vector3 v in vertices)
				center += v;

			return center / vertices.Count;
		}

		public float CalcArea()
		{
			Vector3 center = CalcCenter();

			float area = 0.0f;
			for (int i = 0; i < vertices.Count; i++)
			{
				area += CalcTriangleArea(center, vertices[i], vertices[(i + 1) % vertices.Count]);
			}

			return area;
		}

		
		public Vector3 CalcNormal()
		{
			if (vertices.Count < 3)
			{
				Debug.LogError("Can't calc normal because face doesn't have enough vertices");
				return Vector3.up;
			}

			float lowestDot = float.MaxValue;

			Vector3 normal = Vector3.up;

			Vector3 center = CalcCenter();
			Vector3 d0 = (vertices[0] - center).normalized;

			for (int i=1; i<vertices.Count; i++)
			{
				Vector3 d1 = (vertices[i] - center).normalized;
				float dot = Vector3.Dot(d0, d1);
				if (Vector3.Dot(d0, d1) < 0.9999f)
				{
					normal = Vector3.Cross(d0, d1).normalized;
					break;
				}

				lowestDot = Mathf.Min(lowestDot, dot);
			}

			return normal;
		}

		private static float CalcTriangleArea(Vector3 p0, Vector3 p1, Vector3 p2)
		{
			Vector3 e0 = (p1 - p0);
			Vector3 e1 = (p2 - p0);

			Vector3 cross = Vector3.Cross(e0, e1);

			float area = cross.magnitude * 0.5f;
			return area;
		}
	}



	public class CutEdge
	{
		public Vector3 v0;
		public Vector3 v1;

		public float Length
		{
			get { return Vector3.Distance(v0, v1); }
		}

		public CutEdge(Vector3 v0, Vector3 v1)
		{
			this.v0 = v0;
			this.v1 = v1;
		}

		public float CalcLength()
		{
			return Vector3.Distance(v0, v1);
		}
	}

	public class SortableVector3 : System.IComparable<SortableVector3>
	{
		public Vector3 value;
		public float sortValue;

		public SortableVector3(Vector3 v, float s)
		{
			this.value = v;
			this.sortValue = s;
		}

		public int CompareTo(SortableVector3 other)
		{
			if (this.sortValue > other.sortValue)
				return 1;
			else if (this.sortValue < other.sortValue)
				return -1;
			else
				return 0;
		}
	}

	public class HullSimplifier
	{
		public enum PlaneSelection
		{
			Arbitrary,
			LowestDistance,
			DisparateAngle,
			CombinedDistanceAndAngle,
			WeightedAngle,
		}

		public enum HoleFillMethod
		{
			ConnectEdges,
			SortVertices
		}

		private List<CutEdge> debugEdges = new List<CutEdge>();
		private List<CutEdge> debugUnconnectedEdges = new List<CutEdge>();
		private Face debugCutFace;
		private List<SortableVector3> debugSortableVertices = new List<SortableVector3>();
		private Plane debugClipPlane;
		private Vector3 debugFillCenter;
		private Vector3 debugClipTangent;
		private Face debugNormalFace;
		private List<Plane> debugAppliedPlanes = new List<Plane>();

		public Mesh Simplify(Mesh inputMesh, int maxPlanes, PlaneSelection planeSelection, HoleFillMethod holeFillMethod)
		{
			Profiler.BeginSample("Find initial convex hull");

			Mesh hullMesh = QHullUtil.FindConvexHull("test", inputMesh, true);

			Profiler.EndSample();

			Profiler.BeginSample("Construct Planes");

			// Convert the hull triangles to planes

			List<Plane> inputPlanes = new List<Plane>();
			Vector3[] inputVertices = hullMesh.vertices;
			int[] inputIndices = hullMesh.triangles;
			for (int i = 0; i < inputIndices.Length; i += 3)
			{
				Vector3 v0 = inputVertices[inputIndices[i]];
				Vector3 v1 = inputVertices[inputIndices[i + 1]];
				Vector3 v2 = inputVertices[inputIndices[i + 2]];

				Plane plane = new Plane(v0, v1, v2); // create from three points on plane, in clockwise order
				if (plane.normal.magnitude != 0.0f)
				{
					if (!Contains(inputPlanes, plane))
					{
						inputPlanes.Add(plane);
					}
				}
				else
				{
				//	Debug.LogError("Invalid plane created from input vertices - normal is zero length!");
				}
			}

			//Debug.Log("Found " + inputPlanes.Count + " planes in input hull, reducing to " + maxPlanes + " max planes");

			// Create a hull from the bounding box
			NgonHull ngon = NgonHull.FromBounds(hullMesh.bounds);

			// Pre-populate the applied planes list with the planes that make up the initial bounds
			List<Plane> appliedPlanes = new List<Plane>();
			appliedPlanes.Add(new Plane(new Vector3(1f, 0f, 0f), -hullMesh.bounds.extents.x));
			appliedPlanes.Add(new Plane(new Vector3(-1f, 0f, 0f), -hullMesh.bounds.extents.x));
			appliedPlanes.Add(new Plane(new Vector3(0f, 1f, 0f), -hullMesh.bounds.extents.y));
			appliedPlanes.Add(new Plane(new Vector3(0f, -1f, 0f), -hullMesh.bounds.extents.y));
			appliedPlanes.Add(new Plane(new Vector3(0f, 0f, 1f), -hullMesh.bounds.extents.z));
			appliedPlanes.Add(new Plane(new Vector3(0f, 0f, -1f), -hullMesh.bounds.extents.z));

			// Remove any input planes which happen to line up exactly with the bounding box planes we've already applied
			int numRemoved = 0;
			foreach (Plane applied in appliedPlanes)
			{
				for (int i=0; i<inputPlanes.Count; i++)
				{
					if (Approximately(applied, inputPlanes[i]))
					{
						inputPlanes.RemoveAt(i);
						numRemoved++;
						break;
					}
				}
			}

			//Debug.Log(string.Format("Removed {0} input planes which are the same as the initial bounding box planes", numRemoved));

			Profiler.EndSample();

			// While planes under max plane count:

			Profiler.BeginSample("Clip Planes");

			int numPlanes = 6;
			int numCuts = 0;

			while (numPlanes < maxPlanes && inputPlanes.Count > 0)
			{
				Profiler.BeginSample("Process clip plane");

				//Debug.Log(string.Format("Clipping plane {0}", numCuts));

				//if (numCuts == 9)
				//	Debug.Log("Here");

				// Select the next plane
				Plane nextPlane = PopNextPlane(inputPlanes, appliedPlanes, planeSelection);

				//Debug.Log(string.Format("Clipping against plane with normal {0}", nextPlane.normal.ToString("0.000")));

				// Clip the current hull by the new plane
				ngon = Clip(ngon, nextPlane, holeFillMethod);

				numPlanes++;

				Profiler.EndSample();

				numCuts++;
				//if (numCuts == 1)
				//		break;
			}

			//Debug.Log(string.Format("Simplified hull has {0} planes", ngon.faces.Count));

			Profiler.EndSample();

			// Strip out any faces with near zero area

			Profiler.BeginSample("Post-process output");

			int numFacesRemoved = 0;
			for (int i=ngon.faces.Count-1; i>=0; i--)
			{
				Face face = ngon.faces[i];
				float area = face.CalcArea();
				if (area < 0.00001f)	// min area from experiments in bulk test scene - any lower tends to result in triangles with bad normals
				{
					ngon.faces.RemoveAt(i);
					numFacesRemoved++;
				}
			}

			//Debug.Log(string.Format("Removed {0} faces with near-zero area (now {1} planes)", numFacesRemoved, ngon.faces.Count));

			Mesh finalMesh = ngon.ToMesh();

			this.debugAppliedPlanes = appliedPlanes;

			Profiler.EndSample();

			// MAYBE: Put the simplified hull through QHull one last time to clean up the output?
			//Mesh finalHullMesh = QHullUtil.FindConvexHull("test", finalMesh, true); // TEMP DISABLED THIS so we can see the raw output of the simplification process
			//return finalHullMesh;

			return finalMesh;
		}

		private static Plane PopNextPlane(List<Plane> possiblePlanes, List<Plane> appliedPlanes, PlaneSelection planeSelection)
		{
			Plane result = new Plane();

			if (planeSelection == PlaneSelection.Arbitrary)
			{
				result = possiblePlanes[0];
				possiblePlanes.RemoveAt(0);
			}
			else if (planeSelection == PlaneSelection.LowestDistance)
			{
				Plane bestPlane = new Plane();
				float bestRating = float.MinValue;
				int bestIndex = -1;

				for (int i = 0; i < possiblePlanes.Count; i++)
				{
					Plane p = possiblePlanes[i];
					float distRating = CalcDistanceRating(p, appliedPlanes);
					if (distRating > bestRating)
					{
						bestPlane = p;
						bestRating = distRating;
						bestIndex = i;
					}
				}

				possiblePlanes.RemoveAt(bestIndex);
				result = bestPlane;
			}
			else if (planeSelection == PlaneSelection.DisparateAngle)
			{
				Plane bestPlane = new Plane();
				float bestRating = float.MinValue;
				int bestIndex = -1;

				for (int i = 0; i < possiblePlanes.Count; i++)
				{
					Plane p = possiblePlanes[i];
					float angleRating = CalcAngleRating(p, appliedPlanes);
					if (angleRating > bestRating)
					{
						bestPlane = p;
						bestRating = angleRating;
						bestIndex = i;
					}
				}

				possiblePlanes.RemoveAt(bestIndex);
				result = bestPlane;
			}
			else if (planeSelection == PlaneSelection.CombinedDistanceAndAngle)
			{
				Plane bestPlane = new Plane();
				float bestRating = float.MinValue;
				int bestIndex = -1;

				for (int i = 0; i < possiblePlanes.Count; i++)
				{
					Plane p = possiblePlanes[i];
					float distRating = CalcDistanceRating(p, appliedPlanes);
					float angleRating = CalcAngleRating(p, appliedPlanes);
					float rating = distRating * angleRating;
					if (rating > bestRating)
					{
						bestPlane = p;
						bestRating = rating;
						bestIndex = i;
					}
				}

				possiblePlanes.RemoveAt(bestIndex);
				result = bestPlane;
			}
			else if (planeSelection == PlaneSelection.WeightedAngle)
			{
				Plane bestPlane = new Plane();
				float bestRating = float.MinValue;
				int bestIndex = -1;

				for (int i = 0; i < possiblePlanes.Count; i++)
				{
					Plane p = possiblePlanes[i];
					float angleRating = CalcWeightedAngleRating(p, appliedPlanes);
					if (angleRating > bestRating)
					{
						bestPlane = p;
						bestRating = angleRating;
						bestIndex = i;
					}
				}

				possiblePlanes.RemoveAt(bestIndex);
				result = bestPlane;
			}
			else
			{

			}

			appliedPlanes.Add(result);
			return result;
		}

		// Returns a rating [0,1] where 0 is undesirable and 1 is desirable
		private static float CalcDistanceRating(Plane currentPlane, List<Plane> otherPlanes)
		{
			float largestDist = float.MinValue;
			foreach (Plane plane in otherPlanes)
			{
				if (plane.distance > largestDist)
					largestDist = plane.distance;
			}

			float rating = 1.0f - Mathf.Clamp01(currentPlane.distance / largestDist);
			return rating;
		}

		// 'Disparate angle' rating - rating is based on the most closely aligned plane to the current plane
		// Returns a rating [0,1] where 0 is undesirable and 1 is desirable
		private static float CalcAngleRating(Plane currentPlane, List<Plane> otherPlanes)
		{
			float closestDot = float.MinValue;

			foreach (Plane p in otherPlanes)
			{
				float dot = Vector3.Dot(p.normal, currentPlane.normal);
				if (dot > closestDot)
				{
					closestDot = dot;
				}
			}

			// Convert from [+1,-1] to [0,1]
			float rating = Mathf.Clamp01(1.0f - (closestDot * 0.5f + 0.5f));
			return rating;
		}

		// 'Weighted angle' rating - rating is based on how close the plane's normal is to *all* other planes, not just the closest
		// Returns a rating [0,1] where 0 is undesirable and 1 is desirable
		private static float CalcWeightedAngleRating(Plane currentPlane, List<Plane> otherPlanes)
		{
			float rating = 0f;

			foreach (Plane p in otherPlanes)
			{
				// Single [0,1] rating for this particular plane pair
				float dot = Vector3.Dot(p.normal, currentPlane.normal);
				float r = Mathf.Clamp01(1.0f - (dot * 0.5f + 0.5f));

				// Accumulate the rating for all planes
				rating += r;
			}

			return rating / (float)otherPlanes.Count;
		}

		private NgonHull Clip(NgonHull inputHull, Plane clipPlane, HoleFillMethod holeFillMethod)
		{
			NgonHull outputHull = new NgonHull();

			List<CutEdge> cutEdges = new List<CutEdge>();

			debugEdges.Clear();

			foreach (Face inputFace in inputHull.faces)
			{
				Face outputFace = new Face();

				int cutIndex = -1;

				for (int i = 0; i < inputFace.vertices.Count; i++)
				{
					Vector3 v0 = inputFace.vertices[i];
					Vector3 v1 = inputFace.vertices[(i + 1) % inputFace.vertices.Count];

					bool keepV0 = !clipPlane.GetSide(v0);
					bool keepV1 = !clipPlane.GetSide(v1);
					if (keepV0 && keepV1)
					{
						outputFace.vertices.Add(v0);
					}
					else if (!keepV0 && !keepV1)
					{
						// fully clipped
					}
					else if (keepV0 && !keepV1)
					{
						Vector3 clipV = CalcIntersection(v0, v1, clipPlane, out float weight);
						outputFace.vertices.Add(v0);
						outputFace.vertices.Add(clipV);

						cutIndex = outputFace.vertices.Count - 1;
					}
					else if (!keepV0 && keepV1)
					{
						Vector3 clipV = CalcIntersection(v0, v1, clipPlane, out float weight);
						outputFace.vertices.Add(clipV);
					}
				}

				if (outputFace.vertices.Count >= 3)
				{
					outputHull.faces.Add(outputFace);
				}
				else
				{
					//Debug.LogWarning(string.Format("Discarding face with no triangles ({0} vertices)", outputFace.vertices.Count));
				}

				if (cutIndex != -1)
				{
					Vector3 e0 = outputFace.vertices[cutIndex];
					Vector3 e1 = outputFace.vertices[(cutIndex + 1) % outputFace.vertices.Count];
					cutEdges.Add(new CutEdge(e0, e1));
				}
			}

			// Create a new face from the cut edges (if we have enough edges to at least make a triangle)

			Face cutFace = null;

			if (holeFillMethod == HoleFillMethod.ConnectEdges)
				cutFace = FillHoleByConnectingEdges(clipPlane, cutEdges);
			else if (holeFillMethod == HoleFillMethod.SortVertices)
				cutFace = FillHoleBySortingVertices(clipPlane, cutEdges);

			if (cutFace != null)
				outputHull.faces.Add(cutFace);

			return outputHull;
		}

		// Check the input list to see if the testPlane is already contained within it
		private static bool Contains(List<Plane> inputPlanes, Plane testPlane)
		{
			foreach (Plane inPlane in inputPlanes)
			{
				if (Approximately(inPlane, testPlane))
					return true;
			}
			return false;
		}

		private static bool Approximately(Plane lhs, Plane rhs)
		{
			float dir = Vector3.Dot(lhs.normal, rhs.normal);
			float dist = Mathf.Abs(lhs.distance - rhs.distance);
			return (dir > 0.99f && dist < 0.01f);
		}
		
		private Face FillHoleByConnectingEdges(Plane clipPlane, List<CutEdge> cutEdges)
		{
			// Tries to fill the hole created by the clip plane by connecting up the edges it produces
			
			if (cutEdges.Count < 3)
				return null;

			List<CutEdge> originalCutEdges = new List<CutEdge>(cutEdges);

			Face cutFace = new Face();

			CutEdge currentEdge = cutEdges[0];
			cutEdges.RemoveAt(0);
			cutFace.vertices.Add(currentEdge.v0);
			cutFace.vertices.Add(currentEdge.v1);

			Vector3 currentVertex = currentEdge.v1;

			int iterations = 0;

			while (cutEdges.Count > 0)
			{
				if (PopNextVertex(currentVertex, cutEdges, out Vector3 nextVertex))
				{
					cutFace.vertices.Add(nextVertex);
					currentVertex = nextVertex;
				}
				else
				{
					break;
				}

				iterations++;
				if (iterations > 10000)
				{
					Debug.LogError("Unable to create face to cover cut hole!");
					cutFace.vertices.Clear();
					break;
				}
			}

			if (cutFace.vertices.Count >= 3) // if we got at least a triangle then check if it loops
			{
				float startEndDist = Vector3.Distance(cutFace.vertices[0], cutFace.vertices[cutFace.vertices.Count - 1]);
				bool isLoop = (startEndDist < 0.0001f);
				if (isLoop)
				{
					// Remove duplicate vertex at start<->end
					cutFace.vertices.RemoveAt(cutFace.vertices.Count - 1);
				}
				else
				{
					// Reject this face because we didn't form a complete loop
					
					debugEdges = originalCutEdges;
					debugUnconnectedEdges = cutEdges;
					debugCutFace = cutFace;

					Debug.LogError("Giving up trying to create cut face, originally " + originalCutEdges.Count + " cut edges, of which " + cutEdges.Count + " remain unconnected");
					foreach (CutEdge edge in cutEdges)
						Debug.LogError(string.Format("Remaining edge length: {0}", edge.Length.ToString("0.0000000")));
				}
			}

			if (cutFace.vertices.Count >= 3) // if we got at least a triangle then check it's orientation
			{
				// TODO: Use cutFace.CalcNormal here instead?
				Vector3 cutNormal = Vector3.Cross(cutFace.vertices[1] - cutFace.vertices[0],
													cutFace.vertices[2] - cutFace.vertices[0]);

				if (Vector3.Dot(cutNormal, clipPlane.normal) < 0.0f)
				{
					// Flip the new face
					cutFace.vertices.Reverse();
				}

				// Return the new face as it's valid
				return cutFace;
			}
			else
			{
				return null;
			}
		}

		private Face FillHoleBySortingVertices(Plane clipPlane, List<CutEdge> cutEdges)
		{
			// Tries to create a face to fill the cut hole by projecting the vertices into the cut plane's coord system and connecting them based on their 2d angle
			// This is more robust than connecting edges because we always get a non-ambiguous ordering and is more robust when small edges are very close to each other

			const float UNIQUE_THRESHOLD = 0.000001f;

			// Find the unique vertices from the cut edges

			List<Vector3> allVertices = new List<Vector3>();
			foreach (CutEdge edge in cutEdges)
			{
				allVertices.Add(edge.v0);
				allVertices.Add(edge.v1);
			}

			List<Vector3> uniqueVertices = new List<Vector3>();
			foreach (Vector3 v in allVertices)
			{
				bool exists = false;
				foreach (Vector3 existing in uniqueVertices)
				{
					if (Vector3.Distance(v, existing) < UNIQUE_THRESHOLD)
					{
						exists = true;
						break;
					}
				}
				if (!exists)
					uniqueVertices.Add(v);
			}

			if (uniqueVertices.Count < 3)
			{
				//Debug.Log(string.Format("Not enough unique vertices to fill the cut hole (num unique vertices: {0})", uniqueVertices.Count));
				return null;
			}

			// Create the basis matrix for the cut plane

			Vector3 center = Vector3.zero;
			foreach (Vector3 v in uniqueVertices)
			{
				center += v;
			}
			center /= (float)uniqueVertices.Count;
			
			Matrix4x4 basis = Matrix4x4.TRS(center, Quaternion.LookRotation(clipPlane.normal), Vector3.one);
			Matrix4x4 inverseBasis = basis.inverse;

			List<SortableVector3> sortableVertices = new List<SortableVector3>();

			// Transform all the vertices into the plane basis space

			foreach (Vector3 v in uniqueVertices)
			{
				Vector3 onPlane = inverseBasis.MultiplyPoint(v);
				float angle = Vector2.SignedAngle(new Vector2(onPlane.x, onPlane.y), Vector2.up);

				sortableVertices.Add(new SortableVector3(v, angle));
			}

			// Sort based on the signed angle

			sortableVertices.Sort();

			// Reconstruct the face from the sorted vertices

			Face face = new Face();
			foreach (SortableVector3 v in sortableVertices)
			{
				face.vertices.Add(v.value);
			}
			
			// Check that it's facing the same way as the clip plane, if not then flip it

			if (Vector3.Dot(face.CalcNormal(), clipPlane.normal) < 0.0f)
			{
				//Debug.Log("Flipping winding order because normal is pointing the wrong way");

				face.vertices.Reverse();
			}

			debugSortableVertices = sortableVertices;
			debugClipPlane = clipPlane;
			debugFillCenter = center;
			debugClipTangent = basis.MultiplyVector(Vector3.up);
			debugNormalFace = face;

			return face;
		}

		public void RecalcFaceNormal()
		{
			debugNormalFace.CalcNormal();
		}

		private static bool PopNextVertex(Vector3 pos, List<CutEdge> edges, out Vector3 nextVertex)
		{
			const float threshold = 0.000015f;


			for (int i = 0; i < edges.Count; i++)
			{
				CutEdge e = edges[i];
				if (Vector3.Distance(pos, e.v0) < threshold)
				{
					edges.RemoveAt(i);
					nextVertex = e.v1;
					return true;
				}
				if (Vector3.Distance(pos, e.v1) < threshold)
				{
					edges.RemoveAt(i);
					nextVertex = e.v0;
					return true;
				}
			}


			// Error output

			float closestDist = float.MaxValue;
			int closestEdgeIndex = -1;
			int vertexId = -1;
			for (int i = 0; i < edges.Count; i++)
			{
				CutEdge e = edges[i];
				float d0 = Vector3.Distance(pos, e.v0);
				float d1 = Vector3.Distance(pos, e.v1);
				if (d0 < closestDist)
				{
					closestDist = d0;
					closestEdgeIndex = i;
					vertexId = 0;
				}
				if (d1 < closestDist)
				{
					closestDist = d1;
					closestEdgeIndex = i;
					vertexId = 1;
				}
			}

			Debug.LogWarning(string.Format("Couldn't find an unconnected edge to attach to current vertex - closest is edge {0} {1} - distance of {2} (threshold is {3})", closestEdgeIndex, vertexId, closestDist, threshold));
			nextVertex = Vector3.zero;
			return false;
		}

		private static Vector3 CalcIntersection(Vector3 v0, Vector3 v1, Plane plane, out float weight)
		{
			// Calculate length between vertices and create ray from v0 to v1
			Vector3 deltaA = v1 - v0;
			float lengthA = deltaA.magnitude;
			Ray rayA = new Ray(v0, deltaA / lengthA);

			// Raycast against the plane to find the intersection point and distance
			float distA;
			plane.Raycast(rayA, out distA);
			Vector3 intersectionA = rayA.origin + rayA.direction * distA;

			// Calculate the normalised distance between v0 and v1 of the intersection point
			weight = distA / lengthA;

			return intersectionA;
		}

		public void DrawGizmos(bool showDebugEdges, bool showUnconnectedEdges, bool showCutFace)
		{
			if (showDebugEdges && debugEdges != null)
			{
				foreach (CutEdge e in debugEdges)
				{
					Gizmos.color = Color.cyan;
					Gizmos.DrawLine(e.v0, e.v1);
					Gizmos.DrawSphere(e.v0, 0.001f);
					Gizmos.DrawSphere(e.v1, 0.001f);
				}
			}
			if (showUnconnectedEdges && debugUnconnectedEdges != null)
			{
				foreach (CutEdge e in debugUnconnectedEdges)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawLine(e.v0, e.v1);
					Gizmos.DrawSphere(e.v0, 0.001f);
					Gizmos.DrawSphere(e.v1, 0.001f);
				}
			}
			if (showCutFace && debugCutFace != null)
			{
				foreach (Vector3 v in debugCutFace.vertices)
				{
					Gizmos.color = Color.green;
					Gizmos.DrawSphere(v, 0.002f);
				}
			}

			for (int i=0; i<debugSortableVertices.Count; i++)
			{
				SortableVector3 v = debugSortableVertices[i];
				UnityEditor.Handles.Label(v.value, "v" + i + " " + v.sortValue.ToString("0.0"));
			}
			Gizmos.color = Color.cyan;
			Gizmos.DrawSphere(debugFillCenter, 0.01f);
			Gizmos.DrawLine(debugFillCenter, debugFillCenter + (debugClipTangent * 0.1f));
			Gizmos.DrawLine(debugFillCenter, debugFillCenter + (debugClipPlane.normal * 0.1f));

			if (debugNormalFace != null)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawLine(debugNormalFace.vertices[0], debugNormalFace.vertices[0] + debugNormalFace.CalcNormal());
			}

			foreach (Plane plane in debugAppliedPlanes)
			{
				Gizmos.color = Color.white;
				Vector3 p = plane.ClosestPointOnPlane(Vector3.zero);
				Gizmos.DrawSphere(p, 0.01f);
				Gizmos.DrawLine(p, p + (plane.normal * 0.1f));
			}
		}
	}

} // namespace Technie.PhysicsCreator
