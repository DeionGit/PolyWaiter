using UnityEngine;
using System.Collections.Generic;
using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator
{
	public struct RotatedCapsule
	{
		public Vector3 center; 
		public Vector3 dir;
		public float radius;
		public float height; // total height (ie. includes the two half-spheres at the end of the capsule) this is the same as UnityEngine.CapsuleCollider uses

		public float CalcVolume()
		{
			float internalLength = Mathf.Max(height - (radius * 2), 0.0f); // internal length is distance between the center of the two spheres at the end of the capsule
			return Mathf.PI * (radius * radius) * ((4.0f / 3.0f) * radius * internalLength);
		}

		public void DrawWireframe()
		{
			Vector3 p0 = this.center - this.dir * Mathf.Max((height * 0.5f) - radius, 0f);
			Vector3 p1 = this.center + this.dir * Mathf.Max((height * 0.5f) - radius, 0f);

			float len = (p1 - p0).magnitude;
			float halfLen = len * 0.5f;

			Vector3 right = Vector3.Cross(dir, Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.9f ? Vector3.right : Vector3.up);
			Vector3 up = Vector3.Cross(dir, right);

			Gizmos.DrawWireSphere(p0, radius);
			Gizmos.DrawWireSphere(p1, radius);

			Gizmos.DrawLine(center + (right * radius) - (dir * halfLen), center + (right * radius) + (dir * halfLen));
			Gizmos.DrawLine(center - (right * radius) - (dir * halfLen), center - (right * radius) + (dir * halfLen));

			Gizmos.DrawLine(center + (up * radius) - (dir * halfLen), center + (up * radius) + (dir * halfLen));
			Gizmos.DrawLine(center - (up * radius) - (dir * halfLen), center - (up * radius) + (dir * halfLen));
		}
	}

	public class RotatedCapsuleFitter
	{
		public CapsuleDef Fit(Hull hull, Vector3[] meshVertices, int[] meshIndices)
		{
			// Vertices are in local space?

			// Find find the convex hull - the tightest fitting box will always contain the hull, and this lets us simplify the input data
			Vector3[] hullVertices;
			int[] hullIndices;
			hull.FindConvexHull(meshVertices, meshIndices, out hullVertices, out hullIndices, false);

			// If we can't generate a convex hull (maybe we have a single quad input) then what????
			if (hullVertices == null || hullVertices.Length == 0)
			{
				// ??
				return new CapsuleDef();
			}

			//ConstructionPlane capsulePlane = FindBestCapsulePlane(hull, meshVertices, meshIndices);
			//ConstructionPlane capsulePlane = FindBestCapsulePlane(hull, meshVertices, meshIndices);
			ConstructionPlane capsulePlane = FindBestCapsulePlane(hullVertices, hullIndices);

			// Fit a capsule 
			RotatedCapsule capsule = FitCapsule(capsulePlane, hullVertices);

			// Refine it for a tighter fit
			RotatedCapsule bestCapsule;
			ConstructionPlane bestPlane;
			Refine(capsule, capsulePlane, hullVertices, out bestCapsule, out bestPlane);

			return ToDef(bestCapsule, bestPlane);
		}

		public CapsuleDef Fit(Vector3[] hullVertices, int[] hullIndices)
		{
			if (hullVertices == null || hullVertices.Length == 0 || hullIndices == null || hullIndices.Length == 0)
				return new CapsuleDef();

			ConstructionPlane capsulePlane = FindBestCapsulePlane(hullVertices, hullIndices);

			// Fit a capsule 
			RotatedCapsule capsule = FitCapsule(capsulePlane, hullVertices);

			// Refine it for a tighter fit
			RotatedCapsule bestCapsule;
			ConstructionPlane bestPlane;
			Refine(capsule, capsulePlane, hullVertices, out bestCapsule, out bestPlane);

			return ToDef(bestCapsule, bestPlane);
		}

		//public ConstructionPlane FindBestCapsulePlane(Hull hull, Vector3[] meshVertices, int[] meshIndices)
		public ConstructionPlane FindBestCapsulePlane(Vector3[] hullVertices, int[] hullIndices)
		{
			RotatedBoxFitter boxFitter = new RotatedBoxFitter();
			//BoxDef tightestBox = boxFitter.Fit(hull, meshVertices, meshIndices);
			BoxDef tightestBox = boxFitter.Fit(hullVertices, hullIndices);
			Vector3 boxCenter = tightestBox.boxPosition + (tightestBox.boxRotation * tightestBox.collisionBox.center);

			ConstructionPlane capsulePlane;
			if (tightestBox.collisionBox.size.x > tightestBox.collisionBox.size.y && tightestBox.collisionBox.size.x > tightestBox.collisionBox.size.z)
			{
				// Longest along X axis
				capsulePlane = new ConstructionPlane(boxCenter, tightestBox.boxRotation * Vector3.right, tightestBox.boxRotation * Vector3.forward);
			}
			else if (tightestBox.collisionBox.size.y > tightestBox.collisionBox.size.z)
			{
				// Longest along Y axis
				capsulePlane = new ConstructionPlane(boxCenter, tightestBox.boxRotation * Vector3.up, tightestBox.boxRotation * Vector3.right);
			}
			else
			{
				// Longest along Z axis
				capsulePlane = new ConstructionPlane(boxCenter, tightestBox.boxRotation * Vector3.forward, tightestBox.boxRotation * Vector3.right);
			}
			return capsulePlane;
		}

		public static CapsuleDef ToDef(RotatedCapsule capsule, ConstructionPlane plane)
		{
			CapsuleDef result = new CapsuleDef();
			result.capsuleCenter = Vector3.zero;
			result.capsuleDirection = CapsuleAxis.Z;
			result.capsuleRadius = capsule.radius;
			result.capsuleHeight = capsule.height;
			result.capsulePosition = plane.center;
			result.capsuleRotation = plane.rotation;
			return result;
		}

		public static void Refine(RotatedCapsule inputCapule, ConstructionPlane inputPlane, Vector3[] hullVertices, out RotatedCapsule bestCapsule, out ConstructionPlane bestPlane)
		{
			bestPlane = inputPlane;
			bestCapsule = inputCapule;

			// Refine the initial fit to see if we can find anything tighter

			System.Random random = new System.Random(1234); // use a seeded random generator so we always get consistent results
			int numIterations = 1024; // Usually the most optimum is found at max ~500 iterations, but sometimes more so this seems like a good compromise

			for (int i = 0; i < numIterations; i++)
			{
				float jitter = Mathf.Min(bestCapsule.height, bestCapsule.radius) * 0.01f; // allow a 1% jitter to find a better capsule
				ConstructionPlane variantPlane = new ConstructionPlane(bestPlane, new Vector3(Jitter(jitter, random), Jitter(jitter, random), Jitter(jitter, random)));
				RotatedCapsule variantCapsule = FitCapsule(variantPlane, hullVertices);

				// Is this capsule better? (ie. it contains a smaller volume)
				if (variantCapsule.CalcVolume() < bestCapsule.CalcVolume())
				{
					//Console.output.Log(string.Format("Found better capsule - from {0} to {1} (iteration {2})", bestCapsule.CalcVolume()*10000f, variantCapsule.CalcVolume()* 10000f, i));

					bestCapsule = variantCapsule;
					bestPlane = variantPlane;
				}
			}
		}

		private static float Jitter(float magnitude, System.Random random)
		{
			double result = (random.NextDouble() * (magnitude * 2.0f)) - magnitude;
			return (float)result;
		}

		public static RotatedCapsule FitCapsule(ConstructionPlane plane, Vector3[] points)
		{
			// This technically finds the tightest cylinder, not capsule, so it might not strictly
			// contain all the input points at the ends.
			// However by doing it this way we get capsules that align with the end vertices
			// properly, which is what users intuitively expect

			RotatedCapsule result = new RotatedCapsule();
			result.center = plane.center;
			result.dir = plane.normal;

			for (int i=0; i<points.Length; i++)
			{
				Vector3 p = points[i];
				Vector3 pointOnAxis = ProjectOntoAxis(plane, p);
				float distFromAxis = Vector3.Distance(pointOnAxis, p);
				float distAlongAxis = Vector3.Distance(plane.center, pointOnAxis);
				
				// NB: .height is total height, not the length of the internal line segment
				result.radius = Mathf.Max(result.radius, distFromAxis);
				result.height = Mathf.Max(result.height, distAlongAxis * 2.0f);
			}

			return result;
		}

		private static Vector3 ProjectOntoAxis(ConstructionPlane plane, Vector3 point)
		{
			Vector3 delta = point - plane.center;
			float dot = Vector3.Dot(plane.normal, delta);
			Vector3 projected = plane.center + plane.normal * dot;
			return projected;
		}

		public static Vector3 FindCenter(Vector3[] vertices)
		{
			if (vertices == null || vertices.Length == 0)
				return Vector3.zero;

			Vector3 average = Vector3.zero;
			for (int i=0; i<vertices.Length; i++)
			{
				average += vertices[i];
			}

			average /= (float)vertices.Length;

			return average;
		}
	}

} // namespace Technie.PhysicsCreator
