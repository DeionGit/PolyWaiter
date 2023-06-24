using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator
{
	public interface IHullController
	{
		void Disable();

		void DisconnectAssets(bool deleteChildComponents);

		bool HasData();

		void RecordPaintingChange(string label);

		void MarkPaintingDirty();

		bool TryPipetteSelection(int hitTriIndex);

		void ClearActiveHull();

		bool HasActiveHull();

		IHull GetActiveHull();

		void AddToSelection(IHull hull, int hitTriIndex);

		void RemoveFromSelection(IHull hull, int hitTriIndex);

		ICreatorComponent FindSelectedColliderCreator();

		void SyncTarget(ICreatorComponent selectedHullPainter, MeshFilter selectedMeshFilter);

		void PaintAllFaces();
		void UnpaintAllFaces();
		void PaintUnpaintedFaces();
		void PaintRemainingFaces();
		void GrowPaintedFaces();
		void ShrinkPaintedFaces();
	}

} // namespace Technie.PhysicsCreator