using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using ILGPU;

namespace RayTracerLib
{
    /// <summary>
    /// Phong model for light behavior
    /// </summary>
    internal class PhongModel
    {
        /// <summary>
        /// Computes the phong model for a given collision and light source
        /// </summary>
        /// <param name="ray"> The ray </param>
        /// <param name="material"> The material that was hit </param>
        /// <param name="collisionPoint"> The collision point </param>
        /// <param name="collisionNormal"> The collision normal </param>
        /// <param name="collisionColor"> The color of the material at the collision point </param>
        /// <param name="lightSource"> The light source for which we want to compute the phong model </param>
        /// <param name="ambiantPart"> The ambiant part of lighting </param>
        /// <param name="phongPart"> The diffusive and specular parts of lighting </param>
        internal static void Compute(Ray ray,
            Material material,
            Point3D collisionPoint,
            Vector3D collisionNormal,
            Vector3D collisionColor,
            LightSource lightSource,
            out Vector3D ambiantPart,
            out Vector3D phongPart)
        {
            ambiantPart = new(0,0,0);
            phongPart = new(0,0,0);
            if(Vector3D.DotProduct(collisionNormal, ray.direction) > 0)
            {
                return;
            }

            // Ambiant
            ambiantPart = lightSource.ambiantIntensity * collisionColor;
            ambiantPart.X *= lightSource.color.X / 255;
            ambiantPart.Y *= lightSource.color.Y / 255;
            ambiantPart.Z *= lightSource.color.Z / 255;

            double intensity = 0;

            // diffuse
            Vector3D lightSourceDirection = (lightSource.position - collisionPoint);
            lightSourceDirection.Normalize();
            double perpendicularity = Vector3D.DotProduct(lightSourceDirection, collisionNormal);
            intensity += lightSource.diffuseIntensity * material.diffuseCoeff * perpendicularity;

            // Specular
            Vector3D expectedSpecularDirection = 2 * perpendicularity * collisionNormal - lightSourceDirection;
            expectedSpecularDirection.Normalize();
            intensity += lightSource.specularIntensity * material.specularCoeff *
                Math.Pow(Math.Abs(Vector3D.DotProduct(expectedSpecularDirection, -ray.direction)), material.shininess);

            phongPart = intensity * collisionColor;
            phongPart.X *= lightSource.color.X / 255;
            phongPart.Y *= lightSource.color.Y / 255;
            phongPart.Z *= lightSource.color.Z / 255;
        }
    }
}
