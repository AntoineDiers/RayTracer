using System.Diagnostics;
using System.Threading;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using ILGPU.Runtime;
using ILGPU;
using OpenCvSharp;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using System.Drawing;
using System.Windows;
using System.Security.Policy;
using ILGPU.Algorithms;

namespace RayTracerLib
{
    /// <summary>
    /// Performs the raytracing algorithm
    /// </summary>
    public class RayTracer
    {
        /// <summary> Cuda context <summary/>
        private Context context;
        /// <summary> Cuda accelerator <summary/>
        private Accelerator accelerator;
        /// <summary> Cuda kernel <summary/>
        private System.Action<Index1D,
                ArrayView<Ray>,
                ArrayView<Triangle>,
                ArrayView<GpuSubMesh>,
                ArrayView<LightSource>,
                ArrayView<Material>,
                ArrayView<Vec3b>,
                ArrayView<GpuTextureMetadata>,
                ArrayView<GpuPhotonTreeNode>,
                ArrayView<GpuPhotonNeighbor>,
                ArrayView<byte>,
                ArrayView<Vector3D>> kernel;

        /// <summary>
        /// Inits the cuda context and compiles the kernel
        /// </summary>
        public RayTracer() 
        {
            context = Context.Create(b => b.Default().EnableAlgorithms());
            accelerator = context.CreateCudaAccelerator(0);
            kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D,
                ArrayView<Ray>,
                ArrayView<Triangle>,
                ArrayView<GpuSubMesh>,
                ArrayView<LightSource>,
                ArrayView<Material>,
                ArrayView<Vec3b>,
                ArrayView<GpuTextureMetadata>,
                ArrayView<GpuPhotonTreeNode>,
                ArrayView<GpuPhotonNeighbor>,
                ArrayView<byte>,
                ArrayView <Vector3D>>(Kernel);
        }

        /// <summary>
        /// Runs the raytracing algorithm : 
        /// - Perform the photon mapping step
        /// - Create the photons tree
        /// - Perform the raytracing step
        /// - Create the output image
        /// </summary>
        /// <param name="scene"> The scene to render </param>
        /// <returns> The rendered image </returns>
        public OpenCvSharp.Mat Run(in Scene scene)
        {
            int width = scene.camera.GetResolution().Width;
            int height = scene.camera.GetResolution().Height;
            int nPixels = width * height;

            RayTracerLib.Timer timer = new(4);
            timer.Restart("Loading scene on the GPU ...");

            // Load the scene on the GPU
            SceneLoader.LoadScene(scene, accelerator,
                out var raysBuffer,
                out var trianglesBuffer,
                out var subMeshesBuffer,
                out var lightSourcesBuffer,
                out var materialsBuffer,
                out var texturesDataBuffer,
                out var texturesMetadataBuffer,
                out var pixelsBuffer);

            timer.Print("");
            timer.Restart("Running Photon mapping ...");

            // Run Photon Mapping
            List<Photon> photons = PhotonMapper.Run(scene, 
                accelerator, 
                trianglesBuffer, 
                subMeshesBuffer, 
                materialsBuffer);

            timer.Print("");
            timer.Restart("Constructing Photons tree ...");

            // Construct the photons tree and load it on the GPU
            PhotonTree photonsTree = new(photons);
            PhotonsLoader.LoadPhotonsTree(photonsTree, accelerator, nPixels,
                out MemoryBuffer1D<GpuPhotonTreeNode, Stride1D.Dense> photonsTreeBuffer,
                out MemoryBuffer1D<GpuPhotonNeighbor, Stride1D.Dense> photonsNearestNeighborsBuffer,
                out MemoryBuffer1D<byte, Stride1D.Dense> photonsSearchStateBuffer);

            timer.Print("");
            timer.Restart("Running main raytracing pass ...");

            // Run the main kernel
            kernel(nPixels,
                raysBuffer.View,
                trianglesBuffer.View,
                subMeshesBuffer.View,
                lightSourcesBuffer.View,
                materialsBuffer.View,
                texturesDataBuffer.View,
                texturesMetadataBuffer.View,
                photonsTreeBuffer.View,
                photonsNearestNeighborsBuffer.View,
                photonsSearchStateBuffer.View,
                pixelsBuffer.View);

            // Retrieve the data
            Vector3D[] bufferOut = new Vector3D[pixelsBuffer.Length];
            pixelsBuffer.CopyToCPU(bufferOut);

            timer.Print("");
            timer.Restart("Generating the output image ...");

            // Generate the image
            OpenCvSharp.Mat res = new(scene.camera.GetResolution(), MatType.CV_8UC3, new(255, 255, 255));
            for (int i = 0; i < bufferOut.Length; i++)
            {
                int col = i % width;
                int row = i / width;
                res.At<Vec3b>(row, col) = ConvertColor(bufferOut[i]);
            }

            timer.Print("");

            return res;
        }

