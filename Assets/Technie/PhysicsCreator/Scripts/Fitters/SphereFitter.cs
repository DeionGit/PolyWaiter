using System.Collections.Generic;
using UnityEngine;
using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator
{
	
	public class SphereFitter
	{
		public Sphere Fit(Hull hull, Vector3[] meshVertices, int[] meshIndices)
		{
			Sphere collisionSphere = new Sphere();

			Vector3 sphereCenter;
			float sphereRadius;
			if (CalculateBoundingSphere(hull, meshVertices, meshIndices, out sphereCenter, out sphereRadius))
			{
				collisionSphere.center = sphereCenter;
				collisionSphere.radius = sphereRadius;
			}
			else
			{
				collisionSphere.center = Vector3.zero;
				collisionSphere.radius = 0.0f;
			}

			return collisionSphere;
		}

		public Sphere Fit(Vector3[] hullVertices, int[] hullIndices)
		{
			if (hullVertices == null || hullVertices.Length == 0)
				return new Sphere();

			Sphere s = SphereUtils.MinSphere(new List<Vector3>(hullVertices));
			return s;
		}

		private bool CalculateBoundingSphere(Hull hull, Vector3[] meshVertices, int[] meshIndices, out Vector3 sphereCenter, out float sphereRadius)
		{
			int[] selectedFaces = hull.GetSelectedFaces();

			if (selectedFaces.Length == 0)
			{
				sphereCenter = Vector3.zero;
				sphereRadius = 0.0f;
				return false;
			}

			List<Vector3> points = new List<Vector3>();

			for (int i = 0; i < selectedFaces.Length; i++)
			{
				int faceIndex = selectedFaces[i];

				Vector3 p0 = meshVertices[meshIndices[faceIndex * 3]];
				Vector3 p1 = meshVertices[meshIndices[faceIndex * 3 + 1]];
				Vector3 p2 = meshVertices[meshIndices[faceIndex * 3 + 2]];

				points.Add(p0);
				points.Add(p1);
				points.Add(p2);
			}

			Sphere s = SphereUtils.MinSphere(points);
			sphereCenter = s.center;
			sphereRadius = s.radius;

			return true;
		}
	}
	
} // namespace Technie.PhysicsCreator
