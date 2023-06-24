using System.Text;
using System.Collections.Generic;

using UnityEngine;

namespace Technie.PhysicsCreator.Skinned
{
	public class MeshCache
	{
		/** A single entry in the cache */
		private class CacheEntry
		{
			public Hash160 hash;
			public Mesh mesh;

			public CacheEntry(Hash160 h, Mesh m) { this.hash = h; this.mesh = m; }
		}

		private const int MAX_CACHE_SIZE = 256;

		private List<CacheEntry> entries = new List<CacheEntry>();

		public Hash160 CalcHash(SkinnedMeshRenderer skinnedRenderer, Transform bone, BoneHullData hull)
		{
			StringBuilder str = new StringBuilder();

			str.Append(skinnedRenderer.name);
			str.Append(bone.name);
			str.Append(hull.targetBoneName);
			str.Append(hull.type);
			str.Append(hull.maxPlanes);

			if (hull.type == HullType.Auto)
			{
				str.Append(hull.MinThreshold);
				str.Append(hull.MaxThreshold);
			}
			else
			{
				foreach (int i in hull.GetSelectedFaces())
				{
					str.Append(i);
				}
			}

			return HashUtil.CalcHash(str.ToString());
		}
		
		public bool FindExisting(Hash160 hash, out Mesh mesh)
		{
			foreach (CacheEntry entry in this.entries)
			{
				if (entry.hash == hash)
				{
					mesh = entry.mesh;
					return true;
				}
			}
			// Doesn't exist in the cache
			mesh = null;
			return false;
		}
		
		public void Add(Hash160 hash, Mesh mesh)
		{
			// Remove any existing with this hash
			foreach (CacheEntry entry in this.entries)
			{
				if (entry.hash == hash)
				{
					entries.Remove(entry);
					break;
				}
			}

			// Trim the cache down to size by removing oldest entries
			while (entries.Count > MAX_CACHE_SIZE)
			{
				entries.RemoveAt(0);
			}

			// Cache this mesh
			entries.Add(new CacheEntry(hash, mesh));
		}
	}
	
} // namespace Technie.PhysicsCreator.Skinned
