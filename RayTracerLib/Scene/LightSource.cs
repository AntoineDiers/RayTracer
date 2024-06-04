using System.Windows.Media.Media3D;

namespace RayTracerLib
{
    /// <summary>
    /// A light source
    /// </summary>
    public struct LightSource()
    {
        /// <summary> the position of the light source </summary>
        public Point3D position = new(0,0,0);
        /// <summary> the diffuse intensity of the light source </summary>
        public double diffuseIntensity = 0.5;
        /// <summary> the specular intensity of the light source </summary>
        public double specularIntensity = 0.5;
        /// <summary> the ambiant intensity of the light source </summary>
        public double ambiantIntensity = 0.3;
        /// <summary> the radius of the light source (for soft shadows, not implemented yet)</summary>
        public double radius = 0;
        /// <summary> the color of the light source </summary>
        public Vector3D color = new Vector3D(255,255,255);
    }
}
