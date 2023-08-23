using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerBulletHitbox : MonoBehaviour
    {
        private PlayerLife _playerLife;
        private FirstPersonController _controller;
        private int _tickDiff;
        public void Initialize(PlayerLife playerLife, FirstPersonController controller, int tickDiff)
        {
            foreach (PlayerHitRegion hitRegion in GetComponentsInChildren<PlayerHitRegion>())
                hitRegion.Initialize(this);

            _playerLife = playerLife;
            _controller = controller;
            _tickDiff = tickDiff;

            NetworkManager.Singleton.NetworkTickSystem.Tick += Tick;
        }
        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.NetworkTickSystem.Tick -= Tick;
        }
        public void OnHit(ProjectileInfo info, Vector3 point, bool headshot, ulong ownerId, int tickDiff)
        {
            if (tickDiff == _tickDiff)
                _playerLife.OnHit(info, point - transform.position, headshot, ownerId);
        }
        private void Tick()
        {
            if (_tickDiff == 0)
                return;
            
            if (_controller.GetState(NetworkManager.Singleton.LocalTime.Tick - _tickDiff, out NetworkTransformState state))
            {
                transform.position = state.Position;
                transform.rotation = Quaternion.Euler(0, state.LookRotation.y, 0);
            }
        }
    }
}