using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class FOVSyncronizer : MonoBehaviour
    {
        private Camera _thisCam;
        private Camera _otherCam;

        void Start()
        {
            _thisCam = GetComponent<Camera>();
            _otherCam = transform.parent.GetComponent<Camera>();
        }
        void Update()
        {
            _thisCam.fieldOfView = _otherCam.fieldOfView;
        }
    }
}
