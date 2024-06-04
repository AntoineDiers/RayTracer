using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace RayTracerLib
{
    /// <summary>
    /// A KD-Tree for fast K nearest neighbors search 
    /// </summary>
    internal class PhotonTree
    {
        /// <summary> The photon of this node </summary>
        internal readonly Photon data;
        /// <summary> The axis along which this node splits the photons (0 -> X, 1 -> Y, 2 -> Z) </summary>
        internal readonly int axis;
        /// <summary> The number of photons contained by this tree </summary>
        internal readonly int count = 0;
        /// <summary> The children of this node </summary>
        internal readonly PhotonTree? left, right;
        /// <summary> The depht of this tree </summary>
        internal readonly int depth = 1;

        /// <summary>
        /// Creates a balanced KD-Tree from a list of photons
        /// </summary>
        /// <param name="photons"> The list of photons </param>
        /// <param name="ax"> The axis along which the first node should split the data (0 -> X, 1 -> Y, 2 -> Z) </param>
        internal PhotonTree(List<Photon> photons, int ax = 0)
        {
            if(photons.Count == 0)
            {
                return;
            }

            axis = ax;
            count = photons.Count;
            int nextAxis = (axis + 1) % 3;

            if(photons.Count == 1)
            {
                data = photons.First();
                depth = 1;
                return;
            }

            // Sort Photons according to the axis corrdinates
            photons.Sort((Photon p1, Photon p2) => { 
                return GetAxisValue(p1.ray.origin, axis).
                CompareTo(GetAxisValue(p2.ray.origin, axis)); });

            int medianIndex = photons.Count / 2;
            data = photons[medianIndex];

            // Left
            if(medianIndex >= 1)
            {
                List<Photon> leftPhotons = photons.GetRange(0, medianIndex);
                left = new(leftPhotons, nextAxis);
                depth = Math.Max(1 + left.depth, depth);
            }

            // Right
            if (photons.Count - (medianIndex + 1) >= 1)
            {
                List<Photon> rightPhotons = photons.GetRange(medianIndex + 1, photons.Count - (medianIndex + 1));
                right = new(rightPhotons, nextAxis);
                depth = Math.Max(1 + right.depth, depth);
            }

            
        }

        /// <summary>
        /// Computes the value of a point along a given abciss
        /// </summary>
        /// <param name="p"> The point </param>
        /// <param name="axis"> The abciss </param>
        /// <returns> 
        /// axis = 0 : p.X <br/>
        /// axis = 1 : p.Y <br/>
        /// axis = 2 : p.Z <br/>
        /// default  : 0
        /// </returns>
        internal static double GetAxisValue(in Point3D p, int axis)
        {
            switch (axis)
            {
                case 0: return p.X;
                case 1: return p.Y;
                case 2: return p.Z;
                default: return 0.0; // Can not throw in GPU code :(
            }
        }

    }
}
