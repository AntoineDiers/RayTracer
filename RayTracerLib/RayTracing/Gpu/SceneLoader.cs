using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU.Runtime;
using ILGPU;
using System.Windows.Media.Media3D;
using OpenCvSharp;

namespace RayTracerLib
{
    /// <summary>
    /// Class used to load all the scene data on the GPU
    /// </summary>
    internal class SceneLoader
    {
        /// <summary>
        /// Loads all the scene data on the GPU
        /// </summary>
        /// <param name="scene"> The scene to load on the GPU </param>
        /// <param name="accelerator"> The CUDA accelerator </param>
        /// <param name="raysBuffer"> The GPU buffer that contains the rays for raytracing </param>
        /// <param name="trianglesBuffer"> The GPU buffer that contains the scene triangles </param>
        /// <param name="subMeshesBuffer"> The GPU buffer that contains the scene submeshes </param>
        /// <param name="lightSourcesBuffer"> The GPU buffer that contains the scene light sources </param>
        /// <param name="materialsBuffer"> The GPU buffer that contains the scene materials </param>
        /// <param name="texturesDataBuffer"> The GPU buffer that contains the textures of the assets </param>
        /// <param name="texturesMetadataBuffer"> The GPU buffer that contains the textures metadata </param>
        /// <param name="pixelsBuffer"> The GPU buffer that contains the pixels of the output image </param>
        internal static void LoadScene(in Scene scene, Accelerator accelerator,
            out MemoryBuffer1D<Ray, Stride1D.Dense> raysBuffer,
            out MemoryBuffer1D<Triangle, Stride1D.Dense> trianglesBuffer,
            out MemoryBuffer1D<GpuSubMesh, Stride1D.Dense> subMeshesBuffer,
            out MemoryBuffer1D<LightSource, Stride1D.Dense> lightSourcesBuffer,
            out MemoryBuffer1D<Material, Stride1D.Dense> materialsBuffer,
            out MemoryBuffer1D<Vec3b, Stride1D.Dense> texturesDataBuffer,
            out MemoryBuffer1D<GpuTextureMetadata, Stride1D.Dense> texturesMetadataBuffer,
            out MemoryBuffer1D<Vector3D, Stride1D.Dense> pixelsBuffer)
        {
            // Pixels
            Size resolution = scene.camera.GetResolution();
            pixelsBuffer = accelerator.Allocate1D<Vector3D>(resolution.Width * resolution.Height);

            // Lights
            lightSourcesBuffer = accelerator.Allocate1D<LightSource>(scene.lightSources.Count);
            lightSourcesBuffer.CopyFromCPU(scene.lightSources.ToArray());

            // Initial Rays
            int nRays = resolution.Width * resolution.Height * GlobalVariables.maxRaysPerPixel;
            List<Ray> gpuRays = Enumerable.Repeat(new Ray(), nRays).ToList();
            for (int row = 0; row < resolution.Height; row++)
            {
                for (int col = 0; col < resolution.Width; col++)
                {
                    gpuRays[(row * resolution.Width + col) * GlobalVariables.maxRaysPerPixel] = 
                        scene.camera.GetRay(row, col); ;
                }
            }
            raysBuffer = accelerator.Allocate1D<Ray>(gpuRays.Count);
            raysBuffer.CopyFromCPU(gpuRays.ToArray());

            // Materials
            List<Material> materials = scene.materials.Get();
            materialsBuffer = accelerator.Allocate1D<Material>(materials.Count);
            materialsBuffer.CopyFromCPU(materials.ToArray());

            // Textures
            List<GpuTextureMetadata> texturesMetadata = [];
            List<Vec3b> texturesData = [];
            int textureDataIndex = 0;
            foreach(var texture in scene.textures.Get()) 
            {
                GpuTextureMetadata metadata;
                metadata.width = texture.Width;
                metadata.height = texture.Height;
                metadata.dataIndex = textureDataIndex;
                textureDataIndex += texture.Width * texture.Height;
                texturesMetadata.Add(metadata);

                for(int row = 0; row < texture.Height; row++)
                {
                    for (int col = 0; col < texture.Width; col++)
                    {
                        texturesData.Add(texture.At<Vec3b>(row, col));
                    }
                }
            }

            texturesMetadataBuffer = accelerator.Allocate1D<GpuTextureMetadata>(texturesMetadata.Count);
            texturesMetadataBuffer.CopyFromCPU(texturesMetadata.ToArray());
            texturesDataBuffer = accelerator.Allocate1D<Vec3b>(texturesData.Count);
            texturesDataBuffer.CopyFromCPU(texturesData.ToArray());

            // Assets
            List<GpuSubMesh> gpuSubMeshes = [];
            List<Triangle> gpuTriangles = [];
            foreach (Asset asset in scene.assets)
            {
                LoadSmartMesh(asset.GetSmartMesh(), gpuSubMeshes, gpuTriangles);
            }

            subMeshesBuffer = accelerator.Allocate1D<GpuSubMesh>(gpuSubMeshes.Count);
            subMeshesBuffer.CopyFromCPU(gpuSubMeshes.ToArray());
            
            trianglesBuffer = accelerator.Allocate1D<Triangle>(gpuTriangles.Count);
            trianglesBuffer.CopyFromCPU(gpuTriangles.ToArray());
        }

        /// <summary>
        /// recursive function that converts a smart mesh to GPU format 
        /// </summary>
        /// <param name="mesh"> The mesh to convert </param>
        /// <param name="gpuSubMeshes"> The GPU formated mesh </param>
        /// <param name="gpuTriangles"> The GPU triangles contained by the mesh </param>
        private static void LoadSmartMesh(in SmartMesh mesh,
            List<GpuSubMesh> gpuSubMeshes,
            List<Triangle> gpuTriangles)
        {
            int index = gpuSubMeshes.Count;
            gpuSubMeshes.Add(new());
            foreach (var child in mesh.children)
            {
                LoadSmartMesh(child, gpuSubMeshes, gpuTriangles);
            }

            GpuSubMesh gpuSubMesh = new();
            gpuSubMesh.nextIndex = gpuSubMeshes.Count;
            gpuSubMesh.boundingBox = mesh.boundingBox;
            gpuSubMesh.firstTriangleIndex = gpuTriangles.Count;
            gpuSubMesh.nTriangles = mesh.triangles.Count;
            gpuSubMeshes[index] = gpuSubMesh;

            foreach (var triangle in mesh.triangles)
            {
                gpuTriangles.Add(triangle);
            }
        }
    }
}
