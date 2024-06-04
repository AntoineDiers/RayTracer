using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace RayTracerLib
{
    /// <summary>
    /// The container for the scene's textures
    /// </summary>
    public class TextureLibrary
    {
        /// <summary> The textures</summary>
        private List<OpenCvSharp.Mat> _textures = [];

        /// <summary>
        /// Loads a texture from an image file
        /// </summary>
        /// <param name="filename"> The path to the image to load </param>
        /// <returns>The index of the loaded texture</returns>
        /// <exception cref="Exception"> could not load texture </exception>
        public int LoadTexture(string filename)
        {
            try { _textures.Add(Cv2.ImRead(filename)); }
            catch { throw new Exception("Failed to load texture"); }
            return _textures.Count - 1;
        }

        /// <summary>
        /// Gets the textures list
        /// </summary>
        /// <returns> The textures list </returns>
        internal List<OpenCvSharp.Mat> Get()
        {
            return _textures;
        }
    }
}
