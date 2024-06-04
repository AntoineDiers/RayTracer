using System.Windows.Media.Media3D;
using OpenCvSharp;

namespace RayTracerLib
{
    /// <summary>
    /// Material definition
    /// </summary>
    public struct Material
    {
        /// <summary> Constructor </summary>
        public Material() {}

        /// <summary> The RGB color of this material 
        /// ⚠️ color is encoded with doubles for more precise results but the max value is still 255</summary>
        public Vector3D color = new(255,255,255);
        /// <summary> Ambiant coefficient (See Phong Model) </summary>
        public double ambientCoeff = 1.0;
        /// <summary> Diffuse coefficient (See Phong Model) </summary>
        public double diffuseCoeff = 1.0;
        /// <summary> Specular coefficient (See Phong Model) </summary>
        public double specularCoeff = 0.5;
        /// <summary> The shininess (See Phong Model) </summary>
        public double shininess = 10;
        /// <summary> Reflective coefficient (0 -> 1) </summary>
        public double reflectiveCoeff = 0;
        /// <summary> Transparency coefficient (0 -> 1) </summary>
        public double transparencyCoeff = 0;
        /// <summary> Transparency index (>1 , see Fresnel laws) </summary>
        public double transparencyIndex = 0.0;
    }
}
