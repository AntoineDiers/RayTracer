using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace RayTracerLib
{
    /// <summary>
    /// The camera
    /// </summary>
    public class Camera
    {
        /// <summary> The position of the camera </summary>
        public Transform tf = new();

        /// <summary> The resolution of the camera </summary>
        private readonly OpenCvSharp.Size _resolution;
        /// <summary> The rays that will be cast from the camera in the camera's frame </summary>
        private readonly Ray[] _raysInLocalFrame;

        /// <summary>
        /// Creates a camera from a given resolution and horizontal field of view
        /// </summary>
        /// <param name="resolution"> The resolution of the camera </param>
        /// <param name="hFov_deg"> The horizontal field of view of the camera </param>
        public Camera(OpenCvSharp.Size resolution, double hFov_deg)
        {
            _resolution = resolution;
            _raysInLocalFrame = new Ray[_resolution.Width * _resolution.Height];
            double focalLength = 0.5 * _resolution.Width / Math.Tan(0.5 * Math.PI * hFov_deg / 180.0);
            for (int row = 0; row < _resolution.Height; row++)
            {
                for (int col = 0; col < _resolution.Width; col++)
                {
                    Point3D origin = new(0, 0, 0);
                    Vector3D direction = new(
                        focalLength,
                        -col + _resolution.Width / 2.0,
                        -row + _resolution.Height / 2.0);
                    direction.Normalize();
                    _raysInLocalFrame[row * _resolution.Width + col] = new(origin, direction);
                }
            }
        }

        /// <summary>
        /// Computes the ray that should be cast for a given pixel of the camera in global frame
        /// </summary>
        /// <param name="row"> the row of the pixel </param>
        /// <param name="col"> the col of the pixel </param>
        /// <returns> The ray that should be cast for this pixel </returns>
        internal Ray GetRay(int row, int col)
        {
            Ray res = _raysInLocalFrame[row * _resolution.Width + col];
            res.origin = tf.translation.Transform(new Point3D(0,0,0));
            res.direction = tf.rotation.Transform(res.direction);

            return res;
        }

        /// <summary>
        /// Resolution getter
        /// </summary>
        /// <returns> The resolution of the camera </returns>
        internal OpenCvSharp.Size GetResolution()
        {
            return _resolution;
        }
    }
}
