using UnityEngine;
using UnityEditor;

namespace Technie.PhysicsCreator
{
	public class Icons : ScriptableObject
	{
		[Header("Common")]
		public Texture2D technieIcon;
		public Texture2D addHullIcon;
		public Texture2D errorIcon;
		public Texture2D deleteIcon;
		public Texture2D deleteCollidersIcon;
		public Texture2D paintOnIcon;
		public Texture2D paintOffIcon;
		public Texture2D triggerOnIcon;
		public Texture2D triggerOffIcon;
		public Texture2D isChildIcon;
		public Texture2D nonChildIcon;
		public Texture2D preciseBrushIcon;
		public Texture2D smallBrushIcon;
		public Texture2D mediumBrushIcon;
		public Texture2D largeBrushIcon;
		public Texture2D pipetteIcon;
		public Texture2D hullVisibleIcon;
		public Texture2D hullInvisibleIcon;
		
		public Texture2D toolsIcons;
		public Texture2D settingsIcon;
		public Texture2D defaultsIcon;
		public Texture2D assetsIcon;
		
		public Texture2D axisAlignedIcon;
		public Texture2D minimizeVolumeIcon;
		public Texture2D alignFacesIcon;
		public Texture2D generateIcon;
		public Texture2D paintAllIcon;
		public Texture2D paintNoneIcon;
		public Texture2D autoHullSettingsIcon;
		public Texture2D growIcon;
		public Texture2D shrinkIcon;
		public Texture2D invertIcon;
		public Texture2D otherIcon;

		[Header("Rigid")]
		public Texture2D hullsIcon;

		[Header("Skinned")]
		public Texture2D boneIcon;
		public Texture2D hullIcon;
		public Texture2D colourBlobIcon;
		public Texture2D hasRigidbodyIcon;
		public Texture2D isKinematicIcon;
		public Texture2D hasJointIcon;
		public Texture2D foldoutCollapsedIcon;
		public Texture2D foldoutExpandedIcon;
		public Texture2D actionsIcon;
		public Texture2D hierarchyIcon;
		public Texture2D propertiesIcon;
		public Texture2D autoSetupIcon;
		public Texture2D autoSetupIconSmall;
		public Texture2D remoteBodyDataIcon;
		public Texture2D removeHullDataIcon;

		[Header("Debug")]
		public Texture2D default12;
		public Texture2D default14;
		public Texture2D default16;
		public Texture2D default18;
		public Texture2D default22;
		public Texture2D default24;
		public Texture2D default32;


		// Static cache

		private static Icons darkIcons;
		private static Icons lightIcons;

		public static Icons Active
		{
			get
			{
				if (darkIcons == null || lightIcons == null)
				{
					string installPath = CommonUi.FindInstallPath();
					darkIcons = AssetDatabase.LoadAssetAtPath<Icons>(installPath + "Icons/IconSetDark.asset");
					lightIcons = AssetDatabase.LoadAssetAtPath<Icons>(installPath + "Icons/IconSetLight.asset");
				}

				if (EditorGUIUtility.isProSkin)
					return darkIcons;
				else
					return lightIcons;
			}
		}
	}
	
} // namespace Technie.PhysicsCreator
