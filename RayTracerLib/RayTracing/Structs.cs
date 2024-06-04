using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTracerLib
{
    /// <summary> GPU version of a submesh (triangles container) </summary>
    public struct GpuSubMesh()
    {
        /// <summary> The bounding box of the submesh </summary>
        internal BoundingBox boundingBox;
        /// <summary> The index of the first triangle of this mesh in the triangles buffer </summary>
        internal int firstTriangleIndex;
        /// <summary> The number of triangles in this mesh </summary>
        internal int nTriangles = 0;
        /// <summary> The index of the next submesh in the submesh tree that is at the same level or higher </summary>
        internal int nextIndex = -1;
    }

    /// <summary> Metadata for a texture </summary>
    public struct GpuTextureMetadata
    {
        /// <summary> The widht of the texture </summary>
        internal int width;
        /// <summary> The height of the texture </summary>
        internal int height;
        /// <summary> The index at which the texture data is stored in the textures buffer </summary>
        internal int dataIndex;
    }

    /// <summary> A neighboring photon </summary>
    public struct GpuPhotonNeighbor()
    {
        /// <summary> The photon </summary>
        internal Photon data;
        /// <summary> The distance squared between the photon and the collision point </summary>
        internal double distSquared = -1;
    }

    public struct GpuPhotonTreeNode
    {
        /// <summary> The photon of this node </summary>
        internal Photon data;
        /// <summary> The axis along which this node splits the data (KD tree) </summary>
        internal int axis;
        /// <summary> The index of this node's parent </summary>
        internal int parentIndex;
        /// <summary> The index of this node's left child </summary>
        internal int leftIndex;
        /// <summary> The index of this node's right child </summary>
        internal int rightIndex;
    }
}
