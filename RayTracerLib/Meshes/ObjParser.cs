using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using OpenCvSharp;

namespace RayTracerLib
{
    /// <summary>
    /// Class used to load .obj files, supported features below 
    ///  - Faces (required)
    ///  - Normals (required)
    ///  - Textures (required)
    ///  
    ///  Faces are expected to be convex and to follow the right hand convention <br/>
    ///  ⚠️ materials not supported yet
    /// </summary>
    public class ObjParser
    {
        /// <summary>
        /// Parses a .obj file to generate a SmartMesh
        /// </summary>
        /// <param name="filename"> The path to the file to parse </param>
        /// <param name="materialIndex"> The material index of the generated SmartMesh </param>
        /// <param name="textureIndex"> The texture index of the generated SmartMesh </param>
        /// <returns> a SmartMesh </returns>
        /// <exception cref="IOException"> Failed to read texture file </exception>
        public static SmartMesh Parse(string filename, int materialIndex, int textureIndex)
        {
            List<string> lines = new();
            try { lines = File.ReadAllLines(filename).ToList(); }
            catch { throw new IOException("Failed to read texture file"); }

            List<Point3D> vertices = [];
            List<Vector3D> normals = [];
            List<Point2d> textures = [];
            List<Triangle> res = [];

            foreach (var line in lines) 
            {
                
                if (line.StartsWith("vt")) { textures.Add(ParseTexture(line)); }
                else if (line.StartsWith("vn")) { normals.Add(ParseNormal(line)); }
                else if (line.StartsWith("v ")) { vertices.Add(ParseVertex(line)); }
                else if (line.StartsWith("f ")) 
                { 
                    foreach(var triangle in ParseFace(line, 
                        vertices, 
                        normals, 
                        textures, 
                        materialIndex, 
                        textureIndex))
                    {
                        res.Add(triangle);
                    }
                }
            }

            return new(res);
        }

        /// <summary>
        /// Parses a vertex line
        /// </summary>
        /// <param name="line"> the line to parse </param>
        /// <returns> the parsed vertex </returns>
        /// <exception cref="Exception"> Invalid vertex line </exception>
        private static Point3D ParseVertex(string line)
        {
            string[] split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if(split.Length < 4) { throw new Exception("Vertex line does not have enough fields"); }
            Point3D res;
            res.X = ParseDouble(split[1]);
            res.Y = ParseDouble(split[2]);
            res.Z = ParseDouble(split[3]);
            return res;
        }

        /// <summary>
        /// Parses a normal line
        /// </summary>
        /// <param name="line"> The line to parse </param>
        /// <returns> The parsed normal </returns>
        /// <exception cref="Exception">Invalid normal line</exception>
        private static Vector3D ParseNormal(string line)
        {
            string[] split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length < 4) { throw new Exception("Nomal line does not have enough fields"); }
            Vector3D res;
            res.X = ParseDouble(split[1]);
            res.Y = ParseDouble(split[2]);
            res.Z = ParseDouble(split[3]);
            return res;
        }
        
        /// <summary>
        /// Parses a texture coordinates line
        /// </summary>
        /// <param name="line">The line to parse</param>
        /// <returns>The parsed texture coordinate</returns>
        /// <exception cref="Exception"></exception>
        private static Point2d ParseTexture(string line)
        {
            string[] split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length < 3) { throw new Exception("Texture line does not have enough fields"); }
            Point2d res;
            res.X = ParseDouble(split[1]);
            res.Y = ParseDouble(split[2]);
            return res;
        }

        /// <summary>
        /// Parses a face line
        /// </summary>
        /// <param name="line">The line to parse</param>
        /// <param name="vertices">The vertices previously parsed</param>
        /// <param name="normals">The normals previously parsed</param>
        /// <param name="textures">The textures previously parsed</param>
        /// <param name="materialIndex">The material index for this face</param>
        /// <param name="textureIndex">The texture index for this face</param>
        /// <returns>The triangles of the parsed face</returns>
        /// <exception cref="Exception">Invalid face line</exception>
        private static List<Triangle> ParseFace(
            string line,
            List<Point3D> vertices,
            List<Vector3D> normals,
            List<Point2d> textures,
            int materialIndex,
            int textureIndex)
        {
            string[] split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length < 4) { throw new Exception("Face line does not have enough fields"); }
            
            // Parse vertexes
            List<TriangleVertex> vertexes = [];
            for(int i = 1; i < split.Length; i++)
            {
                vertexes.Add(ParseTriangleVertex(split[i], vertices, normals, textures));
            }

            // Create triangles from the vertices
            List<Triangle> res = [];
            TriangleVertex firstVertex = vertexes.First();
            for (int i = 1; i < vertexes.Count - 1; i++)
            {
                res.Add(new()
                {
                    A = firstVertex,
                    B = vertexes[i],
                    C = vertexes[i + 1],
                    materialIndex = materialIndex,
                    textureIndex = textureIndex
                });
            }

            return res;
        }

        /// <summary>
        /// Parses a TriangleVertex field (pos + normal + texture)
        /// </summary>
        /// <param name="line">The line to parse</param>
        /// <param name="vertices">The previously parsed vertices</param>
        /// <param name="normals">The previously parsed normals</param>
        /// <param name="textures">The previously parsed textures</param>
        /// <returns>The parsed TriangleVertex</returns>
        /// <exception cref="Exception">Invalid field</exception>
        private static TriangleVertex ParseTriangleVertex(string line,
            List<Point3D> vertices,
            List<Vector3D> normals,
            List<Point2d> textures)
        {
            string[] split = line.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length < 3) { throw new Exception("Face vertex does not have enough fields"); }

            TriangleVertex res;
            res.pos = vertices[int.Parse(split[0]) - 1];
            res.texture = textures[int.Parse(split[1]) - 1];
            res.normal = normals[int.Parse(split[2]) - 1];

            return res;
        }

        /// <summary>
        /// Parses a double
        /// </summary>
        /// <param name="s"> The string to parse </param>
        /// <returns>The parsed double</returns>
        private static double ParseDouble(string s)
        {
            return double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        }

    }
}
