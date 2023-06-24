/* C# port of QHull. See 'QHull Licence.txt' for license details */

namespace Technie.PhysicsCreator.QHull
{

	/**
	 * A three-element spatial point.
	 */
	public class Point3d : Vector3d
	{
		/**
		 * Creates a Point3d and initializes it to zero.
		 */
		public Point3d ()
		 {
		 }

		/**
		 * Creates a Point3d by copying a vector
		 *
		 * @param v vector to be copied
		 */
		public Point3d (Vector3d v)
		 {
		   set (v);
		 }

		/**
		 * Creates a Point3d with the supplied element values.
		 *
		 * @param x first element
		 * @param y second element
		 * @param z third element
		 */
		public Point3d (double x, double y, double z)
		 {
		   set (x, y, z);
		 }
	}

} // namespace QHull
