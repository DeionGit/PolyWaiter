using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Technie.PhysicsCreator.QHull;


namespace Technie.PhysicsCreator.Skinned
{

	public class HierarchyElement
	{
		public string name;
		public HierarchyElement parentElement;
		public Transform srcBone;
		public BoneData srcBoneData;
		public BoneHullData srcHull;
		public int indentLevel;
		public bool hasChildren;

		public bool IsBone
		{
			get
			{
				return srcBone != null && srcHull == null;
			}
		}

		public bool IsHull
		{
			get
			{
				return srcHull != null;
			}
		}
	}

	public class SkinnedColliderCreatorWindow : EditorWindow, ICreatorWindow
	{
		// TODO: Apply a similar structure to RigidColliderCreatorWindow for TargetComponent/TargetRenderer/TargetEditorData

		private SkinnedColliderCreator TargetComponent
		{
			get
			{
				if (Selection.activeGameObject != null)
					return Selection.activeGameObject.GetComponent<SkinnedColliderCreator>();
				return null;
			}
		}

		private SkinnedMeshRenderer TargetRenderer
		{
			get
			{
				SkinnedColliderCreator target = TargetComponent;
				return target != null ? target.targetSkinnedRenderer : null;
			}
		}

		private SkinnedColliderEditorData TargetEditorData
		{
			get
			{
				SkinnedColliderCreator target = TargetComponent;
				return target != null ? target.editorData : null;
			}
		}

		private const int INDENT_AMOUNT = 22;

		private const float DEFAULT_MIN_THRESHOLD = 0.4f;
		private const float DEFAULT_MAX_THRESHOLD = 1.0f;

		public static bool IsOpen() { return isOpen; }
		public static SkinnedColliderCreatorWindow instance;
		private static bool isOpen;
		
		private SkinnedColliderCreator prevSelectedComponent;

		private Dictionary<Transform, bool> boneFoldoutState = new Dictionary<Transform, bool>();

		// Section foldout visibility
		private bool areToolsFoldedOut = true;
		private bool areHierarchyFoldedOut = true;
		private bool areSettingsFoldedOut = true;
		private bool areDefaultsFoldedOut = true;
		private bool arePropertiesFoldedOut = true;
		private bool areAssetsFoldedOut = true;

		private UnpackedMesh unpackedMesh;

		private Vector2 hierarchyScrollPosition = Vector2.zero;
		
		private GUIStyle foldoutStyle;

		private bool doAutoScrollToSelection;

		private bool dimInactiveHulls = true;
		private float dimFactor = 0.75f;

		private bool showWireframe = false;
		private float wireframeFactor = 0.6f;

		private float globalHullAlpha = 0.6f;

		private Vector2 scrollPosition;

		private bool hasScrollBar;

		private bool forceResyncNextSelectionChange;

		private SceneManipulator sceneManipulator;

		[MenuItem("Window/Technie Collider Creator/Skinned Collider Creator", false)]
		public static void ShowWindow()
		{
			EditorWindow.GetWindow<SkinnedColliderCreatorWindow>(false);
		}

		public SkinnedColliderCreatorWindow()
		{
#if UNITY_2017_4_OR_NEWER
			EditorApplication.playModeStateChanged += OnPlayModeChange;
#endif
		}

		void OnEnable()
		{
			isOpen = true;
			instance = this;

			this.titleContent = new GUIContent("Skinned Collider Creator", Icons.Active.technieIcon, "Technie Skinned Collider Creator");
			this.minSize = new Vector2(200, 200);
			this.wantsMouseMove = true;

			this.prevSelectedComponent = TargetComponent;

			Undo.undoRedoPerformed += OnUndo;

			sceneManipulator = new SceneManipulator(this, new RigidTrianglePicker(), new SkinnedSceneOverlay(this), new SkinnedHullController());
			
			// force a selection change so that unpackedMesh and overlays will be synced up
			forceResyncNextSelectionChange = true;
			OnSelectionChange();
		}

		void OnFocus()
		{
			// Remove to make sure it's not added, then add it once
#if UNITY_2019_3_OR_NEWER
			SceneView.duringSceneGui -= this.OnSceneGUI;
			SceneView.duringSceneGui += this.OnSceneGUI;
#else
			SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
			SceneView.onSceneGUIDelegate += this.OnSceneGUI;
#endif
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndo;

			isOpen = false;
		}

		void OnDestroy()
		{
#if UNITY_2019_3_OR_NEWER
			SceneView.duringSceneGui -= this.OnSceneGUI;
#else
			SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
#endif

#if UNITY_2017_4_OR_NEWER
			EditorApplication.playModeStateChanged -= OnPlayModeChange;
#endif

			if (sceneManipulator != null)
			{
				sceneManipulator.Destroy();
				sceneManipulator = null;
			}
		}

#if UNITY_2017_4_OR_NEWER
		private void OnPlayModeChange(PlayModeStateChange state)
		{
			// When we exit Play mode our prevSelectedComponent is invalid (because of C# Assembly unload/reload?) so we force a resync next selection change
			if (state == PlayModeStateChange.EnteredEditMode)
				forceResyncNextSelectionChange = true;
		}
#endif

		private void OnSelectionChange()
		{
			Console.output.Log("SkinnedWindow.OnSelectionChanged curr:"+TargetComponent+"  prev:"+prevSelectedComponent);

			if (TargetComponent != prevSelectedComponent || forceResyncNextSelectionChange)
			{
				prevSelectedComponent = TargetComponent;
				forceResyncNextSelectionChange = false;

				unpackedMesh = UnpackedMesh.Create(TargetRenderer);

				PopulateBoneData();

				if (sceneManipulator.Sync())
				{
					//Console.output.Log ("Changed");
				}

				EnsureHierarchySelectionVisible(null, null);
			}

			Repaint();
		}

		private void OnUndo()
		{
			if (TargetEditorData != null)
			{
				TargetEditorData.MarkDirty();
			}
		}

		// Called from RigidColliderCreatorEditor
		public void OnInspectorGUI()
		{
			if (sceneManipulator.Sync())
			{
				Repaint();
			}
		}

		void OnGUI()
		{
			// Only sync on layout so ui gets same calls
			if (Event.current.type == EventType.Layout)
			{
				sceneManipulator.Sync();
			}

			CreateStyles();

			if (Event.current.type == EventType.MouseMove)
				Repaint();
			
			if (TargetComponent != null && TargetEditorData != null)
			{
				BeginMainScrollView();
				{

					GUILayout.Space(10);

					DisplayToolBar();

					EditorUtils.DrawUiDivider(EditorUtils.DIVIDER_COLOUR, 1, 0);

					DrawBoneHierarchyUi();
					
					EditorUtils.DrawUiDivider(EditorUtils.DIVIDER_COLOUR, 1, 0);

					DrawPropertiesUi();

					EditorUtils.DrawUiDivider(EditorUtils.DIVIDER_COLOUR, 1, 0);

					DrawDefaultsUi();

					EditorUtils.DrawUiDivider(EditorUtils.DIVIDER_COLOUR, 1, 0);

					DrawSettingsUi();

					EditorUtils.DrawUiDivider(EditorUtils.DIVIDER_COLOUR, 1, 0);

					//DrawActionsUi(); // Nothing useful in here at the moment
					//EditorUtils.DrawUiDivider();

					DrawAssetUi();
				}
				EndMainScrollView();
			}
			else
			{
				DrawInactiveGui();
			}
		}

