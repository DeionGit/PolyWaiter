
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;

namespace Technie.PhysicsCreator
{
	public class CommonUi
	{
		// The actual install path with be detected at runtime with FindInstallPath
		// If for some reason that fails, the default install path will be used instead
		public const string DEFAULT_INSTALL_PATH = "Assets/Technie/PhysicsCreator/";

		public static string FindInstallPath()
		{
			return FindAssetPath<PhysicsCreatorInstallRoot>(DEFAULT_INSTALL_PATH);
		}

		public static string FindDataPath()
		{
			string path = FindAssetPath<PhysicsCreatorHullFolder>("");

			// Check to see if the data folder exists
			int lastSlash = path.LastIndexOf('/');
			string dir = lastSlash > 0 ? path.Substring(0, lastSlash) : "";
			if (!AssetDatabase.IsValidFolder(dir))
			{
				// Create the folder
				if (!AssetDatabase.IsValidFolder("Assets/Physics Hulls"))
					AssetDatabase.CreateFolder("Assets", "Physics Hulls");

				// Create the marker asset
				PhysicsCreatorHullFolder markerAsset = ScriptableObject.CreateInstance<PhysicsCreatorHullFolder>();
				AssetDatabase.CreateAsset(markerAsset, "Assets/Physics Hulls/PhysicsCreatorHullFolder.asset");

				// Refetch the path now we've created it
				path = "Assets/Physics Hulls/";
			}

			return path;
		}

		public static string FindAssetPath<T>(string defaultPath) where T : ScriptableObject
		{
			string installPath = defaultPath;

			string[] foundIds = AssetDatabase.FindAssets(string.Format("{0} t:{0}", typeof(T).Name));
			if (foundIds.Length > 0)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(foundIds[0]);
				int lastSlashPos = assetPath.LastIndexOf("/");
				if (lastSlashPos != -1)
				{
					string newInstallPath = assetPath.Substring(0, lastSlashPos + 1);
					installPath = newInstallPath;
				}
			}

			return installPath;
		}

		public static void DrawWireframeUi(ref bool showWireframe, ref float wireframeFactor, float[] collumnWidths)
		{
			Profiler.BeginSample("DrawWireframeUi");

			float prevWireframeFactor = wireframeFactor;
			bool prevShowWireframe = showWireframe;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Draw wireframe", GUILayout.Width(collumnWidths[0]));

				GUI.enabled = showWireframe;
				wireframeFactor = GUILayout.HorizontalSlider(wireframeFactor, -1.0f, 1.0f);
				GUI.enabled = true;

				if (GUILayout.Button(showWireframe ? Icons.Active.triggerOnIcon : Icons.Active.triggerOffIcon, GUILayout.Width(collumnWidths[2])))
				{
					showWireframe = !showWireframe;
				}
			}
			GUILayout.EndHorizontal();

			if (wireframeFactor != prevWireframeFactor || showWireframe != prevShowWireframe)
			{
				//Console.output.Log("Wireframe settings changed - triggering repaint");
				SceneView.RepaintAll();
			}

