using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU;
using System.Windows.Media.Media3D;
using ILGPU.Runtime;
using ILGPU.Runtime.OpenCL;
using System.Windows.Forms;
using ILGPU.Algorithms.Random;

namespace RayTracerLib
{
    /// <summary>
    /// Class used to perform the photon mapping computations
    /// </summary>
    internal class PhotonMapper
    {
        /// <summary>
        /// Runs the photon mapping computations
        /// </summary>
        /// <param name="scene"> The scene on which photon mapping should be performed </param>
        /// <param name="accelerator"> The CUDA accelerator </param>
        /// <param name="trianglesBuffer"> The Gpu buffer containing the triangles of the scene </param>
        /// <param name="subMeshesBuffer"> The Gpu buffer containing the sub-meshes of the scene </param>
        /// <param name="materialsBuffer"> The Gpu buffer containing the materials of the scene </param>
        /// <returns></returns>
        internal static List<Photon> Run(in Scene scene, Accelerator accelerator,
            in MemoryBuffer1D<Triangle, Stride1D.Dense> trianglesBuffer,
            in MemoryBuffer1D<GpuSubMesh, Stride1D.Dense> subMeshesBuffer,
            in MemoryBuffer1D<Material, Stride1D.Dense> materialsBuffer)
        {
            // Compile kernel
            var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D,
                ArrayView<Triangle>,
                ArrayView<GpuSubMesh>,
                ArrayView<Material>,
                ArrayView<Photon>,
                ArrayView<XorShift128Plus>>(Kernel);

            // Load ressources on the GPU
            PhotonsLoader.InitPhotonMapping(scene, accelerator, 
                out MemoryBuffer1D<Photon, Stride1D.Dense> photonsBuffer,
                out MemoryBuffer1D<XorShift128Plus, Stride1D.Dense> randomBuffer);

            // Run the kernel
            int nPhotons = (int)photonsBuffer.Length;
            kernel(nPhotons,
                trianglesBuffer.View, 
                subMeshesBuffer.View, 
                materialsBuffer.View, 
                photonsBuffer.View,
                randomBuffer.View);

            // Get data back from the GPU
            Photon[] bufferOut = new Photon[nPhotons];
            photonsBuffer.CopyToCPU(bufferOut);

            // Remove the photons that have not crossed any caustic surfaces
            List<Photon> res = bufferOut.Where(p => p.isCaustic > 0).ToList();
            return res;
        }

        /// <summary>
        /// Propagates a photon across the scene until it hits a non caustic material
        /// (or it bounces / refracts more than N times)
        /// </summary>
        /// <param name="photonIndex"> The index of the photon to propagate in the photons buffer </param>
        /// <param name="trianglesBuffer"> The Gpu buffer containing the triangles of the scene </param>
        /// <param name="subMeshesBuffer"> The Gpu buffer containing the sub-meshes of the scene </param>
        /// <param name="materialsBuffer"> The Gpu buffer containing the materials of the scene </param>
        /// <param name="photonsBuffer"> The Gpu buffer containing the photons </param>
        /// <param name="randomBuffer"> A Gpu buffer to generate random numbers </param>
        private static void Kernel(Index1D photonIndex,
            ArrayView<Triangle> trianglesBuffer,
            ArrayView<GpuSubMesh> subMeshesBuffer,
            ArrayView<Material> materialsBuffer,
            ArrayView<Photon> photonsBuffer,
            ArrayView<XorShift128Plus> randomBuffer)
        {
            int nBounces = 0;
            while (nBounces <= GlobalVariables.maxPhotonBounces) 
            {
                if(CastPhoton(photonIndex,
                    trianglesBuffer,
                    subMeshesBuffer,
                    materialsBuffer,
                    photonsBuffer,
                    randomBuffer))
                {
                    // Photon hit a non caustic material 
                    return;
                }
                else
                {
                    // Photon was reflected / refracted, continue propagating it
                }
                nBounces++;
            }
        }

        /// <summary>
        /// Propagates a photon across the scene until its next collision
        /// </summary>
        /// <param name="photonIndex"> The index of the photon to propagate in the photons buffer </param>
        /// <param name="trianglesBuffer"> The Gpu buffer containing the triangles of the scene </param>
        /// <param name="subMeshesBuffer"> The Gpu buffer containing the sub-meshes of the scene </param>
        /// <param name="materialsBuffer"> The Gpu buffer containing the materials of the scene </param>
        /// <param name="photonsBuffer"> The Gpu buffer containing the photons </param>
        /// <param name="randomBuffer"> A Gpu buffer to generate random numbers </param>
        /// <returns> True if the photon hits a non-caustic material and we can stop propagating it </returns>
        private static bool CastPhoton(int photonIndex,
            ArrayView<Triangle> trianglesBuffer,
            ArrayView<GpuSubMesh> subMeshesBuffer,
            ArrayView<Material> materialsBuffer,
            ArrayView<Photon> photonsBuffer,
            ArrayView<XorShift128Plus> randomBuffer)
        {

            // Find collision info
            Photon photon = photonsBuffer[photonIndex];
            if(!CollisionFinder.GetFirstCollision(photon.ray,
                trianglesBuffer,
                subMeshesBuffer,
                out Triangle collisionTriangle,
                out double collisionAbciss,
                out double u, out double v))
            {
                // Photon does not hit anything, discard it
                photon.isCaustic = 0;
                photonsBuffer[photonIndex] = photon;
                return true;
            }
            Point3D collisionPoint = photon.ray.origin + collisionAbciss * photon.ray.direction;
            Vector3D collisionNormal = collisionTriangle.GetNormal(u, v);
            Material collisionMaterial = materialsBuffer[collisionTriangle.materialIndex];

            // compute reflection probability
            double reflectionProbability = 0;
            if(Reflection.Compute(collisionMaterial, photon.ray, collisionPoint, collisionNormal,
                out Ray reflectedRay, out double reflectionCoeff))
            {
                reflectionProbability = reflectionCoeff;
            }

            // compute refraction probability
            double refractionProbability = 0;
            if (Refraction.Compute(collisionMaterial, photon.ray, collisionPoint, collisionNormal,
                out Ray refractedRay, out double refractionCoeff))
            {
                refractionProbability = refractionCoeff;
            }

            // Throw a dice
            XorShift128Plus rng = randomBuffer[photonIndex];
            double rand = rng.NextDouble();
            randomBuffer[photonIndex] = rng;
            if (rand < reflectionProbability) 
            {
                // Reflection
                photon.ray = reflectedRay;
                photon.isCaustic = 1;
                photon.color *= reflectionProbability;
                photonsBuffer[photonIndex] = photon;
                return false;
            }
            if (rand < reflectionProbability + refractionProbability)
            {
                // Refraction
                photon.ray = refractedRay;
                photon.isCaustic = 1;
                photon.color *= refractionProbability;
                photonsBuffer[photonIndex] = photon;
                return false;
            }

            // If the photon has not been refracted or reflected,
            // update its position and color and stop propagating it
            photon.color.X *= collisionMaterial.color.X / 255;
            photon.color.Y *= collisionMaterial.color.Y / 255;
            photon.color.Z *= collisionMaterial.color.Z / 255;
            photon.ray.origin = collisionPoint;
            photonsBuffer[photonIndex] = photon;
            return true;
        }
    }
}
