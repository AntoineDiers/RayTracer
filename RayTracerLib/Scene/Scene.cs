using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU.Runtime;
using ILGPU;
using System.Windows.Media.Media3D;

namespace RayTracerLib
{
    /// <summary>
    /// The scene, contains all the data that will be rendered
    /// </summary>
    public class Scene()
    {
        /// <summary> The camera </summary>
        public Camera camera = new(new OpenCvSharp.Size(1900, 1050), 100);
        /// <summary> The assets </summary>
        public List<Asset> assets = [];
        /// <summary> The light sources </summary>
        public List<LightSource> lightSources = [];
        /// <summary> The materials </summary>
        public MaterialsLibrary materials = new();
        /// <summary> The textures </summary>
        public TextureLibrary textures = new();
    }
}
