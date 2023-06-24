/* C# port of QHull. See 'QHull Licence.txt' for license details */

/**
 * Computes the convex hull of a set of three dimensional points.
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Technie.PhysicsCreator.QHull
{
	public class CoplanarException : Exception
	{
		public CoplanarException(string msg) : base(msg)
		{
			
		}
	}


	public class QuickHull3D
	{
		/**
		 * Specifies that (on output) vertex indices for a face should be
		 * listed in clockwise order.
		 */
		public const int CLOCKWISE = 0x1;

		/**
		 * Specifies that (on output) the vertex indices for a face should be
		 * numbered starting from 1.
		 */
		public const int INDEXED_FROM_ONE = 0x2;

		/**
		 * Specifies that (on output) the vertex indices for a face should be
		 * numbered starting from 0.
		 */
		public const int INDEXED_FROM_ZERO = 0x4;

		/**
		 * Specifies that (on output) the vertex indices for a face should be
		 * numbered with respect to the original input points.
		 */
		public const int POINT_RELATIVE = 0x8;

		/**
		 * Specifies that the distance tolerance should be
		 * computed automatically from the input point data.
		 */
		public const double AUTOMATIC_TOLERANCE = -1;

		protected int findIndex = -1;

		// estimated size of the point set
		protected double charLength;

		protected bool debug = false;

		protected Vertex[] pointBuffer = new Vertex[0];
		protected int[] vertexPointIndices = new int[0];
		private Face[] discardedFaces = new Face[3];

		private Vertex[] maxVtxs = new Vertex[3];
		private Vertex[] minVtxs = new Vertex[3];

		protected List<Face> faces = new List<Face>(16);
		protected List<HalfEdge> horizon = new List<HalfEdge>(16);

		private FaceList newFaces = new FaceList();
		private VertexList unclaimed = new VertexList();
		private VertexList claimed = new VertexList();

		protected int numVertices;
		protected int numFaces;
		protected int numPoints;

		protected double explicitTolerance = AUTOMATIC_TOLERANCE;
		protected double tolerance;

		/**
		 * Returns true if debugging is enabled.
		 *
		 */
		public bool getDebug()
		{
			return debug;
		}

		/**
		 * Enables the printing of debugging diagnostics.
		 *
		 * @param enable if true, enables debugging
		 */
		public void setDebug(bool enable)
		{
			debug = enable;
		}

		/**
		 * Precision of a double.
		 */
		private const double DOUBLE_PREC = 2.2204460492503131e-16;


		/**
		 * Returns the distance tolerance that was used for the most recently
		 * computed hull. The distance tolerance is used to determine when
		 * faces are unambiguously convex with respect to each other, and when
		 * points are unambiguously above or below a face plane, in the
		 * presence of <a href=#distTol>numerical imprecision</a>. Normally,
		 * this tolerance is computed automatically for each set of input
		 * points, but it can be set explicitly by the application.
		 *
		 * @return distance tolerance
		 * @see QuickHull3D#setExplicitDistanceTolerance
		 */
		public double getDistanceTolerance()
		{
			return tolerance;
		}

		/**
		 * Sets an explicit distance tolerance for convexity tests.
		 * If {@link #AUTOMATIC_TOLERANCE AUTOMATIC_TOLERANCE}
		 * is specified (the default), then the tolerance will be computed
		 * automatically from the point data.
		 *
		 * @param tol explicit tolerance
		 * @see #getDistanceTolerance
		 */
		public void setExplicitDistanceTolerance(double tol)
		{
			explicitTolerance = tol;
		}

		/**
		 * Returns the explicit distance tolerance.
		 *
		 * @return explicit tolerance
		 * @see #setExplicitDistanceTolerance
		 */
		public double getExplicitDistanceTolerance()
		{
			return explicitTolerance;
		}

		private void addPointToFace(Vertex vtx, Face face)
		{
			vtx.face = face;

			if (face.outside == null)
			{
				claimed.add(vtx);
			}
			else
			{
				claimed.insertBefore(vtx, face.outside);
			}
			face.outside = vtx;
		}

		private void removePointFromFace(Vertex vtx, Face face)
		{
			if (vtx == face.outside)
			{
				if (vtx.next != null && vtx.next.face == face)
				{
					face.outside = vtx.next;
				}
				else
				{
					face.outside = null;
				}
			}
			claimed.delete(vtx);
		}

		private Vertex removeAllPointsFromFace(Face face)
		{
			if (face.outside != null)
			{
				Vertex end = face.outside;
				while (end.next != null && end.next.face == face)
				{
					end = end.next;
				}
				claimed.delete(face.outside, end);
				end.next = null;
				return face.outside;
			}
			else
			{
				return null;
			}
		}

		/**
		 * Creates an empty convex hull object.
		 */
		public QuickHull3D()
		{
		}

		/**
		 * Creates a convex hull object and initializes it to the convex hull
		 * of a set of points whose coordinates are given by an
		 * array of doubles.
		 *
		 * @param coords x, y, and z coordinates of each input
		 * point. The length of this array will be three times
		 * the the number of input points.
		 * @throws IllegalArgumentException the number of input points is less
		 * than four, or the points appear to be coincident, colinear, or
		 * coplanar.
		 */
		public QuickHull3D(double[] coords)
		{
			build(coords, coords.Length / 3);
		}

		/**
		 * Creates a convex hull object and initializes it to the convex hull
		 * of a set of points.
		 *
		 * @param points input points.
		 * @throws IllegalArgumentException the number of input points is less
		 * than four, or the points appear to be coincident, colinear, or
		 * coplanar.
		 */
		public QuickHull3D(Point3d[] points)
		{
			build(points, points.Length);
		}

		private HalfEdge findHalfEdge(Vertex tail, Vertex head)
		{
			// brute force ... OK, since setHull is not used much
			foreach (Face face in faces)
			{
				HalfEdge he = face.findEdge(tail, head);
				if (he != null)
				{
					return he;
				}
			}
			return null;
		}

		protected void setHull(double[] coords, int nump, int[][] faceIndices, int numf)
		{
			initBuffers(nump);
			setPoints(coords, nump);
			computeMaxAndMin();
			for (int i = 0; i < numf; i++)
			{
				Face face = Face.create(pointBuffer, faceIndices[i]);
				HalfEdge he = face.he0;
				do
				{
					HalfEdge heOpp = findHalfEdge(he.head(), he.tail());
					if (heOpp != null)
					{
						he.setOpposite(heOpp);
					}
					he = he.next;
				}
				while (he != face.he0);

				faces.Add(face);
			}
		}

		/**
		 * Constructs the convex hull of a set of points whose
		 * coordinates are given by an array of doubles.
		 *
		 * @param coords x, y, and z coordinates of each input
		 * point. The length of this array will be three times
		 * the number of input points.
		 * @throws IllegalArgumentException the number of input points is less
		 * than four, or the points appear to be coincident, colinear, or
		 * coplanar.
		 */
		public void build(double[] coords)
		{
			build(coords, coords.Length / 3);
		}

		/**
		 * Constructs the convex hull of a set of points whose
		 * coordinates are given by an array of doubles.
		 *
		 * @param coords x, y, and z coordinates of each input
		 * point. The length of this array must be at least three times
		 * <code>nump</code>.
		 * @param nump number of input points
		 * @throws IllegalArgumentException the number of input points is less
		 * than four or greater than 1/3 the length of <code>coords</code>,
		 * or the points appear to be coincident, colinear, or
		 * coplanar.
		 */
		public void build(double[] coords, int nump)
		{
			if (nump < 4)
			{
				throw new SystemException("Less than four input points specified");
			}
			if (coords.Length / 3 < nump)
			{
				throw new SystemException("Coordinate array too small for specified number of points");
			}
			initBuffers(nump);
			setPoints(coords, nump);
			buildHull();
		}

		/**
		 * Constructs the convex hull of a set of points.
		 *
		 * @param points input points
		 * @throws IllegalArgumentException the number of input points is less
		 * than four, or the points appear to be coincident, colinear, or
		 * coplanar.
		 */
		public void build(Point3d[] points)
		{
			build(points, points.Length);
		}

		/**
		 * Constructs the convex hull of a set of points.
		 *
		 * @param points input points
		 * @param nump number of input points
		 * @throws IllegalArgumentException the number of input points is less
		 * than four or greater then the length of <code>points</code>, or the
		 * points appear to be coincident, colinear, or coplanar.
		 */
		public void build(Point3d[] points, int nump)
		{
			if (nump < 4)
			{
				throw new SystemException("Less than four input points specified");
			}
			if (points.Length < nump)
			{
				throw new SystemException("Point array too small for specified number of points");
			}
			initBuffers(nump);
			setPoints(points, nump);
			buildHull();
		}

		/**
		 * Triangulates any non-triangular hull faces. In some cases, due to
		 * precision issues, the resulting triangles may be very thin or small,
		 * and hence appear to be non-convex (this same limitation is present
		 * in <a href=http://www.qhull.org>qhull</a>).
		 */
		public void triangulate()
		{
			double minArea = 1000 * charLength * DOUBLE_PREC;
			newFaces.clear();
			
			foreach (Face face in faces)
			{
				if (face.mark == Face.VISIBLE)
				{
					face.triangulate(newFaces, minArea);
				}
			}
			for (Face face = newFaces.first(); face != null; face = face.next)
			{
				faces.Add(face);
			}
		}

		protected void initBuffers(int nump)
		{
			if (pointBuffer.Length < nump)
			{
				Vertex[] newBuffer = new Vertex[nump];
				vertexPointIndices = new int[nump];
				for (int i = 0; i < pointBuffer.Length; i++)
				{
					newBuffer[i] = pointBuffer[i];
				}
				for (int i = pointBuffer.Length; i < nump; i++)
				{
					newBuffer[i] = new Vertex();
				}
				pointBuffer = newBuffer;
			}
			faces.Clear();
			claimed.clear();
			numFaces = 0;
			numPoints = nump;
		}

		protected void setPoints(double[] coords, int nump)
		{
			for (int i = 0; i < nump; i++)
			{
				Vertex vtx = pointBuffer[i];
				vtx.pnt.set(coords[i * 3 + 0], coords[i * 3 + 1], coords[i * 3 + 2]);
				vtx.index = i;
			}
		}

		protected void setPoints(Point3d[] pnts, int nump)
		{
			for (int i = 0; i < nump; i++)
			{
				Vertex vtx = pointBuffer[i];
				vtx.pnt.set(pnts[i]);
				vtx.index = i;
			}
		}

		protected void computeMaxAndMin()
		{
			Vector3d max = new Vector3d();
			Vector3d min = new Vector3d();

			for (int i = 0; i < 3; i++)
			{
				maxVtxs[i] = minVtxs[i] = pointBuffer[0];
			}
			max.set(pointBuffer[0].pnt);
			min.set(pointBuffer[0].pnt);

			for (int i = 1; i < numPoints; i++)
			{
				Point3d pnt = pointBuffer[i].pnt;
				if (pnt.x > max.x)
				{
					max.x = pnt.x;
					maxVtxs[0] = pointBuffer[i];
				}
				else if (pnt.x < min.x)
				{
					min.x = pnt.x;
					minVtxs[0] = pointBuffer[i];
				}
				if (pnt.y > max.y)
				{
					max.y = pnt.y;
					maxVtxs[1] = pointBuffer[i];
				}
				else if (pnt.y < min.y)
				{
					min.y = pnt.y;
					minVtxs[1] = pointBuffer[i];
				}
				if (pnt.z > max.z)
				{
					max.z = pnt.z;
					maxVtxs[2] = pointBuffer[i];
				}
				else if (pnt.z < min.z)
				{
					min.z = pnt.z;
					minVtxs[2] = pointBuffer[i];
				}
			}

			// this epsilon formula comes from QuickHull, and I'm
			// not about to quibble.
			charLength = Math.Max(max.x - min.x, max.y - min.y);
			charLength = Math.Max(max.z - min.z, charLength);
			if (explicitTolerance == AUTOMATIC_TOLERANCE)
			{
				tolerance =
			   3 * DOUBLE_PREC * (Math.Max(Math.Abs(max.x), Math.Abs(min.x)) +
					  Math.Max(Math.Abs(max.y), Math.Abs(min.y)) +
					  Math.Max(Math.Abs(max.z), Math.Abs(min.z)));
			}
			else
			{
				tolerance = explicitTolerance;
			}
		}

		/**
		 * Creates the initial simplex from which the hull will be built.
		 */
		protected void createInitialSimplex()
		{
			double max = 0;
			int imax = 0;

			for (int i = 0; i < 3; i++)
			{
				double diff = maxVtxs[i].pnt.get(i) - minVtxs[i].pnt.get(i);
				if (diff > max)
				{
					max = diff;
					imax = i;
				}
			}

			if (max <= tolerance)
			{
				throw new SystemException("Input points appear to be coincident");
			}
			Vertex[] vtx = new Vertex[4];
			// set first two vertices to be those with the greatest
			// one dimensional separation

			vtx[0] = maxVtxs[imax];
			vtx[1] = minVtxs[imax];

			// set third vertex to be the vertex farthest from
			// the line between vtx0 and vtx1
			Vector3d u01 = new Vector3d();
			Vector3d diff02 = new Vector3d();
			Vector3d nrml = new Vector3d();
			Vector3d xprod = new Vector3d();
			double maxSqr = 0;
			u01.sub(vtx[1].pnt, vtx[0].pnt);
			u01.normalize();
			for (int i = 0; i < numPoints; i++)
			{
				diff02.sub(pointBuffer[i].pnt, vtx[0].pnt);
				xprod.cross(u01, diff02);
				double lenSqr = xprod.normSquared();
				if (lenSqr > maxSqr &&
				pointBuffer[i] != vtx[0] &&  // paranoid
				pointBuffer[i] != vtx[1])
				{
					maxSqr = lenSqr;
					vtx[2] = pointBuffer[i];
					nrml.set(xprod);
				}
			}
			if (Math.Sqrt(maxSqr) <= 100 * tolerance)
			{
				throw new SystemException("Input points appear to be colinear");
			}
			nrml.normalize();


			double maxDist = 0;
			double d0 = vtx[2].pnt.dot(nrml);
			for (int i = 0; i < numPoints; i++)
			{
				double dist = Math.Abs(pointBuffer[i].pnt.dot(nrml) - d0);
				if (dist > maxDist &&
				pointBuffer[i] != vtx[0] &&  // paranoid
				pointBuffer[i] != vtx[1] &&
				pointBuffer[i] != vtx[2])
				{
					maxDist = dist;
					vtx[3] = pointBuffer[i];
				}
			}

			if (Math.Abs(maxDist) <= 100 * tolerance)
			{
				throw new CoplanarException("Input points appear to be coplanar");
			}

			Face[] tris = new Face[4];

			if (vtx[3].pnt.dot(nrml) - d0 < 0)
			{
				tris[0] = Face.createTriangle(vtx[0], vtx[1], vtx[2]);
				tris[1] = Face.createTriangle(vtx[3], vtx[1], vtx[0]);
				tris[2] = Face.createTriangle(vtx[3], vtx[2], vtx[1]);
				tris[3] = Face.createTriangle(vtx[3], vtx[0], vtx[2]);

				for (int i = 0; i < 3; i++)
				{
					int k = (i + 1) % 3;
					tris[i + 1].getEdge(1).setOpposite(tris[k + 1].getEdge(0));
					tris[i + 1].getEdge(2).setOpposite(tris[0].getEdge(k));
				}
			}
			else
			{
				tris[0] = Face.createTriangle(vtx[0], vtx[2], vtx[1]);
				tris[1] = Face.createTriangle(vtx[3], vtx[0], vtx[1]);
				tris[2] = Face.createTriangle(vtx[3], vtx[1], vtx[2]);
				tris[3] = Face.createTriangle(vtx[3], vtx[2], vtx[0]);

				for (int i = 0; i < 3; i++)
				{
					int k = (i + 1) % 3;
					tris[i + 1].getEdge(0).setOpposite(tris[k + 1].getEdge(1));
					tris[i + 1].getEdge(2).setOpposite(tris[0].getEdge((3 - i) % 3));
				}
			}


			for (int i = 0; i < 4; i++)
			{
				faces.Add(tris[i]);
			}

			for (int i = 0; i < numPoints; i++)
			{
				Vertex v = pointBuffer[i];

				if (v == vtx[0] || v == vtx[1] || v == vtx[2] || v == vtx[3])
				{
					continue;
				}

				maxDist = tolerance;
				Face maxFace = null;
				for (int k = 0; k < 4; k++)
				{
					double dist = tris[k].distanceToPlane(v.pnt);
					if (dist > maxDist)
					{
						maxFace = tris[k];
						maxDist = dist;
					}
				}
				if (maxFace != null)
				{
					addPointToFace(v, maxFace);
				}
			}
		}

		/**
		 * Returns the number of vertices in this hull.
		 *
		 * @return number of vertices
		 */
		public int getNumVertices()
		{
			return numVertices;
		}

		/**
		 * Returns the vertex points in this hull.
		 *
		 * @return array of vertex points
		 * @see QuickHull3D#getVertices(double[])
		 * @see QuickHull3D#getFaces()
		 */
		public Point3d[] getVertices()
		{
			Point3d[] vtxs = new Point3d[numVertices];
			for (int i = 0; i < numVertices; i++)
			{
				vtxs[i] = pointBuffer[vertexPointIndices[i]].pnt;
			}
			return vtxs;
		}

		/**
		 * Returns the coordinates of the vertex points of this hull.
		 *
		 * @param coords returns the x, y, z coordinates of each vertex.
		 * This length of this array must be at least three times
		 * the number of vertices.
		 * @return the number of vertices
		 * @see QuickHull3D#getVertices()
		 * @see QuickHull3D#getFaces()
		 */
		public int getVertices(double[] coords)
		{
			for (int i = 0; i < numVertices; i++)
			{
				Point3d pnt = pointBuffer[vertexPointIndices[i]].pnt;
				coords[i * 3 + 0] = pnt.x;
				coords[i * 3 + 1] = pnt.y;
				coords[i * 3 + 2] = pnt.z;
			}
			return numVertices;
		}

		/**
		 * Returns an array specifing the index of each hull vertex
		 * with respect to the original input points.
		 *
		 * @return vertex indices with respect to the original points
		 */
		public int[] getVertexPointIndices()
		{
			int[] indices = new int[numVertices];
			for (int i = 0; i < numVertices; i++)
			{
				indices[i] = vertexPointIndices[i];
			}
			return indices;
		}

		/**
		 * Returns the number of faces in this hull.
		 *
		 * @return number of faces
		 */
		public int getNumFaces()
		{
			return faces.Count;
		}

		/**
		 * Returns the faces associated with this hull.
		 *
		 * <p>Each face is represented by an integer array which gives the
		 * indices of the vertices. These indices are numbered
		 * relative to the
		 * hull vertices, are zero-based,
		 * and are arranged counter-clockwise. More control
		 * over the index format can be obtained using
		 * {@link #getFaces(int) getFaces(indexFlags)}.
		 *
		 * @return array of integer arrays, giving the vertex
		 * indices for each face.
		 * @see QuickHull3D#getVertices()
		 * @see QuickHull3D#getFaces(int)
		 */
		public int[][] getFaces()
		{
			return getFaces(0);
		}

		/**
		 * Returns the faces associated with this hull.
		 *
		 * <p>Each face is represented by an integer array which gives the
		 * indices of the vertices. By default, these indices are numbered with
		 * respect to the hull vertices (as opposed to the input points), are
		 * zero-based, and are arranged counter-clockwise. However, this
		 * can be changed by setting {@link #POINT_RELATIVE
		 * POINT_RELATIVE}, {@link #INDEXED_FROM_ONE INDEXED_FROM_ONE}, or
		 * {@link #CLOCKWISE CLOCKWISE} in the indexFlags parameter.
		 *
		 * @param indexFlags specifies index characteristics (0 results
		 * in the default)
		 * @return array of integer arrays, giving the vertex
		 * indices for each face.
		 * @see QuickHull3D#getVertices()
		 */
		public int[][] getFaces(int indexFlags)
		{
			int[][] allFaces = new int[faces.Count][];

			int k = 0;
			
			foreach (Face face in faces)
			{
				allFaces[k] = new int[face.numVertices()];
				getFaceIndices(allFaces[k], face, indexFlags);
				k++;
			}

			return allFaces;
		}

		private void getFaceIndices(int[] indices, Face face, int flags)
		{
			bool ccw = ((flags & CLOCKWISE) == 0);
			bool indexedFromOne = ((flags & INDEXED_FROM_ONE) != 0);
			bool pointRelative = ((flags & POINT_RELATIVE) != 0);

			HalfEdge hedge = face.he0;
			int k = 0;
			do
			{
				int idx = hedge.head().index;
				if (pointRelative)
				{
					idx = vertexPointIndices[idx];
				}
				if (indexedFromOne)
				{
					idx++;
				}
				indices[k++] = idx;
				hedge = (ccw ? hedge.next : hedge.prev);
			}
			while (hedge != face.he0);
		}

		protected void resolveUnclaimedPoints(FaceList newFaces)
		{
			Vertex vtxNext = unclaimed.first();
			for (Vertex vtx = vtxNext; vtx != null; vtx = vtxNext)
			{
				vtxNext = vtx.next;

				double maxDist = tolerance;
				Face maxFace = null;
				for (Face newFace = newFaces.first(); newFace != null; newFace = newFace.next)
				{
					if (newFace.mark == Face.VISIBLE)
					{
						double dist = newFace.distanceToPlane(vtx.pnt);
						if (dist > maxDist)
						{
							maxDist = dist;
							maxFace = newFace;
						}
						if (maxDist > 1000 * tolerance)
						{
							break;
						}
					}
				}
				if (maxFace != null)
				{
					addPointToFace(vtx, maxFace);
				}
			}
		}

		protected void deleteFacePoints(Face face, Face absorbingFace)
		{
			Vertex faceVtxs = removeAllPointsFromFace(face);
			if (faceVtxs != null)
			{
				if (absorbingFace == null)
				{
					unclaimed.addAll(faceVtxs);
				}
				else
				{
					Vertex vtxNext = faceVtxs;
					for (Vertex vtx = vtxNext; vtx != null; vtx = vtxNext)
					{
						vtxNext = vtx.next;
						double dist = absorbingFace.distanceToPlane(vtx.pnt);
						if (dist > tolerance)
						{
							addPointToFace(vtx, absorbingFace);
						}
						else
						{
							unclaimed.add(vtx);
						}
					}
				}
			}
		}

		private const int NONCONVEX_WRT_LARGER_FACE = 1;
		private const int NONCONVEX = 2;

		protected double oppFaceDistance(HalfEdge he)
		{
			return he.face.distanceToPlane(he.opposite.face.getCentroid());
		}

		private bool doAdjacentMerge(Face face, int mergeType)
		{
			HalfEdge hedge = face.he0;

			bool convex = true;
			do
			{
				Face oppFace = hedge.oppositeFace();
				bool merge = false;
				double dist1;

				if (mergeType == NONCONVEX)
				{
					// then merge faces if they are definitively non-convex
					if (oppFaceDistance(hedge) > -tolerance || oppFaceDistance(hedge.opposite) > -tolerance)
					{
						merge = true;
					}
				}
				else // mergeType == NONCONVEX_WRT_LARGER_FACE
				{
					// merge faces if they are parallel or non-convex
					// wrt to the larger face; otherwise, just mark
					// the face non-convex for the second pass.
					if (face.area > oppFace.area)
					{
						dist1 = oppFaceDistance(hedge);
						if (dist1 > -tolerance)
						{
							merge = true;
						}
						else if (oppFaceDistance(hedge.opposite) > -tolerance)
						{
							convex = false;
						}
					}
					else
					{
						if (oppFaceDistance(hedge.opposite) > -tolerance)
						{
							merge = true;
						}
						else if (oppFaceDistance(hedge) > -tolerance)
						{
							convex = false;
						}
					}
				}

				if (merge)
				{
					int numd = face.mergeAdjacentFace(hedge, discardedFaces);
					for (int i = 0; i < numd; i++)
					{
						deleteFacePoints(discardedFaces[i], face);
					}

					return true;
				}

				hedge = hedge.next;
			}
			while (hedge != face.he0);

			if (!convex)
			{
				face.mark = Face.NON_CONVEX;
			}

			return false;
		}
		
		protected void calculateHorizon(Point3d eyePnt, HalfEdge edge0, Face face, List<HalfEdge> horizon)
		{
			deleteFacePoints(face, null);
			face.mark = Face.DELETED;


			HalfEdge edge;
			if (edge0 == null)
			{
				edge0 = face.getEdge(0);
				edge = edge0;
			}
			else
			{
				edge = edge0.getNext();
			}
			do
			{
				Face oppFace = edge.oppositeFace();
				if (oppFace.mark == Face.VISIBLE)
				{
					if (oppFace.distanceToPlane(eyePnt) > tolerance)
					{
						calculateHorizon(eyePnt, edge.getOpposite(),
								  oppFace, horizon);
					}
					else
					{
						horizon.Add(edge);
					}
				}
				edge = edge.getNext();
			}
			while (edge != edge0);
		}

		private HalfEdge addAdjoiningFace(Vertex eyeVtx, HalfEdge he)
		{
			Face face = Face.createTriangle(eyeVtx, he.tail(), he.head());
			faces.Add(face);
			face.getEdge(-1).setOpposite(he.getOpposite());
			return face.getEdge(0);
		}
		
		protected void addNewFaces(FaceList newFaces, Vertex eyeVtx, List<HalfEdge> horizon)
		{
			newFaces.clear();

			HalfEdge hedgeSidePrev = null;
			HalfEdge hedgeSideBegin = null;
			
			foreach (HalfEdge horizonHe in horizon)
			{
				HalfEdge hedgeSide = addAdjoiningFace(eyeVtx, horizonHe);

				if (hedgeSidePrev != null)
				{
					hedgeSide.next.setOpposite(hedgeSidePrev);
				}
				else
				{
					hedgeSideBegin = hedgeSide;
				}
				newFaces.add(hedgeSide.getFace());
				hedgeSidePrev = hedgeSide;
			}

			hedgeSideBegin.next.setOpposite(hedgeSidePrev);
		}

		protected Vertex nextPointToAdd()
		{
			if (!claimed.isEmpty())
			{
				Face eyeFace = claimed.first().face;
				Vertex eyeVtx = null;
				double maxDist = 0;
				for (Vertex vtx = eyeFace.outside;
				 vtx != null && vtx.face == eyeFace;
				 vtx = vtx.next)
				{
					double dist = eyeFace.distanceToPlane(vtx.pnt);
					if (dist > maxDist)
					{
						maxDist = dist;
						eyeVtx = vtx;
					}
				}
				return eyeVtx;
			}
			else
			{
				return null;
			}
		}

		protected void addPointToHull(Vertex eyeVtx)
		{
			horizon.Clear();
			unclaimed.clear();

			removePointFromFace(eyeVtx, eyeVtx.face);
			calculateHorizon(eyeVtx.pnt, null, eyeVtx.face, horizon);
			newFaces.clear();
			addNewFaces(newFaces, eyeVtx, horizon);

			// first merge pass ... merge faces which are non-convex
			// as determined by the larger face

			for (Face face = newFaces.first(); face != null; face = face.next)
			{
				if (face.mark == Face.VISIBLE)
				{
					while (doAdjacentMerge(face, NONCONVEX_WRT_LARGER_FACE))
						;
				}
			}
			// second merge pass ... merge faces which are non-convex
			// wrt either face	     
			for (Face face = newFaces.first(); face != null; face = face.next)
			{
				if (face.mark == Face.NON_CONVEX)
				{
					face.mark = Face.VISIBLE;
					while (doAdjacentMerge(face, NONCONVEX)) ;
				}
			}
			resolveUnclaimedPoints(newFaces);
		}

		protected void buildHull()
		{
			int cnt = 0;
			Vertex eyeVtx;

			computeMaxAndMin();
			createInitialSimplex();

			while ((eyeVtx = nextPointToAdd()) != null)
			{
				addPointToHull(eyeVtx);
				cnt++;
			}

			reindexFacesAndVertices();
		}

		private void markFaceVertices(Face face, int mark)
		{
			HalfEdge he0 = face.getFirstEdge();
			HalfEdge he = he0;
			do
			{
				he.head().index = mark;
				he = he.next;
			}
			while (he != he0);
		}

		protected void reindexFacesAndVertices()
		{
			for (int i = 0; i < numPoints; i++)
			{
				pointBuffer[i].index = -1;
			}

			// remove inactive faces and mark active vertices
			numFaces = 0;
			
			for (int i = 0; i < faces.Count; i++)
			{
				Face face = faces[i];

				if (face.mark != Face.VISIBLE)
				{
					faces.RemoveAt(i);
					i--;
				}
				else
				{
					markFaceVertices(face, 0);
					numFaces++;
				}
			}

			// reindex vertices
			numVertices = 0;
			for (int i = 0; i < numPoints; i++)
			{
				Vertex vtx = pointBuffer[i];
				if (vtx.index == 0)
				{
					vertexPointIndices[numVertices] = i;
					vtx.index = numVertices++;
				}
			}
		}

		protected bool checkFaceConvexity(Face face, double tol)
		{
			double dist;
			HalfEdge he = face.he0;
			do
			{
				face.checkConsistency();
				// make sure edge is convex
				dist = oppFaceDistance(he);
				if (dist > tol)
				{
					return false;
				}
				dist = oppFaceDistance(he.opposite);
				if (dist > tol)
				{
					return false;
				}

				if (he.next.oppositeFace() == he.oppositeFace())
				{
					return false;
				}
				he = he.next;
			}
			while (he != face.he0);
			return true;
		}
		
		protected bool checkFaces(double tol)
		{
			// check edge convexity
			bool convex = true;
			
			foreach (Face face in faces)
			{
				if (face.mark == Face.VISIBLE)
				{
					if (!checkFaceConvexity(face, tol))
					{
						convex = false;
					}
				}
			}
			return convex;
		}

		/**
		 * Checks the correctness of the hull using the distance tolerance
		 * returned by {@link QuickHull3D#getDistanceTolerance
		 * getDistanceTolerance}; see
		 * {@link QuickHull3D#check(PrintStream,double)
		 * check(PrintStream,double)} for details.
		 *
		 * @param ps print stream for diagnostic messages; may be
		 * set to <code>null</code> if no messages are desired.
		 * @return true if the hull is valid
		 * @see QuickHull3D#check(PrintStream,double)
		 */
		//	public bool check (PrintStream ps)
		public bool check()
		{
			return check(getDistanceTolerance());
		}

		/**
		 * Checks the correctness of the hull. This is done by making sure that
		 * no faces are non-convex and that no points are outside any face.
		 * These tests are performed using the distance tolerance <i>tol</i>.
		 * Faces are considered non-convex if any edge is non-convex, and an
		 * edge is non-convex if the centroid of either adjoining face is more
		 * than <i>tol</i> above the plane of the other face. Similarly,
		 * a point is considered outside a face if its distance to that face's
		 * plane is more than 10 times <i>tol</i>.
		 *
		 * <p>If the hull has been {@link #triangulate triangulated},
		 * then this routine may fail if some of the resulting
		 * triangles are very small or thin.
		 *
		 * @param ps print stream for diagnostic messages; may be
		 * set to <code>null</code> if no messages are desired.
		 * @param tol distance tolerance
		 * @return true if the hull is valid
		 * @see QuickHull3D#check(PrintStream)
		 */
		public bool check(double tol)
		{
			// check to make sure all edges are fully connected
			// and that the edges are convex
			double dist;
			double pointTol = 10 * tol;

			if (!checkFaces(tolerance))
			{
				return false;
			}

			// check point inclusion

			for (int i = 0; i < numPoints; i++)
			{
				Point3d pnt = pointBuffer[i].pnt;
				
				foreach (Face face in faces)
				{
					if (face.mark == Face.VISIBLE)
					{
						dist = face.distanceToPlane(pnt);
						if (dist > pointTol)
						{
							return false;
						}
					}
				}
			}
			return true;
		}
	}

} // namespace QHull
