using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTracerLib
{
    /// <summary>
    /// The container for the scene's materials
    /// </summary>
    public class MaterialsLibrary
    {
        /// <summary> The materials </summary>
        private List<Material> _materials = [];
        
        /// <summary>
        /// Adds a new material
        /// </summary>
        /// <param name="material"> The material to add </param>
        /// <returns> The index of the added material </returns>
        public int AddMaterial(Material material) 
        {
            _materials.Add(material);
            return _materials.Count - 1;
        }

        /// <summary>
        /// Gets the list of materials
        /// </summary>
        /// <returns> the list of materials </returns>
        internal List<Material> Get()
        {
            return _materials;
        }

    }
}