        /// <summary>
        /// Converts a color from (double r, double g, double b) to opencv format
        /// </summary>
        /// <param name="c"> the color to convert </param>
        /// <returns> The converted opencv color </returns>
        private static Vec3b ConvertColor(Vector3D c)
        {
            double r = Math.Min(254, Math.Max(0, c.X));
            double g = Math.Min(254, Math.Max(0, c.Y));
            double b = Math.Min(254, Math.Max(0, c.Z));
            return new(Convert.ToByte(b), Convert.ToByte(g), Convert.ToByte(r));
        }

        /// <summary>
        /// The Cuda kernel that will perform the raytracing algorithm for a given pixel
        /// </summary>
        /// <param name="index"> The index of the pixel </param>
        /// <param name="raysBuffer"> The GPU buffer that contains the rays that we should cast </param>
        /// <param name="trianglesBuffer"> The GPU buffer that contains the triangles of the scene </param>
        /// <param name="subMeshesBuffer"> The GPU buffer that contains the submeshes of the scene </param>
        /// <param name="lightSourcesBuffer"> The GPU buffer that contains the light sources of the scene </param>
        /// <param name="materialsBuffer"> The GPU buffer that contains the materials of the scene </param>
        /// <param name="texturesDataBuffer"> The GPU buffer that contains the textures of the scene </param>
        /// <param name="texturesMetadataBuffer"> The GPU buffer that contains the textures metadata </param>
        /// <param name="photonsTreeBuffer"> The GPU buffer that contains the photons computed by the photon mapping step </param>
        /// <param name="photonsNearestNeighborsBuffer"> The GPU buffer that will contain the nearest neighbor photons for each pixel </param>
        /// <param name="photonsSearchStateBuffer"> The GPU buffer that will contain the nearest neighbor search state for each pixel </param>
        /// <param name="pixelsBuffer"> The GPU buffer that contains the data of the output image </param>
        private static void Kernel(Index1D index,
            ArrayView<Ray> raysBuffer,
            ArrayView<Triangle> trianglesBuffer,
            ArrayView<GpuSubMesh> subMeshesBuffer,
            ArrayView<LightSource> lightSourcesBuffer,
            ArrayView<Material> materialsBuffer,
            ArrayView<Vec3b> texturesDataBuffer,
            ArrayView<GpuTextureMetadata> texturesMetadataBuffer,
            ArrayView<GpuPhotonTreeNode> photonsTreeBuffer,
            ArrayView<GpuPhotonNeighbor> photonsNearestNeighborsBuffer,
            ArrayView<byte> photonsSearchStateBuffer,
            ArrayView<Vector3D> pixelsBuffer)
        {
            int photonsTreeDepth = (int)(photonsSearchStateBuffer.Length / pixelsBuffer.Length);
            int pixelIndex = index.X;
            int firstRayIndex = pixelIndex * GlobalVariables.maxRaysPerPixel;
            int nextRayIndex = firstRayIndex + 1;

            pixelsBuffer[index.X] = new(0, 0, 0);
            for (int rayIndex = firstRayIndex;
                rayIndex < firstRayIndex + GlobalVariables.maxRaysPerPixel; rayIndex++)
            {
                Ray ray = raysBuffer[rayIndex];
                Vector3D color = new(0, 0, 0);
                if (CollisionFinder.GetFirstCollision(ray,
                    trianglesBuffer,
                    subMeshesBuffer,
                    out Triangle collisionTriangle,
                    out double collisionAbciss,
                    out double u,
                    out double v))
                {
                    Point3D collisionPoint = ray.origin + collisionAbciss * ray.direction;
                    Vector3D collisionNormal = collisionTriangle.GetNormal(u, v);
                    Material collisionMaterial = materialsBuffer[collisionTriangle.materialIndex];
                    Vector3D collisionColor = GetTriangleColor(
                            collisionTriangle, u, v,
                            materialsBuffer,
                            texturesDataBuffer, 
                            texturesMetadataBuffer);

                    color = ProcessRayCollision(pixelIndex,
                        rayIndex,
                        ref nextRayIndex,
                        photonsTreeDepth,
                        collisionMaterial,
                        collisionPoint,
                        collisionNormal,
                        collisionColor,
                        raysBuffer,
                        trianglesBuffer,
                        subMeshesBuffer,
                        lightSourcesBuffer,
                        photonsTreeBuffer,
                        photonsNearestNeighborsBuffer,
                        photonsSearchStateBuffer);
                }

                pixelsBuffer[index.X] += color;

                if (rayIndex + 1 == nextRayIndex)
                {
                    // No more rays to cast
                    break;
                }
            }
        }

