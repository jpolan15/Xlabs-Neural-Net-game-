using System;
using UnityEngine;

namespace Convergence.Gameplay
{
    /// <summary>
    /// A tray button. XR select raises the request. Presentation measures the photo.
    /// </summary>
    public class PhotoRequest : MonoBehaviour
    {
        [SerializeField] private bool fileAsEarth;

        /// <summary>True when this tray is the EARTH tray.</summary>
        public bool FileAsEarth => fileAsEarth;

        /// <summary>Raised on select. Payload is the tray choice.</summary>
        public event Action<bool> OnPhotoRequested;

        /// <summary>Called by the XRI bridge.</summary>
        public void Request()
        {
            OnPhotoRequested?.Invoke(fileAsEarth);
        }
    }
}
