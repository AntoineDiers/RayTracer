using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using ILGPU;

namespace RayTracerLib
{
    /// <summary>
    /// Shadows behavior
    /// </summary>
    internal class Shadow
    {
        /// <summary>
        /// Computes the shadow intensity at a given point for a given light source
        /// </summary>
        /// <param name="collisionPoint"> The point at which we want to compute the shadow intensity </param>
        /// <param name="lightSource"> The light source for which we want to compute the shadow intensity </param>
        /// <param name="trianglesBuffer"> The triangles of the scene </param>
        /// <param name="subMeshesBuffer"> The sub-meshes of the scene </param>
        /// <returns> a double between 0 and 1 (0 -> no shadow, 1 -> full shadow) </returns>
        internal static double Compute(Point3D collisionPoint,
            LightSource lightSource,
            ArrayView<Triangle> trianglesBuffer,
            ArrayView<GpuSubMesh> subMeshesBuffer)
        {
            Vector3D lightSourceDirection = (lightSource.position - collisionPoint);
            double lightSourceDistance = lightSourceDirection.Length;
            lightSourceDirection = lightSourceDirection / lightSourceDistance;
            Ray shadowTest = new(collisionPoint + GlobalVariables.epsilon * lightSourceDirection, lightSourceDirection);
            if (CollisionFinder.GetFirstCollision(
                shadowTest, 
                trianglesBuffer, 
                subMeshesBuffer,
                out _, 
                out _, 
                out _,
                out _,
                lightSourceDistance))
            {
                return 1;
            }
            return 0;
        }
    }
}
