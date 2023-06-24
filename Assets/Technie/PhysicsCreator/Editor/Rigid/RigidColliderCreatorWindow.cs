
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;

using Technie.PhysicsCreator;
using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator.Rigid
{

	[Flags]
	public enum Collumn
	{
		None				= 0,
		Visibility			= 1 << 1,
		Name				= 1 << 2,
		Colour				= 1 << 3,
		Type				= 1 << 4,
		Material			= 1 << 5,
		IsChild				= 1 << 6,
		Inflate				= 1 << 7,
		BoxFitMethod		= 1 << 8,
		MaxPlanes			= 1 << 9,
		Trigger				= 1 << 10,
		Paint				= 1 << 11,
		Delete				= 1 << 12,
		All					= ~0
	}

	public class GridRect
	{
		public int row;
		public Collumn col;
		public Rect rect;
	}

	public class RigidColliderCreatorWindow : EditorWindow, ICreatorWindow
	{
		private static readonly Collumn[] COLLUMN_ORDER = new Collumn[]
		{
			Collumn.Visibility,
			Collumn.Name,
			Collumn.Colour,
			Collumn.Type,
			Collumn.Material,
			Collumn.Inflate,
			Collumn.BoxFitMethod,
			Collumn.MaxPlanes,
			Collumn.IsChild,
			Collumn.Trigger,
			Collumn.Paint,
			Collumn.Delete
		};

		public static bool IsOpen() { return isOpen; }
		public static RigidColliderCreatorWindow instance;
		private static bool isOpen;

		//private int activeMouseButton = -1;

		private bool repaintSceneView = false;
		private bool regenerateOverlay = false;
		private int hullToDelete = -1;

		private SceneManipulator sceneManipulator;

		public SceneManipulator SceneManipulator { get { return this.sceneManipulator;  } }
		public bool IsGeneratingColliders { get { return this.isGeneratingColliders; } }

		// Foldout visibility
		private bool areToolsFoldedOut = true;
		private bool areHullsFoldedOut = true;
		private bool areSettingsFoldedOut = true;
		private bool areDefaultsFoldedOut = true;
		private bool areVhacdSettingsFoldedOut = true;
		private bool areErrorsFoldedOut = true;
		private bool areAssetsFoldedOut = true;

		private static Collumn visibleCollumns = Collumn.Visibility | Collumn.Name | Collumn.Colour | Collumn.Type | Collumn.Material | Collumn.IsChild | Collumn.Trigger | Collumn.Paint | Collumn.Delete;

		private Vector2 scrollPosition;

		private HullType defaultType = HullType.ConvexHull;
		private PhysicMaterial defaultMaterial;
		private bool defaultIsChild;
		private bool defaultIsTrigger;
		private int defaultMaxPlanes = 255;

		private bool showWireframe = true;
		private float wireframeFactor = -1.0f;

		private bool dimInactiveHulls = true;
		private float dimFactor = 0.7f;

		private float globalHullAlpha = 0.6f;

		private GUIStyle foldoutStyle;

		private GUIStyle horizontalScrollStyle;

		private GUIStyle toolbarStyle;

		private List<GridRect> gridRects = new List<GridRect>();

		private Renderer cachedRenderer;
		private UnpackedMesh cachedUnpackedMesh;

		private bool isGeneratingColliders;

		// Button locations for integration tests
		//public static Rect generateAssetButtonRect;
		public static Vector2 generateAssetButtonCenter;
		public static Vector3 addHullButtonPos;

		[MenuItem("Window/Technie Collider Creator/Rigid Collider Creator", false)]
		public static void ShowWindow()
		{
			EditorWindow.GetWindow(typeof(RigidColliderCreatorWindow));
		}

		void OnEnable()
		{
			Console.output.Log("Technie Collider Creator - debug output enabled. Disable by setting IS_DEBUG_OUTPUT_ENABLED=false in Technie/PhysicsCreator/Scripts/Console.cs");

			isOpen = true;
			instance = this;

			this.titleContent = new GUIContent ("Rigid Collider Creator", Icons.Active.technieIcon, "Technie Rigid Collider Creator");

			sceneManipulator = new SceneManipulator(this, new RigidTrianglePicker(), new RigidSceneOverlay(this), new RigidHullController());

		}

		private void OnDisable()
		{
			Console.output.Log("RigidColliderCreatorWindow.OnDisable");
		}

		void OnDestroy()
		{
#if UNITY_2019_1_OR_NEWER
			SceneView.beforeSceneGui -= this.OnBeforeSceneGUI;
			SceneView.duringSceneGui -= this.OnDuringSceneGUI;
#else
			SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
#endif
			
			if (sceneManipulator != null)
			{
				sceneManipulator.Destroy();
				sceneManipulator = null;
			}

			isOpen = false;
			instance = null;
		}

		void OnFocus()
		{
			// Remove to make sure it's not added, then add it once
#if UNITY_2019_1_OR_NEWER
			SceneView.beforeSceneGui -= this.OnBeforeSceneGUI;
			SceneView.beforeSceneGui += this.OnBeforeSceneGUI;

			SceneView.duringSceneGui -= this.OnDuringSceneGUI;
			SceneView.duringSceneGui += this.OnDuringSceneGUI;
#else
			SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
			SceneView.onSceneGUIDelegate += this.OnSceneGUI;

			GizmoUtils.ToggleGizmos(true);
#endif
		}

		void OnSelectionChange()
		{
#if !UNITY_2019_1_OR_NEWER
			GizmoUtils.ToggleGizmos(true);
#endif

			if (sceneManipulator.Sync ())
			{
			//	Console.output.Log ("Changed");
			}

			// Always repaint as we need to change inactive gui
			Console.output.Log("Repaint from OnSelectionChanged");
			Repaint();
		}

		// Called from RigidColliderCreatorEditor
		public void OnInspectorGUI()
		{
			if (sceneManipulator.Sync ())
			{
				Console.output.Log("Repaint from OnInspectorGUI/Sync");
				Repaint();
			}
		}

		private void CreateStyles()
		{
			// Creating styles in OnEnable can throw NPEs if the window is docked
			// Instead it's more reliable to lazily init them just before we need them

			if (foldoutStyle == null)
			{
				foldoutStyle = new GUIStyle(EditorStyles.foldout);
				foldoutStyle.fontStyle = FontStyle.Bold;
			//	foldoutStyle.fixedHeight = 20;
			}

			if (horizontalScrollStyle == null)
			{
				horizontalScrollStyle = new GUIStyle(GUI.skin.horizontalScrollbar);
				horizontalScrollStyle.fixedHeight = horizontalScrollStyle.fixedWidth = 0;
			}

			if (toolbarStyle == null)
			{
				//toolbarStyle = new GUIStyle(UnityEditor.EditorStyles.miniButton);
				toolbarStyle = new GUIStyle(GUI.skin.button);
			//	toolbarStyle.margin.top -= 1;
			//	toolbarStyle.normal.background = null;
				
			}
		}

		void OnGUI ()
		{
			// Only sync on layout so ui gets same calls
			if (Event.current.type == EventType.Layout)
			{
				sceneManipulator.Sync ();
			}

			CreateStyles();

			repaintSceneView = false;
			regenerateOverlay = false;
			hullToDelete = -1;

			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			if (currentHullPainter != null && currentHullPainter.paintingData != null)
			{
				DrawActiveGui(currentHullPainter);
			}
			else
			{
				DrawInactiveGui();
			}
		}

		/** Gui drawn if the selected object has a vaild hull painter and initialised asset data
		 */
		private void DrawActiveGui(RigidColliderCreator currentHullPainter)
		{
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, horizontalScrollStyle, GUI.skin.verticalScrollbar);

			GUILayout.Space (10);

			DrawToolGui ();
			
			DrawHullGUI();

			DrawVhacdConfigGUI();
			DrawDefaultsGui();
			DrawSettingsGui();
			
			DrawHullWarnings (currentHullPainter);

			DrawAssetUi();
			
			if (currentHullPainter.paintingData.hulls.Count == 0)
			{
				GUILayout.Label("No hulls created. Add a hull to start painting.");
			}

			GUILayout.Space (16);
			
			GUILayout.EndScrollView ();

			// Now actually perform queued up actions

			if (hullToDelete != -1)
			{
				RemoveHull(hullToDelete);
			}

			if (regenerateOverlay)
				sceneManipulator.Sync (); // may need to explicitly resync overlay data?

			if (repaintSceneView)
			{
				Console.output.Log("Repaint from repaintSceneView flag");
				SceneView.RepaintAll();
			}
		}

		/** Gui drawn if the selected object does not have a valid and initialised hull painter on it
		 */
		private void DrawInactiveGui()
		{
			if (Selection.transforms.Length == 1)
			{
				// Have a single scene selection, is it viable?

				GameObject selectedObject = SelectionUtil.FindSelectedGameObject();
				MeshFilter srcMesh = SelectionUtil.FindSelectedMeshFilter();
				RigidColliderCreatorChild child = SelectionUtil.FindSelectedHullPainterChild();

				if (srcMesh != null)
				{
					GUILayout.Space(10);
					GUILayout.Label("Generate an asset to start painting:");
					DrawGenerateOrReconnectGui(selectedObject, srcMesh.sharedMesh);
				}
				else if (child != null)
				{
					GUILayout.Space(10);
					GUILayout.Label("Child hulls are not edited directly - select the parent to continue painting this hull");
				}
				else
				{
					// No mesh filter, might have a hull painter (or not)

					GUILayout.Space(10);
					GUILayout.Label("No MeshFilter selected", EditorStyles.boldLabel);

					if (selectedObject.GetComponent<SkinnedMeshRenderer>() != null)
					{
						GUILayout.Label("To continue select a single scene object with a MeshFilter component");
						GUILayout.BeginHorizontal();
						GUILayout.Label("or to create colliders for this skinned object use Skinned Collider Creator");
						if (GUILayout.Button("Open"))
						{
							Technie.PhysicsCreator.Skinned.SkinnedColliderCreatorWindow.ShowWindow();
						}
						GUILayout.EndHorizontal();
					}
					else
					{
						GUILayout.Label("To continue select a single scene object with a MeshFilter component");
					}
				}
			}
			else
			{
				// No single scene selection
				// Could be nothing selected
				// Could be multiple selection
				// Could be an asset in the project selected

				GUILayout.Space(10);
				GUILayout.Label("To start painting, select a single scene object");
				GUILayout.Label("The object must contain a MeshFilter");

				if (GUILayout.Button("Open quick start guide"))
				{
					string projectPath = Application.dataPath.Replace("Assets", "");
					string docsPdf = projectPath + CommonUi.FindInstallPath() + "Technie Collider Creator Readme.pdf";
					Application.OpenURL(docsPdf);
				}
			}
		}

		private void DrawToolGui()
		{
			areToolsFoldedOut = EditorGUILayout.Foldout(areToolsFoldedOut, new GUIContent("Tools", Icons.Active.toolsIcons), foldoutStyle);
			if (areToolsFoldedOut)
			{
				GUILayout.BeginHorizontal();
				{
					sceneManipulator.DrawToolSelectionUi(Icons.Active);

					GUILayout.FlexibleSpace();

					if (GUILayout.Button(new GUIContent(Icons.Active.generateIcon, "Generate colliders"), GUILayout.Width(34)))
					{
						GenerateColliders();
					}
					
					/* Removed because it's confusingly similar to DeleteGenerated and I'm not sure there's a real benifit to having both, but there's definite confusion to having two nearly identical buttons with subtle differences
					if (GUILayout.Button(new GUIContent(icons.deleteCollidersIcon, "Delete collider components"), GUILayout.Width(34)))
					{
						DeleteColliders();
					}
					*/

					//if (GUILayout.Button(new GUIContent(deleteCollidersIcon, "Delete generated colliders and game objects"), GUILayout.MinWidth(10)))
					if (GUILayout.Button(new GUIContent(Icons.Active.deleteCollidersIcon, "Delete generated colliders and game objects"), GUILayout.Width(34)))
					{
						DeleteGenerated();
					}
				}
				GUILayout.EndHorizontal();

			} // end foldout

			EditorUtils.DrawUiDivider();
		}

		private void ClearGridLayout()
		{
			gridRects.Clear();
		}

		private void InsertCell(int row, Collumn col, Rect rect)
		{
			GridRect r = new GridRect();
			r.row = row;
			r.col = col;
			r.rect = rect;
			this.gridRects.Add(r);
		}

		private Rect GetCellRect(int row, Collumn col)
		{
			foreach (GridRect r in gridRects)
			{
				if (r.row == row && r.col == col)
					return r.rect;
			}
			return new Rect();
		}

		private void DrawHullGUI()
		{
			areHullsFoldedOut = EditorGUILayout.Foldout(areHullsFoldedOut, new GUIContent("Hulls", Icons.Active.hullsIcon), foldoutStyle);
			if (areHullsFoldedOut)
			{
				RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

				// Figure out collumn widths based on which are actually visible

				Dictionary<Collumn, float> collumnWidths = new Dictionary<Collumn, float>();
				collumnWidths.Add(Collumn.Visibility,		IsCollumnVisible(Collumn.Visibility) ? 45 : 0);
				collumnWidths.Add(Collumn.Colour,			IsCollumnVisible(Collumn.Colour) ? 45 : 0);
				collumnWidths.Add(Collumn.Type,				IsCollumnVisible(Collumn.Type) ? 80 : 0);
				collumnWidths.Add(Collumn.Inflate,			IsCollumnVisible(Collumn.Inflate) ? 12+40 : 0);
				collumnWidths.Add(Collumn.BoxFitMethod,		IsCollumnVisible(Collumn.BoxFitMethod) ? 60 : 0);
				collumnWidths.Add(Collumn.MaxPlanes,		IsCollumnVisible(Collumn.MaxPlanes) ? 70 : 0);
				collumnWidths.Add(Collumn.IsChild,			IsCollumnVisible(Collumn.IsChild) ? 55 : 0);
				collumnWidths.Add(Collumn.Trigger,			IsCollumnVisible(Collumn.Trigger) ? 45 : 0);
				collumnWidths.Add(Collumn.Paint,			IsCollumnVisible(Collumn.Paint) ? 40 : 0);
				collumnWidths.Add(Collumn.Delete,			IsCollumnVisible(Collumn.Delete) ? 45 : 0);

				float fixedWidth = 0;
				int numOptional = 0;
				foreach (float width in collumnWidths.Values)
				{
					fixedWidth += width;
					if (width > 0)
						numOptional++;
				}
				fixedWidth += 20 * EditorGUIUtility.pixelsPerPoint; // extra space for window chrome
				fixedWidth += (numOptional * 4);
				if (IsCollumnVisible(Collumn.Material))
					fixedWidth += 4;
				
				float baseWidth = EditorGUIUtility.currentViewWidth;
				float flexibleWidth = baseWidth - fixedWidth;

				int numFlexible = 0;
				if (IsCollumnVisible(Collumn.Name))
					numFlexible++;
				if (IsCollumnVisible(Collumn.Material))
					numFlexible++;

				if (IsCollumnVisible(Collumn.Name))
					collumnWidths.Add(Collumn.Name, flexibleWidth / (float)numFlexible);
				else
					collumnWidths.Add(Collumn.Name, 0.0f);

				if (IsCollumnVisible(Collumn.Material))
					collumnWidths.Add(Collumn.Material, flexibleWidth / (float)numFlexible);
				else
					collumnWidths.Add(Collumn.Material, 0.0f);

				// Is there enough space (under optional collumns) to put the 'Add Hull' button inline, or does it need to go on a new line?
				bool putAddHullInline = IsCollumnVisible(Collumn.Name) || IsCollumnVisible(Collumn.Colour) || IsCollumnVisible(Collumn.Type) || IsCollumnVisible(Collumn.Material) || IsCollumnVisible(Collumn.Inflate) || IsCollumnVisible(Collumn.BoxFitMethod) || IsCollumnVisible(Collumn.MaxPlanes) || IsCollumnVisible(Collumn.IsChild) || IsCollumnVisible(Collumn.Trigger);

				// Build the grid of layout rects from the collumn widths

				ClearGridLayout();

				int numRows = currentHullPainter.paintingData.hulls.Count + 2; // +1 for collumn names, +1 for bottom buttons
				for (int row = 0; row < numRows; row++)
				{
					GUILayout.Label("");
					Rect rowRect = GUILayoutUtility.GetLastRect();

					float x = rowRect.x;
					for (int c=0; c < COLLUMN_ORDER.Length; c++)
					{
						Collumn col = COLLUMN_ORDER[c];

						if (IsCollumnVisible(col))
						{
							Rect gridRect = new Rect();
							gridRect.position = new Vector2(x, rowRect.y);
							gridRect.size = new Vector2(collumnWidths[col], rowRect.height);

							InsertCell(row, col, gridRect);

							x += collumnWidths[col] + 4;
						}
					}
				}

				// Collumn headings for the hull rows

				GUILayout.BeginHorizontal();
				{
					if (IsCollumnVisible(Collumn.Visibility))
						GUI.Label(GetCellRect(0, Collumn.Visibility), "Visible");

					if (IsCollumnVisible(Collumn.Name))
						GUI.Label(GetCellRect(0, Collumn.Name), "Name");

					if (IsCollumnVisible(Collumn.Colour))
						GUI.Label(GetCellRect(0, Collumn.Colour), "Colour");

					if (IsCollumnVisible(Collumn.Type))
						GUI.Label(GetCellRect(0, Collumn.Type), "Type");

					if (IsCollumnVisible(Collumn.Material))
						GUI.Label(GetCellRect(0, Collumn.Material), "Material");

					if (IsCollumnVisible(Collumn.Inflate))
						GUI.Label(GetCellRect(0, Collumn.Inflate), "Inflation");

					if (IsCollumnVisible(Collumn.BoxFitMethod))
						GUI.Label(GetCellRect(0, Collumn.BoxFitMethod), "Box Fit");

					if (IsCollumnVisible(Collumn.MaxPlanes))
						GUI.Label(GetCellRect(0, Collumn.MaxPlanes), "Max Planes");

					if (IsCollumnVisible(Collumn.IsChild))
						GUI.Label(GetCellRect(0, Collumn.IsChild), "As Child");

					if (IsCollumnVisible(Collumn.Trigger))
						GUI.Label(GetCellRect(0, Collumn.Trigger), "Trigger");

					if (IsCollumnVisible(Collumn.Paint))
						GUI.Label(GetCellRect(0, Collumn.Paint), "Paint");

					if (IsCollumnVisible(Collumn.Delete))
						GUI.Label(GetCellRect(0, Collumn.Delete), "Delete");
				}
				GUILayout.EndHorizontal();

				// The actual hull rows with all the data for an individual hull

				for (int i = 0; i < currentHullPainter.paintingData.hulls.Count; i++)
				{
					DrawHullGUILine(i, currentHullPainter.paintingData.hulls[i]);
				}

				// The row of macro buttons at the bottom of each hull collumn (Show all, Delete all, etc.)

				GUILayout.BeginHorizontal();
				{
					if (IsCollumnVisible(Collumn.Visibility))
					{
						bool allHullsVisible = AreAllHullsVisible();
						if (GUI.Button(GetCellRect(numRows-1, Collumn.Visibility), new GUIContent(" All", allHullsVisible ? Icons.Active.hullInvisibleIcon : Icons.Active.hullVisibleIcon), EditorStyles.miniButton))
						{
							if (allHullsVisible)
								SetAllHullsVisible(false); // Hide all
							else
								SetAllHullsVisible(true); // Show all
						}
					}

					if (putAddHullInline)
					{
						// If we're drawing the Add Hull button inline, then we don't want it tied to a collumn like other buttons because we always want it to be visible
						// We also want it to span multiple collumns if it needs to
						// We calculate it's position manually, then draw it via GUI.Button (rather than GUILayout.Button) so that we don't interupt the auto-layout of the rest of the grid

						Collumn colToUse = Collumn.Name;
						for (int i=1; i<COLLUMN_ORDER.Length; i++)
						{
							if (IsCollumnVisible(COLLUMN_ORDER[i]))
							{
								colToUse = COLLUMN_ORDER[i];
								break;
							}
						}
						Rect addRect = GetCellRect(numRows - 1, colToUse);
						addRect.size = new Vector2(70.0f, addRect.size.y);

						//if (GUI.Button(addRect, new GUIContent("Add Hull", Icons.Active.addHullIcon), EditorStyles.miniButton))
						if (ToolGUILayout.Button("addHull", addRect, new GUIContent("Add Hull", Icons.Active.addHullIcon), EditorStyles.miniButton))
						{
							AddHull();
						}
					}
					
					if (IsCollumnVisible(Collumn.Paint))
					{
						if (GUI.Button(GetCellRect(numRows-1, Collumn.Paint), "Stop", EditorStyles.miniButton))
						{
							StopPainting();
						}
					}

					if (IsCollumnVisible(Collumn.Delete))
					{
						if (GUI.Button(GetCellRect(numRows-1, Collumn.Delete), new GUIContent(" All", Icons.Active.deleteIcon), EditorStyles.miniButton))
						{
							DeleteHulls();
						}
					}
				}
				GUILayout.EndHorizontal();

				// If we didn't draw the Add Hull button inline, then make a new row for it and draw it here
				if (!putAddHullInline)
				{
					GUILayout.BeginHorizontal();

					//if (GUILayout.Button(new GUIContent("Add Hull", Icons.Active.addHullIcon), EditorStyles.miniButton, GUILayout.Width(70.0f)))
					if (ToolGUILayout.Button("addHull", new GUIContent("Add Hull", Icons.Active.addHullIcon), EditorStyles.miniButton, GUILayout.Width(70.0f)))
					{
						AddHull();
					}

					GUILayout.EndHorizontal();
				}
			}
			EditorUtils.DrawUiDivider();
		}

		private void DrawHullGUILine(int hullIndex, Hull hull)
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			int row = hullIndex + 1;

			Undo.RecordObject (currentHullPainter.paintingData, "Edit Hull");

			GUILayout.BeginHorizontal ();
			{
				if (IsCollumnVisible(Collumn.Visibility))
				{
					if (GUI.Button(GetCellRect(row, Collumn.Visibility), hull.isVisible ? Icons.Active.hullVisibleIcon : Icons.Active.hullInvisibleIcon, EditorStyles.miniButton))
						{
						hull.isVisible = !hull.isVisible;
						regenerateOverlay = true;
					}
				}

				if (IsCollumnVisible(Collumn.Name))
				{
					hull.name = EditorGUI.TextField(GetCellRect(row, Collumn.Name), hull.name);
				}


				if (IsCollumnVisible(Collumn.Colour))
				{
					Color prevColour = hull.colour;
#if UNITY_2017_4_OR_NEWER
					hull.colour = EditorGUI.ColorField(GetCellRect(row, Collumn.Colour), new GUIContent(""), hull.colour, true, false, false);
#else
					hull.colour = EditorGUI.ColorField(GetCellRect(row, Collumn.Colour), new GUIContent(""), hull.colour, true, false, false, new ColorPickerHDRConfig(0,0,0,0));
#endif
					if (prevColour != hull.colour)
					{
						regenerateOverlay = true;
						repaintSceneView = true;
					}
				}

				if (IsCollumnVisible(Collumn.Type))
				{
					hull.type = (HullType)EditorGUI.EnumPopup(GetCellRect(row, Collumn.Type), hull.type);
				}

				if (IsCollumnVisible(Collumn.Material))
				{
					hull.material = (PhysicMaterial)EditorGUI.ObjectField(GetCellRect(row, Collumn.Material), hull.material, typeof(PhysicMaterial), false);
				}

				if (IsCollumnVisible(Collumn.Inflate))
				{
					Rect baseRect = GetCellRect(row, Collumn.Inflate);
					Rect toggleRect = new Rect(baseRect.position, new Vector2(14.0f, baseRect.size.y));
					Rect amountRect = new Rect(baseRect.position + new Vector2(16.0f, 0.0f), baseRect.size - new Vector2(16.0f, 0.0f));

					hull.enableInflation = EditorGUI.Toggle(toggleRect, hull.enableInflation);

					hull.inflationAmount = EditorGUI.FloatField(amountRect, hull.inflationAmount);
				}

				if (IsCollumnVisible(Collumn.BoxFitMethod))
				{
					if (hull.type == HullType.Box)
					{
						GUIContent[] options = new GUIContent[3];
						options[0] = new GUIContent("Axis", Icons.Active.axisAlignedIcon);
						options[1] = new GUIContent("Tight", Icons.Active.minimizeVolumeIcon);
						options[2] = new GUIContent("Face", Icons.Active.alignFacesIcon);

						if (hull.isChildCollider)
						{
							int selected = EditorGUI.Popup(GetCellRect(row, Collumn.BoxFitMethod), (int)hull.boxFitMethod, options, EditorStyles.popup);
							hull.boxFitMethod = (BoxFitMethod)selected;
						}
						else
						{
							GUI.enabled = false;
							EditorGUI.Popup(GetCellRect(row, Collumn.BoxFitMethod), (int)BoxFitMethod.AxisAligned, options, EditorStyles.popup);
							GUI.enabled = true;
						}
					}
				}

				if (IsCollumnVisible(Collumn.MaxPlanes))
				{
					Rect baseRect = GetCellRect(row, Collumn.MaxPlanes);

					// Keep a small label prefix so users can scrub the value by dragging it
					float prevWidth = EditorGUIUtility.labelWidth;
					EditorGUIUtility.labelWidth = 12f;

					int currValue = hull.maxPlanes;
					int newValue = EditorGUI.IntField(baseRect, new GUIContent("#"), currValue);
					newValue = Mathf.Clamp(newValue, 6, 255);

					if (currValue != newValue)
					{
						hull.maxPlanes = newValue;
						EditorUtility.SetDirty(currentHullPainter.paintingData);
					}

					EditorGUIUtility.labelWidth = prevWidth;
				}

				if (IsCollumnVisible(Collumn.IsChild))
				{
					if (GUI.Button(GetCellRect(row, Collumn.IsChild), hull.isChildCollider ? Icons.Active.isChildIcon : Icons.Active.nonChildIcon, EditorStyles.miniButton))
					{
						hull.isChildCollider = !hull.isChildCollider;
					}
				}

				if (IsCollumnVisible(Collumn.Trigger))
				{
					if (GUI.Button(GetCellRect(row, Collumn.Trigger), hull.isTrigger ? Icons.Active.triggerOnIcon : Icons.Active.triggerOffIcon, EditorStyles.miniButton))
					{
						hull.isTrigger = !hull.isTrigger;
					}
				}

				if (IsCollumnVisible(Collumn.Paint))
				{
					int prevHullIndex = currentHullPainter.paintingData.activeHull;

					bool isPainting = (currentHullPainter.paintingData.activeHull == hullIndex);
					int nowSelected = GUI.Toolbar(GetCellRect(row, Collumn.Paint), isPainting ? 0 : -1, new Texture[] { isPainting ? Icons.Active.paintOnIcon : Icons.Active.paintOffIcon }, EditorStyles.miniButton);
					if (nowSelected == 0 && prevHullIndex != hullIndex)
					{
						// Now painting this index!
						currentHullPainter.paintingData.activeHull = hullIndex;
					}
				}

				if (IsCollumnVisible(Collumn.Delete))
				{
					if (GUI.Button(GetCellRect(row, Collumn.Delete), Icons.Active.deleteIcon, EditorStyles.miniButton))
					{
						hullToDelete = hullIndex;
						regenerateOverlay = true;
						repaintSceneView = true;
					}
				}
			}
			GUILayout.EndHorizontal ();
		}

		private void DrawVhacdConfigGUI()
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			bool hasAutoHulls = currentHullPainter.paintingData.HasAutoHulls();

			areVhacdSettingsFoldedOut = EditorGUILayout.Foldout(areVhacdSettingsFoldedOut, new GUIContent("Auto Hull Settings", Icons.Active.autoHullSettingsIcon), foldoutStyle);
			if (areVhacdSettingsFoldedOut)
			{
				GUI.enabled = hasAutoHulls;

				currentHullPainter.paintingData.autoHullPreset = (AutoHullPreset)EditorGUILayout.EnumPopup(new GUIContent("Preset"), currentHullPainter.paintingData.autoHullPreset);

				VhacdParameters vhacdParams = GetParameters(currentHullPainter.paintingData, currentHullPainter.paintingData.autoHullPreset);

				GUI.enabled = hasAutoHulls && currentHullPainter.paintingData.autoHullPreset == AutoHullPreset.Custom;

				float concavity = EditorUtils.RoundToTwoDecimalPlaces( Mathf.Clamp01(1.0f - vhacdParams.concavity) * 100.0f );
				concavity = EditorGUILayout.Slider(new GUIContent("Concavity", "Lower for more convex, higher for more concave"), concavity, 0f, 100.0f); // invert this. %age
				float newConcavity = 1.0f - (concavity / 100.0f);

				// Resolution / granularity has a huge range (1k to 50 million) and there's serious diminishing returns at the high end
				// (plus it takes a lot longer to process)
				// Remap the resolution into a non-linear 'granularity' where the slider has more low-end values than high end
				// We do this by converting the linear resolution to an exponential granularity [0..1] and then into a %age
				// Converting back is just the reverse: %age -> [0..1] -> resolution

				float granularity = EditorUtils.Remap((float)vhacdParams.resolution, 1000u, 50000000u, 0.0f, 1.0f);
				granularity = EditorUtils.RoundToTwoDecimalPlaces(EditorUtils.ToExponential(granularity) * 100.0f );
				granularity = EditorGUILayout.Slider(new GUIContent("Granularity", "Higher values are more sensitive to fine detail in the input, but also increases time to calculate the result"), granularity, 0f, 100f); // resolution - %age this [10,000 - 50,000,000]
				granularity = EditorUtils.FromExponential(granularity / 100.0f);
				uint newResolution = (uint)EditorUtils.Remap(granularity, 0.0f, 1.0f, 1000u, 50000000u);
								
				float smoothness = EditorUtils.RoundToTwoDecimalPlaces(EditorUtils.Remap(vhacdParams.minVolumePerCH, 0.0f, 0.02f, 100.0f, 0.0f) );
				smoothness = EditorGUILayout.Slider(new GUIContent("Smoothness", "Higher values generate higher poly output, lower values are blockier but more efficient"), smoothness, 0, 100);
				float newMinVolumePerCH = EditorUtils.Remap(smoothness, 0.0f, 100.0f, 0.02f, 0.0f);

				float symBias = EditorUtils.RoundToTwoDecimalPlaces(EditorUtils.Remap(vhacdParams.alpha, 0f, 1f, 0f, 100f) );
				symBias = EditorGUILayout.Slider(new GUIContent("Symmetry bias", "Bias the cut planes to symetric axies"), symBias, 0, 100);
				float newAlpha = EditorUtils.Remap(symBias, 0f, 100f, 0f, 1f);

				float revBias = EditorUtils.RoundToTwoDecimalPlaces(EditorUtils.Remap(vhacdParams.beta, 0f, 1f, 0f, 100f) );
				revBias = EditorGUILayout.Slider(new GUIContent("Revolution bias", "Bias the cut planes to revolution axies"), revBias, 0, 100);
				float newBeta = EditorUtils.Remap(revBias, 0f, 100f, 0f, 1f);

				uint newMaxConvexHulls = (uint)EditorGUILayout.IntSlider(new GUIContent("Max number of hulls", "The maximum number of hulls that the algorithm will target"), (int)vhacdParams.maxConvexHulls, 0, 1024);
				
				if (vhacdParams.concavity != newConcavity
					|| vhacdParams.resolution != newResolution
					|| vhacdParams.minVolumePerCH!= newMinVolumePerCH
					|| vhacdParams.alpha != newAlpha
					|| vhacdParams.beta != newBeta
					|| vhacdParams.maxConvexHulls != newMaxConvexHulls)
				{

					vhacdParams.concavity = newConcavity;
					vhacdParams.resolution = newResolution;
					vhacdParams.minVolumePerCH = newMinVolumePerCH;
					vhacdParams.alpha = newAlpha;
					vhacdParams.beta = newBeta;
					vhacdParams.maxConvexHulls = newMaxConvexHulls;

					currentHullPainter.paintingData.hasLastVhacdTimings = false;

					EditorUtility.SetDirty(currentHullPainter.paintingData);
				}

				if (GUILayout.Button("Reset to defaults"))
				{
					currentHullPainter.paintingData.vhacdParams = GetParameters(null, AutoHullPreset.Medium);
					currentHullPainter.paintingData.hasLastVhacdTimings = false;
					EditorUtility.SetDirty(currentHullPainter.paintingData);
				}

				GUI.enabled = true;
			}
			EditorUtils.DrawUiDivider();
		}

		private static VhacdParameters GetParameters(PaintingData paintingData, AutoHullPreset presetType)
		{
			VhacdParameters vhacdParams = new VhacdParameters();

			switch (presetType)
			{
				case AutoHullPreset.Low:
					vhacdParams = new VhacdParameters();
					vhacdParams.concavity = 0.01f;
					vhacdParams.resolution = 10000;
					vhacdParams.minVolumePerCH = 0.004f;
					vhacdParams.maxConvexHulls = 256;
					break;
				case AutoHullPreset.Medium:
					vhacdParams = new VhacdParameters();
					vhacdParams.concavity = 0.002f;
					vhacdParams.resolution = 100000;
					vhacdParams.minVolumePerCH = 0.002f;
					vhacdParams.maxConvexHulls = 512;
					break;
				case AutoHullPreset.High:
					vhacdParams = new VhacdParameters();
					vhacdParams.concavity = 0.000f;
					vhacdParams.resolution = 5000000;
					vhacdParams.minVolumePerCH = 0.001f;
					vhacdParams.maxConvexHulls = 1024;
					break;
				case AutoHullPreset.Placebo:
					vhacdParams = new VhacdParameters();
					vhacdParams.concavity = 0.000f;
					vhacdParams.resolution = 20000000;
					vhacdParams.minVolumePerCH = 0.000f;
					vhacdParams.maxConvexHulls = 1024;
					break;
				case AutoHullPreset.Custom:
					vhacdParams = paintingData.vhacdParams;
					break;
			}

			return vhacdParams;
		}

		private void DrawDefaultsGui()
		{
			areDefaultsFoldedOut = EditorGUILayout.Foldout(areDefaultsFoldedOut, new GUIContent("Defaults", Icons.Active.defaultsIcon), foldoutStyle);
			if (areDefaultsFoldedOut)
			{
				float[] collumnWidths = CommonUi.CalcSettingCollumns();

				DrawDefaultType(collumnWidths);
				DrawDefaultAsChild(collumnWidths);
				DrawDefaultTrigger(collumnWidths);
				DrawDefaultMaterial(collumnWidths);
				DrawDefaultMaxPlanes(collumnWidths);
				DrawFaceDepth(collumnWidths);
			}
			EditorUtils.DrawUiDivider();
		}

		private void DrawSettingsGui()
		{
			areSettingsFoldedOut = EditorGUILayout.Foldout(areSettingsFoldedOut, new GUIContent("Settings", Icons.Active.settingsIcon), foldoutStyle);
			if (areSettingsFoldedOut)
			{
				float[] collumnWidths = CommonUi.CalcSettingCollumns();

				DrawVisibilityToggles(collumnWidths);

				CommonUi.DrawWireframeUi(ref showWireframe, ref wireframeFactor, collumnWidths);
				CommonUi.DrawDimmingUi(ref dimInactiveHulls, ref dimFactor, collumnWidths);
				CommonUi.DrawHullAlphaUi(ref globalHullAlpha, collumnWidths);

				// TODO: EditorGUILayout.EnumFlagsField added in 2017.3 - use this for collumn visibility drop down instead of lots of tickboxes
				// Will need a bit of extra work so that we avoid showing the 'Paint' and 'Delete' options because we don't want the user to be able to toggle those
				/*
#if UNITY_2017_3_OR_NEWER
				GUILayout.BeginHorizontal();
				GUILayout.Label("Show columns:", GUILayout.Width(collumnWidths[0]));

				//EditorGUILayout.EnumFlagsField(Collumn.BoxFitMethod | Collumn.Colour, GUILayout.Width(100));
				visibleCollumns = (Collumn)EditorGUILayout.EnumFlagsField(visibleCollumns, GUILayout.Width(100));
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Reset", GUILayout.Width(collumnWidths[2])))
				{
					visibleCollumns = (Collumn)(-1);
				}
				GUILayout.EndHorizontal();
#endif
				*/
			}
			EditorUtils.DrawUiDivider();
		}



		private void DrawCollumnToggle(Collumn colType, string label, float width)
		{
			bool isVisible = IsCollumnVisible(colType);
			bool nowVisible = GUILayout.Toggle(isVisible, label, GUILayout.Width(width));
			if (nowVisible)
			{
				visibleCollumns |= colType;
			}
			else
			{
				visibleCollumns &= ~colType;
			}
		}

		private void DrawDefaultType (float[] collumnWidths)
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label("Default type", GUILayout.Width(collumnWidths[0]));

				defaultType = (HullType)EditorGUILayout.EnumPopup(defaultType, GUILayout.Width(100));

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Apply To All", GUILayout.Width(collumnWidths[2])) )
				{
					currentHullPainter.SetAllTypes(defaultType);
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawDefaultMaterial(float[] collumnWidths)
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Default material", GUILayout.Width(collumnWidths[0]));

				defaultMaterial = (PhysicMaterial)EditorGUILayout.ObjectField(defaultMaterial, typeof(PhysicMaterial), false);

				if (GUILayout.Button("Apply To All", GUILayout.Width(collumnWidths[2])))
				{
					currentHullPainter.SetAllMaterials(defaultMaterial);
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawDefaultAsChild(float[] collumnWidths)
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Default as child", GUILayout.Width(collumnWidths[0]));

				if (GUILayout.Button(defaultIsChild ? Icons.Active.isChildIcon : Icons.Active.nonChildIcon, GUILayout.Width(100)))
				{
					defaultIsChild = !defaultIsChild;
				}

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Apply To All", GUILayout.Width(collumnWidths[2])))
				{
					currentHullPainter.SetAllAsChild(defaultIsChild);
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawDefaultTrigger (float[] collumnWidths)
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label("Default trigger", GUILayout.Width(collumnWidths[0]));

				if (GUILayout.Button(defaultIsTrigger ? Icons.Active.triggerOnIcon : Icons.Active.triggerOffIcon, GUILayout.Width(100)))
				{
					defaultIsTrigger = !defaultIsTrigger;
				}

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Apply To All", GUILayout.Width(collumnWidths[2])))
				{
					currentHullPainter.SetAllAsTrigger(defaultIsTrigger);
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawFaceDepth (float[] collumnWidths)
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label("Face thickness", GUILayout.Width(collumnWidths[0]));

				currentHullPainter.paintingData.faceThickness = EditorGUILayout.FloatField(currentHullPainter.paintingData.faceThickness);

				float inc = 0.1f;
				if (GUILayout.Button("+", GUILayout.Width((collumnWidths[2]-4)/2)))
				{
					currentHullPainter.paintingData.faceThickness = currentHullPainter.paintingData.faceThickness + inc;
				}
				if (GUILayout.Button("-", GUILayout.Width((collumnWidths[2]-4)/2)))
				{
					currentHullPainter.paintingData.faceThickness = currentHullPainter.paintingData.faceThickness - inc;
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawDefaultMaxPlanes(float[] collumnWidths)
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Default max planes", GUILayout.Width(collumnWidths[0]));

				int curValue = this.defaultMaxPlanes;
				int newValue = EditorGUILayout.IntSlider(curValue, 6, 255);
				if (curValue != newValue)
				{
					this.defaultMaxPlanes = newValue;
				}

				if (GUILayout.Button("Apply To All", GUILayout.Width(collumnWidths[2])))
				{
					currentHullPainter.SetAllMaxPlanes(defaultMaxPlanes);
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawVisibilityToggles(float[] collumnWidths)
		{
			float toggleWidth = 80.0f;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Show columns", GUILayout.Width(collumnWidths[0]));
			DrawCollumnToggle(Collumn.Visibility, "Visibility", toggleWidth);
			DrawCollumnToggle(Collumn.Name, "Name", toggleWidth);
			DrawCollumnToggle(Collumn.Colour, "Colour", toggleWidth);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("", GUILayout.Width(collumnWidths[0]));
			DrawCollumnToggle(Collumn.Type, "Type", toggleWidth);
			DrawCollumnToggle(Collumn.Material, "Material", toggleWidth);
			DrawCollumnToggle(Collumn.Inflate, "Inflation", toggleWidth);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("", GUILayout.Width(collumnWidths[0]));
			DrawCollumnToggle(Collumn.BoxFitMethod, "Box Fit", toggleWidth);
			DrawCollumnToggle(Collumn.MaxPlanes, "Max Planes", toggleWidth);
			DrawCollumnToggle(Collumn.IsChild, "As Child", toggleWidth);
			DrawCollumnToggle(Collumn.Trigger, "Trigger", toggleWidth);
			GUILayout.EndHorizontal();
		}

		private void DrawHullWarnings (RigidColliderCreator currentHullPainter)
		{
			List<string> warnings = new List<string> ();

			for (int i=0; i<currentHullPainter.paintingData.hulls.Count; i++)
			{
				Hull hull = currentHullPainter.paintingData.hulls[i];
				
				if (hull.hasColliderError)
				{
					warnings.Add(string.Format("'{0}' generates a collider with {1} faces\n Unity only allows max 256 faces per hull - inflation has been enabled, adjust the inflation amount to simplify this further", hull.name, hull.numColliderFaces));
				}

				if (hull.noInputError)
				{
					warnings.Add(string.Format("'{0}' has no painted faces, so no colliders could be generated", hull.name));
				}
			}
			
			if (warnings.Count > 0)
			{
				areErrorsFoldedOut = EditorGUILayout.Foldout(areErrorsFoldedOut, new GUIContent("Warnings", Icons.Active.errorIcon), foldoutStyle);
				if (areErrorsFoldedOut)
				{
					foreach (string str in warnings)
					{
						GUILayout.Label(str, EditorStyles.wordWrappedLabel);
					}
				}
				EditorUtils.DrawUiDivider();
			}
		}

		private void DrawAssetUi()
		{
			areAssetsFoldedOut = EditorGUILayout.Foldout(areAssetsFoldedOut, new GUIContent("Assets", Icons.Active.assetsIcon), foldoutStyle);
			if (areAssetsFoldedOut)
			{
				RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

				string paintingPath = AssetDatabase.GetAssetPath(currentHullPainter.paintingData);
				GUILayout.Label("Painting data: "+paintingPath, EditorStyles.centeredGreyMiniLabel);
			
				string hullPath = AssetDatabase.GetAssetPath(currentHullPainter.hullData);
				GUILayout.Label("Hull data: "+hullPath, EditorStyles.centeredGreyMiniLabel);

				if (GUILayout.Button("Disconnect from assets"))
				{
					bool deleteChildren = !EditorUtility.DisplayDialog("Disconnect from assets",
													"Also delete child painter components?\n\n"
													+ "These are not needed but leaving them make it easier to reconnect the painting data in the future.",
													"Leave",    // ok option - returns true
													"Delete");	// cancel option - returns false

					sceneManipulator.DisconnectAssets(deleteChildren);

					currentHullPainter = null;
					repaintSceneView = true;
					regenerateOverlay = true;
				}
			}
		}

#if UNITY_2019_1_OR_NEWER
		public void OnBeforeSceneGUI(SceneView sceneView)
		{
			UnpackedMesh unpackedMesh = FindOrCreateUnpackedMesh();

			sceneManipulator.ProcessSceneEvents(unpackedMesh);
		}

		public void OnDuringSceneGUI(SceneView sceneView)
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			if (currentHullPainter != null && currentHullPainter.HasEditorData())
			{
				DrawWireframe();
				sceneManipulator.DrawCustomCursor();
				sceneManipulator.DrawBrushCursor();
			}
			//sceneManipulator.DrawGizmoDebug();
		}
#endif

		public void OnSceneGUI(SceneView sceneView)
		{
#if !UNITY_2019_1_OR_NEWER
			UnpackedMesh unpackedMesh = FindOrCreateUnpackedMesh();

			sceneManipulator.ProcessSceneEvents(unpackedMesh);

			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			if (currentHullPainter != null && currentHullPainter.HasEditorData())
			{
				DrawWireframe();
				sceneManipulator.DrawCustomCursor();
				sceneManipulator.DrawBrushCursor();
			}
#endif
		}

		private UnpackedMesh FindOrCreateUnpackedMesh()
		{
			// Is cached mesh still valid?
			Renderer selectedRenderer = SelectionUtil.FindSelectedRenderer();
			if (selectedRenderer != cachedRenderer)
			{
				// Recreate cached mesh
				cachedUnpackedMesh = UnpackedMesh.Create(selectedRenderer);
				cachedRenderer = selectedRenderer;
			}

			return cachedUnpackedMesh;
		}

		private void DrawWireframe()
		{
			if (!showWireframe)
				return;

			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			if (currentHullPainter != null && currentHullPainter.paintingData != null && currentHullPainter.paintingData.sourceMesh != null && Camera.current != null)
			{
				Mesh srcMesh = currentHullPainter.paintingData.sourceMesh;

				CommonUi.DrawWireframe(currentHullPainter.transform, srcMesh.vertices, srcMesh.triangles, wireframeFactor);
			}
		}

		public void AdvanceBrushSize()
		{
			sceneManipulator.AdvanceBrushSize();
		}

		public void SelectPipette()
		{
			sceneManipulator.SetTool(ToolSelection.Pipette);
		}

		public void PaintAllFaces()
		{
			sceneManipulator.PaintAllFaces();
		}

		public void UnpaintAllFaces()
		{
			sceneManipulator.UnpaintAllFaces();
		}

		public void PaintUnpaintedFaces()
		{
			sceneManipulator.PaintUnpaintedFaces();
		}

		public void PaintRemainingFaces()
		{
			sceneManipulator.PaintRemainingFaces();
		}

		public void GrowPaintedFaces()
		{
			sceneManipulator.GrowPaintedFaces();
		}

		public void ShrinkPaintedFaces()
		{
			sceneManipulator.ShrinkPaintedFaces();
		}


		// NB: Used by Shortcuts
		public void CycleHullType()
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			if (currentHullPainter.paintingData.activeHull == -1)
				return;

			Hull hull = currentHullPainter.paintingData.hulls[currentHullPainter.paintingData.activeHull];

			HullType type = hull.type;
			if (type == HullType.Box)
				type = HullType.ConvexHull;
			else if (type == HullType.ConvexHull)
				type = HullType.Sphere;
			else if (type == HullType.Sphere)
				type = HullType.Face;
			else if (type == HullType.Face)
				type = HullType.FaceAsBox;
			else if (type == HullType.FaceAsBox)
				type = HullType.Auto;
			else if (type == HullType.Auto)
				type = HullType.Capsule;
			else if (type == HullType.Capsule)
				type = HullType.Box;

			Undo.RecordObject(currentHullPainter.paintingData, "Change "+hull.name+" to "+type);
			hull.type = type;
			EditorUtility.SetDirty(currentHullPainter.paintingData);
		}
		

		public void ToggleIsTrigger()
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			if (currentHullPainter.paintingData.activeHull == -1)
				return;

			Hull hull = currentHullPainter.paintingData.hulls[currentHullPainter.paintingData.activeHull];

			Undo.RecordObject(currentHullPainter.paintingData, "Change " + hull.name + " as trigger to " + (hull.isTrigger ? "OFF" : "ON"));
			hull.isTrigger = !hull.isTrigger;
			EditorUtility.SetDirty(currentHullPainter.paintingData);
		}

		public void ToggleIsChild()
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			if (currentHullPainter.paintingData.activeHull == -1)
				return;

			Hull hull = currentHullPainter.paintingData.hulls[currentHullPainter.paintingData.activeHull];

			Undo.RecordObject(currentHullPainter.paintingData, "Change " + hull.name + " as child to " + (hull.isChildCollider ? "OFF" : "ON"));
			hull.isChildCollider = !hull.isChildCollider;
			EditorUtility.SetDirty(currentHullPainter.paintingData);
		}

		// NB: Used by Shortcuts
		public void AdvanceSelectedHull(int dir)
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			int active = currentHullPainter.paintingData.activeHull;
			if (active == -1)
				active = 0;
			else
			{
				active += dir;
				if (active < 0)
					active = currentHullPainter.paintingData.hulls.Count - 1;
				if (active >= currentHullPainter.paintingData.hulls.Count)
					active = 0;
			}

			currentHullPainter.paintingData.activeHull = active;
		}

		public void GenerateColliders()
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			if (currentHullPainter == null)
				return;

			// TODO: Don't trigger the generate routine if it's already running
			// ..

			VhacdParameters parameters = GetParameters(currentHullPainter.paintingData, currentHullPainter.paintingData.autoHullPreset);
			
			EditorCoroutines.Execute(GenerateCollidersRoutine(currentHullPainter, parameters));
		}

		private IEnumerator GenerateCollidersRoutine(RigidColliderCreator currentHullPainter, VhacdParameters parameters)
		{
			isGeneratingColliders = true;

			yield return null;

			Mesh[] autoHulls = null;

			DateTime startTime = DateTime.Now;
			float progress = 0.0f;

			// Do we need to run VHACD to generate auto hulls?
			if (currentHullPainter.paintingData.HasAutoHulls())
			{
				// Run VHACD in a background thread

				VhacdTask task = new VhacdTask();
				task.Init(currentHullPainter.paintingData.sourceMesh, parameters);
				task.Run();

				float expectedDuration = currentHullPainter.paintingData.hasLastVhacdTimings ? (currentHullPainter.paintingData.lastVhacdDurationSecs+30) : 45.0f;

				do
				{
					// Idea: Keep track of how long this took last run (for this mesh) and make the progress bar animate over the same time

					float duration = (float)(DateTime.Now - startTime).TotalSeconds;
					progress = Utils.TimeProgression(duration, expectedDuration);

					EditorUtility.DisplayProgressBar("Generating convex hulls",
													string.Format("Calculating... {0} seconds so far... {1}%", duration.ToString("0.0"), (progress*100).ToString("0.0")),
													progress);
					yield return null;
				}
				while (!task.IsFinished());

				task.Finalise();

				// TODO: Keep track of auto hulls in PaintingData? (or new AutoHullData?)
				// Also keep track of parameters used and time taken
				// Then avoid recalculating this if we've not changed mesh or parameters and just re-use it

				autoHulls = task.OutputHulls;
			}

			Undo.SetCurrentGroupName("Generate Colliders");
			Undo.RegisterCompleteObjectUndo (currentHullPainter.gameObject, "Generate");

			// Fetch the data assets

			PaintingData paintingData = currentHullPainter.paintingData;
			HullData hullData = currentHullPainter.hullData;

			string hullAssetPath = AssetDatabase.GetAssetPath (hullData);
			
			// Create / update the hull meshes

			foreach (Hull hull in paintingData.hulls)
			{
				hull.GenerateCollisionMesh(sceneManipulator.GetTargetVertices(), sceneManipulator.GetTargetTriangles(), autoHulls, paintingData.faceThickness);
			}

			// Sync the in-memory hull meshes with the asset meshes in hullAssetPath

			List<Mesh> existingMeshes = GetAllMeshesInAsset (hullAssetPath);

			foreach (Mesh existing in existingMeshes)
			{
				if (!paintingData.ContainsMesh(existing))
				{
					GameObject.DestroyImmediate(existing, true);
				}
			}

			foreach (Hull hull in paintingData.hulls)
			{
				if (hull.collisionMesh != null)
				{
					if (!existingMeshes.Contains(hull.collisionMesh))
					{
						AssetDatabase.AddObjectToAsset(hull.collisionMesh, hullAssetPath);
					}
				}
				if (hull.faceCollisionMesh != null)
				{
					if (!existingMeshes.Contains(hull.faceCollisionMesh))
					{
						AssetDatabase.AddObjectToAsset(hull.faceCollisionMesh, hullAssetPath);
					}
				}

				if (hull.autoMeshes != null)
				{
					foreach (Mesh auto in hull.autoMeshes)
					{
						if (!existingMeshes.Contains(auto))
						{
							AssetDatabase.AddObjectToAsset(auto, hullAssetPath);
						}
					}
				}
			}

			
			EditorUtility.SetDirty (hullData);

			AssetDatabase.SaveAssets ();

			// Add collider components to the target object

			currentHullPainter.CreateColliderComponents (autoHulls);

			EditorUtility.SetDirty(currentHullPainter);

			// Zip the progress bar up to 100% to finish it, otherwise it disappears before reaching the end and that looks broken

			int numSteps = progress < 50 ? 10 : 30; // If we've not made it to 50%, do a quick zip, otherwise do a slightly longer one
			float inc = (1.0f - progress) / numSteps;
			for (int i=0; i<numSteps; i++)
			{
				progress += inc;
				float duration = (float)(DateTime.Now - startTime).TotalSeconds;
				EditorUtility.DisplayProgressBar("Generating convex hulls",
													string.Format("Calculating... {0} seconds so far... {1}%", duration.ToString("0.0"), (progress * 100).ToString("0.0")),
													progress);
				yield return null;
			}

			// Output overal stats to the console (todo: move this to an output section in the window)

			float totalDurationSecs = (float)(DateTime.Now - startTime).TotalSeconds;
			int numColliders = paintingData.TotalOutputColliders;
			Console.output.Log(string.Format("Collider Creator created {0} colliders in {1} seconds", numColliders, totalDurationSecs.ToString("0.00")));

			// Finished! hide the progress bar
			UnityEditor.EditorUtility.ClearProgressBar();

			isGeneratingColliders = false;
		}

		public Hull AddHull()
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			Hull result = null;

			if (currentHullPainter != null)
			{
				Undo.RecordObject (currentHullPainter.paintingData, "Add Hull");
				result = currentHullPainter.paintingData.AddHull(defaultType, defaultMaterial, defaultIsChild, defaultIsTrigger);

				EditorUtility.SetDirty (currentHullPainter.paintingData);
			}

			return result;
		}

		public void DeleteActiveHull()
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			RemoveHull(currentHullPainter.paintingData.activeHull);
		}

		private void RemoveHull(int hullIndex)
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			Undo.RecordObject(currentHullPainter.paintingData, "Delete Hull");
			currentHullPainter.paintingData.RemoveHull(hullIndex);

			EditorUtility.SetDirty(currentHullPainter.paintingData);
		}

		private void SetAllHullsVisible(bool visible)
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			if (currentHullPainter != null && currentHullPainter.paintingData != null)
			{
				for (int i=0; i<currentHullPainter.paintingData.hulls.Count; i++)
				{
					currentHullPainter.paintingData.hulls[i].isVisible = visible;
				}
			}

			regenerateOverlay = true;
		}

		public void StopPainting()
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			if (currentHullPainter != null && currentHullPainter.paintingData != null)
			{
				currentHullPainter.paintingData.activeHull = -1;
			}
		}

		/** Just deletes generated colliders - leaves created child GOs and RigidColliderCreatorChild components */
		private void DeleteColliders()
		{
			Undo.SetCurrentGroupName ("Delete Colliders");

			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			currentHullPainter.RemoveAllColliders ();
		}

		/** Delete *all* generated objects and components. Ie. colliders, created child GOs, and RigidColliderCreatorChild components */
		public void DeleteGenerated()
		{
			Undo.SetCurrentGroupName("Delete Generated Objects");

			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			currentHullPainter.RemoveAllGenerated ();
		}

		private void DeleteHulls ()
		{
			Undo.SetCurrentGroupName("Delete All Hulls");

			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			if (currentHullPainter != null && currentHullPainter.hullData != null)
			{
				currentHullPainter.paintingData.RemoveAllHulls ();
				repaintSceneView = true;
			}
		}

		private bool AreAllHullsVisible()
		{
			bool allVisible = true;

			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();
			if (currentHullPainter != null && currentHullPainter.paintingData != null)
			{
				for (int i = 0; i < currentHullPainter.paintingData.hulls.Count; i++)
				{
					if (!currentHullPainter.paintingData.hulls[i].isVisible)
					{
						allVisible = false;
						break;
					}
				}
			}

			return allVisible;
		}

		private bool IsCollumnVisible(Collumn col)
		{
			return (visibleCollumns & col) > 0;
		}

		private static List<Mesh> GetAllMeshesInAsset(string assetPath)
		{
			List<Mesh> meshes = new List<Mesh> ();

			foreach (UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath(assetPath))
			{
				if (o is Mesh)
				{
					meshes.Add((Mesh)o);
				}
			}

			return meshes;
		}

		public bool ShouldDimInactiveHulls()
		{
			return dimInactiveHulls;
		}

		public float GetInactiveHullDimFactor()
		{
			return dimFactor;
		}

		public float GetGlobalHullAlpha()
		{
			return globalHullAlpha;
		}

		public bool ShouldReceiveShortcuts()
		{
			RigidColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<RigidColliderCreator>();

			return (currentHullPainter != null && currentHullPainter.paintingData != null);
		}

		public static void DrawGenerateOrReconnectGui(GameObject selectedObject, Mesh srcMesh)
		{
			//if (GUILayout.Button("Generate Asset"))
			if (ToolGUILayout.Button("Generate Asset", ref generateAssetButtonCenter))
			{
				GenerateAsset(selectedObject, srcMesh);
			}
			//CaptureButtonPosition(ref generateAssetButtonCenter);

			GUILayout.Label("Or reconnect to existing asset:");

			PaintingData newPaintingData = (PaintingData)EditorGUILayout.ObjectField(null, typeof(PaintingData), false);
			if (newPaintingData != null)
			{
				Reconnect(selectedObject, newPaintingData);
			}
		}

		/*

		public static void CaptureButtonPosition(ref Vector2 buttonPos)
		{
			if (Event.current.type == EventType.Repaint)
			{
				Rect lastRect = GUILayoutUtility.GetLastRect();
				buttonPos = GUIUtility.GUIToScreenPoint(lastRect.center);
			}
		}
		*/

		public static PaintingData GenerateAsset(GameObject selectedObject, Mesh srcMesh)
		{
			Console.output.Log("GenerateAsset(" + selectedObject + ", " + srcMesh + ")");

			string path = CommonUi.FindDataPath();

			// Find suitable asset names
			string paintAssetName, hullAssetName;
			CreateAssetPaths(path, selectedObject.name, out paintAssetName, out hullAssetName);

			// Painting asset
			PaintingData painting = ScriptableObject.CreateInstance<PaintingData>();
			painting.sourceMesh = srcMesh;
			AssetDatabase.CreateAsset(painting, paintAssetName);

			// Mesh asset
			HullData hulls = ScriptableObject.CreateInstance<HullData>();
			AssetDatabase.CreateAsset(hulls, hullAssetName);

			// Connect the painting data to the hull data

			painting.hullData = hulls;

			// Get the hull painter (or create one if it doesn't exist)

			RigidColliderCreator selectedPainter = selectedObject.GetComponent<RigidColliderCreator>();
			if (selectedPainter == null)
				selectedPainter = selectedObject.AddComponent<RigidColliderCreator>();

			// Point the painter at the asset data

			selectedPainter.paintingData = painting;
			selectedPainter.hullData = hulls;

			// Start with a single empty hull
			selectedPainter.paintingData.AddHull(HullType.ConvexHull, null, false, false);

			EditorUtility.SetDirty(painting);
			EditorUtility.SetDirty(hulls);

			// Ping the painting asset in the ui (can only ping one object at once, so do the more important one)
			EditorGUIUtility.PingObject(painting);

			// Open the creator window
			EditorWindow.GetWindow(typeof(RigidColliderCreatorWindow));

			// Force a Sync() so that API usage doesn't have to wait a frame
			if (RigidColliderCreatorWindow.instance != null)
				RigidColliderCreatorWindow.instance.SceneManipulator.Sync();

			return painting;
		}

		private static void CreateAssetPaths(string basePath, string baseName, out string paintingAssetPath, out string hullAssetPath)
		{
			paintingAssetPath = basePath + baseName + " Painting Data.asset";
			hullAssetPath = basePath + baseName + " Hull Data.asset";

			int nextNumber = 0;

			while (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(paintingAssetPath)) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(hullAssetPath)))
			{
				nextNumber++;
				paintingAssetPath = basePath + baseName + " " + nextNumber + " Painting Data.asset";
				hullAssetPath = basePath + baseName + " " + nextNumber + " Hull Data.asset";
			}
		}

		public static void Reconnect(GameObject selectedObject, PaintingData newPaintingData)
		{
			//Console.output.Log("Reconnect "+selectedObject.name+" to "+newPaintingData.name);

			// Get the hull painter (or create one if it doesn't exist)

			RigidColliderCreator hullPainter = selectedObject.GetComponent<RigidColliderCreator>();
			if (hullPainter == null)
				hullPainter = selectedObject.AddComponent<RigidColliderCreator>();

			// Point the hull painter at the assets

			hullPainter.paintingData = newPaintingData;
			hullPainter.hullData = newPaintingData.hullData;

			EditorWindow.GetWindow(typeof(RigidColliderCreatorWindow)).Repaint();
		}
	}

} // namespace Technie.PhysicsCreator