        /// <summary>
        /// Computes the color for a given collision, can generate new rays to cast
        /// </summary>
        /// <param name="pixelIndex"> The index of the pixel on which this collision happened </param>
        /// <param name="rayIndex"> The index of the ray for which we process this collision </param>
        /// <param name="nextRayIndex"> The index at which we should add a new ray if needed </param>
        /// <param name="photonsTreeDepth"> The depth of the photons tree computed in the photons mapping step </param>
        /// <param name="collisionMaterial"> The material of the collision </param>
        /// <param name="collisionPoint"> The collision point </param>
        /// <param name="collisionNormal"> The collision normal </param>
        /// <param name="collisionColor"> The color of the material at the collision point </param>
        /// <param name="raysBuffer"> The GPU buffer that contains the rays that we should cast </param>
        /// <param name="trianglesBuffer"> The GPU buffer that contains the triangles of the scene </param>
        /// <param name="subMeshesBuffer"> The GPU buffer that contains the submeshes of the scene </param>
        /// <param name="lightSourcesBuffer"> The GPU buffer that contains the light sources of the scene </param>
        /// <param name="photonsTreeBuffer"> The GPU buffer that contains the photons computed by the photon mapping step </param>
        /// <param name="photonsNearestNeighborsBuffer"> The GPU buffer that will contain the nearest neighbor photons for each pixel </param>
        /// <param name="photonsSearchStateBuffer"> The GPU buffer that will contain the nearest neighbor search state for each pixel </param>
        /// <returns></returns>
        private static Vector3D ProcessRayCollision(int pixelIndex, int rayIndex, ref int nextRayIndex, int photonsTreeDepth,
            Material collisionMaterial,
            Point3D collisionPoint,
            Vector3D collisionNormal,
            Vector3D collisionColor,
            ArrayView<Ray> raysBuffer,
            ArrayView<Triangle> trianglesBuffer,
            ArrayView<GpuSubMesh> subMeshesBuffer,
            ArrayView<LightSource> lightSourcesBuffer,
            ArrayView<GpuPhotonTreeNode> photonsTreeBuffer,
            ArrayView<GpuPhotonNeighbor> photonsNearestNeighborsBuffer,
            ArrayView<byte> photonsSearchStateBuffer)
        {
            Vector3D res = new(0, 0, 0);

            Ray ray = raysBuffer[rayIndex];

            // Phong + Shadows
            for(int lightSourceIndex = 0; lightSourceIndex < lightSourcesBuffer.Length; lightSourceIndex ++)
            {
                LightSource lightSource = lightSourcesBuffer[lightSourceIndex];
                PhongModel.Compute(
                    ray, 
                    collisionMaterial, 
                    collisionPoint, 
                    collisionNormal,
                    collisionColor,
                    lightSource,
                    out Vector3D ambiantPart,
                    out Vector3D phongPart);

                double shadowRatio = Shadow.Compute(
                    collisionPoint, 
                    lightSource, 
                    trianglesBuffer, 
                    subMeshesBuffer);

                res += (1 - shadowRatio) * phongPart + ambiantPart;
            }

            // Caustics
            if(photonsTreeBuffer.Length > 0) 
            {
                int nNeighborPhotons = NearestNeighborSearch.Run(
                pixelIndex,
                collisionPoint,
                photonsTreeDepth,
                photonsTreeBuffer,
                photonsNearestNeighborsBuffer,
                photonsSearchStateBuffer);

                res += Caustic.Compute(pixelIndex * GlobalVariables.nNearestNeighbors,
                nNeighborPhotons,
                photonsNearestNeighborsBuffer);
            }
            
            // Reflection
            if (Reflection.Compute(collisionMaterial, ray, collisionPoint, collisionNormal,
                out Ray reflectedRay, out double reflectionCoeff))
            {
                if (ray.weight * reflectionCoeff > GlobalVariables.minReflectionRefractionCoeff)
                {
                    res *= (1 - reflectionCoeff);
                    reflectedRay.weight = ray.weight * reflectionCoeff;
                    RecastRay(reflectedRay, ref nextRayIndex, raysBuffer);
                }
            }

            // Refraction
            if (Refraction.Compute(collisionMaterial, ray, collisionPoint, collisionNormal,
                out Ray refractedRay, out double refractionCoeff))
            {
                if (ray.weight * refractionCoeff > GlobalVariables.minReflectionRefractionCoeff)
                {
                    res *= (1 - refractionCoeff);
                    refractedRay.weight = ray.weight * refractionCoeff;
                    RecastRay(refractedRay, ref nextRayIndex, raysBuffer);
                }
            }

            return ray.weight * res;
        }

