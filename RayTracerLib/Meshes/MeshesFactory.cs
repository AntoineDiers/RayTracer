using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace RayTracerLib
{
    /// <summary>
    /// Util class to generate basic meshes
    /// </summary>
    public class MeshesFactory
    {
        /// <summary>
        /// Creates a cube mesh
        /// </summary>
        /// <param name="materialIndex"> The material index of the cube </param>
        /// <returns> The cube mesh </returns>
        public static SmartMesh CreateCube(int materialIndex)
        {
            return new(CreateRhomboid3D(
                new Vector3D(1, 0, 0), 
                new Vector3D(0, 1, 0), 
                new Vector3D(0, 0, 1), materialIndex));
        }

        /// <summary>
        /// Creates a sphere mesh
        /// </summary>
        /// <param name="sampling"> The amount of sampling </param>
        /// <param name="materialIndex"> The material index of the sphere </param>
        /// <returns> The sphere mesh </returns>
        public static SmartMesh CreateSphere(int sampling, int materialIndex)
        {
            List<Triangle> res = new();
            for (int i = 0; i < sampling; i++)
            {
                for (int j = 0; j < sampling; j++)
                {
                    double theta = 2 * i * Math.PI / sampling;
                    double nextTheta = 2 * (i + 1) * Math.PI / sampling;
                    double phi = j * Math.PI / sampling;
                    double nextPhi = (j + 1) * Math.PI / sampling;
                    
                    Point3D zero = new(0, 0, 0);
                    TriangleVertex A = new(), B = new(), C = new(), D = new();
                    A.pos = FromSphericCoordinates(theta, phi);
                    A.normal = A.pos - zero;
                    B.pos = FromSphericCoordinates(nextTheta, phi);
                    B.normal = B.pos - zero;
                    C.pos = FromSphericCoordinates(nextTheta, nextPhi);
                    C.normal = C.pos - zero;
                    D.pos = FromSphericCoordinates(theta, nextPhi);
                    D.normal = D.pos - zero;
                    if (j != 0)
                    {
                        res.Add(new()
                        {
                            A = A, B = C, C = B,
                            materialIndex = materialIndex,
                        });
                    }
                    if (j != sampling - 1)
                    {
                        res.Add(new()
                        {
                            A = A, B = D, C = C,
                            materialIndex = materialIndex
                        });
                    }
                }
            }
            return new(res);
        }

        /// <summary>
        /// Create triangles that form a rhomboid
        /// </summary>
        private static List<Triangle> CreateRhomboid(Point3D O, Vector3D X, Vector3D Y, int materialIndex)
        {
            return [new(O, O + X, O + Y, materialIndex), 
                new(O + X + Y, O + Y, O + X, materialIndex)];
        }

        /// <summary>
        /// Create triangles that form a 3D rhomboid
        /// </summary>
        private static List<Triangle> CreateRhomboid3D(Vector3D X, Vector3D Y, Vector3D Z, int materialIndex)
        {
            List<Triangle> res  = [];
            Point3D O = new(0, 0, 0);

            res = res.Concat(CreateRhomboid(O, Y, X, materialIndex)).ToList();
            res = res.Concat(CreateRhomboid(O, X, Z, materialIndex)).ToList();
            res = res.Concat(CreateRhomboid(O, Z, Y, materialIndex)).ToList();

            res = res.Concat(CreateRhomboid(O + X + Y + Z, -X, -Y, materialIndex)).ToList();
            res = res.Concat(CreateRhomboid(O + X + Y + Z, -Z, -X, materialIndex)).ToList();
            res = res.Concat(CreateRhomboid(O + X + Y + Z, -Y, -Z, materialIndex)).ToList();

            return res;
        }

        /// <summary>
        /// Converts from spherical to cartesian coordinates
        /// </summary>
        private static Point3D FromSphericCoordinates(double theta, double phi)
        {
            Point3D res;
            res.X = Math.Sin(phi) * Math.Cos(theta);
            res.Y = Math.Sin(phi) * Math.Sin(theta);
            res.Z = Math.Cos(phi);
            return res;
        }
    }
}
