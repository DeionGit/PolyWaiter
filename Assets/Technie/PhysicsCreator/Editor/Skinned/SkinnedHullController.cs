using UnityEngine;
using UnityEditor;

using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator.Skinned
{
	public class SkinnedHullController : IHullController
	{
		private SkinnedColliderCreator creatorComponent;

		public SkinnedHullController()
		{
			
		}

		public void Disable()
		{

		}

		public void DisconnectAssets(bool deleteChildComponents)
		{
			if (creatorComponent != null)
			{
				Undo.SetCurrentGroupName("Disconnect Skinned Collider Creator");

				// First delete child components if needed
			//	if (deleteChildComponents)
			//	{
			//		foreach (SkinnedColliderCreatorChild child in currentHullPainter.GetComponentsInChildren<RigidColliderCreatorChild>())
			//		{
			//			Undo.DestroyObjectImmediate(child);
			//		}
			//	}

				// Then destroy the actual hull painter component
				Undo.DestroyObjectImmediate(creatorComponent);

				Undo.IncrementCurrentGroup();

				creatorComponent = null;
			}
		}

		public void SyncTarget(ICreatorComponent selectedComponent, MeshFilter selectedMeshFilter)
		{
			this.creatorComponent = selectedComponent as SkinnedColliderCreator;

			if (creatorComponent != null)
				EditorUtils.UpgradeStoredData(this.creatorComponent);
		}

		public bool HasData()
		{
			return (creatorComponent != null && creatorComponent.editorData != null);
		}

		public void RecordPaintingChange(string label)
		{
			Undo.RecordObject(creatorComponent.editorData, label);
		}

		public void MarkPaintingDirty()
		{
			creatorComponent.editorData.MarkDirty();
		}

		public bool TryPipetteSelection(int hitTriIndex)
		{
			for (int i = 0; i < creatorComponent.editorData.boneHullData.Count; i++)
			{
				BoneHullData hull = creatorComponent.editorData.boneHullData[i];
				if (hull.IsTriangleSelected(hitTriIndex, creatorComponent.targetSkinnedRenderer, creatorComponent.targetSkinnedRenderer.sharedMesh))
				{
					// Now painting this hull

					Console.output.Log("Pipette selected hull: " + hull.targetBoneName);

					creatorComponent.editorData.SetSelection(hull);
					return true;
				}
			}
			return false;
		}

		public void ClearActiveHull()
		{
			creatorComponent.editorData.ClearSelection();
		}

		public bool HasActiveHull()
		{
			return HasData() && creatorComponent.editorData.GetSelectedHull() != null;
		}

		public IHull GetActiveHull()
		{
			return creatorComponent.editorData.GetSelectedHull();
		}

		public void AddToSelection(IHull hull, int newTriangleIndex)
		{
			BoneHullData boneHull = hull as BoneHullData;
			if (boneHull != null && boneHull.type == HullType.Manual)
			{
				boneHull.AddToSelection(newTriangleIndex, creatorComponent.targetSkinnedRenderer.sharedMesh);
			}
			creatorComponent.editorData.MarkDirty();
		}

		public void RemoveFromSelection(IHull hull, int triIndex)
		{
			BoneHullData boneHull = hull as BoneHullData;
			if (boneHull != null && boneHull.type == HullType.Manual)
			{
				boneHull.RemoveFromSelection(triIndex, creatorComponent.targetSkinnedRenderer.sharedMesh);
			}
			creatorComponent.editorData.MarkDirty();
		}

		public ICreatorComponent FindSelectedColliderCreator()
		{
			return SelectionUtil.FindSelectedHullPainter();
		}
		
		public void PaintAllFaces()
		{
			if (creatorComponent == null || creatorComponent.editorData == null)
				return;

			CommonController.SelectAllFaces(creatorComponent.editorData.GetSelectedHull(), creatorComponent.targetSkinnedRenderer.sharedMesh);

			creatorComponent.editorData.MarkDirty();
		}

		public void UnpaintAllFaces()
		{
			if (creatorComponent == null || creatorComponent.editorData == null)
				return;

			CommonController.UnpaintAllFaces(creatorComponent.editorData.GetSelectedHull(), creatorComponent.targetSkinnedRenderer.sharedMesh);

			creatorComponent.editorData.MarkDirty();
		}

		public void PaintUnpaintedFaces()
		{
			if (creatorComponent == null || creatorComponent.editorData == null)
				return;

			CommonController.PaintUnpaintedFaces(creatorComponent.editorData.GetSelectedHull(), creatorComponent.targetSkinnedRenderer.sharedMesh);

			creatorComponent.editorData.MarkDirty();
		}

		public void PaintRemainingFaces()
		{
			if (creatorComponent == null || creatorComponent.editorData == null)
				return;

			CommonController.PaintRemainingFaces(creatorComponent.editorData.GetSelectedHull(), creatorComponent.editorData.boneHullData.ToArray(), creatorComponent.targetSkinnedRenderer.sharedMesh);

			creatorComponent.editorData.MarkDirty();
		}

		public void GrowPaintedFaces()
		{
			if (creatorComponent == null || creatorComponent.editorData == null)
				return;

			CommonController.GrowPaintedFaces(creatorComponent.editorData.GetSelectedHull(), creatorComponent.targetSkinnedRenderer.sharedMesh);

			creatorComponent.editorData.MarkDirty();
		}

		public void ShrinkPaintedFaces()
		{
			if (creatorComponent == null || creatorComponent.editorData == null)
				return;

			CommonController.ShrinkPaintedFaces(creatorComponent.editorData.GetSelectedHull(), creatorComponent.targetSkinnedRenderer.sharedMesh);

			creatorComponent.editorData.MarkDirty();
		}
	}
	
} // namespace Technie.PhysicsCreator
