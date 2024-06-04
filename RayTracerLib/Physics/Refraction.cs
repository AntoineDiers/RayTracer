using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;

namespace RayTracerLib
{
    /// <summary>
    /// Refracted light behavior
    /// </summary>
    internal class Refraction
    {
        /// <summary>
        /// Computes the probability of refraction and the refracted ray
        /// </summary>
        /// <param name="material"> The material of the collision </param>
        /// <param name="ray"> The ray that will be refracted </param>
        /// <param name="collisionPoint"> The collision point </param>
        /// <param name="collisionNormal"> The collision normal </param>
        /// <param name="refractedRay"> The ray after refraction </param>
        /// <param name="refractionCoeff"> The probability of refraction </param>
        /// <returns> true if a refraction can happen </returns>
        internal static bool Compute(in Material material,
            in Ray ray,
            in Point3D collisionPoint,
            in Vector3D collisionNormal,
            out Ray refractedRay,
            out double refractionCoeff)
        {
            refractedRay = new();

            refractionCoeff  = 0.0;
            if (material.transparencyCoeff > 0)
            {
                double normalCoeff = Vector3D.DotProduct(ray.direction, collisionNormal);
                Vector3D tangeantVector = ray.direction - collisionNormal * normalCoeff;
                double n1 = normalCoeff > 0 ? material.transparencyIndex : 1;
                double n2 = normalCoeff > 0 ? 1 : material.transparencyIndex;

                double sinRefracted = Math.Min(0.99,(n1 / n2) * tangeantVector.Length);
                double cosRefracted = Math.Sqrt(1 - sinRefracted * sinRefracted);
                Vector3D refractionVector = (normalCoeff > 0 ? 1 : -1) * cosRefracted * collisionNormal +
                    (n1 / n2) * tangeantVector;
                refractionVector.Normalize();

                refractedRay = new(collisionPoint + GlobalVariables.epsilon * refractionVector, refractionVector);

                double reflectionCoeff = Reflection.GetReflectionCoeff(material, ray, collisionNormal);

                refractionCoeff = (1 - reflectionCoeff) * material.transparencyCoeff;

                return true;
            }
            return false;
        }

        
    }
}
