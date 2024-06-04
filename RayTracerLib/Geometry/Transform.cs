using System.Windows.Media.Media3D;

namespace RayTracerLib
{
    /// <summary>
    /// Defines a 3D transformation
    /// </summary>
    /// 
    public class Transform
    {
        /// <summary> The rotation part of the transformation </summary>
        internal Matrix3D rotation = Matrix3D.Identity;
        /// <summary> The translation part of the transformation </summary>
        internal Matrix3D translation = Matrix3D.Identity;
        /// <summary> The scaling part of the transformation </summary>
        internal Matrix3D scaling = Matrix3D.Identity;

        /// <summary>
        /// Sets the translation component of the transform
        /// </summary>
        /// <param name="vec"> The given translation vector </param>
        public void SetTranslation(in Vector3D vec)
        {
            translation.OffsetX = vec.X;
            translation.OffsetY = vec.Y;
            translation.OffsetZ = vec.Z;
        }

        /// <summary>
        /// Sets the scaling component of the transform
        /// </summary>
        /// <param name="scale"> The scaling vector </param>
        public void SetScaling(in Vector3D scale)
        {
            scaling = Matrix3D.Identity;
            scaling.Scale(scale);
        }

        /// <summary>
        /// Sets the scaling component of the transform
        /// </summary>
        /// <param name="scale"> The scaling value (will be applied to X, Y and Z) </param>
        public void SetScaling(double scale)
        {
            scaling = Matrix3D.Identity;
            scaling.Scale(new Vector3D(scale, scale, scale));
        }

        /// <summary>
        /// Sets the rotation component of the transform such that applying this rotation 
        /// to the x and y axis will return the X and Y vectors
        /// </summary>
        /// <param name="X"> The value of the x-axis vector after applying the rotation </param>
        /// <param name="Y"> The value of the y-axis vector after applying the rotation </param>
        /// <exception cref="ArgumentException"> X and Y shall not be of length 0  </exception>
        /// <exception cref="ArgumentException"> X and Y shall be perpendicular  </exception>
        public void SetRotation(Vector3D X, Vector3D Y)
        {
            if(X.LengthSquared == 0 || Y.LengthSquared == 0) { 
                throw new ArgumentException("X and Y shall not be of length 0"); }

            if (Math.Abs(Vector3D.DotProduct(X,Y)) > GlobalVariables.epsilon)
            { throw new ArgumentException("X and Y shall be perpendicular"); }

            X.Normalize();
            Y.Normalize();
            Vector3D Z = Vector3D.CrossProduct(X, Y);
            rotation.M11 = X.X;
            rotation.M12 = X.Y;
            rotation.M13 = X.Z;
            rotation.M21 = Y.X;
            rotation.M22 = Y.Y;
            rotation.M23 = Y.Z;
            rotation.M31 = Z.X;
            rotation.M32 = Z.Y;
            rotation.M33 = Z.Z;
        }

        /// <summary>
        /// Sets the rotation component from an axis of rotation and an angle
        /// </summary>
        /// <param name="axis"> The axis of rotation </param>
        /// <param name="angle"> The angle in radians </param>
        /// <exception cref="ArgumentException"> The given axis is the zero vector </exception>
        public void SetRotation(Vector3D axis, double angle)
        {
            if(axis.Length == 0) { throw new ArgumentException("Rotation axis shall be a non-zero vector"); }
            
            axis.Normalize();
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            rotation = Matrix3D.Identity;
            rotation.M11 = cos + axis.X * axis.X * (1 - cos);
            rotation.M12 = axis.X * axis.Y * (1 - cos) - axis.Z * sin;
            rotation.M13 = axis.X * axis.Z * (1 - cos) + axis.Y * sin;
            rotation.M21 = axis.Y * axis.X * (1 - cos) + axis.Z * sin;
            rotation.M22 = cos + axis.Y * axis.Y * (1 - cos);
            rotation.M23 = axis.Y * axis.Z * (1 - cos) - axis.X * sin;
            rotation.M31 = axis.X * axis.Z * (1 - cos) - axis.Y * sin;
            rotation.M32 = axis.Y * axis.Z * (1 - cos) + axis.X * sin;
            rotation.M33 = cos + axis.Z * axis.Z * (1 - cos);
        }

        /// <summary>
        /// Applies this transformation to a point
        /// </summary>
        /// <param name="p"> The point to transform </param>
        /// <returns> The transformed point </returns>
        internal Point3D Apply(in Point3D p)
        {
            Point3D res = scaling.Transform(p);
            res = rotation.Transform(res);
            res = translation.Transform(res);
            return res;
        }

        /// <summary>
        /// Applies this transformation to a triangle
        /// </summary>
        /// <param name="t"> The triangle to transform </param>
        /// <returns> The transformed triangle </returns>
        internal Triangle Apply(in Triangle t)
        {
            Triangle res = t;
            res.A.pos = Apply(t.A.pos);
            res.B.pos = Apply(t.B.pos);
            res.C.pos = Apply(t.C.pos);
            res.A.normal = rotation.Transform(t.A.normal);
            res.B.normal = rotation.Transform(t.B.normal);
            res.C.normal = rotation.Transform(t.C.normal);
            return res;
        }
    }
}
