using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU;
using System.Windows.Media.Media3D;

namespace RayTracerLib
{
    /// <summary>
    /// Provides functions to find collisions
    /// </summary>
    internal class CollisionFinder
    {
        /// <summary>
        /// Finds the first collision between a ray and the scene
        /// </summary>
        /// <param name="ray"> The ray for which we want to find collisions </param>
        /// <param name="trianglesBuffer"> The list of triangles of the scene </param>
        /// <param name="subMeshesBuffer"> The list of submeshes of the scene for faster collision search </param>
        /// <param name="collisionTriangle"> The triangle with which the ray has collided </param>
        /// <param name="collisionAbciss"> The abciss (along the ray direction) at which the collision happened </param>
        /// <param name="u"> The u component of the UV representation of the collision point on the triangle </param>
        /// <param name="v"> The v component of the UV representation of the collision point on the triangle </param>
        /// <param name="maxAbciss"> The abciss (along the ray direction after which all collisions shall be ignored) </param>
        /// <returns> True if a collision has been found </returns>
        internal static bool GetFirstCollision(in Ray ray,
            ArrayView<Triangle> trianglesBuffer,
            ArrayView<GpuSubMesh> subMeshesBuffer,
            out Triangle collisionTriangle,
            out double collisionAbciss,
            out double u,
            out double v,
            double maxAbciss = -1)
        {
            bool res = false;
            u = 0;
            v = 0;
            collisionTriangle = new();

            collisionAbciss = maxAbciss;
            int subMeshIndex = 0;
            while (subMeshIndex < subMeshesBuffer.Length)
            {
                GpuSubMesh subMesh = subMeshesBuffer[subMeshIndex];
                if (!ray.Intersects(subMesh.boundingBox))
                {
                    // If the ray does not intersect this bounding box,
                    // it will not intersect the submeshes it contains.
                    subMeshIndex = subMesh.nextIndex;
                    continue;
                }
                for (int triangleIndex = subMesh.firstTriangleIndex; triangleIndex < subMesh.firstTriangleIndex + subMesh.nTriangles; triangleIndex++)
                {
                    Triangle triangle = trianglesBuffer[triangleIndex];
                    double abcissLowerBound = ray.GetDistanceLowerBound(triangle);
                    if (collisionAbciss >= 0 && abcissLowerBound > collisionAbciss)
                    {
                        continue;
                    }
                    if (ray.Intersects(triangle, out double abciss, out double _u, out double _v))
                    {
                        if (abciss < collisionAbciss || collisionAbciss < 0)
                        {
                            res = true;
                            u = _u; 
                            v = _v;
                            collisionAbciss = abciss;
                            collisionTriangle = triangle;
                        }
                    }
                }
                subMeshIndex++;
            }
            return res;
        }
    }
}
