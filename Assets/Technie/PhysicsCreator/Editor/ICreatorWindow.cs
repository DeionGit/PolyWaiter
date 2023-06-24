using UnityEngine;

namespace Technie.PhysicsCreator
{
	public interface ICreatorWindow
	{
		bool ShouldReceiveShortcuts();

		// Tool selection
		void SelectPipette();
		void AdvanceBrushSize();
		void StopPainting();

		// Toolbar actions
		void PaintAllFaces();
		void UnpaintAllFaces();
		void PaintUnpaintedFaces();
		void PaintRemainingFaces();
		void GrowPaintedFaces();
		void ShrinkPaintedFaces();
		
		// More toolbar actions
		void GenerateColliders();
		void DeleteGenerated();

		
		void Repaint();
	}
	
} // namespace Technie.PhysicsCreator