		private void DrawInactiveGui()
		{
			if (Selection.transforms.Length == 1)
			{
				// Have a single selection, is it viable?

				GameObject selectedObject = SelectionUtil.FindSelectedGameObject();
				SkinnedMeshRenderer skinnedRenderer = SelectionUtil.FindSelectedRenderer() as SkinnedMeshRenderer;
				MeshFilter filter = SelectionUtil.FindSelectedMeshFilter();

				if (skinnedRenderer != null)
				{
					GUILayout.Label("Generate an asset to start painting:");
					DrawGenerateOrReconnectGui(selectedObject, skinnedRenderer);
				}
				else
				{
					GUILayout.Space(10);
					GUILayout.Label("No SkinnedMeshRenderer selected", EditorStyles.boldLabel);

					if (filter != null)
					{
						GUILayout.Label("To continue select a single scene object with a SkinnedMeshRenderer component");
						GUILayout.BeginHorizontal();
						GUILayout.Label("or to create colliders for this rigid object use Rigid Collider Creator");
						if (GUILayout.Button("Open"))
						{
							Technie.PhysicsCreator.Rigid.RigidColliderCreatorWindow.ShowWindow();
						}
						GUILayout.EndHorizontal();
					}
					else
					{
						GUILayout.Label("To continue select a single scene object with a SkinnedMeshRenderer component");
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
				GUILayout.Label("The object must contain a SkinnedMeshRenderer");

				if (GUILayout.Button("Open quick start guide"))
				{
					string projectPath = Application.dataPath.Replace("Assets", "");
					string docsPdf = projectPath + CommonUi.FindInstallPath() + "Technie Collider Creator Readme.pdf";
					Application.OpenURL(docsPdf);
				}
			}
		}

		public static void DrawGenerateOrReconnectGui(GameObject selectedObject, SkinnedMeshRenderer skinnedRenderer)
		{
			if (GUILayout.Button("GenerateAsset"))
			{
				GenerateAsset(selectedObject, skinnedRenderer);
			}

			GUILayout.Label("Or reconnect to existing asset:");

			SkinnedColliderEditorData existingEditorData = (SkinnedColliderEditorData)EditorGUILayout.ObjectField(null, typeof(SkinnedColliderEditorData), false);
			if (existingEditorData != null)
			{
				Reconnect(selectedObject, existingEditorData);
			}
		}

		public static void GenerateAsset(GameObject selectedObject, SkinnedMeshRenderer skinnedRenderer)
		{
			if (skinnedRenderer.rootBone == null || skinnedRenderer.bones == null || skinnedRenderer.bones.Length == 0 || skinnedRenderer.sharedMesh.boneWeights.Length == 0)
			{
				string msg = "This model has incomplete skinning data, and cannot be used to create colliders.\n\n"
					+ "Either fix the model file to include bones and bone weights and try again.\n\n"
					+ "Or change the import settings to import as a non-skinned mesh (Import settings -> Rig -> set 'Animation Type' to 'None') and then use Rigid Collider Creator component.";
				EditorUtility.DisplayDialog("Skinned mesh has no skinning data", msg, "Ok");
				return;
			}

			// Add a creator component (if it doesn't already exist)

			GameObject selectedGo = SelectionUtil.FindSelectedGameObject();
			SkinnedColliderCreator component = selectedGo.GetComponent<SkinnedColliderCreator>();

			if (component == null)
				component = selectedGo.AddComponent<SkinnedColliderCreator>();
			
			// Generate editor+runtime SO assets

			string path = CommonUi.FindDataPath();

			string runtimeAssetName = AssetDatabase.GenerateUniqueAssetPath(string.Format("{0}/{1}.runtimeData.asset", path, component.targetSkinnedRenderer.name));
			string editorAssetName = AssetDatabase.GenerateUniqueAssetPath(string.Format("{0}/{1}.editorData.asset", path, component.targetSkinnedRenderer.name));

			SkinnedColliderRuntimeData runtimeData = ScriptableObject.CreateInstance<SkinnedColliderRuntimeData>();
			AssetDatabase.CreateAsset(runtimeData, runtimeAssetName);

			SkinnedColliderEditorData editorData = ScriptableObject.CreateInstance<SkinnedColliderEditorData>();
			editorData.sourceMesh = component.targetSkinnedRenderer.sharedMesh;
			editorData.runtimeData = runtimeData;
			AssetDatabase.CreateAsset(editorData, editorAssetName);

			// Populate with default data
			Transform[] bones = component.targetSkinnedRenderer.bones;
			foreach (Transform bone in bones)
			{
				editorData.Add(new BoneData(bone));
			}
			EditorUtility.SetDirty(editorData);

			component.editorData = editorData;
			EditorUtility.SetDirty(component);

			AssetDatabase.SaveAssets();

			EditorGUIUtility.PingObject(editorData);

			// Open editor window

			SkinnedColliderCreatorWindow window = EditorWindow.GetWindow<SkinnedColliderCreatorWindow>();
			window.forceResyncNextSelectionChange = true;
			window.OnSelectionChange();
			window.Repaint();
		}

		public static void Reconnect(GameObject selectedObject, SkinnedColliderEditorData existingEditorData)
		{
			// Get the hull painter (or create one if it doesn't exist)

			SkinnedColliderCreator component = selectedObject.GetComponent<SkinnedColliderCreator>();
			if (component == null)
				component = selectedObject.AddComponent<SkinnedColliderCreator>();

			// Point the hull painter at the assets

			component.editorData = existingEditorData;
			component.targetSkinnedRenderer = selectedObject.GetComponent<SkinnedMeshRenderer>();

			// Open editor window

			SkinnedColliderCreatorWindow window = EditorWindow.GetWindow< SkinnedColliderCreatorWindow>();
			window.forceResyncNextSelectionChange = true;
			window.OnSelectionChange();
			window.Repaint();
		}

		private void BeginMainScrollView()
		{
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			//scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, horizontalScrollStyle, GUI.skin.verticalScrollbar);
			//scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar); // force hides the horizontal bar, but our content still overflows because *something* still screwy
		}

		private void EndMainScrollView()
		{
			// Finishes the main scroll view, with checks to determine if the vertical scroll bar is visible
			// Check the GetLastRect before and after the EndScrollView - if the yMax is larger beforehand, then it must be beyond the scroll region and so we must have a scroll bar

			float scrollMax0 = 0.0f;
			float scrollMax1 = 0.0f;
			if (Event.current.type == EventType.Repaint)
			{
				scrollMax0 = GUILayoutUtility.GetLastRect().yMax;
			}

			EditorGUILayout.EndScrollView();

			if (Event.current.type == EventType.Repaint)
			{
				scrollMax1 = GUILayoutUtility.GetLastRect().yMax;
				this.hasScrollBar = (scrollMax0 >= scrollMax1 - 1);
			}
		}

		private void CreateStyles()
		{
			if (foldoutStyle == null)
			{
				foldoutStyle = new GUIStyle(EditorStyles.foldout);
				foldoutStyle.fontStyle = FontStyle.Bold;
			}
		}

		private void DrawBoneHierarchyUi()
		{
			areHierarchyFoldedOut = EditorGUILayout.Foldout(areHierarchyFoldedOut, new GUIContent("Bone Hierarchy", Icons.Active.hierarchyIcon), foldoutStyle);
			if (!areHierarchyFoldedOut)
			{
				return;
			}

			SkinnedColliderEditorData targetData = TargetEditorData;

			List<HierarchyElement> hierarchyElements = new List<HierarchyElement>();

			Transform[] rootBones = FindTrueRootBones();
			foreach (Transform root in rootBones)
			{
				BuildHierarchyList(0, null, root, hierarchyElements);
			}

			int shrink = (hasScrollBar ? 20 : 5);
			hierarchyScrollPosition = GUILayout.BeginScrollView(hierarchyScrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(EditorGUIUtility.currentViewWidth - shrink), GUILayout.Height(400));
			GUILayout.BeginVertical();

			const int ICON_HEIGHT = 18;
			const int TYPE_ICON_WIDTH = 20;
			const int INDENT_AMOUNT = 14;
			const int EXPAND_BUTTON_SIZE = 16;
			const int COLOR_BLOB_SIZE = 20;
			const int RIGIDBODY_ICON_SIZE = 20;
			const int JOINT_ICON_SIZE = 20;
			const int ROW_HEIGHT = 18;
			const int MARGIN = -3;

			Transform selectedBone = TargetComponent.FindBone(targetData.GetSelectedBone());
			BoneHullData selectedHull = targetData.GetSelectedHull();

			for (int i = 0; i < hierarchyElements.Count; i++)
			{
				HierarchyElement elem = hierarchyElements[i];

				Rect rowArea = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 20, ROW_HEIGHT);
				rowArea.width = rowArea.width - 20;

				// Auto scroll to selection
				if (Event.current.type == EventType.Repaint && doAutoScrollToSelection && elem.IsHull && elem.srcHull == selectedHull) // rowArea will only be valid in Repaint phase
				{
					hierarchyScrollPosition = rowArea.position + new Vector2(0f, -ROW_HEIGHT); // -ROW_HEIGHT so we can see the selection's parent as well
					doAutoScrollToSelection = false;
					Repaint();
				}

				//	Color col = (i % 2) == 0 ? new Color(0.5f, 0.5f, 0.5f, 0.1f) : Color.clear; // Stripy background lines version
				Color col = Color.clear;
				if (Event.current.type == EventType.Repaint)
				{
					if (elem.IsBone && elem.srcBone == selectedBone)
					{
						col = new Color(0.5f, 0.5f, 1.0f, 0.5f);
					}
					else if (elem.IsHull && elem.srcHull == selectedHull)
					{
						col = new Color(0.5f, 0.5f, 1.0f, 0.5f);
					}
					else if (rowArea.Contains(Event.current.mousePosition))
					{
						col = new Color(0.5f, 0.5f, 1.0f, 0.3f);
					}
				}
				EditorGUI.DrawRect(rowArea, col);

				int indent = Mathf.Max((elem.indentLevel + 1) * INDENT_AMOUNT, 0);

				Texture icon = elem.IsBone ? Icons.Active.boneIcon : Icons.Active.hullIcon;
				float textLength = EditorStyles.label.CalcSize(new GUIContent(elem.name)).x + 4; // +4 to give it some breathing room

				float xOffset = rowArea.x + indent;
				
				if (elem.hasChildren)
				{
					bool isExpanded;
					boneFoldoutState.TryGetValue(elem.srcBone, out isExpanded);
					bool clicked = GUI.Button(new Rect(xOffset, rowArea.y, EXPAND_BUTTON_SIZE + 2, ICON_HEIGHT + 2), isExpanded ? new GUIContent(Icons.Active.foldoutExpandedIcon) : new GUIContent(Icons.Active.foldoutCollapsedIcon), EditorStyles.label);
					if (clicked)
						isExpanded = !isExpanded;
					boneFoldoutState[elem.srcBone] = isExpanded;
					xOffset += EXPAND_BUTTON_SIZE + MARGIN;


					GUI.Label(new Rect(xOffset, rowArea.y, TYPE_ICON_WIDTH, ICON_HEIGHT), new GUIContent(icon));
					xOffset += TYPE_ICON_WIDTH + MARGIN;

					GUI.Label(new Rect(xOffset, rowArea.y, textLength, rowArea.height), new GUIContent(elem.name));
					xOffset += textLength;
				}
				else
				{
					xOffset += EXPAND_BUTTON_SIZE + MARGIN + 2;

					GUI.Label(new Rect(xOffset, rowArea.y, TYPE_ICON_WIDTH, ICON_HEIGHT), new GUIContent(icon));
					xOffset += TYPE_ICON_WIDTH + MARGIN;

					GUI.Label(new Rect(xOffset, rowArea.y, textLength, rowArea.height), new GUIContent(elem.name));
					xOffset += textLength;
				}

				xOffset += 2; // gap between text and rigidbody/joint icons

				if (elem.IsBone)
				{
					if (elem.srcBoneData != null && elem.srcBoneData.addRigidbody)
					{
						GUIContent bodyIcon = elem.srcBoneData.isKinematic ? new GUIContent(Icons.Active.isKinematicIcon, "Kinematic Rigidbody") : new GUIContent(Icons.Active.hasRigidbodyIcon, "Rigidbody");
						GUI.Label(new Rect(rowArea.x + xOffset, rowArea.y, RIGIDBODY_ICON_SIZE, rowArea.height), bodyIcon);
						xOffset += RIGIDBODY_ICON_SIZE + MARGIN;
					}

					if (elem.srcBoneData != null && elem.srcBoneData.addRigidbody && elem.srcBoneData.addJoint)
					{
						GUI.Label(new Rect(rowArea.x + xOffset, rowArea.y, JOINT_ICON_SIZE, rowArea.height), new GUIContent(Icons.Active.hasJointIcon, "Joint"));
						xOffset += JOINT_ICON_SIZE + MARGIN;
					}
				}

				if (elem.IsHull)
				{
					GUI.contentColor = elem.srcHull.previewColour;

					GUI.Label(new Rect(xOffset, rowArea.y, COLOR_BLOB_SIZE, rowArea.height), new GUIContent(Icons.Active.colourBlobIcon));
					xOffset += COLOR_BLOB_SIZE + MARGIN;

					GUI.contentColor = Color.white;
				}
				


				if (i == 0)
				{
					const int buttonWidth = 80;
					const int buttonGap = buttonWidth + 4;
					if (GUI.Button(new Rect(rowArea.xMax- buttonGap * 2, rowArea.y, buttonWidth, rowArea.height), "Expand all"))
					{
						ExpandAll(TargetRenderer.bones);
					}
					if (GUI.Button(new Rect(rowArea.xMax - buttonGap, rowArea.y, buttonWidth, rowArea.height), "Collapse all"))
					{
						CollapseAll();
					}
				}



				if (Event.current.type == EventType.MouseDown && rowArea.Contains(Event.current.mousePosition))
				{
					if (elem.IsBone)
					{
						TargetEditorData.SetSelection(elem.srcBoneData);
					}
					else if (elem.IsHull)
					{
						TargetEditorData.SetSelection(elem.srcHull);
					}

					Event.current.Use();
				}
			}

			GUILayout.EndVertical();
			GUILayout.EndScrollView();
		}

		private void ExpandAll(Transform[] bones)
		{
			foreach (Transform bone in bones)
				boneFoldoutState[bone] = true;
		}

		private void CollapseAll()
		{
			boneFoldoutState.Clear();
		}

		private void DisplayToolBar()
		{
			areToolsFoldedOut = EditorGUILayout.Foldout(areToolsFoldedOut, new GUIContent("Tools", Icons.Active.toolsIcons), foldoutStyle);
			if (areToolsFoldedOut)
			{
				GUILayout.BeginHorizontal();
				{
					sceneManipulator.DrawToolSelectionUi(Icons.Active);

					GUILayout.FlexibleSpace();

					if (GUILayout.Button(new GUIContent(Icons.Active.autoSetupIcon, "Auto setup"), GUILayout.Width(34)))
					{
						AutoPopulate();						
					}

					if (GUILayout.Button(new GUIContent(Icons.Active.remoteBodyDataIcon, "Remove all bodies"), GUILayout.Width(34)))
					{
						DeleteAllBoneData();
					}

					if (GUILayout.Button(new GUIContent(Icons.Active.removeHullDataIcon, "Remove all hulls"), GUILayout.Width(34)))
					{
						DeleteAllHullData();
					}

					GUILayout.Space(20);

					if (GUILayout.Button(new GUIContent(Icons.Active.generateIcon, "Generate colliders"), GUILayout.Width(34)))
					{
						GenerateColliders();
					}
					if (GUILayout.Button(new GUIContent(Icons.Active.deleteCollidersIcon, "Delete generated"), GUILayout.Width(34)))
					{
						DeleteExistingColliders(TargetRenderer);
					}
				}
				GUILayout.EndHorizontal();
			}
		}

		private void ExpandHierarchy(BoneHullData selectedHull)
		{
			Transform hullTransform = TargetComponent.FindBone(selectedHull);
			ExpandHierarchy(hullTransform);
		}

		private void ExpandHierarchy(BoneData selectedBone)
		{
			Transform boneTransform = TargetComponent.FindBone(selectedBone);
			ExpandHierarchy(boneTransform);
		}

		private void ExpandHierarchy(Transform boneTransform)
		{
			Console.output.Log("ExpandHierarchy(" + boneTransform + ")");

			if (boneTransform == null)
				return;

			if (TargetRenderer == null)
				return;

			Transform next = boneTransform;
			do
			{
				boneFoldoutState[next] = true;
				next = next.parent;
			}
			while (next != null);
		}

		private bool IsBone(Transform transform)
		{
			foreach (Transform bone in TargetRenderer.bones)
			{
				if (bone == transform)
					return true;
			}
			return false;
		}

		/** SkinnedMeshRenderer has a 'bones' array and a 'rootBone'. But there's no gurantee that 'rootBone' is actually at the root of a transform hierarchy
		 *  that contains all of the actual bones (see Briefcase test model).
		 *  Instead we ignore the root bone and look for the 'true' root bones - any bone that's got a regular Transform as a parent and not another bone
		 *  Then the UI can display the hierarchy using the true root bones as the roots of the UI tree
		 */
		private Transform[] FindTrueRootBones()
		{
			List<Transform> trueRoots = new List<Transform>();

			foreach (Transform bone in TargetRenderer.bones)
			{
				if (IsTrueRoot(bone) && !trueRoots.Contains(bone))
				{
					trueRoots.Add(bone);
				}
			}

			return trueRoots.ToArray();
		}

		private bool IsTrueRoot(Transform bone)
		{
			if (bone == null)
				return false;

			return IsBone(bone) && !IsBone(bone.parent);
		}

		private void BuildHierarchyList(int depth, HierarchyElement parentElement, Transform currentBone, List<HierarchyElement> destElements)
		{
			if (currentBone == null)
				return;

			if (IsBone(currentBone))
			{
				HierarchyElement elem = AddHierarchyElement(depth, parentElement, currentBone, destElements);
				BoneHullData[] hullData = TargetEditorData.GetBoneHullData(currentBone.name);

				bool isExpanded;
				boneFoldoutState.TryGetValue(currentBone, out isExpanded);

				if (isExpanded)
				{
					foreach (BoneHullData hull in hullData)
					{
						HierarchyElement hullElem = new HierarchyElement();
						hullElem.name = hull.type == HullType.Auto ? "Auto hull" : "Manual hull";
						hullElem.srcBone = currentBone;
						hullElem.srcHull = hull;
						hullElem.indentLevel = depth + 1;
						destElements.Add(hullElem);
					}
				
					for (int i = 0; i < currentBone.childCount; i++)
					{
						BuildHierarchyList(depth + 1, elem, currentBone.GetChild(i), destElements);
					}
				}
			}
			else
			{
				// Just a transform, not an actual bone. Keep trying the children
				for (int i = 0; i < currentBone.childCount; i++)
				{
					BuildHierarchyList(depth + 1, null, currentBone.GetChild(i), destElements);
				}
			}
		}

		private HierarchyElement AddHierarchyElement(int depth, HierarchyElement parentElement, Transform bone, List<HierarchyElement> destElements)
		{
			BoneData boneData = TargetEditorData.GetBoneData(bone.name); // NPE HERE
			BoneHullData[] hullData = TargetEditorData.GetBoneHullData(bone.name);

			HierarchyElement elem = new HierarchyElement();
			elem.parentElement = parentElement;
			elem.srcBone = bone;
			elem.srcBoneData = boneData;
			elem.indentLevel = depth;
			elem.name = bone.name;
			elem.hasChildren = bone.childCount > 0 || (hullData != null && hullData.Length > 0);
			destElements.Add(elem);

			return elem;
		}

		private void DrawHierarchyBone(int depth, Transform currentBone)
		{
			GUILayout.BeginHorizontal();

			GUILayout.Space(depth * 10);

			bool hasChildren = currentBone.childCount > 0;
			bool isExpanded = false;

			if (hasChildren)
			{
				boneFoldoutState.TryGetValue(currentBone, out isExpanded);

				isExpanded = EditorGUILayout.Foldout(isExpanded, new GUIContent(currentBone.name), false);
				boneFoldoutState[currentBone] = isExpanded;
			}

			GUILayout.EndHorizontal();

			// Now process children

			if (isExpanded)
			{
				for (int i = 0; i < currentBone.childCount; i++)
				{
					Transform childBone = currentBone.GetChild(i);
					DrawHierarchyBone(depth + 1, childBone);
				}
			}
		}

		private void DrawPropertiesUi()
		{
			if (TargetEditorData == null)
				return;

			BoneData selectedBone = TargetEditorData.GetSelectedBone();
			BoneHullData selectedHull = TargetEditorData.GetSelectedHull();

			arePropertiesFoldedOut = EditorGUILayout.Foldout(arePropertiesFoldedOut, new GUIContent("Properties", Icons.Active.propertiesIcon), foldoutStyle);
			if (arePropertiesFoldedOut)
			{
				if (selectedHull != null)
				{
					DrawHullPropertiesUi();
				}
				else if (selectedBone != null)
				{
					DrawBonePropertiesUi();
				}
				else
				{
					// ..?
				}
			}
		}

		private void DrawBonePropertiesUi()
		{
			bool wasWideMode = EditorGUIUtility.wideMode;
			EditorGUIUtility.wideMode = true;

			BoneData boneData = TargetEditorData.GetSelectedBone();
			Transform selectedBone = TargetComponent.FindBone(boneData);

			if (boneData != null)
			{
				//bool wasAddRigidbody = boneData.addRigidbody;
				bool wasIsKinematic = boneData.isKinematic;
				bool wasAddJoint = boneData.addJoint;
				BoneJointType prevJointType = boneData.jointType;

				// Rigidbody properties

				GUILayout.Label(new GUIContent("Rigidbody", Icons.Active.hasRigidbodyIcon));

				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(INDENT_AMOUNT);
					boneData.addRigidbody = EditorGUILayout.Toggle("Add Rigidbody", boneData.addRigidbody);
				}
				GUILayout.EndHorizontal();

				if (boneData.addRigidbody)
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(INDENT_AMOUNT);
						boneData.isKinematic = EditorGUILayout.Toggle("Is Kinematic?", boneData.isKinematic);
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(INDENT_AMOUNT);
						boneData.mass = EditorGUILayout.FloatField("Mass", boneData.mass);
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(INDENT_AMOUNT);
						boneData.linearDrag = EditorGUILayout.FloatField("Linear Drag", boneData.linearDrag);
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(INDENT_AMOUNT);
						boneData.angularDrag = EditorGUILayout.FloatField("Angular Drag", boneData.angularDrag);
					}
					GUILayout.EndHorizontal();

					// Joint properties
					DrawJointUi(boneData, selectedBone);
				}

