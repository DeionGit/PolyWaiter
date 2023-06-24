using UnityEngine;

namespace Technie.PhysicsCreator
{
	public interface IEditorData
	{
		bool HasCachedData
		{
			get;
		}
		
		Mesh SourceMesh
		{
			get;
		}
		
		Hash160 CachedHash
		{
			get;
			set;
		}
		
		IHull[] Hulls
		{
			get;
		}
		
		bool HasSuppressMeshModificationWarning
		{
			get;
		}
		
		void SetAssetDirty();
	}
}
