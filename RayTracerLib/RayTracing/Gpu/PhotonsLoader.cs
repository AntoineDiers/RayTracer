using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU.Runtime;
using ILGPU;
using OpenCvSharp;
using System.Numerics;
using System.Windows.Media.Media3D;
using ILGPU.Algorithms.Random;
using ILGPU.IR;

namespace RayTracerLib
{
    /// <summary>
    /// Class used to load data to the GPU for photon mapping
    /// </summary>
    internal class PhotonsLoader
    {
        /// <summary>
        /// Loads all the data needed on the GPU to perform the photon mapping algorithm
        /// </summary>
        /// <param name="scene"> The scene </param>
        /// <param name="accelerator"> The CUDA accelerator </param>
        /// <param name="photonsBuffer"> The buffer containing the photons </param>
        /// <param name="randomBuffer"> The buffer used for random numbers generation </param>
        internal static void InitPhotonMapping(in Scene scene, Accelerator accelerator,
            out MemoryBuffer1D<Photon, Stride1D.Dense> photonsBuffer,
            out MemoryBuffer1D<XorShift128Plus, Stride1D.Dense> randomBuffer)
        {
            // Photons buffer
            List<Photon> initialPhotons = CreatePhotonEmissionPattern();
            int nPhotons = initialPhotons.Count * scene.lightSources.Count;
            List<Photon> photons = new(nPhotons);
            foreach (var lightSource in scene.lightSources)
            {
                foreach (var photon in initialPhotons)
                {
                    Photon p = photon;
                    p.color = lightSource.color;
                    p.ray.origin = lightSource.position;
                    photons.Add(p);
                }
            }
            photonsBuffer = accelerator.Allocate1D<Photon>(nPhotons);
            photonsBuffer.CopyFromCPU(photons.ToArray());

            // RNG Buffer
            randomBuffer = accelerator.Allocate1D<XorShift128Plus>(nPhotons);
            List<XorShift128Plus> randomList = new(nPhotons);
            Random rng = new();
            for (int i = 0; i < nPhotons; i++)
            {
                randomList.Add(XorShift128Plus.Create(rng));
            }
            randomBuffer.CopyFromCPU(randomList.ToArray());
        }

        /// <summary>
        /// Creates the photon emission pattern of a light source
        /// </summary>
        /// <returns> The list of photons that a light source will emit </returns>
        private static List<Photon> CreatePhotonEmissionPattern()
        {
            List<Photon> res = new(GlobalVariables.photonsSampling);
            Photon p = new();

            double dTheta = 2 * Math.PI / (GlobalVariables.photonsSampling);
            double dPhi = Math.PI / (GlobalVariables.photonsSampling);

            for (int i = 0; i < GlobalVariables.photonsSampling; i++)
            {
                double theta = i * dTheta;
                double sinTheta = Math.Sin(theta);
                double cosTheta = Math.Cos(theta);
                for (int j = 0; j < GlobalVariables.photonsSampling; j++)
                {
                    double phi = j * dPhi;
                    double sinPhi = Math.Sin(phi);
                    double cosPhi = Math.Cos(phi);
                    p.ray.direction.X = sinPhi * cosTheta;
                    p.ray.direction.Y = sinPhi * sinTheta;
                    p.ray.direction.Z = cosPhi;
                    res.Add(p);
                }
            }
            return res;
        }

        /// <summary>
        /// Loads the photons tree on the GPU after the photon mapping step
        /// </summary>
        /// <param name="tree"> The photons tree to load </param>
        /// <param name="accelerator"> The cuda accelerator </param>
        /// <param name="nPixels"> The number of pixels that the final image will contain </param>
        /// <param name="photonsTreeBuffer"> The buffer that will contain the tree </param>
        /// <param name="photonsNearestNeighborsBuffer"> The buffer that will contain the nearest photons for a given pixel </param>
        /// <param name="photonsSearchStateBuffer"> The buffer that will contain the k nearest neighbors search state for each pixel </param>
        internal static void LoadPhotonsTree(
            PhotonTree tree, Accelerator accelerator, int nPixels,
            out MemoryBuffer1D<GpuPhotonTreeNode, Stride1D.Dense> photonsTreeBuffer,
            out MemoryBuffer1D<GpuPhotonNeighbor, Stride1D.Dense> photonsNearestNeighborsBuffer,
            out MemoryBuffer1D<byte, Stride1D.Dense> photonsSearchStateBuffer)
        {
            List<GpuPhotonTreeNode> photonsTree = new(tree.count);
            ConstructPhotonsTree(tree, photonsTree);
            photonsTreeBuffer = accelerator.Allocate1D<GpuPhotonTreeNode>(photonsTree.Count);
            photonsTreeBuffer.CopyFromCPU(photonsTree.ToArray());

            List<GpuPhotonNeighbor> photonNeighbors = Enumerable.Repeat(new GpuPhotonNeighbor(), nPixels * GlobalVariables.nNearestNeighbors).ToList();
            photonsNearestNeighborsBuffer = accelerator.Allocate1D<GpuPhotonNeighbor>(
                nPixels * GlobalVariables.nNearestNeighbors);
            photonsNearestNeighborsBuffer.CopyFromCPU(photonNeighbors.ToArray());

            List<byte> searchStateList = Enumerable.Repeat((byte)0, nPixels * tree.depth).ToList();
            photonsSearchStateBuffer = accelerator.Allocate1D<byte>(
                nPixels * tree.depth);
            photonsSearchStateBuffer.CopyFromCPU(searchStateList.ToArray());
        }

        /// <summary>
        /// Recursive function to turn a photons tree to a GpuPhotonTreeNode flat array
        /// </summary>
        /// <param name="tree"> The tree to convert </param>
        /// <param name="photonsTreeBuffer"> The array that will contain the data </param>
        /// <returns> The index of the root node </returns>
        private static int ConstructPhotonsTree(
            PhotonTree tree,
            List<GpuPhotonTreeNode> photonsTreeBuffer)
        {
            if(tree.count == 0) { return 0; }

            int index = photonsTreeBuffer.Count;
            photonsTreeBuffer.Add(new());

            GpuPhotonTreeNode node;
            node.data = tree.data;
            node.axis = tree.axis;
            node.parentIndex = -1;
            node.leftIndex = -1;
            node.rightIndex = -1;
            if (tree.left != null)
            {
                node.leftIndex = ConstructPhotonsTree(tree.left, photonsTreeBuffer);
                GpuPhotonTreeNode leftNode = photonsTreeBuffer[node.leftIndex];
                leftNode.parentIndex = index;
                photonsTreeBuffer[node.leftIndex] = leftNode;
            }
            if (tree.right != null)
            {
                node.rightIndex = ConstructPhotonsTree(tree.right, photonsTreeBuffer);
                GpuPhotonTreeNode rightNode = photonsTreeBuffer[node.rightIndex];
                rightNode.parentIndex = index;
                photonsTreeBuffer[node.rightIndex] = rightNode;
            }
            photonsTreeBuffer[index] = node;

            return index;
        }
    }
}
