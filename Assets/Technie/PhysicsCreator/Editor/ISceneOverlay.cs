using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Technie.PhysicsCreator
{

	public interface ISceneOverlay
	{
		void Destroy();

		void Disable();

		void SyncOverlay(ICreatorComponent currentComponent, Mesh inputMesh);

		void SyncParentChain(GameObject srcLeafObj);

		void FindOrCreateOverlay();
	}

}
