using UnityEngine;
using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator
{
	
	public class NullHullController : IHullController
	{
		public void Disable() { }

		public void DisconnectAssets(bool deleteChildComponents) { }

		public bool HasData() { return false; }

		public void RecordPaintingChange(string label) { }

		public void MarkPaintingDirty() { }

		public bool TryPipetteSelection(int hitTriIndex) { return false; }

		public void ClearActiveHull() { }

		public bool HasActiveHull() { return false; }

		public IHull GetActiveHull() { return null; }

		public void AddToSelection(IHull hull, int hitTriIndex) { }

		public void RemoveFromSelection(IHull hull, int hitTriIndex) { }

		public ICreatorComponent FindSelectedColliderCreator() { return null; }

		public void SyncTarget(ICreatorComponent selectedHullPainter, MeshFilter selectedMeshFilter) { }

		public void PaintAllFaces() { }
		public void UnpaintAllFaces() { }
		public void PaintUnpaintedFaces() { }
		public void PaintRemainingFaces() { }
		public void GrowPaintedFaces() { }
		public void ShrinkPaintedFaces() { }
	}
	
} // namespace Technie.PhysicsCreator
