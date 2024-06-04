using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Xaml.Schema;

namespace RayTracerLib
{
    /// <summary>
    /// Smallest axis-aligned cuboid that contains a set of points
    /// </summary>
    /// <remarks> This has to be a public struct because it is used in GPU code </remarks>
    internal struct BoundingBox
    {
        /// <summary> The center of the bounding box </summary>
        internal Point3D center;
        /// <summary> The width of the bounding box in all 3 directions </summary>
        internal Point3D size;

        /// <summary>
        /// Creates the smallest BoundingBox containing all the given triangles
        /// </summary>
        /// <param name="triangles"> The given list of triangles </param>
        /// <exception cref="ArgumentException"> Given triangle list is empty </exception>
        internal BoundingBox(in List<Triangle> triangles)
        {
            if(triangles.Count == 0) { 
                throw new ArgumentException("Can not create a bounding box around an empty set of triangles"); }

            List<Point3D> points = new (3 * triangles.Count);
            foreach (Triangle triangle in triangles) 
            {
                points.Add(triangle.A.pos);
                points.Add(triangle.B.pos);
                points.Add(triangle.C.pos);
            }
            var res = new BoundingBox(points);
            center = res.center;
            size = res.size;
        }

        /// <summary>
        /// Creates the smallest BoundingBox containing all the given points 
        /// </summary>
        /// <param name="points"> The given list of points </param>
        /// <exception cref="ArgumentException"> Given point list is empty </exception>
        private BoundingBox(in List<Point3D> points)
        {
            if (points.Count == 0){
                throw new ArgumentException("Can not create a bounding box around an empty set of points");}


            Point3D minPoint;
            minPoint.X = double.MaxValue;
            minPoint.Y = double.MaxValue;
            minPoint.Z = double.MaxValue;
            Point3D maxPoint;
            maxPoint.X = double.MinValue;
            maxPoint.Y = double.MinValue;
            maxPoint.Z = double.MinValue;

            foreach (Point3D p in points)
            {
                maxPoint.X = Math.Max(maxPoint.X, p.X);
                maxPoint.Y = Math.Max(maxPoint.Y, p.Y);
                maxPoint.Z = Math.Max(maxPoint.Z, p.Z);

                minPoint.X = Math.Min(minPoint.X, p.X);
                minPoint.Y = Math.Min(minPoint.Y, p.Y);
                minPoint.Z = Math.Min(minPoint.Z, p.Z);
            }

            center = new Point3D(
                (minPoint.X + maxPoint.X) / 2, 
                (minPoint.Y + maxPoint.Y) / 2, 
                (minPoint.Z + maxPoint.Z) / 2);
            size = new Point3D(
                maxPoint.X - minPoint.X,
                maxPoint.Y - minPoint.Y,
                maxPoint.Z - minPoint.Z);
        }

        /// <summary>
        /// Returns the smallest BoundingBox that would 
        /// contain "this" if "this" was rotated by the given rotation
        /// </summary>
        /// <param name="rotation"> The given rotation </param>
        /// <returns>The resulting BoundingBox</returns>
        internal BoundingBox Rotate(in Matrix3D rotation)
        {
            List<Point3D> points = new(8);
            foreach (double dx in (double[])[-0.5,0.5])
            {
                foreach (double dy in (double[])[-0.5, 0.5])
                {
                    foreach (double dz in (double[])[-0.5, 0.5])
                    {
                        points.Add(rotation.Transform(center + new Vector3D(size.X * dx, size.Y * dy, size.Z * dz)));
                    }
                }
            }

            return new BoundingBox(points);
        }

        /// <summary>
        /// Check if the BoundingBox contains the given point
        /// </summary>
        /// <param name="point"> The given point </param>
        /// <returns>True if the BoundingBox contains the given point</returns>
        internal bool Contains(in Point3D point)
        {
            return Math.Abs(point.X - center.X) <= size.X / 2 + GlobalVariables.epsilon
                && Math.Abs(point.Y - center.Y) <= size.Y / 2 + GlobalVariables.epsilon
                && Math.Abs(point.Z - center.Z) <= size.Z / 2 + GlobalVariables.epsilon;
        }

        /// <summary>
        /// Check if the BoundingBox contains the given triangle
        /// </summary>
        /// <param name="triangle"> The given triangle </param>
        /// <returns>True if the BoundingBox contains the given triangle</returns>
        internal bool Contains(in Triangle triangle) 
        { 
            return Contains(triangle.A.pos) && 
                Contains(triangle.B.pos) && 
                Contains(triangle.C.pos);
        }
    }
}
