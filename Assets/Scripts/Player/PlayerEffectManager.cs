using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerEffectManager : NetworkBehaviour
    {
        // For the original scope of the project this would have to be replaced with a more versatile effect manager
        private float stunTimer;
        [SerializeField] private float maxStun = 2;
        public float Slowdown {
            get => stunTimer > 0 ? 0.1f : 1;
        }
        private void Start() {
            GetComponent<FirstPersonController>().TransformStateChanged += OnTransformStateChanged;
        }
        public void Stun() {
            stunTimer = maxStun;
        }
        private void OnTransformStateChanged(NetworkTransformState state) {
            if (state.SpeedMultiplier < 1)
                stunTimer -= NetworkManager.NetworkTickSystem.LocalTime.FixedDeltaTime;
        }
    }
}