				// Did anything change?
				if (wasAddJoint != boneData.addJoint
					|| wasIsKinematic != boneData.isKinematic
					|| wasAddJoint != boneData.addJoint
					|| prevJointType != boneData.jointType)
				{
					TargetEditorData.MarkDirty();
				}

				// Hull buttons

				GUILayout.Label(new GUIContent("Collider", Icons.Active.hullIcon));
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(INDENT_AMOUNT);
					if (GUILayout.Button(new GUIContent("Add Auto Hull", Icons.Active.autoSetupIconSmall), GUILayout.MaxWidth(150)))
					{
						BoneHullData newHull = CreateAutoHull(selectedBone);
						TargetEditorData.SetSelection(newHull);
					}

					if (GUILayout.Button(new GUIContent("Add Manual Hull", Icons.Active.paintOnIcon), GUILayout.MaxWidth(150)))
					{
						BoneHullData newHull = CreateManualHull(selectedBone);
						TargetEditorData.SetSelection(newHull);

						CreateHullColliders();
					}
				}
				GUILayout.EndHorizontal();
			}

			EditorGUIUtility.wideMode = wasWideMode;
		}

		private void DrawJointUi(BoneData boneData, Transform selectedBone)
		{
#pragma warning disable 0162
			if (!Console.ENABLE_JOINT_SUPPORT)
				return;

			GUILayout.Label(new GUIContent("Joint", Icons.Active.hasJointIcon));

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(INDENT_AMOUNT);
				boneData.addJoint = EditorGUILayout.Toggle("Add joint", boneData.addJoint);
			}
			GUILayout.EndHorizontal();

			if (boneData.addJoint)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(INDENT_AMOUNT);
					boneData.jointType = (BoneJointType)EditorGUILayout.EnumPopup("Joint type", boneData.jointType);
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(INDENT_AMOUNT);
					boneData.primaryAxis = EditorGUILayout.Vector3Field("Primary axis", boneData.primaryAxis);
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(INDENT_AMOUNT);
					boneData.secondaryAxis = EditorGUILayout.Vector3Field("Secondary axis", boneData.secondaryAxis);
				}
				GUILayout.EndHorizontal();

				// Primary rotation limits

				float newLowerLimit;
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(INDENT_AMOUNT);
					newLowerLimit = EditorGUILayout.FloatField("Primary min angle", boneData.primaryLowerAngularLimit);
				}
				GUILayout.EndHorizontal();

				float newUpperLimit;
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(INDENT_AMOUNT);
					newUpperLimit = EditorGUILayout.FloatField("Primary max angle", boneData.primaryUpperAngularLimit);
				}
				GUILayout.EndHorizontal();

				if (newLowerLimit < boneData.primaryUpperAngularLimit)
					boneData.primaryLowerAngularLimit = newLowerLimit;
				else
					boneData.primaryLowerAngularLimit = boneData.primaryUpperAngularLimit;

				if (newUpperLimit > boneData.primaryLowerAngularLimit)
					boneData.primaryUpperAngularLimit = newUpperLimit;
				else
					boneData.primaryUpperAngularLimit = boneData.primaryLowerAngularLimit;

				// Secondary rotation limits

				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(INDENT_AMOUNT);
					boneData.secondaryAngularLimit = Mathf.Max(EditorGUILayout.FloatField("Secondary angle limit", boneData.secondaryAngularLimit), 0.0f);
				}
				GUILayout.EndHorizontal();

				// Tertiary rotation limits

				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(INDENT_AMOUNT);
					boneData.tertiaryAngularLimit = Mathf.Max(EditorGUILayout.FloatField("Tertiary angle limit", boneData.tertiaryAngularLimit), 0.0f);
				}
				GUILayout.EndHorizontal();

				// Translation limits

				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(INDENT_AMOUNT);
					boneData.translationLimit = Mathf.Max(EditorGUILayout.FloatField("Translation limit", boneData.translationLimit), 0.0f);
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(INDENT_AMOUNT);
					if (GUILayout.Button(new GUIContent("Guess joint setup", Icons.Active.autoSetupIconSmall), GUILayout.MaxWidth(150)))
					{
						DoGuessJointSetup(boneData, selectedBone);
					}
				}
				GUILayout.EndHorizontal();
			}

