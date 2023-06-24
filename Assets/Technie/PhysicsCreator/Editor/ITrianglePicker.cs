using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Technie.PhysicsCreator
{
	public interface ITrianglePicker
	{
		void Destroy();

		void Disable();

		bool HasValidTarget();

		Mesh GetInputMesh();

		void FindOrCreatePickClone();

		void SyncPickClone(Renderer selectedRenderer);

		bool Raycast(Ray pickRay, UnpackedMesh unpackedMesh, out int hitTriIndex);

		Vector3[] GetTargetVertices();

		int[] GetTargetTriangles();
	}

} // namespace Technie.PhysicsCreator