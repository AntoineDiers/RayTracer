using System;
using System.Windows.Media.Converters;
using System.Windows.Media.Media3D;

namespace RayTracerLib
{
    /// <summary>
    /// A light ray (3D point with a direction vector)
    /// </summary>
    /// <remarks> This has to be a public struct because it is used in GPU code </remarks>
    public struct Ray
    {
        /// <summary> The starting point of the ray </summary>
        internal Point3D origin;
        /// <summary> The direction of the ray </summary>
        internal Vector3D direction;
        /// <summary> The weight of this photon (intensity) </summary>
        internal double weight;

        /// <summary>
        /// Creates a ray with the given origin and direction <br/>
        /// </summary>
        /// <param name="_origin"> The ray's origin </param>
        /// <param name="_direction"> The ray's direction <br/>
        /// <param name="_weight"> The ray's weight (intensity) <br/>
        /// ⚠️ Should not be (0,0,0), No exception can be thrown in that case as this will run on GPU
        /// </param>
        internal Ray(Point3D _origin, Vector3D _direction, double _weight = 1)
        {
            _direction.Normalize();
            origin = _origin;
            direction = _direction;
            weight = _weight;
        }

        /// <summary>
        /// Computes a lower bound for the distance between this ray and a triangle 
        /// </summary>
        /// <param name="triangle"> The triangle to which we want to compute a distance lower bound </param>
        /// <returns> The distance lower bound </returns>
        internal readonly double GetDistanceLowerBound(in Triangle triangle)
        {
            return Math.Min(Math.Min(
                Vector3D.DotProduct(triangle.A.pos - origin, direction),
                Vector3D.DotProduct(triangle.B.pos - origin, direction)),
                Vector3D.DotProduct(triangle.C.pos - origin, direction));
        }

        /// <summary>
        /// Checks for intersection with a triangle
        /// </summary>
        /// <param name="triangle"> The triangle with which we want to test for collision </param>
        /// <param name="abciss"> The abciss at which the ray intersects with the triangle </param>
        /// <param name="u"> The u component of the UV representation of the collision point </param>
        /// <param name="v"> The v component of the UV representation of the collision point </param>
        /// <returns> True if there is an intersection </returns>
        internal readonly bool Intersects(in Triangle triangle,
            out double abciss, out double u, out double v)
        {
            Vector3D E1 = triangle.B.pos - triangle.A.pos;
            Vector3D E2 = triangle.C.pos - triangle.A.pos;
            Vector3D N = Vector3D.CrossProduct(E1, E2);
            double det = - Vector3D.DotProduct(direction, N);
            double invdet = 1.0 / det;
            Vector3D AO = origin - triangle.A.pos;
            Vector3D DAO = Vector3D.CrossProduct(AO, direction);
            u = Vector3D.DotProduct(E2, DAO) * invdet;
            v = -Vector3D.DotProduct(E1, DAO) * invdet;
            abciss = Vector3D.DotProduct(AO, N) * invdet;
            N.Normalize();

            return (Math.Abs(Vector3D.DotProduct(direction, N)) > GlobalVariables.epsilon && abciss >= 0.0 && u >= 0.0 && v >= 0.0 && (u + v) <= 1.0);
        }

        /// <summary>
        /// Checks for intersection with a bounding box
        /// </summary>
        /// <param name="boundingBox"> The bounding box with which we want to check for intersection </param>
        /// <returns> True if an intersection is found </returns>
        internal readonly bool Intersects(BoundingBox boundingBox)
        {
            BoundingBoxIntersectionAbciss(boundingBox.center.X - boundingBox.size.X / 2,
                boundingBox.center.X + boundingBox.size.X / 2, origin.X, direction.X, 
                out double tXMin, out double tXMax);
            BoundingBoxIntersectionAbciss(boundingBox.center.Y - boundingBox.size.Y / 2,
                boundingBox.center.Y + boundingBox.size.Y / 2, origin.Y, direction.Y, 
                out double tYMin, out double tYMax);
            BoundingBoxIntersectionAbciss(boundingBox.center.Z - boundingBox.size.Z / 2,
                boundingBox.center.Z + boundingBox.size.Z / 2, origin.Z, direction.Z, 
                out double tZMin, out double tZMax);

            double tMax = Math.Min(tXMax, Math.Min(tYMax, tZMax));
            double tMin = Math.Max(tXMin, Math.Max(tYMin, tZMin));
            return tMax >= tMin && tMax >= 0;
        }

        /// <summary>
        /// Computes the min and max values of t such that xMin \< dx * t + x0 \< xMax
        /// </summary>
        /// <param name="xMin"></param>
        /// <param name="xMax"></param>
        /// <param name="x0"></param>
        /// <param name="dx"></param>
        /// <param name="tMin"></param>
        /// <param name="tMax"></param>
        private readonly void BoundingBoxIntersectionAbciss(
            double xMin, double xMax, 
            double x0, double dx, 
            out double tMin, out double tMax)
        {
            tMin = (xMin - x0) / dx;
            tMax = (xMax - x0) / dx;
            if(dx < 0)
            {
                (tMin, tMax) = (tMax, tMin);
            }
        }
    }

    
}
