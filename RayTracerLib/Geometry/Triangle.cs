using System.Windows.Media.Media3D;
using OpenCvSharp;

namespace RayTracerLib
{
    /// <summary>
    /// The vertex of a triangle
    /// </summary>
    /// <remarks> This has to be a public struct because it is used in GPU code </remarks>
    public struct TriangleVertex
    {
        /// <summary> The position of this vertex </summary>
        internal Point3D pos;
        /// <summary> The normal at this vertex </summary>
        internal Vector3D normal;
        /// <summary> The textures coordinates at this vertex (in UV coordinates) <br/>
        /// ⚠️ May not be defined for non-textured geometries </summary>
        internal Point2d texture;
    }

    /// <summary>
    /// A triangle
    /// </summary>
    /// <remarks> This has to be a public struct because it is used in GPU code </remarks>
    public struct Triangle
    {
        /// <summary> The A vertex </summary>
        internal TriangleVertex A;
        /// <summary> The B vertex </summary>
        internal TriangleVertex B;
        /// <summary> The C vertex </summary>
        internal TriangleVertex C;
        /// <summary> Texture index in the Textures Library, -1 for non-textured triangle </summary>
        internal int textureIndex = -1;
        /// <summary> Material index in the Materials Library </summary>
        internal int materialIndex;

        /// <summary>
        /// Creates a basic triangle from 3 points and a material index <br/>
        /// ⚠️ The triangle will not be textured and there will be no normals interpolation, more complex triangles must be handcrafted
        /// </summary>
        /// <param name="_A"> First point of the triangle </param>
        /// <param name="_B"> Second point of the triangle </param>
        /// <param name="_C"> Third point of the triangle </param>
        /// <param name="_materialIndex"> The index of the triangle's material in the materials library </param>
        /// <exception cref="ArgumentException"> _A _B and _C do not form a triangle </exception>
        internal Triangle(in Point3D _A, in Point3D _B, in Point3D _C, 
            int _materialIndex)
        {
            Vector3D N = Vector3D.CrossProduct((_B - _A), (_C - _A));
            if(N.LengthSquared == 0) {
                throw new ArgumentException("_A, _B and _C do not form a triangle"); }
            N.Normalize();
            A.pos = _A;
            A.normal = N;
            B.pos = _B;
            B.normal = N;
            C.pos = _C;
            C.normal = N;
            materialIndex = _materialIndex;
        }

        /// <summary>
        /// Computes the interpolated normal at a given point in UV coordinates
        /// </summary>
        /// <param name="u">The u coordinate (along the AB segment)</param>
        /// <param name="v">The v coordinate (along the AC segment)</param>
        /// ⚠️ u and v must be valid UV coordinates (0 <= u,v <= 1, u+v <= 1)
        /// No exception can be thrown in that case as this will run on GPU
        /// <returns> The normal at the given point </returns>
        internal readonly Vector3D GetNormal(double u, double v)
        {
            Vector3D uVec = u * B.normal + 0.5 * (1 - (u + v)) * A.normal;
            Vector3D vVec = v * C.normal + 0.5 * (1 - (u + v)) * A.normal;
            Vector3D res = uVec + vVec;            
            res.Normalize();
            return res;
        }
    }
}
