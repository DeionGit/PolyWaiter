/* C# port of QHull. See 'QHull Licence.txt' for license details */

namespace Technie.PhysicsCreator.QHull
{

	/**
	 * Represents the half-edges that surround each
	 * face in an anti-clockwise direction.
	 */
	public class HalfEdge
	{
		/**
		 * The vertex associated with the head of this half-edge.
		 */
		public Vertex vertex;

		/**
		 * Triangular face associated with this half-edge.
		 */
		public Face face;

		/**
		 * Next half-edge in the triangle.
		 */
		public HalfEdge next;

		/**
		 * Previous half-edge in the triangle.
		 */
		public HalfEdge prev;

		/**
		 * Half-edge associated with the opposite triangle
		 * adjacent to this edge.
		 */
		public HalfEdge opposite;

		/**
		 * Constructs a HalfEdge with head vertex <code>v</code> and
		 * left-hand triangular face <code>f</code>.
		 *
		 * @param v head vertex
		 * @param f left-hand triangular face
		 */
		public HalfEdge(Vertex v, Face f)
		{
			vertex = v;
			face = f;
		}

		public HalfEdge()
		{
		}

		/**
		 * Sets the value of the next edge adjacent
		 * (counter-clockwise) to this one within the triangle.
		 *
		 * @param edge next adjacent edge */
		public void setNext(HalfEdge edge)
		{
			next = edge;
		}

		/**
		 * Gets the value of the next edge adjacent
		 * (counter-clockwise) to this one within the triangle.
		 *
		 * @return next adjacent edge */
		public HalfEdge getNext()
		{
			return next;
		}

		/**
		 * Sets the value of the previous edge adjacent (clockwise) to
		 * this one within the triangle.
		 *
		 * @param edge previous adjacent edge */
		public void setPrev(HalfEdge edge)
		{
			prev = edge;
		}

		/**
		 * Gets the value of the previous edge adjacent (clockwise) to
		 * this one within the triangle.
		 *
		 * @return previous adjacent edge
		 */
		public HalfEdge getPrev()
		{
			return prev;
		}

		/**
		 * Returns the triangular face located to the left of this
		 * half-edge.
		 *
		 * @return left-hand triangular face
		 */
		public Face getFace()
		{
			return face;
		}

		/**
		 * Returns the half-edge opposite to this half-edge.
		 *
		 * @return opposite half-edge
		 */
		public HalfEdge getOpposite()
		{
			return opposite;
		}

		/**
		 * Sets the half-edge opposite to this half-edge.
		 *
		 * @param edge opposite half-edge
		 */
		public void setOpposite(HalfEdge edge)
		{
			opposite = edge;
			edge.opposite = this;
		}

		/**
		 * Returns the head vertex associated with this half-edge.
		 *
		 * @return head vertex
		 */
		public Vertex head()
		{
			return vertex;
		}

		/**
		 * Returns the tail vertex associated with this half-edge.
		 *
		 * @return tail vertex
		 */
		public Vertex tail()
		{
			return prev != null ? prev.vertex : null;
		}

		/**
		 * Returns the opposite triangular face associated with this
		 * half-edge.
		 *
		 * @return opposite triangular face
		 */
		public Face oppositeFace()
		{
			return opposite != null ? opposite.face : null;
		}

		/**
		 * Produces a string identifying this half-edge by the point
		 * index values of its tail and head vertices.
		 *
		 * @return identifying string
		 */
		public string getVertexString()
		{
			if (tail() != null)
			{
				return "" +
			   tail().index + "-" +
			   head().index;
			}
			else
			{
				return "?-" + head().index;
			}
		}

		/**
		 * Returns the length of this half-edge.
		 *
		 * @return half-edge length
		 */
		public double length()
		{
			if (tail() != null)
			{
				return head().pnt.distance(tail().pnt);
			}
			else
			{
				return -1;
			}
		}

		/**
		 * Returns the length squared of this half-edge.
		 *
		 * @return half-edge length squared
		 */
		public double lengthSquared()
		{
			if (tail() != null)
			{
				return head().pnt.distanceSquared(tail().pnt);
			}
			else
			{
				return -1;
			}
		}

	}

} // namespace Technie.PhysicsCreator.QHull
