using UnityEngine;
using System.Collections.Generic;
using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator
{
	public class AlignedCapsuleFitter
	{
		public CapsuleDef Fit(Hull hull, Vector3[] meshVertices, int[] meshIndices)
		{
			// TODO: Should have this as a common pre-processing step for all collider gen - both simplifies and reduces all input verts into only the selected triangle verts
			// But also need a backup that just falls back to the selected verts (for cases where all the verts lie on a single plane)
			Vector3[] hullVertices;
			int[] hullIndices;
			hull.FindConvexHull(meshVertices, meshIndices, out hullVertices, out hullIndices, false);

			// If we can't generate a convex hull (maybe we have a single quad input) then what????
			if (hullVertices == null || hullVertices.Length == 0)
			{
				// ??
				return new CapsuleDef();
			}

			return Fit(hullVertices, hullIndices);
		}

		public CapsuleDef Fit(Vector3[] hullVertices, int[] hullIndices)
		{
			if (hullVertices == null || hullVertices.Length == 0 || hullIndices == null || hullIndices.Length == 0)
				return new CapsuleDef();

			// Have to align the capsule along one of the three primary axies
			// Fit a box to the points to get a rough first approximation

			ConstructionPlane basePlane = new ConstructionPlane(Vector3.zero);
			RotatedBox tightestBox = RotatedBoxFitter.FindTightestBox(basePlane, hullVertices);

			// TODO: Check for tightestBox being null (can happen if input points are colinear)

			// Find the longest axis to put the primary capsule axis along

			ConstructionPlane capsulePlane;
			CapsuleAxis axis;
			if (tightestBox.size.x > tightestBox.size.y && tightestBox.size.x > tightestBox.size.z)
			{
				// Longest along X axis
				capsulePlane = new ConstructionPlane(tightestBox.center, Vector3.right, Vector3.forward);
				axis = CapsuleAxis.X;
			}
			else if (tightestBox.size.y > tightestBox.size.z)
			{
				// Longest along Y axis
				capsulePlane = new ConstructionPlane(tightestBox.center, Vector3.up, Vector3.right);
				axis = CapsuleAxis.Y;
			}
			else
			{
				// Longest along Z axis
				capsulePlane = new ConstructionPlane(tightestBox.center, Vector3.forward, Vector3.right);
				axis = CapsuleAxis.Z;
			}

			// Fit a capsule 
			RotatedCapsule capsule = RotatedCapsuleFitter.FitCapsule(capsulePlane, hullVertices);

			// Refine it for a tighter fit
			RotatedCapsule bestCapsule;
			ConstructionPlane bestPlane;
			RotatedCapsuleFitter.Refine(capsule, capsulePlane, hullVertices, out bestCapsule, out bestPlane);

			CapsuleDef result = new CapsuleDef();
			result.capsuleDirection = axis;
			result.capsuleRadius = bestCapsule.radius;
			result.capsuleHeight = bestCapsule.height;
			result.capsuleCenter = bestCapsule.center;
			result.capsulePosition = Vector3.zero;
			result.capsuleRotation = Quaternion.identity;
		
			return result;
		}
	}
}
