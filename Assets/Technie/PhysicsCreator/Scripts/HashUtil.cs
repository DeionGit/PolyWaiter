
using UnityEngine;
using System.Security.Cryptography;

namespace Technie.PhysicsCreator
{
	[System.Serializable]
	public class Hash160
	{
		public byte[] data;

		public Hash160()
		{
			this.data = new byte[0];
		}

		public Hash160(byte[] data)
		{
			this.data = data;
		}

		public bool IsValid()
		{
			return this.data != null && this.data.Length > 0;
		}

		public override int GetHashCode()
		{
			if (data == null)
				return 0;

			int value = 0;
			for (int i = 0; i < data.Length; i += 4)
			{
				value |= data[i + 1];
				value |= data[i + 1] << 8;
				value |= data[i + 1] << 16;
				value |= data[i + 1] << 24;
			}
			return value;
		}

		public override bool Equals(object obj)
		{
			Hash160 other = obj as Hash160;
			if (other == null)
				return false;

			if (ReferenceEquals(this.data, other.data))
				return true;

			if (this.data == null || other.data == null)
				return false;

			if (this.data.Length != other.data.Length)
				return false;

			for (int i = 0; i < data.Length; i++)
			{
				if (this.data[i] != other.data[i])
					return false;
			}
			return true;
		}

		public static bool operator==(Hash160 lhs, Hash160 rhs)
		{
			if (object.ReferenceEquals(lhs, null)) // NB: can't use ==null because we're inside operator==
			{
				if (object.ReferenceEquals(rhs, null))
				{
					return true;
				}
				return false;
			}
			return lhs.Equals(rhs);
		}

		public static bool operator !=(Hash160 lhs, Hash160 rhs)
		{
			return !(lhs == rhs);
		}
	}

	public class HashUtil
	{
		public static Hash160 CalcHash(Mesh srcMesh)
		{
			if (srcMesh == null)
				return new Hash160();

			System.DateTime startTime = System.DateTime.Now;

			HashAlgorithm algo = SHA1.Create();

			Vector3[] vertices = srcMesh.vertices;
			for (int i = 0; i < vertices.Length; i++)
			{
				byte[] tmp = ToBytes(vertices[i]);
				algo.TransformBlock(tmp, 0, tmp.Length, null, 0);
			}

			for (int s = 0; s < srcMesh.subMeshCount; s++)
			{
				int[] triangles = srcMesh.GetTriangles(s);

				for (int i = 0; i < triangles.Length; i++)
				{
					byte[] tmp = System.BitConverter.GetBytes(triangles[i]);
					algo.TransformBlock(tmp, 0, tmp.Length, null, 0);
				}
			}

			algo.TransformFinalBlock(new byte[0], 0, 0);

			byte[] outputHash = algo.Hash;

			System.DateTime finishTime = System.DateTime.Now;
			double elapsedTimeSecs = (finishTime - startTime).TotalSeconds;
			//Console.output.Log("Hash calculation took: " + elapsedTimeSecs.ToString("0.00"));

			return new Hash160(outputHash);
		}

		public static Hash160 CalcHash(string input)
		{
			HashAlgorithm algo = SHA1.Create();
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
			algo.TransformFinalBlock(bytes, 0, bytes.Length);
			byte[] outputHash = algo.Hash;
			return new Hash160(outputHash);
		}

		private static byte[] ToBytes(Vector3 vec)
		{
			byte[] result = new byte[4 * 3];

			byte[] xBytes = System.BitConverter.GetBytes(vec.x);
			byte[] yBytes = System.BitConverter.GetBytes(vec.x);
			byte[] zBytes = System.BitConverter.GetBytes(vec.x);

			xBytes.CopyTo(result, 0);
			yBytes.CopyTo(result, 4);
			zBytes.CopyTo(result, 8);

			return result;
		}
	}

} // namespace Technie.PhysicsCreator
