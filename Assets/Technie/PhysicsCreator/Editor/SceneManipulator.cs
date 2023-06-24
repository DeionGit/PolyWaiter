using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator
{
	public enum PickMode
	{
		Additive,
		Subtractive,
		Undecided
	}

	public enum ToolSelection
	{
		TrianglePainting,
		Pipette
	}

	public enum PaintingBrush
	{
		Precise,
		Small,
		Medium,
		Large
	}

	public class SceneManipulator
	{
		private ICreatorComponent currentHullPainter;

		private ToolSelection currentToolSelection = ToolSelection.TrianglePainting;
		private PaintingBrush currentBrushSize = PaintingBrush.Precise;

		private bool isSelectingFaces;
		private bool isXrayOn;
		private PickMode pickMode;

		private EditorWindow parentWindow;

		private int activeMouseButton = -1;

		private ITrianglePicker picker;
		private ISceneOverlay overlay;
		private IHullController controller;

		private bool isInEditMode;

		public SceneManipulator(EditorWindow parentWindow, ITrianglePicker picker, ISceneOverlay overlay, IHullController controller)
		{
			this.parentWindow = parentWindow;
			this.picker = picker;
			this.overlay = overlay;
			this.controller = controller;

			isInEditMode = !EditorApplication.isPlayingOrWillChangePlaymode;
#if UNITY_2017_4_OR_NEWER
			EditorApplication.playModeStateChanged += LogPlayModeState;
#endif
		}

		public void Destroy()
		{
			InternalDestroy();
		}
		
		private void InternalDestroy()
		{
			//Console.output.Log("SceneManipulator.InternalDestroy");

			// Destroy all temporary objects
			
			overlay.Destroy();

			picker.Destroy();

			SceneView.RepaintAll();
		}

#if UNITY_2017_4_OR_NEWER
		private void LogPlayModeState(PlayModeStateChange state)
		{
			isInEditMode = (state == PlayModeStateChange.EnteredEditMode);

			Console.output.Log("SCENE_MANIP    PlayMode State now:" + state + "    |    isPlayingOrWillChange: " + EditorApplication.isPlayingOrWillChangePlaymode+"    this.isInEditMode: "+isInEditMode);
		}
#endif

		public void DisconnectAssets (bool deleteChildComponents)
		{
			controller.DisconnectAssets(deleteChildComponents);
		}

		public ToolSelection GetCurrentTool()
		{
			return this.currentToolSelection;
		}

		public PaintingBrush GetCurrentBrush()
		{
			return this.currentBrushSize;
		}

		public void SetTool(ToolSelection sel)
		{
			this.currentToolSelection = sel;
		}

		public void SetBrush(PaintingBrush brush)
		{
			this.currentBrushSize = brush;
		}

		public void SetXRayOn(bool isOn)
		{
			this.isXrayOn = isOn;
		}

		public bool IsXrayOn()
		{
			return this.isXrayOn;
		}

		public int GetBrushPixelSize()
		{
			switch (currentBrushSize)
			{
				case PaintingBrush.Precise:
					{
						return 0;
					}
				case PaintingBrush.Small:
					{
						return 8;
					}
				case PaintingBrush.Medium:
					{
						return 20;
					}
				case PaintingBrush.Large:
					{
						return 32;
					}
			}
			return 0;
		}

		public void AdvanceBrushSize()
		{
			if (GetCurrentTool() != ToolSelection.TrianglePainting)
			{
				SetTool(ToolSelection.TrianglePainting);
			}
			else
			{
				PaintingBrush brush = GetCurrentBrush();
				if (brush == PaintingBrush.Precise)
					brush = PaintingBrush.Small;
				else if (brush == PaintingBrush.Small)
					brush = PaintingBrush.Medium;
				else if (brush == PaintingBrush.Medium)
					brush = PaintingBrush.Large;
				else if (brush == PaintingBrush.Large)
					brush = PaintingBrush.Precise;

				SetBrush(brush);
			}
		}

		public void ProcessSceneEvents(UnpackedMesh unpackedMesh)
		{
			if (this.Sync())
			{
				Console.output.Log("Repaint from ProcessSceneEvents");
				parentWindow.Repaint();
			}

			int controlId = GUIUtility.GetControlID(FocusType.Passive);

			if (Event.current.type == EventType.MouseDown && (Event.current.button == 0) && !Event.current.alt)
			{
				// If shift is held then always add, if control then always subtract, otherwise use intelligent pick mode
				//	PickMode mode = PickMode.Undecided;
				//	if (Event.current.shift)
				//		mode = PickMode.Additive;
				//	else if (Event.current.control)
				//		mode = PickMode.Subtractive;

				bool eventConsumed = this.DoMouseDown(unpackedMesh);
				if (eventConsumed)
				{
					activeMouseButton = Event.current.button;
					GUIUtility.hotControl = controlId;
					Event.current.Use();
				}
			}
			else if (Event.current.type == EventType.MouseDrag && Event.current.button == activeMouseButton && !Event.current.alt)
			{
				bool eventConsumed = this.DoMouseDrag(unpackedMesh);
				if (eventConsumed)
				{
					GUIUtility.hotControl = controlId;
					Event.current.Use();
					Console.output.Log("Repaint from DoMouseDrag");
					parentWindow.Repaint();
				}
			}
			else if (Event.current.type == EventType.MouseUp && Event.current.button == activeMouseButton && !Event.current.alt)
			{
				bool eventConsumed = this.DoMouseUp();
				if (eventConsumed)
				{
					activeMouseButton = -1;
					GUIUtility.hotControl = 0;
					Event.current.Use();
				}
			}
		}

		public bool DoMouseDown(UnpackedMesh unpackedMesh)
		{
			// If shift is held then always add, if control then always subtract, otherwise use intelligent pick mode
			PickMode initialMode = PickMode.Undecided;
			if (Event.current.shift)
				initialMode = PickMode.Additive;
			else if (Event.current.control)
				initialMode = PickMode.Subtractive;

			if (currentToolSelection == ToolSelection.TrianglePainting)
			{
				if (picker.HasValidTarget())
				{
					if (controller.HasData())
					{
						controller.RecordPaintingChange("Paint Hull");

						pickMode = PickTriangle(initialMode, unpackedMesh);
						if (pickMode != PickMode.Undecided)
						{
							//Console.output.Log ("Start drag");

							Sync();

							controller.MarkPaintingDirty();

							isSelectingFaces = true;

							return true;
						}
						else
						{
							//Console.output.Log ("Abandon drag");
						}
					}
					else
					{
						// This can happen when unity triggers scene callbacks in an odd order and the currentHullPainter isn't set yet
						//Console.output.LogError("SceneManipulator has no currentHullPainter!");
					}
				}
				else
				{
					//Console.output.Log("Mouse down but no targetMeshCollider, ignoring");
				}
			}
			else if (currentToolSelection == ToolSelection.Pipette)
			{
				// Raycast against the target mesh collider and see if the triangle we hit is in any current hull

				bool anyFound = false;

				Ray pickRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

				int hitTriIndex;
				if (picker.Raycast(pickRay, unpackedMesh, out hitTriIndex))
				{
					if (controller.TryPipetteSelection(hitTriIndex))
					{
						currentToolSelection = ToolSelection.TrianglePainting;
						Console.output.Log("Repaint from PipetteSelection");
						parentWindow.Repaint();
						anyFound = true;
					}
				}

				if (anyFound)
				{
					return true;
				}
				else
				{
					controller.ClearActiveHull();
					return true;
				}
			}

			return false;
		}

		public bool DoMouseDrag(UnpackedMesh unpackedMesh)
		{
			if (isSelectingFaces)
			{
				controller.RecordPaintingChange("Paint Hull");

				PickTriangle(pickMode, unpackedMesh);
				
				overlay.SyncOverlay(currentHullPainter, picker.GetInputMesh());

				Console.output.Log("SceneManip.MarkPaintingDirty");
				controller.MarkPaintingDirty();

				return true;
			}
			return false;
		}

		public bool DoMouseUp()
		{
			if (isSelectingFaces)
			{
				isSelectingFaces = false;
				return true;
			}
			return false;
		}

		public T GetCreatorComponent<T>() where T : ICreatorComponent
		{
			if (currentHullPainter is T)
				return (T)currentHullPainter;
			return
				default(T);
		}

		public bool Sync()
		{
			//if (EditorApplication.isPlayingOrWillChangePlaymode)
			if (!isInEditMode)
			{
				// If we're in play mode then don't try and keep the overlay in sync
				// This also means we delete the overlay when entering playmode which stops unity getting confused about objects that shouldn't really exist
				InternalDestroy();
				return false;
			}
			
			ICreatorComponent selectedHullPainter = controller.FindSelectedColliderCreator();
			MeshFilter selectedMeshFilter = SelectionUtil.FindSelectedMeshFilter();
			Renderer selectedRenderer = SelectionUtil.FindSelectedRenderer();

			if (selectedHullPainter != null)
			{
				overlay.SyncParentChain(selectedHullPainter.GetGameObject());

				overlay.FindOrCreateOverlay();
				picker.FindOrCreatePickClone();

				picker.SyncPickClone(selectedRenderer);	// FIXME: Skinned meshes only have a SkinnedMeshRenderer, no MeshFilter component.
				overlay.SyncOverlay(selectedHullPainter, picker.GetInputMesh());
			}
			else
			{
				//Console.output.Log("SceneManip no selected component - do disable");

				picker.Disable();
				
				overlay.Disable();

				controller.Disable();

				SceneView.RepaintAll();
			}

			bool changed = false;
			if (!ReferenceEquals(currentHullPainter, selectedHullPainter))
			{
				if (selectedHullPainter != null)
					Console.output.Log("SceneManipulator now sync'd with " + selectedHullPainter);
				else
					Console.output.Log("SceneManipulator now sync'd with  NULL");

				currentHullPainter = selectedHullPainter;

				controller.SyncTarget(selectedHullPainter, selectedMeshFilter);

				changed = true;
			}

			return changed;
		}

		public void PaintAllFaces()
		{
			if (!picker.HasValidTarget())// || currentHullPainter == null || currentHullPainter.paintingData == null)
				return;

			controller.PaintAllFaces();
		}

		public void UnpaintAllFaces()
		{
			if (!picker.HasValidTarget())// || currentHullPainter == null || currentHullPainter.paintingData == null)
				return;

			controller.UnpaintAllFaces();
		}

		public void PaintUnpaintedFaces()
		{
			if (!picker.HasValidTarget())// || currentHullPainter == null || currentHullPainter.paintingData == null)
				return;

			controller.PaintUnpaintedFaces();
		}

		public void PaintRemainingFaces()
		{
			if (!picker.HasValidTarget())// || currentHullPainter == null || currentHullPainter.paintingData == null)
				return;

			controller.PaintRemainingFaces();
		}

		public void GrowPaintedFaces()
		{
			if (!picker.HasValidTarget())// || currentHullPainter == null || currentHullPainter.paintingData == null)
				return;

			controller.GrowPaintedFaces();
		}

		public void ShrinkPaintedFaces()
		{
			if (!picker.HasValidTarget())// || currentHullPainter == null || currentHullPainter.paintingData == null)
				return;

			controller.ShrinkPaintedFaces();
		}


		private PickMode PickTriangle(PickMode pickMode, UnpackedMesh unpackedMesh)
		{
			if (Camera.current == null)
				return PickMode.Undecided;

			Ray pickRay = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);

			int hitTriIndex;
			if (picker.Raycast(pickRay, unpackedMesh, out hitTriIndex))
			{
				if (controller.HasActiveHull())
				{
					IHull hull = controller.GetActiveHull();
					if (pickMode == PickMode.Additive)
					{
						controller.AddToSelection(hull, hitTriIndex);

						PickArea(hull, Event.current.mousePosition, true, unpackedMesh);

						return PickMode.Additive;
					}
					else if (pickMode == PickMode.Subtractive)
					{
						controller.RemoveFromSelection(hull, hitTriIndex);

						PickArea(hull, Event.current.mousePosition, false, unpackedMesh);

						return PickMode.Subtractive;
					}
					else if (pickMode == PickMode.Undecided)
					{
						if (hull.IsTriangleSelected(hitTriIndex, SelectionUtil.FindSelectedRenderer(), picker.GetInputMesh()))
						{
							controller.RemoveFromSelection(hull, hitTriIndex);

							PickArea(hull, Event.current.mousePosition, false, unpackedMesh);

							return PickMode.Subtractive;
						}
						else
						{
							controller.AddToSelection(hull, hitTriIndex);

							PickArea(hull, Event.current.mousePosition, true, unpackedMesh);

							return PickMode.Additive;
						}
					}
				}
			}

			return PickMode.Undecided;
		}

		private void PickArea (IHull hull, Vector2 clickPos, bool asAdditive, UnpackedMesh unpackedMesh)
		{
			int range = GetBrushPixelSize();
			int numRays = range + 1;

			for (int i = 0; i < numRays; i++)
			{
				Vector2 jitteredPos;

				if (i == 0)
					jitteredPos = clickPos;
				else
					jitteredPos = new Vector2 (clickPos.x + Random.Range (-range, range), clickPos.y + Random.Range (-range, range));

				Ray pickRay = HandleUtility.GUIPointToWorldRay (jitteredPos);

				int hitTriIndex;
				if (picker.Raycast(pickRay, unpackedMesh, out hitTriIndex))
				{
					if (asAdditive)
					{
						controller.AddToSelection(hull, hitTriIndex);
					}
					else
					{
						controller.RemoveFromSelection(hull, hitTriIndex);
					}
				}
			}
		}

		

		
		public Vector3[] GetTargetVertices()
		{
			return picker.GetTargetVertices();
		}

		public int[] GetTargetTriangles()
		{
			return picker.GetTargetTriangles();
		}		

		public void DrawCustomCursor()
		{
			MouseCursor cursor = MouseCursor.Arrow;

			if (isSelectingFaces)
			{
				if (pickMode == PickMode.Additive)
					cursor = MouseCursor.ArrowPlus;

				if (pickMode == PickMode.Subtractive)
					cursor = MouseCursor.ArrowMinus;
			}
			else
			{
				if (Event.current.shift)
					cursor = MouseCursor.ArrowPlus;
				if (Event.current.control)
					cursor = MouseCursor.ArrowMinus;
			}
			
			Rect sceneRect = new Rect(0, 0, SceneView.lastActiveSceneView.position.width, SceneView.lastActiveSceneView.position.height);
			EditorGUIUtility.AddCursorRect(sceneRect, cursor);
		}

		public void DrawBrushCursor()
		{
			if (Event.current.type == EventType.Repaint)
			{
				if (GetCurrentTool() == ToolSelection.TrianglePainting)
				{
					Handles.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);

					int pickRadius = GetBrushPixelSize();

					Ray centerRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					Ray rightRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition + new Vector2(pickRadius, 0.0f));
					Ray upRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition + new Vector2(0.0f, pickRadius));

					Vector3 centerPos = centerRay.origin + centerRay.direction;
					Vector3 upPos = upRay.origin + upRay.direction;
					Vector3 rightPos = rightRay.origin + rightRay.direction;

					Vector3 upVec = upPos - centerPos;
					Vector3 rightVec = rightPos - centerPos;

					List<Vector3> points = new List<Vector3>();

					int numSegments = 20;

					for (int i = 0; i < numSegments; i++)
					{
						float angle0 = (float)i / (float)numSegments * Mathf.PI * 2.0f;
						float angle1 = (float)(i + 1) / (float)numSegments * Mathf.PI * 2.0f;

						Vector3 p0 = centerPos + (rightVec * Mathf.Cos(angle0)) + (upVec * Mathf.Sin(angle0));
						Vector3 p1 = centerPos + (rightVec * Mathf.Cos(angle1)) + (upVec * Mathf.Sin(angle1));

						points.Add(p0);
						points.Add(p1);
					}

					Handles.DrawLines(points.ToArray());
				}
			}
		}

		
		public void DrawToolSelectionUi(Icons icons)
		{
			ToolSelection currentToolSelection = GetCurrentTool();
			PaintingBrush currentBrushSize = GetCurrentBrush();

			int pipetteId = (currentToolSelection == ToolSelection.Pipette ? 0 : -1);
			GUIContent pipette = new GUIContent(currentToolSelection == ToolSelection.Pipette ? icons.pipetteIcon : icons.pipetteIcon, "Pipette tool");
			int newPipetteId = GUILayout.Toolbar(pipetteId, new GUIContent[] { pipette }, GUI.skin.button, GUILayout.Width(30 + 4), GUILayout.Height(22 + 5));

			GUIContent[] brushIcons = new GUIContent[]
			{
				new GUIContent(icons.preciseBrushIcon, "Precise brush"),
				new GUIContent(icons.smallBrushIcon, "Small brush"),
				new GUIContent(icons.mediumBrushIcon, "Medium brush"),
				new GUIContent(icons.largeBrushIcon, "Large brush")
			};

			int brushId = (currentToolSelection == ToolSelection.TrianglePainting) ? (int)currentBrushSize : -1;
			int newBrushId = GUILayout.Toolbar(brushId, brushIcons, GUI.skin.button, GUILayout.Width(120 + 12), GUILayout.Height(22 + 5));

			if (newBrushId != brushId)
			{
				this.SetTool(ToolSelection.TrianglePainting);
				this.SetBrush((PaintingBrush)newBrushId);
			}
			else if (newPipetteId != pipetteId)
			{
				this.SetTool(newPipetteId == 0 ? ToolSelection.Pipette : ToolSelection.TrianglePainting);
			}

			if (GUILayout.Button(new GUIContent(icons.paintAllIcon, "Select all"), GUILayout.Width(30 + 4)))
			{
				PaintAllFaces();
			}

			if (GUILayout.Button(new GUIContent(icons.invertIcon, "Invert selection"), GUILayout.Width(34)))
			{
				PaintUnpaintedFaces();
			}

			if (GUILayout.Button(new GUIContent(icons.otherIcon, "Select unpainted faces"), GUILayout.Width(34)))
			{
				PaintRemainingFaces();
			}

			if (GUILayout.Button(new GUIContent(icons.growIcon, "Grow selection"), GUILayout.Width(34)))
			{
				GrowPaintedFaces();
			}

			if (GUILayout.Button(new GUIContent(icons.shrinkIcon, "Shrink selection"), GUILayout.Width(34)))
			{
				ShrinkPaintedFaces();
			}

			if (GUILayout.Button(new GUIContent(icons.paintNoneIcon, "Select none"), GUILayout.Width(30 + 4)))
			{
				UnpaintAllFaces();
			}
		}
		
		/*
		public void DrawGizmoDebug()
		{
			foreach (ResampleDebugInfo info in resampleDebug)
			{
				Handles.color = info.wasHit ? Color.green : Color.red;
				Handles.DrawSphere(0, info.ray.origin, Quaternion.identity, 0.005f);
				Handles.DrawLine(info.ray.origin, info.ray.origin + info.ray.direction * info.rayLength);

				if (info.wasHit)
				{
					Handles.color = Color.white;
					Handles.DrawSphere(0, info.ray.origin + info.ray.direction * info.intersectDist, Quaternion.identity, 0.005f);
				}
			}
		}
		*/
	}

} // namespace Technie.PhysicsCreator

