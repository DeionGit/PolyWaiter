using UnityEngine;

namespace Technie.PhysicsCreator
{
	
	public class NullTrianglePicker : ITrianglePicker
	{
		public void Destroy() { }

		public void Disable() { }

		public bool HasValidTarget() { return false; }

		public Mesh GetInputMesh() { return null; }

		public void FindOrCreatePickClone() { }

		public void SyncPickClone(Renderer selectedRenderer) { }

		public bool Raycast(Ray pickRay, UnpackedMesh unpackedMesh, out int hitTriIndex) { hitTriIndex = -1; return false; }

		public Vector3[] GetTargetVertices() { return new Vector3[0]; }

		public int[] GetTargetTriangles() { return new int[0]; }
	}
	
} // namespace Technie.PhysicsCreator