        /// <summary>
        /// Adds a new ray to the rays buffer (in case of reflection / refraction)
        /// </summary>
        /// <param name="ray"> The ray that we want to cast </param>
        /// <param name="nextRayIndex"> A reference to the index of the next ray </param>
        /// <param name="raysBuffer"> The GPU buffer that contains the rays </param>
        /// <returns></returns>
        private static bool RecastRay(in Ray ray, ref int nextRayIndex, ArrayView<Ray> raysBuffer)
        {
            if ((nextRayIndex + 1) / GlobalVariables.maxRaysPerPixel ==
                nextRayIndex / GlobalVariables.maxRaysPerPixel)
            {
                raysBuffer[nextRayIndex] = ray;
                nextRayIndex++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the color of a triangle at a given point (texture color if it is textured, material color if not)
        /// </summary>
        /// <param name="triangle"> The triangle </param>
        /// <param name="u"> The u coordinate in the triangle (along AB) </param>
        /// <param name="v"> The v coordinate in the triangle (along AC) </param>
        /// <param name="materialsBuffer"> The GPU buffer containing the materials of the scene </param>
        /// <param name="texturesDataBuffer"> The GPU buffer containing the textures of the scene  </param>
        /// <param name="texturesMetadataBuffer"> The GPU buffer containing the textures metadata </param>
        /// <returns></returns>
        private static Vector3D GetTriangleColor(
            Triangle triangle,
            double u,
            double v,
            ArrayView<Material> materialsBuffer,
            ArrayView<Vec3b> texturesDataBuffer,
            ArrayView<GpuTextureMetadata> texturesMetadataBuffer)
        {
            if (triangle.textureIndex < 0)
            {
                return materialsBuffer[triangle.materialIndex].color;
            }

            double uVecX = u * triangle.B.texture.X + 0.5 * (1 - (u + v)) * triangle.A.texture.X;
            double uVecY = u * triangle.B.texture.Y + 0.5 * (1 - (u + v)) * triangle.A.texture.Y;
            double vVecX = v * triangle.C.texture.X + 0.5 * (1 - (u + v)) * triangle.A.texture.X;
            double vVecY = v * triangle.C.texture.Y + 0.5 * (1 - (u + v)) * triangle.A.texture.Y;
            Point2d textureCoordinates = new(uVecX + vVecX, uVecY + vVecY);

            GpuTextureMetadata metadata = texturesMetadataBuffer[triangle.textureIndex];
            int col = (int)(textureCoordinates.X * (metadata.width - 1));
            int row = (int)((metadata.height - 1) - textureCoordinates.Y * (metadata.height - 1));
            Vec3b color = texturesDataBuffer[metadata.dataIndex + row * metadata.width + col];
            return new(color.Item2, color.Item1, color.Item0);
        }
    }
}
