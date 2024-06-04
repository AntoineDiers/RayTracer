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
    /// Caustic light behavior
    /// </summary>
    internal class Caustic
    {
        /// <summary>
        /// Computes the caustic light of a ray given the neighboring photons
        /// </summary>
        /// <param name="neighborsStartIndex"> The index of the first neighboring photon in the photons buffer </param>
        /// <param name="nNeighbors"> The number of neighboring photons </param>
        /// <param name="photonsNeighborsBuffer"> The buffer of neighboring photons </param>
        /// <returns> The color of the caustic light </returns>
        internal static Vector3D Compute(
            int neighborsStartIndex, 
            int nNeighbors,
            ArrayView<GpuPhotonNeighbor> photonsNeighborsBuffer)
        {
            Vector3D res = new(0, 0, 0);

            double worstNeighborDistSquared = photonsNeighborsBuffer[neighborsStartIndex + nNeighbors - 1].distSquared;
            for(int i = neighborsStartIndex; i < neighborsStartIndex + nNeighbors; i++)
            {
                GpuPhotonNeighbor neighbor = photonsNeighborsBuffer[i];
                res += Math.Min(1,(GlobalVariables.causticWeight / Math.Sqrt(worstNeighborDistSquared))) * neighbor.data.color;
            }

            return res;
        }
    }
}
