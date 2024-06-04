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
    /// Class used to perform a non-recursive K nearest neighbors search in a flat list 
    /// </summary>
    internal class NearestNeighborSearch
    {
        /// <summary>
        /// performs a non-recursive K nearest neighbors search in a flat list. <br/>
        /// Algorithm : 
        /// - First time we arrive on a node, we go to its "best" child (the one that is on the same size as the input point)
        /// - Second time we arrive on a node, we go to its "worst" child if we need to (see knn search in kd trees) 
        /// - Third time we arrive on a node, we go back to its parent
        /// </summary>
        /// <param name="pixelIndex"> The index of the pixel for which we are running this search </param>
        /// <param name="point"> The point for which we want to search neighbors</param>
        /// <param name="treeDepth"> The depth of the "tree" we are searching </param>
        /// <param name="photonsTreeBuffer"> The GPU buffer containing the tree data </param>
        /// <param name="photonsNeighborsBuffer"> The GPU buffer that will contain the neighbors found </param>
        /// <param name="photonsSearchStateBuffer"> The GPU buffer that will contain the state of the search (the number of visits at each depth)</param>
        /// <returns> The number of neighbors found </returns>
        internal static int Run(int pixelIndex, Point3D point, int treeDepth,
            ArrayView<GpuPhotonTreeNode> photonsTreeBuffer,
            ArrayView<GpuPhotonNeighbor> photonsNeighborsBuffer,
            ArrayView<byte> photonsSearchStateBuffer)
        {
            // Reset search state
            for(int i = 0; i < treeDepth; i++)
            {
                photonsSearchStateBuffer[pixelIndex * treeDepth + i] = 0;
            }

            int neighborsStartIndex = pixelIndex * GlobalVariables.nNearestNeighbors;
            int nNeighborsFound = 0;
            int currentDepth = 0;
            int treeIndex = 0;
            while(treeIndex >= 0) 
            {
                GpuPhotonTreeNode node = photonsTreeBuffer[treeIndex];
                byte nVisits = UpdateNumberOfVisits(pixelIndex, currentDepth, treeDepth,
                    photonsSearchStateBuffer);

                // First time we arrive on this node, select the best path
                if(nVisits == 0) 
                {
                    AddToNearestNeighbors(treeIndex,
                        neighborsStartIndex,
                        ref nNeighborsFound,
                        point,
                        photonsTreeBuffer,
                        photonsNeighborsBuffer);

                    double pointVal = PhotonTree.GetAxisValue(point, node.axis);
                    double nodeVal = PhotonTree.GetAxisValue(node.data.ray.origin, node.axis);
                    int newIndex = pointVal < nodeVal ? node.leftIndex : node.rightIndex;
                    if (newIndex >= 0)
                    {
                        treeIndex = newIndex;
                        currentDepth++;
                    }
                }
                // Second time we arrive on this node, select the worst path if needed
                else if (nVisits == 1)
                {
                    double pointVal = PhotonTree.GetAxisValue(point, node.axis);
                    double nodeVal = PhotonTree.GetAxisValue(node.data.ray.origin, node.axis);
                    double u = pointVal - nodeVal;
                    
                    if(u * u < GetWorstNeighborDistSquared(
                        neighborsStartIndex, 
                        nNeighborsFound,
                        photonsNeighborsBuffer) 
                        || nNeighborsFound < GlobalVariables.nNearestNeighbors)
                    {
                        //treeIndex = pointVal < nodeVal ? node.rightIndex : node.leftIndex;
                        //currentDepth++;
                        int newIndex = pointVal < nodeVal ? node.rightIndex : node.leftIndex;
                        if (newIndex >= 0)
                        {
                            treeIndex = newIndex;
                            currentDepth++;
                        }
                    }
                    else
                    {
                        treeIndex = node.parentIndex;
                        currentDepth--;
                    }
                    
                }
                // Third time we arrive on this node, go back up, we will never come back
                else
                {
                    treeIndex = node.parentIndex;
                    currentDepth--;
                }
            }

            return nNeighborsFound;
        }

        /// <summary>
        /// Updates the number of visits of a node in the photons tree
        /// </summary>
        /// <param name="pixelIndex"> The index of the pixel for which we are running this search </param>
        /// <param name="currentDepth"> The current depth in the tree </param>
        /// <param name="treeDepth"> The depth of the tree </param>
        /// <param name="photonsSearchStateBuffer"> The state of the search (number of visits at each depth) </param>
        /// <returns></returns>
        private static byte UpdateNumberOfVisits(
            int pixelIndex, int currentDepth, int treeDepth,
            ArrayView<byte> photonsSearchStateBuffer)
        {
            int index = pixelIndex * treeDepth + currentDepth;
            byte res = photonsSearchStateBuffer[index];

            photonsSearchStateBuffer[index] = (byte)(res + 1);
            // If we are going UP, set the lower depths to 0
            if (res > 0)
            {
                int maxIndex = (pixelIndex + 1) * treeDepth;
                for (int i = index + 1; i < maxIndex; i++) 
                {
                    photonsSearchStateBuffer[i] = 0;
                }
            }
            return res;
        }

        /// <summary>
        /// Adds a node to the nearest neighbors buffer if it belongs in there
        /// </summary>
        /// <param name="nodeIndex"> The index of the node to add </param>
        /// <param name="neighborsStartIndex"> The first index of the nearest neighbors buffer </param>
        /// <param name="nNeighborsFound"> The number of neighbors found </param>
        /// <param name="point"> The point for which we want to find neighbors </param>
        /// <param name="photonsTreeBuffer"> The buffer containing the tree data </param>
        /// <param name="photonsNeighborsBuffer"> The buffer containing the neighbors </param>
        private static void AddToNearestNeighbors(
            int nodeIndex, 
            int neighborsStartIndex, 
            ref int nNeighborsFound, 
            Point3D point,
            ArrayView<GpuPhotonTreeNode> photonsTreeBuffer,
            ArrayView<GpuPhotonNeighbor> photonsNeighborsBuffer)
        {
            // Add the current node to the neighbors if it belongs there
            GpuPhotonTreeNode node = photonsTreeBuffer[nodeIndex];
            double distSquared = (node.data.ray.origin - point).LengthSquared;
            if (distSquared < GetWorstNeighborDistSquared(
                neighborsStartIndex,
                nNeighborsFound,
                photonsNeighborsBuffer) || 
                nNeighborsFound < GlobalVariables.nNearestNeighbors)
            {
                nNeighborsFound = Math.Min(nNeighborsFound + 1, GlobalVariables.nNearestNeighbors);

                GpuPhotonNeighbor neighbor;
                neighbor.data = node.data;
                neighbor.distSquared = distSquared;

                int index = neighborsStartIndex + (nNeighborsFound - 1);
                photonsNeighborsBuffer[index] = neighbor;
                while(index > neighborsStartIndex)
                {
                    GpuPhotonNeighbor next = photonsNeighborsBuffer[index - 1];
                    if (next.distSquared > distSquared)
                    {
                        photonsNeighborsBuffer[index - 1] = neighbor;
                        photonsNeighborsBuffer[index] = next;
                        index--;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the distance of the worst neighbor among the k nearest
        /// </summary>
        /// <param name="neighborsStartIndex"> The first index of the nearest neighbors buffer </param>
        /// <param name="nNeighborsFound"> The number of neighbors found </param>
        /// <param name="photonsNeighborsBuffer"> The buffer containing the neighbors </param>
        /// <returns></returns>
        private static double GetWorstNeighborDistSquared(int neighborsStartIndex, int nNeighborsFound,
            ArrayView<GpuPhotonNeighbor> photonsNeighborsBuffer)
        {
            return nNeighborsFound == 0 ? 
                double.MaxValue : 
                photonsNeighborsBuffer[neighborsStartIndex + nNeighborsFound - 1].distSquared;   
        }


    }
}
