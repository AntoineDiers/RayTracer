using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace RayTracerLib
{
    /// <summary> A photon, used for photon mapping </summary>
    public struct Photon()
    {
        /// <summary> The color of the photon (r g b) </summary>
        internal Vector3D color;
        /// <summary> The position and direction of the photon </summary>
        internal Ray ray;
        /// <summary> Has this photon hit a transparent / reflective material ? </summary>
        internal byte isCaustic = 0;
    }
}
