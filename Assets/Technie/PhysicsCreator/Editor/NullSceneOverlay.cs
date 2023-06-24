using UnityEngine;

namespace Technie.PhysicsCreator
{
	
	public class NullSceneOverlay : ISceneOverlay
	{
		public void Destroy() { }

		public void Disable() { }

		public void SyncOverlay(ICreatorComponent currentComponent, Mesh inputMesh) { }

		public void SyncParentChain(GameObject srcLeafObj) { }

		public void FindOrCreateOverlay() { }
	}
	
} // namespace Technie.PhysicsCreator