#pragma warning restore 0162
		}

		private void DoGuessJointSetup(BoneData boneData, Transform selectedBone)
		{
			if (boneData.jointType == BoneJointType.Fixed)
			{
				Console.output.Log("TODO!");
			}
			else if (boneData.jointType == BoneJointType.Hinge)
			{
				if (selectedBone.parent != null && selectedBone.childCount > 0)
				{
					Transform parent = selectedBone.parent;
					Transform child = selectedBone.GetChild(0);
					Vector3 incomming = (selectedBone.position - parent.position).normalized;
					Vector3 outgoing = (child.position - selectedBone.position).normalized;
					Vector3 normal = Vector3.Cross(incomming, outgoing).normalized;
					boneData.primaryAxis = normal;
					boneData.secondaryAxis = outgoing;
				}
				else
				{
					Console.output.LogWarning(Console.Technie, "Couldn't guess at hinge - bone is a leaf bone");
				}
			}
			else if (boneData.jointType == BoneJointType.BallAndSocket)
			{
				Console.output.Log("TODO!");

				boneData.primaryLowerAngularLimit = -90.0f;
				boneData.primaryUpperAngularLimit = +90.0f;
				boneData.secondaryAngularLimit = 90.0f;
				boneData.tertiaryAngularLimit = 90.0f;
			}
			else if (boneData.jointType == BoneJointType.Tentacle)
			{
				Console.output.Log("TODO!");
			}
		}

		private void DrawHullPropertiesUi()
		{
			bool wasChanged = false;

			BoneHullData selectedHull = TargetEditorData.GetSelectedHull();
			
			Transform bone = TargetComponent.FindBone(selectedHull);

			float[] collumnWidths = CommonUi.CalcSettingCollumns2();

			// Hull-specific UI

			if (selectedHull.type == HullType.Auto)
			{
				wasChanged = DrawAutoHullPropertiesUi(selectedHull, bone);
			}
			else if (selectedHull.type == HullType.Manual)
			{
				wasChanged = DrawManualHullPropertiesUi(selectedHull, bone);
			}

			// Common hull UI

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(INDENT_AMOUNT);
				GUILayout.Label("Collider type", GUILayout.Width(collumnWidths[0]));
				selectedHull.colliderType = (ColliderType)EditorGUILayout.EnumPopup(selectedHull.colliderType);
			}
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(INDENT_AMOUNT);
				GUILayout.Label("Colour", GUILayout.Width(collumnWidths[0]));

				Color prevColour = selectedHull.previewColour;
#if UNITY_2017_4_OR_NEWER
				Color newColour = EditorGUILayout.ColorField(new GUIContent(), selectedHull.previewColour, true, false, false);
#else
				Color newColour = EditorGUILayout.ColorField(new GUIContent(), selectedHull.previewColour, true, false, false, new ColorPickerHDRConfig(1.0f, 1.0f, 0.0f, 1.0f));
