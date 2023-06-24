using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Need to have a think about exactly what ones we have here and how to name them
// Eg. do we have 'elbow' and 'knee' types even though they're both just a 1dof hinge?
//
// Weird joints to think about:
//	Base of thumb
//	Jaw
//	Spine
//	Wrist
//	Ankle (2-dof rotation)
//	Wheels?
//	Extendable (allows position movement as well as rotation, like tentacles) 'Noodle'?
//	'Hands off' - the user is going to put a joint here and configure it manually, so don't add/remove/change it
namespace Technie.PhysicsCreator.Skinned
{
	public enum BoneJointType
	{
		Fixed, // no translation or rotation (might have break force?)
		Hinge,
		BallAndSocket,  // 3dof rotation (todo: is a ball-and-socket actually 3dof? what's a 2dof joint? (ie. rotates but no twist)
		Tentacle, // like a ball-and-socket, but also allows translation along the primary axis (so 3dof rotation + 1dof translation)
	}

	public enum AxisType
	{
		XAxis,
		YAxis,
		ZAxis,
		Custom
	}

	// TODO: Think of a better name for this which is clearer separated from BoneHullData?
	[System.Serializable]
	public class BoneData
	{
		public string targetBoneName;

		public bool addRigidbody;
		public float mass = 1.0f;
		public float linearDrag = 0.0f;
		public float angularDrag = 0.05f;
		public bool isKinematic;

		// Joint Data

		public bool addJoint;

		public BoneJointType jointType = BoneJointType.Fixed;

		public Vector3 primaryAxis = Vector3.forward;
		public Vector3 secondaryAxis = Vector3.up;

		public float primaryLowerAngularLimit = 0.0f;   // in degrees
		public float primaryUpperAngularLimit = 0.0f;   // in degrees

		public float secondaryAngularLimit = 0.0f; // in degrees
		public float tertiaryAngularLimit = 0.0f;

		public float translationLimit = 0.0f;   // The max distance allowed from the idle position (ie. the object can move between -translationLimit to +translationLimit)
												// This mimics what the translation limit on the ConfigurableJoint uses

		public float linearDamping = 0.0f;
		public float angularDamping = 0.0f;
		
		public BoneData(Transform src)
		{
			this.targetBoneName = src.name;
		}
		
		public Vector3 GetThirdAxis()
		{
			return Vector3.Cross(primaryAxis, secondaryAxis);
		}
	}


	public enum HullType
	{
		Auto,
		Manual
	}

	public enum ColliderType
	{
		Convex,
		Capsule,
		Box,
		Sphere
	}


	public class SkinnedColliderEditorData : ScriptableObject, IEditorData
	{
		public const int INVALID_INDEX = -1;

		

		public SkinnedColliderRuntimeData runtimeData;

		// Rigidbody defaults
		public bool defaultAddRigidbody = true;
		public float defaultMass = 1.0f; // TODO: Would be nice to have default density (eg. 10 kg/m^3) and the final mass is calculated by taking the collider volume into account
		public float defaultLinearDrag = 0.0f;
		public float defaultAngularDrag = 0.05f;

		// Joint defaults
		public float defaultLinearDamping = 0.0f;
		public float defaultAngularDamping = 0.0f;

		// Collider defaults
		public PhysicMaterial defaultMaterial;
		public ColliderType defaultColliderType = ColliderType.Convex;
		public bool defaultIsTrigger = false;
		public int defaultMaxPlanes = 255;

		public List<BoneData> boneData = new List<BoneData>();
		public List<BoneHullData> boneHullData = new List<BoneHullData>();

		// Current selection - one of these will be valid (or neither)
		// TODO: Should these be serialised?
		//	pro: serializing them means selection is saved and restored between sessions
		//  con: serializing this means the asset file changes just by using the UI and not actually changing any data
		private int selectedBoneIndex = INVALID_INDEX;
		private int selectedHullIndex = INVALID_INDEX;

		private int lastModifiedFrame;

		// Source mesh
		public Mesh sourceMesh;
		public Hash160 sourceMeshHash;

		public bool suppressMeshModificationWarning = false;

		// Properties

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
			get { return sourceMesh; }
		}

		public IHull[] Hulls
		{
			get { return boneHullData.ToArray(); }
		}

		public	bool HasSuppressMeshModificationWarning
		{
			get { return suppressMeshModificationWarning; }
		}

		// Methods

		public void SetSelection(BoneData bone)
		{
			for (int i=0; i<boneData.Count; i++)
			{
				if (boneData[i] == bone)
				{
					selectedBoneIndex = i;
					selectedHullIndex = INVALID_INDEX;
					break;
				}
			}

			MarkDirty();
		}

		public void SetSelection(BoneHullData hull)
		{
			for (int i = 0; i < boneHullData.Count; i++)
			{
				if (boneHullData[i] == hull)
				{
					selectedHullIndex = i;
					selectedBoneIndex = INVALID_INDEX;
					break;
				}
			}

			MarkDirty();
		}

		public void ClearSelection()
		{
			selectedBoneIndex = INVALID_INDEX;
			selectedHullIndex = INVALID_INDEX;

			MarkDirty();
		}

		public BoneData GetSelectedBone()
		{
			if (selectedBoneIndex >= 0 && selectedBoneIndex < boneData.Count)
				return boneData[selectedBoneIndex];
			else
				return null;
		}

		public BoneHullData GetSelectedHull()
		{
			if (selectedHullIndex >= 0 && selectedHullIndex < boneHullData.Count)
				return boneHullData[selectedHullIndex];
			else
				return null;
		}



		public BoneData GetBoneData(Transform bone)
		{
			if (bone == null)
				return null;
			return GetBoneData(bone.name);
		}

		public BoneData GetBoneData(string boneName)
		{
			foreach (BoneData data in boneData)
			{
				if (data.targetBoneName == boneName)
					return data;
			}
			return null;
		}

		public BoneHullData[] GetBoneHullData(Transform bone)
		{
			if (bone == null)
				return new BoneHullData[0];
			return GetBoneHullData(bone.name);
		}

		public BoneHullData[] GetBoneHullData(string boneName)
		{
			List<BoneHullData> allHulls = new List<BoneHullData>();

			foreach (BoneHullData data in boneHullData)
			{
				if (data.targetBoneName == boneName)
					allHulls.Add(data);
			}

			return allHulls.ToArray();
		}

		public void SetAssetDirty()
		{
			MarkDirty();
		}

		public void MarkDirty()
		{
#if UNITY_EDITOR
			EditorUtility.SetDirty(this);

			lastModifiedFrame = Time.frameCount;
#endif
		}

		public int GetLastModifiedFrame()
		{
			return lastModifiedFrame;
		}

		public void Add(BoneData data)
		{
			boneData.Add(data);

			MarkDirty();
		}

		public void Remove(BoneData data)
		{
			boneData.Remove(data);

			MarkDirty();
		}

		public void Add(BoneHullData data)
		{
			boneHullData.Add(data);

			MarkDirty();
		}

		public void Remove(BoneHullData data)
		{
			boneHullData.Remove(data);

			MarkDirty();
		}
	}
}
