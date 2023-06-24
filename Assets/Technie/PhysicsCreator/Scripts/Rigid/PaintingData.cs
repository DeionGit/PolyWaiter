
using UnityEngine;
using System.Collections.Generic;
using Technie.PhysicsCreator.Rigid;

namespace Technie.PhysicsCreator
{
	public enum HullType
	{
		Box,
		ConvexHull,
		Sphere,
		Face,
		FaceAsBox,
		Auto,
		Capsule
	}

	public enum AutoHullPreset
	{
		Low,
		Medium,
		High,
		Placebo,
		Custom
	}

	// Enum which corresponds to the CapsuleCollider.direction int field
	public enum CapsuleAxis
	{
		X = 0,
		Y = 1,
		Z = 2
	}

	public struct BoxDef
	{
		public Bounds collisionBox; // The computed box data
		public Vector3 boxPosition; // The local position for the Box collider (when used as a child object)
		public Quaternion boxRotation; // The local rotation for the Box collider (when used as a child object)
	}

	public struct CapsuleDef
	{
		public Vector3 capsuleCenter;
		public CapsuleAxis capsuleDirection;
		public float capsuleRadius;
		public float capsuleHeight;
		public Vector3 capsulePosition;     // for child transforms
		public Quaternion capsuleRotation;  // for child transforms
	}




	public class PaintingData : ScriptableObject, IEditorData
	{
		public int TotalOutputColliders
		{
			get
			{
				int total = 0;
				foreach (Hull h in hulls)
				{
					if (h.type == HullType.Auto)
					{
						total += (h.autoMeshes != null) ? h.autoMeshes.Length : 0;
					}
					else
					{
						total++;
					}
				}
				return total;
			}
		}

		public Hash160 CachedHash
		{
			get { return sourceMeshHash; }
			set { this.sourceMeshHash = value; }
		}

		public bool HasCachedData
		{
			get
			{
				return sourceMeshHash != null && sourceMeshHash.IsValid();
			}
		}

		public Mesh SourceMesh
		{
			get
			{
				return sourceMesh;
			}
		}

		public IHull[] Hulls
		{
			get { return hulls.ToArray(); }
		}

		public bool HasSuppressMeshModificationWarning
		{
			get { return suppressMeshModificationWarning; }
		}

		// Serialised Data

		public HullData hullData;

		public Mesh sourceMesh;
		public Hash160 sourceMeshHash;

		public int activeHull = -1;

		public float faceThickness = 0.1f;

		public List<Hull> hulls = new List<Hull>();

		public AutoHullPreset autoHullPreset = AutoHullPreset.Medium;
		public VhacdParameters vhacdParams = new VhacdParameters();

		public bool hasLastVhacdTimings = false;
		public AutoHullPreset lastVhacdPreset = AutoHullPreset.Medium;
		public float lastVhacdDurationSecs = 0.0f;

		public bool suppressMeshModificationWarning = false;

		public Hull AddHull(HullType type, PhysicMaterial material, bool isChild, bool isTrigger)
		{
			hulls.Add( new Hull() );
			
			// Name the new hull
			hulls [hulls.Count - 1].name = "Hull " + hulls.Count;
			
			// Set selection to new hull
			activeHull = hulls.Count - 1;
			
			// Set the colour for the new hull
			hulls[hulls.Count-1].colour = GizmoUtils.GetHullColour(activeHull);
			hulls[hulls.Count-1].type = type;
			hulls[hulls.Count-1].material = material;
			hulls[hulls.Count-1].isTrigger = isTrigger;
			hulls[hulls.Count-1].isChildCollider = isChild;

			return hulls[activeHull];
		}

		public void RemoveHull (int index)
		{
			if (index < 0 || index >= hulls.Count)
				return;

			hulls [index].Destroy ();
			hulls.RemoveAt (index);
		}

		public void RemoveAllHulls ()
		{
			for (int i = 0; i < hulls.Count; i++)
			{
				hulls[i].Destroy();
			}
			hulls.Clear();
		}

		public bool HasActiveHull()
		{
			return activeHull >= 0 && activeHull < hulls.Count;
		}
		
		public Hull GetActiveHull()
		{
			if (activeHull < 0 || activeHull >= hulls.Count)
				return null;
			
			return hulls [activeHull];
		}

		
		

		



		

		
		public bool ContainsMesh(Mesh m)
		{
			foreach (Hull h in hulls)
			{
				if (h.collisionMesh == m)
					return true;
				if (h.faceCollisionMesh == m)
					return true;
				if (h.autoMeshes != null)
				{
					foreach (Mesh autoMesh in h.autoMeshes)
					{
						if (autoMesh == m)
							return true;
					}
				}
			}
			return false;
		}



		public bool HasAutoHulls()
		{
			foreach (Hull h in hulls)
			{
				if (h.type == HullType.Auto)
					return true;
			}
			return false;
		}

		public void SetAssetDirty()
		{
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
	}

} // namespace Technie.PhysicsCreator

