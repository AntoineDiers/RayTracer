using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Media.Media3D;

namespace RayTracerLib
{
    /// <summary>
    /// A mesh structure that recursively stores triangles 
    /// in a tree structure that makes collision detection very fast. <br/>
    /// ⚠️ The triangles will be moved and scaled such that 
    /// their bounding box is centered around 0 and has a max size of 1 in any direction
    /// </summary>
    public class SmartMesh
    {
        /// <summary> The triangles contained by this node of the SmartMesh tree </summary>
        internal List<Triangle> triangles = [];
        /// <summary> The bounding box associated with this node </summary>
        internal BoundingBox boundingBox;
        /// <summary> The children of this node </summary>
        internal List<SmartMesh> children = [];

        /// <summary> Constructor </summary>
        private SmartMesh(){}

        /// <summary>
        /// Actual constructor :
        ///  - compute the bounding box of the given triangles
        ///  - split this bounding box in 8 smaller ones if it contains too many triangles
        ///  - allocate each triangle to the bounding box that contains it
        ///  - repeat recursively
        /// </summary>
        /// <param name="mesh"> The list of triangles this SmartMesh will contain</param>
        /// <param name="isRoot"> True if this is the root (used to normalize the root bounding box)</param>
        /// <exception cref="ArgumentException"> The input list can not be empty </exception>
        internal SmartMesh(List<Triangle> mesh, bool isRoot = true)
        {
            if (mesh.Count == 0) { throw new ArgumentException("Input list can not be empty"); }

            // Create Bounding Box
            boundingBox = new BoundingBox(mesh);

            // Center it on 0,0,0 and scale it to 1,1,1, do the same transformation on the triangles
            if (isRoot) 
            {
                NormalizeRootBoundingBox(mesh, ref boundingBox);
            }

            if (mesh.Count <= GlobalVariables.trianglePerSubMesh) 
            {
                triangles = mesh;
                return;
            }

            // Create children
            List<bool> remainingTriangles = Enumerable.Repeat(true, mesh.Count).ToList();
            foreach(double dx in (double[])[-0.25,0.25])
            {
                foreach (double dy in (double[])[-0.25, 0.25])
                {
                    foreach (double dz in (double[])[-0.25, 0.25])
                    {
                        Transform tf = new();
                        tf.SetTranslation(new(dx * boundingBox.size.X, dy * boundingBox.size.Y, dz * boundingBox.size.Z));
                        tf.SetScaling(0.5);
                        BoundingBox bb = boundingBox;
                        bb.size = tf.scaling.Transform(boundingBox.size);
                        bb.center = tf.translation.Transform(boundingBox.center);

                        List<Triangle> childTriangles = [];
                        for(int i = 0; i < mesh.Count; i++) 
                        {
                            Triangle triangle = mesh[i];
                            if (bb.Contains(triangle))
                            {
                                childTriangles.Add(triangle);
                                remainingTriangles[i] = false;
                            }
                        }
                        if(childTriangles.Count > 0) 
                        {
                            children.Add(new SmartMesh(childTriangles, false));
                        }
                    }
                }
            }
            triangles = new();
            for (int i = 0; i < remainingTriangles.Count; i++)
            {
                if(remainingTriangles[i]) 
                {
                    triangles.Add(mesh[i]);
                }
            }
        }

        /// <summary>
        /// Normalizes the root bounding box such that it is centered on 0 and of size 1 in all directions,
        /// applies the same transformation to the triangles 
        /// </summary>
        /// <param name="triangles"> The triangles contained by the bounding box </param>
        /// <param name="boundingBox"> The bounding box to transform </param>
        private static void NormalizeRootBoundingBox(List<Triangle> triangles, ref BoundingBox boundingBox)
        {
            double scale = Math.Max(boundingBox.size.X,
                Math.Max(boundingBox.size.Y, boundingBox.size.Z));
            Transform tf = new();
            tf.SetScaling(1 / scale);
            tf.SetTranslation(new Point3D(0, 0, 0) - boundingBox.center);
            boundingBox.center = tf.translation.Transform(boundingBox.center);
            boundingBox.size = tf.scaling.Transform(boundingBox.size);

            // Also apply this to the triangles
            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle t = triangles[i];
                t.A.pos = tf.translation.Transform(t.A.pos);
                t.B.pos = tf.translation.Transform(t.B.pos);
                t.C.pos = tf.translation.Transform(t.C.pos);

                t.A.pos = tf.scaling.Transform(t.A.pos);
                t.B.pos = tf.scaling.Transform(t.B.pos);
                t.C.pos = tf.scaling.Transform(t.C.pos);
                triangles[i] = t;
            }
        }

        /// <summary>
        /// Transforms this SmartMesh in the global frame
        /// </summary>
        /// <param name="tf"> The tf to apply to transform this SmartMesh </param>
        /// <returns>The transformed SmartMesh</returns>
        internal SmartMesh ToGlobalFrame(Transform tf)
        {
            SmartMesh res = new();

            // Bounding Box
            res.boundingBox = boundingBox;
            res.boundingBox.size = tf.scaling.Transform(res.boundingBox.size);
            res.boundingBox = res.boundingBox.Rotate(tf.rotation);
            res.boundingBox.center = tf.scaling.Transform(res.boundingBox.center);
            res.boundingBox.center = tf.translation.Transform(res.boundingBox.center);

            // Triangles
            res.triangles = new(triangles.Count);
            foreach(var triangle in triangles) 
            {
                res.triangles.Add(tf.Apply(triangle));
            }

            // children
            res.children = new(children.Count);
            foreach(var child in children) 
            {
                res.children.Add(child.ToGlobalFrame(tf));
            }

            return res;
        }
    }
}
