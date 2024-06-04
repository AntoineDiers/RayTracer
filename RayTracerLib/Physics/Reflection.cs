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
    /// Reflected light behavior
    /// </summary>
    internal class Reflection
    {
        /// <summary>
        /// Compute the probability of reflection and the reflection behavior for a given collision
        /// </summary>
        /// <param name="material"> The material of the collision </param>
        /// <param name="ray"> The ray that will be reflected </param>
        /// <param name="collisionPoint"> The collision point </param>
        /// <param name="collisionNormal"> The collision normal </param>
        /// <param name="reflectedRay"> The ray after reflection </param>
        /// <param name="reflectionCoeff"> The probability of reflection </param>
        /// <returns> true if a reflection can happen </returns>
        internal static bool Compute(in Material material, 
            in Ray ray, 
            in Point3D collisionPoint, 
            in Vector3D collisionNormal, 
            out Ray reflectedRay,
            out double reflectionCoeff)
        {
            reflectedRay = new();

            // Get reflection probability
            reflectionCoeff = GetReflectionCoeff(
                    material,
                    ray,
                    collisionNormal);

            // Compute reflected ray
            if (reflectionCoeff > 0)
            {
                Vector3D expectedReflectionDirection = 2 * Vector3D.DotProduct(-ray.direction, collisionNormal) * collisionNormal + ray.direction;
                expectedReflectionDirection.Normalize();
                reflectedRay = new(collisionPoint + GlobalVariables.epsilon * expectedReflectionDirection, expectedReflectionDirection);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Computes the reflection probability of a collision
        /// </summary>
        /// <param name="material"> The material with which the ray collides </param>
        /// <param name="ray"> The ray </param>
        /// <param name="collisionNormal"> The collision normal </param>
        /// <returns> The reflection probability </returns>
        internal static double GetReflectionCoeff(in Material material,
            in Ray ray,
            in Vector3D collisionNormal)
        {
            if(material.reflectiveCoeff > 0)
            {
                return material.reflectiveCoeff;
            }
            if(material.transparencyCoeff > 0)
            {
                // Use fresnel equations

                double dot = Vector3D.DotProduct(ray.direction, collisionNormal);

                bool entering = dot < 0;
                double n1 = entering ? 1 : material.transparencyIndex;
                double n2 = entering ? material.transparencyIndex : 1;

                double rayCos = Math.Abs(dot);
                double raySin = Math.Sqrt(1 - rayCos * rayCos);

                double refractedSin = Math.Min(1, n1 * raySin / n2);
                double refractedCos = Math.Sqrt(1 - refractedSin * refractedSin);

                double rs = Math.Abs((n1 * rayCos - n2 * refractedCos) / (n1 * rayCos + n2 * refractedCos));
                double rp = Math.Abs((n2 * rayCos - n1 * refractedCos) / (n2 * rayCos + n1 * refractedCos));

                return material.transparencyCoeff * Math.Min(1, 0.5 * (rs * rs + rp * rp));
            }
            return 0;
        }
    }
}
