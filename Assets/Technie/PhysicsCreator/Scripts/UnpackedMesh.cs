using UnityEngine;

namespace Technie.PhysicsCreator
{
	public class UnpackedMesh
	{
		public SkinnedMeshRenderer SkinnedRenderer
		{
			get { return this.skinnedRenderer; }
		}

		public Mesh Mesh
		{
			get { return this.srcMesh; }
		}

		public Transform ModelSpaceTransform
		{
			get
			{
				if (skinnedRenderer != null)
					return skinnedRenderer.rootBone.parent;
				else
					return rigidRenderer.transform;
			}
		}

		public Vector3[] RawVertices
		{
			get
			{
				return vertices;
			}
		}

		public Vector3[] ModelSpaceVertices
		{
			get
			{
				return modelSpaceVertices;
			}
		}

		public BoneWeight[] BoneWeights
		{
			get { return weights; }
		}

		public int NumVertices
		{
			get { return vertices.Length; }
		}

		public int[] Indices
		{
			get { return this.indices; }
		}

		private MeshRenderer rigidRenderer;
		private SkinnedMeshRenderer skinnedRenderer;

		private Mesh srcMesh;
		private Vector3[] vertices;
		private Vector3[] normals;
		private BoneWeight[] weights;
		private int[] indices;

		// when unpacking MeshRenderers, model space is easy, it's just MeshRenderer.transform
		//
		// For SkinnedMeshRenderers, while it's tempting to use SkinnedMeshRenderer.transform as the 'model space' this transform doesn't really define the position of the model
		// Instead usually the SkinnedMeshRenderer component and the SkinnedMeshRenderer.rootBone have a common Transform which is the 'real' model space (usually where the Animator component is, but may be a child of it)
		// The exact hierarchy seems to depend on the file format used and exactly how the model was exported, so SkinnedMeshRenderer.rootBone.parent seems like the best definition of 'model space'
		// Unfortunately this causes us to scatter skinnedRenderer.rootBone.parent in various places, so you have to be careful of that

		private Vector3[] modelSpaceVertices;

		//private Vector3[] worldSpaceVertices; // transformed by the renderer's location (and the bindPose + bones if skinned)
		// TODO: add BoneSpace vertices?

		public static UnpackedMesh Create(Renderer renderer)
		{
			SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;
			MeshRenderer rigidRenderer = renderer as MeshRenderer;

			if (skinnedRenderer != null)
				return new UnpackedMesh(skinnedRenderer);
			else if (rigidRenderer != null)
				return new UnpackedMesh(rigidRenderer);
			else
				return null;
		}

		public UnpackedMesh(MeshRenderer rigidRenderer)
		{
			this.rigidRenderer = rigidRenderer;

			MeshFilter filter = rigidRenderer.GetComponent<MeshFilter>();
			this.srcMesh = filter != null ? filter.sharedMesh : null;

			if (srcMesh != null)
			{
				vertices = srcMesh.vertices;
				normals = srcMesh.normals;
				indices = srcMesh.triangles;
				weights = null;

				modelSpaceVertices = srcMesh.vertices; // Vertices are always model space for rigid meshes
			}
		}

		public UnpackedMesh(SkinnedMeshRenderer skinnedRenderer)
		{
			this.skinnedRenderer = skinnedRenderer;

			this.srcMesh = skinnedRenderer.sharedMesh;

			this.vertices = srcMesh.vertices;
			this.normals = srcMesh.normals;
			this.weights = srcMesh.boneWeights;
			this.indices = srcMesh.triangles;

			Transform[] bones = skinnedRenderer.bones;
			Transform rootBone = skinnedRenderer.rootBone;
			Transform outputLocalSpace = rootBone != null ? rootBone.parent : skinnedRenderer.transform.parent; // See comments on modelSpaceVertices declaration

			Matrix4x4[] bindPose = srcMesh.bindposes;

			this.modelSpaceVertices = new Vector3[vertices.Length];
			for (int i = 0; i < vertices.Length; i++)
			{
				if (rootBone != null) // tmp hack
					modelSpaceVertices[i] = ApplyBindPoseWeighted(vertices[i], weights[i], bindPose, bones, outputLocalSpace); // weights is zero-length. bindPose is zero-length, bones is zero-length
				else
					modelSpaceVertices[i] = vertices[i];
			}
		}


		private static Vector3 ApplyBindPoseWeighted(Vector3 inputVertex, BoneWeight weight, Matrix4x4[] bindPoses, Transform[] bones, Transform outputLocalSpace)
		{
			Vector3 v0 = bindPoses[weight.boneIndex0].MultiplyPoint(inputVertex);
			Vector3 v1 = bindPoses[weight.boneIndex1].MultiplyPoint(inputVertex);
			Vector3 v2 = bindPoses[weight.boneIndex2].MultiplyPoint(inputVertex);
			Vector3 v3 = bindPoses[weight.boneIndex3].MultiplyPoint(inputVertex);

			Vector3 worldSpace0 = bones[weight.boneIndex0].TransformPoint(v0);
			Vector3 worldSpace1 = bones[weight.boneIndex1].TransformPoint(v1);
			Vector3 worldSpace2 = bones[weight.boneIndex2].TransformPoint(v2);
			Vector3 worldSpace3 = bones[weight.boneIndex3].TransformPoint(v3);

			Vector3 worldSpace = (worldSpace0 * weight.weight0)
									+ (worldSpace1 * weight.weight1)
									+ (worldSpace2 * weight.weight2)
									+ (worldSpace3 * weight.weight3);
			Vector3 localSpace = outputLocalSpace.InverseTransformPoint(worldSpace);
			return localSpace;
		}
	}
	
} // namespace Technie.PhysicsCreator