#endif
				if (newColour != prevColour)
				{
					Undo.RecordObject(TargetEditorData, "change hull colour");

					selectedHull.previewColour = newColour;
					wasChanged = true;
				}
			}
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(INDENT_AMOUNT);
				GUILayout.Label("Material", GUILayout.Width(collumnWidths[0]));
				selectedHull.material = (PhysicMaterial)EditorGUILayout.ObjectField(selectedHull.material, typeof(PhysicMaterial), false);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(INDENT_AMOUNT);
				GUILayout.Label("Is Trigger", GUILayout.Width(collumnWidths[0]));

				selectedHull.isTrigger = EditorGUILayout.Toggle(selectedHull.isTrigger);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(INDENT_AMOUNT);
				GUILayout.Label("Max Planes", GUILayout.Width(collumnWidths[0]));

				int currValue = selectedHull.maxPlanes;
				int newValue = EditorGUILayout.IntSlider(currValue, 6, 255);
				if (currValue != newValue)
				{
					selectedHull.maxPlanes = newValue;
					wasChanged = true;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(new GUIContent("Delete hull", Icons.Active.deleteIcon), GUILayout.MaxWidth(collumnWidths[2])))
				{
					TargetEditorData.Remove(selectedHull);
					DeleteExistingColliders(TargetRenderer);

					wasChanged = true;
				}
			}
			GUILayout.EndHorizontal();

			if (wasChanged)
			{
				TargetEditorData.MarkDirty();
			}
		}

		private bool DrawAutoHullPropertiesUi(BoneHullData hullData, Transform bone)
		{
			bool wasChanged = false;

			float[] collumnWidths = CommonUi.CalcSettingCollumns2();

			GUILayout.Label(new GUIContent("Collider", Icons.Active.hullIcon));

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(INDENT_AMOUNT);

				GUILayout.Label("Weight threshold", GUILayout.Width(collumnWidths[0]));

				float min = hullData.MinThreshold;
				float max = hullData.MaxThreshold;
				EditorGUILayout.MinMaxSlider(ref min, ref max, 0.0f, 1.0f);
				if (hullData.MinThreshold != min || hullData.MaxThreshold != max)
				{
					Undo.RecordObject(TargetEditorData, "change threshold value");
					hullData.SetThresholds(min, max, TargetRenderer, TargetRenderer.sharedMesh);
					wasChanged = true;
				}
			}
			GUILayout.EndHorizontal();

			return wasChanged;
		}

		private bool DrawManualHullPropertiesUi(BoneHullData hullData, Transform bone)
		{
			GUILayout.Label(new GUIContent("Collider", Icons.Active.hullIcon));

			bool wasChanged = false;

			GUILayout.BeginHorizontal();

			
			GUILayout.EndHorizontal();

			return wasChanged;
		}

		private void DrawDefaultsUi()
		{
			areDefaultsFoldedOut = EditorGUILayout.Foldout(areDefaultsFoldedOut, new GUIContent("Defaults", Icons.Active.defaultsIcon), foldoutStyle);
			if (!areDefaultsFoldedOut)
				return;

			float[] collumnWidths = CommonUi.CalcSettingCollumns2();

			SkinnedColliderEditorData targetData = TargetEditorData;
			if (targetData != null)
			{
				GUILayout.Label(new GUIContent("Rigidbody", Icons.Active.hasRigidbodyIcon));

				if (DrawDefaultBool("Add rigidbody", targetData, ref targetData.defaultAddRigidbody, collumnWidths, true, "Apply to all"))
				{
					foreach (BoneData data in targetData.boneData)
						data.addRigidbody = targetData.defaultAddRigidbody;
				}

				if (DrawDefaultFloat("Mass", targetData, ref targetData.defaultMass, collumnWidths, true, "Apply to all"))
				{
					foreach (BoneData data in targetData.boneData)
						data.mass = targetData.defaultMass;
				}

				if (DrawDefaultFloat("Linear Drag", targetData, ref targetData.defaultLinearDrag, collumnWidths, true, "Apply to all"))
				{
					foreach (BoneData data in targetData.boneData)
						data.linearDrag = targetData.defaultLinearDrag;
				}

				if (DrawDefaultFloat("Angular Drag", targetData, ref targetData.defaultAngularDrag, collumnWidths, true, "Apply to all"))
				{
					foreach (BoneData data in targetData.boneData)
						data.angularDrag = targetData.defaultAngularDrag;
				}

				if (DrawDefaultFloat("Linear Damping", targetData, ref targetData.defaultLinearDamping, collumnWidths, true, "Apply to all"))
				{
					foreach (BoneData data in targetData.boneData)
						data.linearDamping = targetData.defaultLinearDamping;
				}

				if (DrawDefaultFloat("Angular Damping", targetData, ref targetData.defaultAngularDamping, collumnWidths, true, "Apply to all"))
				{
					foreach (BoneData data in targetData.boneData)
						data.angularDamping = targetData.defaultAngularDamping;
				}

				GUILayout.Label(new GUIContent("Collider", Icons.Active.hullIcon));

				if (DrawDefaultBool("Is trigger", targetData, ref targetData.defaultIsTrigger, collumnWidths, true, "Apply to all"))
				{
					foreach (BoneHullData hull in targetData.boneHullData)
						hull.isTrigger = targetData.defaultIsTrigger;
				}

				if (DrawDefaultSlider("Max planes", targetData, ref targetData.defaultMaxPlanes, 6, 255, collumnWidths, true, "Apply to all"))
				{
					foreach (BoneHullData hull in targetData.boneHullData)
					{
						hull.maxPlanes = targetData.defaultMaxPlanes;
					}
					targetData.MarkDirty();
				}

				DrawDefaultMaterial(collumnWidths);
				DrawDefaultColliderType(collumnWidths);
			}
		}
		
		private bool DrawDefaultFloat(string label, Object dataSource, ref float floatValue, float[] collumnWidths, bool showButton, string buttonLabel="")
		{
			bool clicked = false;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(INDENT_AMOUNT);

				GUILayout.Label(label, GUILayout.Width(collumnWidths[0]));
				
				float newFloatValue = EditorGUILayout.FloatField(floatValue);

				if (!Mathf.Approximately(floatValue, newFloatValue))
				{
					floatValue = newFloatValue;
					EditorUtility.SetDirty(dataSource);
				}
				
				if (showButton && GUILayout.Button(buttonLabel, GUILayout.Width((collumnWidths[2]))))
				{
					clicked = true;
				}
			}
			GUILayout.EndHorizontal();

			return clicked;
		}

		private bool DrawDefaultBool(string label, Object dataSource, ref bool boolValue, float[] collumnWidths, bool showButton, string buttonLabel="")
		{
			bool clicked = false;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(INDENT_AMOUNT);
				GUILayout.Label(label, GUILayout.Width(collumnWidths[0]));

				bool newBoolValue = EditorGUILayout.Toggle(boolValue);

				if (newBoolValue != boolValue)
				{
					boolValue = newBoolValue;
					EditorUtility.SetDirty(dataSource);
				}

				if (showButton && GUILayout.Button(buttonLabel, GUILayout.Width(collumnWidths[2])))
				{
					clicked = true;
				}
			}
			GUILayout.EndHorizontal();

			return clicked;
		}

		private bool DrawDefaultSlider(string label, Object dataSource, ref int intValue, int minValue, int maxValue, float[] collumnWidths, bool showButton, string buttonLabel="")
		{
			bool clicked = false;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(INDENT_AMOUNT);
				GUILayout.Label(label, GUILayout.Width(collumnWidths[0]));

				int newIntValue = EditorGUILayout.IntSlider(intValue, minValue, maxValue);

				if (newIntValue != intValue)
				{
					intValue = newIntValue;
					EditorUtility.SetDirty(dataSource);
				}

				if (showButton && GUILayout.Button(buttonLabel, GUILayout.Width(collumnWidths[2])))
				{
					clicked = true;
				}
			}
			GUILayout.EndHorizontal();

			return clicked;
		}



		private void DrawDefaultMaterial(float[] collumnWidths)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(INDENT_AMOUNT);

				GUILayout.Label("Material", GUILayout.Width(collumnWidths[0]));

				SkinnedColliderEditorData targetData = TargetEditorData;

				targetData.defaultMaterial = (PhysicMaterial)EditorGUILayout.ObjectField(targetData.defaultMaterial, typeof(PhysicMaterial), false);

				if (GUILayout.Button("Apply To All", GUILayout.Width(collumnWidths[2])))
				{
					foreach (BoneHullData hull in targetData.boneHullData)
					{
						hull.material = targetData.defaultMaterial;
					}
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawDefaultColliderType(float[] collumnWidths)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(INDENT_AMOUNT);

				GUILayout.Label("Collider type", GUILayout.Width(collumnWidths[0]));

				SkinnedColliderEditorData targetData = TargetEditorData;

				targetData.defaultColliderType = (ColliderType)EditorGUILayout.EnumPopup(targetData.defaultColliderType);

				if (GUILayout.Button("Apply To All", GUILayout.Width(collumnWidths[2])))
				{
					foreach (BoneHullData hull in targetData.boneHullData)
					{
						hull.colliderType = targetData.defaultColliderType;
					}
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawSettingsUi()
		{
			float[] collumnWidths = CommonUi.CalcSettingCollumns2();

			areSettingsFoldedOut = EditorGUILayout.Foldout(areSettingsFoldedOut, new GUIContent("Settings", Icons.Active.settingsIcon), foldoutStyle);
			if (areSettingsFoldedOut)
			{
				CommonUi.DrawWireframeUi(ref showWireframe, ref wireframeFactor, collumnWidths);
				CommonUi.DrawDimmingUi(ref dimInactiveHulls, ref dimFactor, collumnWidths);
				CommonUi.DrawHullAlphaUi(ref globalHullAlpha, collumnWidths);

				// TODO: General hull opacity slider?
			}
		}

		private void DrawAssetUi()
		{
			areAssetsFoldedOut = EditorGUILayout.Foldout(areAssetsFoldedOut, new GUIContent("Assets", Icons.Active.assetsIcon), foldoutStyle);
			if (areAssetsFoldedOut)
			{
				SkinnedColliderEditorData editorData = TargetEditorData;

				string paintingPath = AssetDatabase.GetAssetPath(editorData);
				GUILayout.Label("Editor data: " + paintingPath, EditorStyles.centeredGreyMiniLabel);

				string hullPath = AssetDatabase.GetAssetPath(editorData != null ? editorData.runtimeData : null);
				GUILayout.Label("Runtime data: " + hullPath, EditorStyles.centeredGreyMiniLabel);

				if (GUILayout.Button("Disconnect from assets"))
				{
					//bool deleteChildren = !EditorUtility.DisplayDialog("Disconnect from assets",
					//								"Also delete child painter components?\n\n"
					//								+ "These are not needed but leaving them make it easier to reconnect the painting data in the future.",
					//								"Leave",    // ok option - returns true
					//								"Delete");  // cancel option - returns false

					sceneManipulator.DisconnectAssets(true);
					Repaint();
				}
			}
		}
		
		public void DeleteGenerated()
		{
			DeleteExistingColliders(TargetRenderer);
		}

		private void DeleteExistingColliders(SkinnedMeshRenderer skinnedMesh)
		{
			if (skinnedMesh == null)
				return;

			// NB: Check each bone individually rather than gathering everything from the root bone as that's more robust to different skinned hierarchy formats
			foreach (Transform bone in skinnedMesh.bones)
			{
				// Remove joints first as otherwise they prevent the rigidbodies being removed

				if (Console.ENABLE_JOINT_SUPPORT)
				{
#pragma warning disable 0162
					Joint[] existingJoints = bone.GetComponents<Joint>();
					foreach (Joint joint in existingJoints)
					{
						GameObject.DestroyImmediate(joint);
					}
#pragma warning restore 0162
				}

				Rigidbody[] existingBodies = bone.GetComponents<Rigidbody>();
				foreach (Rigidbody body in existingBodies)
				{
					GameObject.DestroyImmediate(body);
				}

				Collider[] existingCols = bone.GetComponents<Collider>();
				foreach (Collider col in existingCols)
				{
					GameObject.DestroyImmediate(col);
				}
			}
		}

		private void AutoPopulate()
		{
			foreach (Transform bone in TargetRenderer.bones)
			{
				int count = Utils.NumVerticesForBone(unpackedMesh, bone, DEFAULT_MIN_THRESHOLD, DEFAULT_MAX_THRESHOLD);
				if (count >= 4)
				{
					if (!HasAutoBone(bone))
						CreateAutoBone(bone);

					if (!HasAutoHull(bone))
						CreateAutoHull(bone);
				}

				// TODO: Some kind of event/callback for when editorData is modified so we always call these
				//	DeleteExistingColliders(TargetRenderer);
				//	CreateHullColliders();
				//UpdatePreviewMeshes();
			}
			TargetEditorData.MarkDirty();
		}



		private void DeleteAllBoneData()
		{
			if (TargetEditorData != null)
			{
				TargetEditorData.boneData.Clear();
				TargetEditorData.MarkDirty();

				//UpdatePreviewMeshes();
			}
		}

		private void DeleteAllHullData()
		{
			if (TargetEditorData != null)
			{
				TargetEditorData.boneHullData.Clear();
				TargetEditorData.MarkDirty();

				//UpdatePreviewMeshes();
			}
		}

		public void GenerateColliders()
		{
			DeleteExistingColliders(TargetRenderer);
			CreateHullColliders();
			TargetEditorData.MarkDirty();
		}

		private void CreateHullColliders()
		{
			SkinnedMeshRenderer targetSkinnedRenderer = TargetRenderer;
			SkinnedColliderEditorData targetData = TargetEditorData;

			Transform[] bones = targetSkinnedRenderer.bones;

			// First add the colliders

			for (int i = 0; i < bones.Length; i++)
			{
				Transform bone = bones[i];
				BoneHullData[] hulls = targetData.GetBoneHullData(bone.name);
				foreach (BoneHullData data in hulls)
				{
					// TODO: Some duplication with UpdateColliders here
					
					Mesh hullMesh = CreateHullMesh(targetSkinnedRenderer, unpackedMesh, bone, data, CoordSpace.Bone);
					data.hullMesh = hullMesh;

					if (hullMesh != null)
					{
						if (data.colliderType == ColliderType.Convex)
						{
							MeshCollider col = bone.gameObject.AddComponent<MeshCollider>();
#if UNITY_2017_4_OR_NEWER
							col.cookingOptions &= ~MeshColliderCookingOptions.EnableMeshCleaning; // Disable the 'mesh cleaning' flag as unity likes to collapse our nice accurate hulls into garbage.
#endif
							col.sharedMesh = hullMesh;
							col.convex = true;
							col.sharedMaterial = data.material;
							col.isTrigger = data.isTrigger;
						}
						else if (data.colliderType == ColliderType.Box)
						{
							Vector3[] selectedVertices = hullMesh.vertices;
							ConstructionPlane basePlane = new ConstructionPlane(Vector3.zero, Vector3.forward, Vector3.up);
							RotatedBox box = RotatedBoxFitter.FindTightestBox(basePlane, selectedVertices);
							BoxDef boxDef = RotatedBoxFitter.ToBoxDef(box);

							BoxCollider col = bone.gameObject.AddComponent<BoxCollider>();
							col.center = boxDef.collisionBox.center;
							col.size = boxDef.collisionBox.size;
							col.sharedMaterial = data.material;
							col.isTrigger = data.isTrigger;
						}
						else if (data.colliderType == ColliderType.Capsule)
						{
							AlignedCapsuleFitter fitter = new AlignedCapsuleFitter();
							CapsuleDef capDef = fitter.Fit(hullMesh.vertices, hullMesh.triangles);

							CapsuleCollider col = bone.gameObject.AddComponent<CapsuleCollider>();
							col.center = capDef.capsuleCenter;
							col.direction = (int)capDef.capsuleDirection;
							col.radius = capDef.capsuleRadius;
							col.height = capDef.capsuleHeight;
							col.sharedMaterial = data.material;
							col.isTrigger = data.isTrigger;
						}
						else if (data.colliderType == ColliderType.Sphere)
						{
							SphereFitter fitter = new SphereFitter();
							Sphere sphereDef = fitter.Fit(hullMesh.vertices, hullMesh.triangles);

							SphereCollider col = bone.gameObject.AddComponent<SphereCollider>();
							col.center = sphereDef.center;
							col.radius = sphereDef.radius;
							col.sharedMaterial = data.material;
							col.isTrigger = data.isTrigger;
						}
						EditorUtility.SetDirty(bone.gameObject);
					}
				}
			}

			// Then add the rigid bodies

			for (int i = 0; i < bones.Length; i++)
			{
				Transform bone = bones[i];
				BoneData data = targetData.GetBoneData(bone.name);
				if (data != null && data.addRigidbody)
				{
					Rigidbody body = bone.gameObject.GetComponent<Rigidbody>();
					if (body == null)
						body = bone.gameObject.AddComponent<Rigidbody>();

					body.isKinematic = data.isKinematic;
					body.mass = data.mass;
					body.drag = data.linearDrag;
					body.angularDrag = data.angularDrag;
				}
			}

			// Finally add the joints connecting the bodies

			for (int i = 0; i < bones.Length; i++)
			{
#pragma warning disable 0162
				if (!Console.ENABLE_JOINT_SUPPORT)
					continue;

				Transform bone = bones[i];
				BoneData data = targetData.GetBoneData(bone.name);
				if (data != null && data.addJoint)
				{
					Rigidbody body = bone.gameObject.GetComponent<Rigidbody>();
					Rigidbody parentBody = bone.parent.gameObject.GetComponent<Rigidbody>();

					if (body != null && parentBody != null)
					{
						ConfigurableJoint joint = bone.gameObject.GetComponent<ConfigurableJoint>();
						if (joint == null)
							joint = bone.gameObject.AddComponent<ConfigurableJoint>();

						joint.connectedBody = parentBody;
						joint.projectionMode = JointProjectionMode.PositionAndRotation;
						joint.enablePreprocessing = false;

						joint.axis = data.primaryAxis;
						joint.secondaryAxis = data.secondaryAxis;

						// Set the default spring damping for linear and angular behaviour

						SoftJointLimitSpring springInfo;

						springInfo = joint.linearLimitSpring;
						springInfo.damper = data.linearDamping;
						joint.linearLimitSpring = springInfo;

						springInfo = joint.angularXLimitSpring;
						springInfo.damper = data.linearDamping;
						joint.angularXLimitSpring = springInfo;

						springInfo = joint.angularYZLimitSpring;
						springInfo.damper = data.angularDamping;
						joint.angularYZLimitSpring = springInfo;

						// Now set per-joint-type properties

						if (data.jointType == BoneJointType.Hinge)
						{
							// No translation
							joint.xMotion = ConfigurableJointMotion.Locked;
							joint.yMotion = ConfigurableJointMotion.Locked;
							joint.zMotion = ConfigurableJointMotion.Locked;

							// Rotation only on x axis
							joint.angularXMotion = ConfigurableJointMotion.Limited;
							joint.angularYMotion = ConfigurableJointMotion.Locked;
							joint.angularZMotion = ConfigurableJointMotion.Locked;

							// Set up limits for primary axis

							SoftJointLimit limitInfo;

							limitInfo = joint.lowAngularXLimit;
							limitInfo.limit = data.primaryLowerAngularLimit;
							joint.lowAngularXLimit = limitInfo;

							limitInfo = joint.highAngularXLimit;
							limitInfo.limit = data.primaryUpperAngularLimit;
							joint.highAngularXLimit = limitInfo;

						}
						else if (data.jointType == BoneJointType.BallAndSocket)
						{
							// No translation
							joint.xMotion = ConfigurableJointMotion.Locked;
							joint.yMotion = ConfigurableJointMotion.Locked;
							joint.zMotion = ConfigurableJointMotion.Locked;

							// Rotation only on primary and secondary axies
							joint.angularXMotion = ConfigurableJointMotion.Limited;
							joint.angularYMotion = ConfigurableJointMotion.Limited;
							joint.angularZMotion = ConfigurableJointMotion.Limited;

							// Set up limits for primary axis

							SoftJointLimit limitInfo;

							limitInfo = joint.lowAngularXLimit;
							limitInfo.limit = data.primaryLowerAngularLimit;
							joint.lowAngularXLimit = limitInfo;

							limitInfo = joint.highAngularXLimit;
							limitInfo.limit = data.primaryUpperAngularLimit;
							joint.highAngularXLimit = limitInfo;

							// Set up limits for secondary axis

							limitInfo = joint.angularYLimit;
							limitInfo.limit = data.secondaryAngularLimit;
							joint.angularYLimit = limitInfo;

							// Set up limits for third axis

							limitInfo = joint.angularZLimit;
							limitInfo.limit = data.tertiaryAngularLimit;
							joint.angularZLimit = limitInfo;
						}
						else if (data.jointType == BoneJointType.Tentacle)
						{
							// Translation only along the primary axis
							joint.xMotion = ConfigurableJointMotion.Limited;
							joint.yMotion = ConfigurableJointMotion.Locked;
							joint.zMotion = ConfigurableJointMotion.Locked;

							// Rotation only on primary and secondary axies
							joint.angularXMotion = ConfigurableJointMotion.Limited;
							joint.angularYMotion = ConfigurableJointMotion.Limited;
							joint.angularZMotion = ConfigurableJointMotion.Limited;

							SoftJointLimit limitInfo;

							// Set up translation limits

							limitInfo = joint.linearLimit;
							limitInfo.limit = data.translationLimit;
							joint.linearLimit = limitInfo;

							// Set up limits for primary axis

							limitInfo = joint.lowAngularXLimit;
							limitInfo.limit = data.primaryLowerAngularLimit;
							joint.lowAngularXLimit = limitInfo;

							limitInfo = joint.highAngularXLimit;
							limitInfo.limit = data.primaryUpperAngularLimit;
							joint.highAngularXLimit = limitInfo;

							// Set up limits for secondary axis

							limitInfo = joint.angularYLimit;
							limitInfo.limit = data.secondaryAngularLimit;
							joint.angularYLimit = limitInfo;

							// Set up limits for third axis

							limitInfo = joint.angularZLimit;
							limitInfo.limit = data.tertiaryAngularLimit;
							joint.angularZLimit = limitInfo;
						}
						EditorUtility.SetDirty(joint);
					}
					else
					{
						Debug.LogError("Error: No bodies to connect with joint", bone);
					}
				}
				EditorUtility.SetDirty(bone);
				EditorUtility.SetDirty(bone.gameObject);

#pragma warning restore 0162
			}


			SaveHullsToAsset();
		}

		private void SaveHullsToAsset()
		{
			if (TargetEditorData.runtimeData == null)
			{
				// TODO: Ensure this isn't possible? (currently possible if code throws an exception at just the wrong place)
				Debug.LogError("Can't write hull meshes to asset as there's no valid runtime data asset");
				return;
			}

			// TODO: This destroys and recreates all the sub-assets fresh each time
			// Ideally we should reuse existing ones and update them and create new ones when they don't exist (and delete any that are no-longer referenced)
			// That would prevent references being dropped elsewhere in the project
			// This might involve updating CreateHullMesh to reuse existing meshes where appropriate

			Transform[] bones = TargetRenderer.bones;

			List<Mesh> hullMeshes = new List<Mesh>();

			for (int i = 0; i < bones.Length; i++)
			{
				Transform bone = bones[i];
				BoneHullData[] data = TargetEditorData.GetBoneHullData(bone.name);
				foreach (BoneHullData hull in data)
				{
					if (hull.hullMesh != null)
						hullMeshes.Add(hull.hullMesh);
				}
			}
			Console.output.Log("Found " + hullMeshes.Count + " hull meshes to save to runtime asset");

			// Find the runtime data + path to it
			SkinnedColliderRuntimeData runtimeData = TargetEditorData.runtimeData;
			string runtimeDataPath = AssetDatabase.GetAssetPath(runtimeData);
			Console.output.Log("Saving hull colliders to asset: " + runtimeDataPath);

			// Remove all existing meshes in the asset
			foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(runtimeDataPath))
			{
				if (o is SkinnedColliderRuntimeData)
					continue;

#if UNITY_2018_4_OR_NEWER
				AssetDatabase.RemoveObjectFromAsset(o);
#else
				// TODO FIXME - figure out how to reimplement this for 5.6
#endif
			}

			// Save all the meshes in the runtime asset
			foreach (Mesh hull in hullMeshes)
			{
				Console.output.Log(string.Format("Adding {0} to asset at path {1}", hull, runtimeDataPath));
				AssetDatabase.AddObjectToAsset(hull, runtimeDataPath);
			}
			EditorUtility.SetDirty(TargetEditorData.runtimeData);
		}

		public static Mesh CreateHullMesh(SkinnedMeshRenderer skinnedRenderer, UnpackedMesh unpackedMesh, Transform bone, BoneHullData hull, CoordSpace outputSpace)
		{
			List<Point3d> hullPoints = null;

			if (hull.type == HullType.Auto)
			{
				hullPoints = FindAutoHullPoints(skinnedRenderer, unpackedMesh, bone, hull.MinThreshold, hull.MaxThreshold, outputSpace);
			}
			else if (hull.type == HullType.Manual)
			{
				hullPoints = FindManualHullPoints(skinnedRenderer, unpackedMesh, bone, hull.GetSelectedFaces(), outputSpace);
			}

			return CalculateConvexHull(bone.name, hullPoints, hull.maxPlanes);
		}

		public static List<Point3d> FindAutoHullPoints(SkinnedMeshRenderer skinnedRenderer, UnpackedMesh unpackedMesh, Transform bone, float minThreshold, float maxThreshold, CoordSpace outputSpace)
		{
			List<Point3d> hullPoints = new List<Point3d>();

			BoneWeight[] weights = unpackedMesh.BoneWeights;
			Transform[] bones = skinnedRenderer.bones;

			int ownBoneIndex = -1;
			for (int i = 0; i < bones.Length; i++)
			{
				if (bones[i] == bone)
				{
					ownBoneIndex = i;
					break;
				}
			}

			for (int i = 0; i < weights.Length; i++)
			{
				BoneWeight w = weights[i];
				if (Utils.IsWeightAboveThreshold(w.boneIndex0, w.weight0, ownBoneIndex, minThreshold, maxThreshold)
					|| Utils.IsWeightAboveThreshold(w.boneIndex1, w.weight1, ownBoneIndex, minThreshold, maxThreshold)
					|| Utils.IsWeightAboveThreshold(w.boneIndex2, w.weight2, ownBoneIndex, minThreshold, maxThreshold)
					|| Utils.IsWeightAboveThreshold(w.boneIndex3, w.weight3, ownBoneIndex, minThreshold, maxThreshold))
				{
					Point3d p = ConvertToPoint(unpackedMesh.ModelSpaceVertices[i], outputSpace, skinnedRenderer, bone);
					hullPoints.Add(p);
				}
			}

			return hullPoints;
		}

		private static Vector3 Convert(Vector3 modelSpaceVert, CoordSpace outputSpace, SkinnedMeshRenderer skinnedRenderer, Transform bone)
		{
			if (outputSpace == CoordSpace.Model)
			{
				return modelSpaceVert;
			}
			else if (outputSpace == CoordSpace.World)
			{
				Vector3 worldPos = skinnedRenderer.rootBone.parent.TransformPoint(modelSpaceVert);
				return worldPos;
			}
			else if (outputSpace == CoordSpace.Bone)
			{
				Vector3 worldPos = skinnedRenderer.rootBone.parent.TransformPoint(modelSpaceVert);
				Vector3 boneLocalPos = bone.InverseTransformPoint(worldPos);
				return boneLocalPos;
			}
			return Vector3.zero;
		}

		private static Point3d ConvertToPoint(Vector3 modelSpaceVert, CoordSpace outputSpace, SkinnedMeshRenderer skinnedRenderer, Transform bone)
		{
			Vector3 v = Convert(modelSpaceVert, outputSpace, skinnedRenderer, bone);
			return new Point3d(v.x, v.y, v.z);
		}

		private static List<Point3d> FindManualHullPoints(SkinnedMeshRenderer skinnedRenderer, UnpackedMesh unpackedMesh, Transform bone, int[] selectedFaceIndices, CoordSpace outputSpace)
		{
			List<Point3d> hullPoints = new List<Point3d>();

			int[] indices = unpackedMesh.Indices;
			Vector3[] vertices = unpackedMesh.ModelSpaceVertices;

			for (int i=0; i< selectedFaceIndices.Length; i++)
			{
				int baseIndex = selectedFaceIndices[i] * 3;
				Vector3 v0 = vertices[indices[baseIndex]];
				Vector3 v1 = vertices[indices[baseIndex+1]];
				Vector3 v2 = vertices[indices[baseIndex+2]];

				hullPoints.Add(ConvertToPoint(v0, outputSpace, skinnedRenderer, bone));
				hullPoints.Add(ConvertToPoint(v1, outputSpace, skinnedRenderer, bone));
				hullPoints.Add(ConvertToPoint(v2, outputSpace, skinnedRenderer, bone));
			}
			
			return hullPoints;
		}

		private static Mesh CalculateConvexHull(string meshName, List<Point3d> hullPoints, int maxPlanes)
		{
			// TODO: There's quite a bit of duplication here between this and the same hull calculation in Hull.GenerateConvexHull
			// Ideally we'd share more logic (or at least port the robustness additions to this version)
			// Also see QHullUtil.FindConvexHull to hide some of this complexity

			if (hullPoints == null || hullPoints.Count == 0)
				return null;

			//Console.output.Log("CalculateConvexHull(" + meshName + ") with " + hullPoints.Count + " points");


			QuickHull3D qHull = new QuickHull3D();
			try
			{
				qHull.build(hullPoints.ToArray());
			}
			catch (CoplanarException ex)
			{
				Console.output.LogError(Console.Technie, "Could not generate hull: " + ex);
			}
			catch (System.Exception ex)
			{
				Console.output.LogError(Console.Technie, "Could not generate hull: " + ex);
			}

			// Get calculated hull vertices and indices

			Point3d[] hullVertices = qHull.getVertices();
			int[][] hullFaceIndices = qHull.getFaces();

			if (hullVertices == null || hullVertices.Length == 0)
			{
				Console.output.LogError(Console.Technie, "Calculated convex hull has zero vertices");
				return null;
			}

			if (hullFaceIndices == null || hullFaceIndices.Length == 0)
			{
				Console.output.LogError(Console.Technie, "Calculated convex hull has zero indices");
				return null;
			}

			// Convert to dest vertices

			Vector3[] destVertices = new Vector3[hullVertices.Length];
			for (int i = 0; i < destVertices.Length; i++)
			{
				destVertices[i] = new Vector3((float)hullVertices[i].x, (float)hullVertices[i].y, (float)hullVertices[i].z);
			}

			// Convert to dest indices

			List<int> destIndices = new List<int>();

			for (int i = 0; i < hullFaceIndices.Length; i++)
			{
				int faceVerts = hullFaceIndices[i].Length;
				for (int j = 1; j < faceVerts - 1; j++)
				{
					destIndices.Add(hullFaceIndices[i][0]);
					destIndices.Add(hullFaceIndices[i][j]);
					destIndices.Add(hullFaceIndices[i][j + 1]);
				}
			}


			// TEST - SIMPLIFY!
			
			if (qHull.getNumFaces() > maxPlanes)
			{
				Mesh tmpMesh = new Mesh();
				tmpMesh.vertices = destVertices;
				tmpMesh.triangles = destIndices.ToArray();

				HullSimplifier simplifier = new HullSimplifier();
				Mesh simplifiedMesh = simplifier.Simplify(tmpMesh, maxPlanes, HullSimplifier.PlaneSelection.DisparateAngle, HullSimplifier.HoleFillMethod.SortVertices);

				destVertices = simplifiedMesh.vertices;
				destIndices = new List<int>(simplifiedMesh.triangles);
			}
			

			// Create the final mesh

			Mesh hullMesh = new Mesh();
			hullMesh.name = meshName;
			hullMesh.vertices = destVertices;
			hullMesh.triangles = destIndices.ToArray();

			hullMesh.RecalculateNormals();
			hullMesh.RecalculateBounds();

			return hullMesh;
		}

		// Ensure we have BoneData entries for all bones, keep existing data if it already exists or create default data otherwise
		private void PopulateBoneData()
		{
			if (TargetEditorData == null)
				return;

			foreach (Transform bone in TargetRenderer.bones)
			{
				BoneData data = TargetEditorData.GetBoneData(bone.name);
				if (data == null)
				{
					CreateDefaultBone(bone);
				}
			}
			TargetEditorData.MarkDirty();
		}

		private BoneData CreateDefaultBone(Transform bone)
		{
			BoneData data = new BoneData(bone);
			data.addRigidbody = false;
			data.addJoint = false;

			TargetEditorData.Add(data);

			return data;
		}

		private BoneData CreateAutoBone(Transform bone)
		{
			// Get existing data or create a new one if it doesn't exist
			BoneData data = TargetEditorData.GetBoneData(bone);
			if (data == null)
			{
				data = new BoneData(bone);
				TargetEditorData.Add(data);
			}

			// Reset to auto defaults
			data.addRigidbody = true;
			data.isKinematic = true;
			data.addJoint = false;
			data.jointType = BoneJointType.BallAndSocket;

			return data;
		}

		private bool HasAutoBone(Transform srcBone)
		{
			BoneData data = TargetEditorData.GetBoneData(srcBone);
			return (data != null && data.addRigidbody);
		}
		
		private bool HasAutoHull(Transform srcBone)
		{
			BoneHullData[] hullData = TargetEditorData.GetBoneHullData(srcBone);
			foreach (BoneHullData data in hullData)
			{
				if (data.type == HullType.Auto)
					return true;
			}
			return false;
		}

		private BoneHullData CreateAutoHull(Transform srcBone)
		{
			BoneHullData data = new BoneHullData();
			data.type = HullType.Auto;
			data.targetBoneName = srcBone.name;
			data.previewColour = GizmoUtils.GetHullColour(TargetEditorData.boneHullData.Count);

			data.SetThresholds(DEFAULT_MIN_THRESHOLD, DEFAULT_MAX_THRESHOLD, TargetRenderer, TargetRenderer.sharedMesh);

			TargetEditorData.Add(data);

			return data;
		}

		private BoneHullData CreateManualHull(Transform srcBone)
		{
			BoneHullData data = new BoneHullData();
			data.type = HullType.Manual;
			data.targetBoneName = srcBone.name;

			data.previewColour = GizmoUtils.GetHullColour(TargetEditorData.boneHullData.Count);

			TargetEditorData.Add(data);

			return data;
		}

		public bool ShouldReceiveShortcuts()
		{
			SkinnedColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<SkinnedColliderCreator>();

			return (currentHullPainter != null && currentHullPainter.editorData != null);
		}

		public void StopPainting()
		{
			SkinnedColliderCreator creatorComponent = sceneManipulator.GetCreatorComponent<SkinnedColliderCreator>();

			if (creatorComponent != null && creatorComponent.editorData != null)
			{
				creatorComponent.editorData.ClearSelection();
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

		public void OnSceneGUI(SceneView sceneView)
		{
			if (TargetComponent == null)
				return;

			if (TargetEditorData == null)
				return;
			
			if (TargetRenderer == null)
				return;

			//Console.output.Log("Skinned.OnSceneGUI " + Time.time);

			BoneData selectedBoneData = TargetEditorData.GetSelectedBone();
			BoneHullData selectedHull = TargetEditorData.GetSelectedHull();
			Transform selectedBone = TargetComponent.FindBone(selectedBoneData);

			if (selectedBone != null && TargetEditorData != null)
			{
				//Console.output.Log("Draw handle for +" + selectedBone);

				BoneData boneData = TargetEditorData.GetBoneData(selectedBone.name);
				if (boneData != null && boneData.addJoint)
				{
					if (boneData.jointType == BoneJointType.Hinge)
					{
						DrawPrimaryArc(selectedBone, boneData);

					}
					else if (boneData.jointType == BoneJointType.BallAndSocket)
					{
						DrawPrimaryArc(selectedBone, boneData);
						DrawSecondaryArc(selectedBone, boneData, boneData.secondaryAngularLimit, boneData.secondaryAxis, boneData.GetThirdAxis(), new Color(0, 1, 0, 0.3f));
						DrawSecondaryArc(selectedBone, boneData, boneData.tertiaryAngularLimit, boneData.GetThirdAxis(), boneData.secondaryAxis, new Color(0, 0, 1, 0.3f));
					}
					else if (boneData.jointType == BoneJointType.Tentacle)
					{
						DrawTranslationAxis(selectedBone, boneData, boneData.primaryAxis, new Color(0, 1, 1, 0.3f));

						DrawPrimaryArc(selectedBone, boneData);
						DrawSecondaryArc(selectedBone, boneData, boneData.secondaryAngularLimit, boneData.secondaryAxis, boneData.GetThirdAxis(), new Color(0, 1, 0, 0.3f));
						DrawSecondaryArc(selectedBone, boneData, boneData.tertiaryAngularLimit, boneData.GetThirdAxis(), boneData.secondaryAxis, new Color(0, 0, 1, 0.3f));
					}
				}

				HandleUtility.Repaint();
			}

			if (selectedHull != null)
			{
				Transform bone = TargetComponent.FindBone(selectedHull);
				if (bone != null)
				{
					//Handles.PositionHandle(bone.position, bone.rotation);
					//HandleUtility.Repaint();
				}
			}

			SkinnedColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<SkinnedColliderCreator>();
			if (currentHullPainter != null && currentHullPainter.HasEditorData())
			{
				DrawWireframe();

				sceneManipulator.ProcessSceneEvents(unpackedMesh);
				sceneManipulator.DrawCustomCursor();
				sceneManipulator.DrawBrushCursor();
			}
			EnsureHierarchySelectionVisible(selectedBoneData, selectedHull);
		}

		private void EnsureHierarchySelectionVisible(BoneData prevSelectedBone, BoneHullData prevSelectedHull)
		{
			if (TargetEditorData == null)
				return;

			BoneData newSelectedBoneData = TargetEditorData.GetSelectedBone();
			BoneHullData newSelectedHull = TargetEditorData.GetSelectedHull();

			if (newSelectedHull != null && newSelectedHull != prevSelectedHull)
			{
				Console.output.Log("pick selection to " + (newSelectedHull != null ? newSelectedHull.targetBoneName : "<null>"));

				ExpandHierarchy(newSelectedHull);
				doAutoScrollToSelection = true;

				Repaint();
			}
			if (newSelectedBoneData != null && newSelectedBoneData != prevSelectedBone)
			{
				Console.output.Log("pick selection to " + (newSelectedBoneData != null ? newSelectedBoneData.targetBoneName : "<null>"));

				ExpandHierarchy(newSelectedBoneData);
				doAutoScrollToSelection = true;

				Repaint();
			}
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

		private void DrawWireframe()
		{
			if (!showWireframe)
				return;

			SkinnedColliderCreator currentHullPainter = sceneManipulator.GetCreatorComponent<SkinnedColliderCreator>();
			if (currentHullPainter != null && currentHullPainter.editorData != null && unpackedMesh != null && Camera.current != null)
			{
				CommonUi.DrawWireframe(unpackedMesh.ModelSpaceTransform, unpackedMesh.ModelSpaceVertices, unpackedMesh.Indices, wireframeFactor);
			}
		}

		private static void DrawTranslationAxis(Transform bone, BoneData boneData, Vector3 primaryAxis, Color colour)
		{
			// Do all the handle drawing in local bone space (which might be different from joint primary/secondary axies)
			Handles.matrix = Matrix4x4.TRS(bone.position, Quaternion.LookRotation(bone.forward, bone.up), Vector3.one);

			// Translation is allowed from -limit to +limit
			Vector3 p0 = boneData.primaryAxis * -boneData.translationLimit;
			Vector3 p1 = boneData.primaryAxis * boneData.translationLimit;

			Handles.color = colour;

			Handles.DrawLine(p0, p1);
			Handles.SphereHandleCap(-1, p0, Quaternion.identity, 0.02f, Event.current.type);
			Handles.SphereHandleCap(-1, p1, Quaternion.identity, 0.02f, Event.current.type);

			Handles.matrix = Matrix4x4.identity;
		}


		private static void DrawPrimaryArc(Transform bone, BoneData boneData)
		{
			Handles.color = new Color(1.0f, 1.0f, 0.0f, 0.3f);

			float angleRange = boneData.primaryUpperAngularLimit - boneData.primaryLowerAngularLimit;

			// Do all the handle drawing in local bone space (which might be different from joint primary/secondary axies)
			Handles.matrix = Matrix4x4.TRS(bone.position, Quaternion.LookRotation(bone.forward, bone.up), Vector3.one);

			// NB: To make the arc match up with the way ConfigurableJoint displays these, we have to invert the primary axis (the rotation +/- goes the other way)
			Vector3 invertedPrimaryAxis = -boneData.primaryAxis;

			// Find the start edge of the arc to draw (take the secondary axis and rotate by the lower limit angle)
			Vector3 arcStartVec = Quaternion.AngleAxis(boneData.primaryLowerAngularLimit, invertedPrimaryAxis) * boneData.secondaryAxis;

			// Draw the arc between the start edge and the total range of the min/max arc angles
			Handles.DrawSolidArc(Vector3.zero, invertedPrimaryAxis, arcStartVec, angleRange, 0.1f);

			Handles.matrix = Matrix4x4.identity;
		}

		private static void DrawSecondaryArc(Transform bone, BoneData boneData, float limitAngle, Vector3 rotateAxis, Vector3 arcOriginAxis, Color colour)
		{
			Handles.color = colour;

			// Do all the handle drawing in local bone space (which might be different from joint primary/secondary axies)
			Handles.matrix = Matrix4x4.TRS(bone.position, Quaternion.LookRotation(bone.forward, bone.up), Vector3.one);

			// NB: To make the arc match up with the way ConfigurableJoint displays these, we have to invert the primary axis (the rotation +/- goes the other way)
			Vector3 invertedSecondaryAxis = rotateAxis;


			Vector3 arcStartVec = Quaternion.AngleAxis(-limitAngle, invertedSecondaryAxis) * arcOriginAxis;


			Handles.DrawSolidArc(Vector3.zero, invertedSecondaryAxis, arcStartVec, limitAngle * 2.0f, 0.1f);

			Handles.matrix = Matrix4x4.identity;
		}
	}
}
