using UnityEngine;

namespace Technie.PhysicsCreator
{
	public interface ICreatorComponent
	{
		GameObject GetGameObject();

		bool HasEditorData();

		IEditorData GetEditorData();
	}
}

