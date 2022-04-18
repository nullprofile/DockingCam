using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OLDD_camera
{
    /// <summary>
    /// The class to interface with kRPC
    /// </summary>
    public partial class API
    {
        private static API instance;
        private static API Instance
        {
            get
            {
                if (instance == null)
                    instance = new API();
                return instance;
            }
        }

        /// <summary>
        /// Gets the image from the current instance.
        /// </summary>
        public byte[] InstanceGetImage(Part part)
        {
            Debug.Log("DockingCamera: Instance get image");
            var cameraModules = part.Modules.OfType<Modules.PartCameraModule>();

            foreach (var camera in cameraModules)
            {
                Debug.Log("DockingCamera: Camera image");
                // Return the first one
                return camera.GetPic();
            }
            return Array.Empty<byte>();
        }

        /// <summary>
        /// Gets the current image. Called by kRPC.
        /// </summary>
        public static byte[] GetImage(Part part)
        {
            return Instance.InstanceGetImage(part);
        }
    }
}

