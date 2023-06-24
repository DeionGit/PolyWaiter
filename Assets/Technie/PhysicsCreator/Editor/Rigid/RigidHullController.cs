using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator
{
	public class Edge
	{
		public Vector3 start;
		public Vector3 end;

		public Edge(Vector3 v0, Vector3 v1)
		{
			if (v0.x == v1.x)
			{
				if (v0.y == v1.y)
				{
					if (v0.z == v1.z)
					{
						// Both equal, either order is fine
						start = v0;
						end = v1;
					}
					else
					{
						// Sort by z
						if (v0.z < v1.z)
						{
							start = v0;
							end = v1;
						}
						else
						{
							start = v1;
							end = v0;
						}
					}
				}
				else
				{
					// Sort by y
					if (v0.y < v1.y)
					{
						start = v0;
						end = v1;
					}
					else
					{
						start = v1;
						end = v0;
					}
				}
			}
			else
			{
				// sort by x
				if (v0.x < v1.x)
				{
					start = v0;
					end = v1;
				}
				else
				{
					start = v1;
					end = v0;
				}
			}
		}

		public override bool Equals(object obj)
		{
			Edge other = obj as Edge;

			if (other == null)
				return false;

			return this.start == other.start && this.end == other.end;
		}

		public override int GetHashCode()
		{
			return start.GetHashCode() ^ end.GetHashCode();
		}

		public static bool operator ==(Edge lhs, Edge rhs)
		{
			if (object.ReferenceEquals(lhs, null)) // NB: can't use ==null because we're inside operator==
			{
				if (object.ReferenceEquals(rhs, null))
				{
					return true;
				}

				// Only the left side is null.
				return false;
			}

			// Equals handles case of null on right side.
			return lhs.Equals(rhs);
		}

		public static bool operator !=(Edge lhs, Edge rhs)
		{
			return lhs != rhs;
		}
	}

	public class RigidHullController : IHullController
	{
		private RigidColliderCreator currentHullPainter;
		private MeshFilter selectedMeshFilter;

		public void Disable()
		{

		}

		public ICreatorComponent FindSelectedColliderCreator()
		{
			return SelectionUtil.FindSelectedHullPainter();
		}

		public void SyncTarget(ICreatorComponent currentHullPainter, MeshFilter selectedMeshFilter)
		{
			this.currentHullPainter = currentHullPainter as RigidColliderCreator;
			this.selectedMeshFilter = selectedMeshFilter;

			if (currentHullPainter != null)
				EditorUtils.UpgradeStoredData(this.currentHullPainter);
		}

		public bool HasData()
		{
			return (currentHullPainter != null && currentHullPainter.paintingData != null);
		}

		public IHull GetActiveHull()
		{
			Hull hull = currentHullPainter.paintingData.GetActiveHull();
			return hull;
		}

		public void PaintAllFaces()
		{
			if (currentHullPainter == null || currentHullPainter.paintingData == null || currentHullPainter.paintingData.GetActiveHull() == null)
				return;

			CommonController.SelectAllFaces(currentHullPainter.paintingData.GetActiveHull(), selectedMeshFilter.sharedMesh);
		}

		public void UnpaintAllFaces()
		{
			if (currentHullPainter == null || currentHullPainter.paintingData == null)
				return;

			CommonController.UnpaintAllFaces(currentHullPainter.paintingData.GetActiveHull(), selectedMeshFilter.sharedMesh);
		}

		public void PaintUnpaintedFaces()
		{
			if (currentHullPainter == null || currentHullPainter.paintingData == null || currentHullPainter.paintingData.GetActiveHull() == null)
				return;

			Hull hull = currentHullPainter.paintingData.GetActiveHull();
			CommonController.PaintUnpaintedFaces(hull, selectedMeshFilter.sharedMesh);
		}

		public void PaintRemainingFaces()
		{
			if (currentHullPainter == null || currentHullPainter.paintingData == null || currentHullPainter.paintingData.GetActiveHull() == null)
				return;

			Hull hull = currentHullPainter.paintingData.GetActiveHull();
			CommonController.PaintRemainingFaces(hull, currentHullPainter.paintingData.hulls.ToArray(), selectedMeshFilter.sharedMesh);
		}

		public void GrowPaintedFaces()
		{
			if (currentHullPainter == null || currentHullPainter.paintingData == null || currentHullPainter.paintingData.GetActiveHull() == null)
				return;

			Hull hull = currentHullPainter.paintingData.GetActiveHull();
			CommonController.GrowPaintedFaces(hull, selectedMeshFilter.sharedMesh);
		}

		public void ShrinkPaintedFaces()
		{
			if (currentHullPainter == null || currentHullPainter.paintingData == null || currentHullPainter.paintingData.GetActiveHull() == null)
				return;

			Hull hull = currentHullPainter.paintingData.GetActiveHull();
			CommonController.ShrinkPaintedFaces(hull, selectedMeshFilter.sharedMesh);
		}

		public void AddToSelection(IHull hull, int hitTriIndex)
		{
			// TODO: Ugly cast here
			Hull h = hull as Hull;
			h.AddToSelection(hitTriIndex, currentHullPainter.paintingData.sourceMesh);
		}

		public void RemoveFromSelection(IHull hull, int hitTriIndex)
		{
			// TODO: Ugly cast here
			Hull h = hull as Hull;
			h.RemoveFromSelection(hitTriIndex, currentHullPainter.paintingData.sourceMesh);
		}

		public void DisconnectAssets(bool deleteChildComponents)
		{
			if (currentHullPainter != null)
			{
				Undo.SetCurrentGroupName("Disconnect Rigid Collider Creator");

				// First delete child components if needed
				if (deleteChildComponents)
				{
					foreach (RigidColliderCreatorChild child in currentHullPainter.GetComponentsInChildren<RigidColliderCreatorChild>())
					{
						Undo.DestroyObjectImmediate(child);
					}
				}

				// Then destroy the actual hull painter component
				Undo.DestroyObjectImmediate(currentHullPainter);

				Undo.IncrementCurrentGroup();

				currentHullPainter = null;
			}
		}

		public bool TryPipetteSelection(int hitTriIndex)
		{
			for (int i = 0; i < currentHullPainter.paintingData.hulls.Count; i++)
			{
				Hull hull = currentHullPainter.paintingData.hulls[i];
				if (hull.IsTriangleSelected(hitTriIndex, null, null))
				{
					// Now painting this hull!
					currentHullPainter.paintingData.activeHull = i;
					return true;
				}
			}
			return false;
		}

		public void RecordPaintingChange(string label)
		{
			Undo.RecordObject(currentHullPainter.paintingData, label);
		}

		public void MarkPaintingDirty()
		{
			EditorUtility.SetDirty(currentHullPainter.paintingData);
		}

		
		public bool HasActiveHull()
		{
			return currentHullPainter.paintingData.HasActiveHull();
		}

		public void ClearActiveHull()
		{
			currentHullPainter.paintingData.activeHull = -1;
		}
	}

} // namespace Technie.PhysicsCreator