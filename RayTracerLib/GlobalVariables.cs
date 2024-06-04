using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTracerLib
{
    public class GlobalVariables
    {
        public static readonly int trianglePerSubMesh = 200;
        public static readonly double epsilon = 1e-10;
        public static readonly int maxRaysPerPixel = 11;
        public static readonly int photonsSampling = 500;
        public static readonly int maxPhotonBounces = 20;
        public static readonly int nNearestNeighbors = 10;
        public static readonly double causticWeight = 0.02;
        public static readonly double minReflectionRefractionCoeff = 0.1;

    }
}