			Profiler.EndSample();
		}

		public static void DrawDimmingUi(ref bool dimInactiveHulls, ref float dimFactor, float[] collumnWidths)
		{
			bool prevDimInactive = dimInactiveHulls;
			float prevDimFactor = dimFactor;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Dim other hulls", GUILayout.Width(collumnWidths[0]));

				GUI.enabled = dimInactiveHulls;
				dimFactor = GUILayout.HorizontalSlider(dimFactor, 0.0f, 1.0f);
				GUI.enabled = true;

				if (GUILayout.Button(dimInactiveHulls ? Icons.Active.triggerOnIcon : Icons.Active.triggerOffIcon, GUILayout.Width(collumnWidths[2])))
				{
					dimInactiveHulls = !dimInactiveHulls;
				}
			}
			GUILayout.EndHorizontal();

			if (dimInactiveHulls != prevDimInactive || dimFactor != prevDimFactor)
			{
				SceneView.RepaintAll();
			}
		}

		public static void DrawHullAlphaUi(ref float hullAlpha, float[] collumnWidths)
		{
			float prevAlpha = hullAlpha;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Hull transparency", GUILayout.Width(collumnWidths[0]));

			//	GUI.enabled = dimInactiveHulls;
				hullAlpha = GUILayout.HorizontalSlider(hullAlpha, 0.0f, 1.0f);
				//	GUI.enabled = true;

				//	if (GUILayout.Button(dimInactiveHulls ? Icons.Active.triggerOnIcon : Icons.Active.triggerOffIcon, GUILayout.Width(collumnWidths[2])))
				//	{
				//		dimInactiveHulls = !dimInactiveHulls;
				//	}
				GUILayout.Label("", GUILayout.Width(collumnWidths[2]));
			}
			GUILayout.EndHorizontal();

			if (hullAlpha != prevAlpha)
			{
				SceneView.RepaintAll();
			}
		}

		public static float[] CalcSettingCollumns()
		{
			float firstColWidth = 145;
			float lastColWidth = 90;

			float baseWidth = EditorGUIUtility.currentViewWidth - (30 * EditorGUIUtility.pixelsPerPoint); // -20px for window chrome
			float fixedWidth = firstColWidth + lastColWidth + 4;
			float flexibleWidth = baseWidth - fixedWidth;
			float[] collumnWidth =
			{
					firstColWidth,
					flexibleWidth,
					lastColWidth,
			};
			return collumnWidth;
		}

		public static float[] CalcSettingCollumns2()
		{
			float firstColWidth = 0;
			float lastColWidth = 90;

			float baseWidth = EditorGUIUtility.currentViewWidth - (30 * EditorGUIUtility.pixelsPerPoint); // -20px for window chrome
			float fixedWidth = firstColWidth + lastColWidth + 4;
			float flexibleWidth = baseWidth - fixedWidth;

			float[] collumnWidth =
			{
					flexibleWidth * 0.4f,
					flexibleWidth * 0.6f,
					lastColWidth,
			};
			return collumnWidth;
		}

		private static Vector3[] transformedVertices = new Vector3[256];
		private static Vector3[] handleLines = new Vector3[256];

		public static void DrawWireframe(Transform modelTransform, Vector3[] vertices, int[] indices, float wireframeFactor)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			Profiler.BeginSample("DrawWireframe");

			if (wireframeFactor < 0.0f)
				Handles.color = new Color(0.0f, 0.0f, 0.0f, -wireframeFactor);
			else
				Handles.color = new Color(1.0f, 1.0f, 1.0f, wireframeFactor);
			
			Matrix4x4 localToWorld = Utils.CreateSkewableTRS(modelTransform);

			if (transformedVertices.Length < vertices.Length)
				System.Array.Resize(ref transformedVertices, vertices.Length);

			for (int i=0; i<vertices.Length; i++)
			{
				transformedVertices[i] = localToWorld.MultiplyPoint(vertices[i]);
			}

			if (handleLines.Length != indices.Length * 2)
				System.Array.Resize(ref handleLines, indices.Length * 2);

			for (int i = 0; i < indices.Length; i += 3)
			{
				int i0 = indices[i];
				int i1 = indices[i + 1];
				int i2 = indices[i + 2];

				Vector3 v0 = transformedVertices[i0];
				Vector3 v1 = transformedVertices[i1];
				Vector3 v2 = transformedVertices[i2];

				int j = i * 2;
				handleLines[j] = v0;
				handleLines[j+1] = v1;

				handleLines[j+2] = v1;
				handleLines[j+3] = v2;

				handleLines[j+4] = v2;
				handleLines[j+5] = v0;
			}

			Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
			Handles.DrawLines(handleLines);

			Profiler.EndSample();
		}

	}

} // namespace Technie.PhysicsCreator
