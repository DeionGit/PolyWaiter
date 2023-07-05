using System;
using UnityEngine;

namespace RayFire
{
	[Serializable]
    public class RFMeshExport
    {
	    public enum MeshExportType
        {
            LastFragments         = 0,
            Children              = 3
        }
        
        public MeshExportType source;
        public string         suffix = "_frags";
    	
    	// by path, by window
    	// public string path = "RayFireFragments";
    	
    	// all, last
        // generate colliders
    }
    
	[Serializable]
	public class RFShatterAdvanced
	{
		public int   seed;
		public bool  decompose;
		public bool  removeCollinear;
		public bool  copyComponents;
		public bool  inputPrecap;
        public bool  outputPrecap;
        public bool  removeDoubleFaces;
        public int   elementSizeThreshold;
        public bool  combineChildren;
        public bool  smooth;
        public bool  postWeld;
        public bool  inner;
        public bool  planar;
        public int   relativeSize;
        public float absoluteSize;
        public bool  sizeLimitation;
        public float sizeAmount;
        public bool  vertexLimitation;
        public int   vertexAmount;
        public bool  triangleLimitation;
        public int   triangleAmount;
        //public bool  bake;
        
        // Planar mesh vert offset threshold
        public static float     planarThreshold = 0.01f;
        public static Vector3[] vertices;
        public static Plane     plane;
        
        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////
		
		// Constructor
		public RFShatterAdvanced()
		{
			seed                 = 0;
			decompose            = true;
			removeCollinear      = false;
			copyComponents       = false;
			inputPrecap          = true;
			outputPrecap         = false;
			removeDoubleFaces    = true;
			elementSizeThreshold = 5;
			combineChildren      = false;
			smooth               = false;
			postWeld             = false;
			inner                = false;
			planar               = false;
			absoluteSize         = 0.1f;
			relativeSize         = 4;
			sizeLimitation       = false;
			sizeAmount           = 5f;
			vertexLimitation     = false;
			vertexAmount         = 300;
			triangleLimitation   = false;
			triangleAmount       = 300;
			//bake                 = true;
		}
        
        // Constructor
        public RFShatterAdvanced (RFShatterAdvanced src)
        {
	        seed                 = src.seed;
	        decompose            = src.decompose;
	        removeCollinear      = src.removeCollinear;
	        copyComponents       = src.copyComponents;
	        inputPrecap          = src.inputPrecap;
	        outputPrecap         = src.outputPrecap;
	        removeDoubleFaces    = src.removeDoubleFaces;
	        elementSizeThreshold = src.elementSizeThreshold;
	        combineChildren      = src.combineChildren;
	        smooth               = src.smooth;
	        postWeld             = src.postWeld;
	        inner                = src.inner;
	        planar               = src.planar;
	        absoluteSize         = src.absoluteSize;
	        relativeSize         = src.relativeSize;
	        sizeLimitation       = src.sizeLimitation;
	        sizeAmount           = src.sizeAmount;
	        vertexLimitation     = src.vertexLimitation;
	        vertexAmount         = src.vertexAmount;
	        triangleLimitation   = src.triangleLimitation;
	        triangleAmount       = src.triangleAmount;
	        //bake                 = src.bake;
        }

        /// /////////////////////////////////////////////////////////
        /// Static
        /// /////////////////////////////////////////////////////////
        
        // Check if mesh is coplanar. All verts on a plane
        public static bool IsCoplanar(Mesh mesh, float threshold)
        {
	        // Coplanar 3 verts
            if (mesh.vertexCount <= 3)
                return true;

            // Get first plane vertex index
            int index1 = 0;
            int index2 = 0;
            int index3 = 0;

            // Set array of vertices
            vertices = mesh.vertices;
            
            // Get second plane vertex index
            int ind = 1;
            for (int i = ind; i < vertices.Length; i++)
            {
	            if (Vector3.Distance (vertices[index1], vertices[i]) > threshold)
	            {
		            index2 = i;
		            ind = i;
		            break;
	            }
            }
            
            // No second vert
            if (index2 == 0)
                return true;

            // Second vert is the last ver
            if (ind == vertices.Length - 1)
                return true;
            
            // Get third vert
            ind++;
            float   distance;
            Vector3 vector2;
            Vector3 vector1 = (vertices[index2] - vertices[index1]).normalized;
            for (int i = ind; i < vertices.Length; i++)
            {
                if (Vector3.Distance (vertices[index1], vertices[i]) > threshold)
                {
                    vector2  = (vertices[i] - vertices[index1]).normalized;
                    distance = Vector3.Cross (vector1, vector2).magnitude;
                    if (distance > threshold)
                    {
	                    index3 = i;
                        break;
                    }
                }
            }
            
            // No third vert
            if (index3 == 0)
                return true;
            
            // Create plane and check other verts for coplanar
            plane = new Plane(vertices[index1], vertices[index2], vertices[index3]);
            for (int i = 0; i < vertices.Length; i++)
            {
                if (i != index1 && i != index2 && i != index3)
                {
	                distance = plane.GetDistanceToPoint (vertices[i]);
                    if (Math.Abs (distance) > threshold)
                        return false;
                }
            }
            
            return true;
        }
	}
}