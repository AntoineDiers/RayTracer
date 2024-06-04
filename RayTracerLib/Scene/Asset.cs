using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;

namespace RayTracerLib
{
    /// <summary>
    /// An asset that contains the geometry and position of an object in the scene
    /// </summary>
    public class Asset
    {
        /// <summary> The position of the asset </summary>
        public Transform tf = new();
        /// <summary> The geometry of the asset </summary>
        private readonly SmartMesh _smartMesh;

        /// <summary>
        /// Creates an asset from a smartmesh
        /// </summary>
        /// <param name="smartMesh"> the input mesh </param>
        public Asset(SmartMesh smartMesh)
        {
            _smartMesh = smartMesh;
        }
        
        /// <summary>
        /// Computes the geometry of this asset in global frame
        /// </summary>
        /// <returns></returns>
        internal SmartMesh GetSmartMesh()
        {
            return _smartMesh.ToGlobalFrame(tf);
        }
    }
}
