using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerBulletHitbox : MonoBehaviour
    {
        private PlayerLife _playerLife;
        private FirstPersonController _controller;
        private int _tickDiff;
        private bool _active;
        public void Initialize(PlayerLife playerLife, FirstPersonController controller, int tickDiff, bool active)
        {
            foreach (PlayerHitRegion hitRegion in GetComponentsInChildren<PlayerHitRegion>())
                hitRegion.Initialize(this);

            _playerLife = playerLife;
            _controller = controller;
            _tickDiff = tickDiff;
            SetActive(active);

            NetworkManager.Singleton.NetworkTickSystem.Tick += Tick;
        }
        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.NetworkTickSystem.Tick -= Tick;
        }
        public void OnHit(ProjectileInfo info, Vector3 point, DamageType damageType, float projectileVelocity, ulong ownerId, int tickDiff)
        {
            if (tickDiff == _tickDiff)
                _playerLife.OnHit(info, point - transform.position, damageType, projectileVelocity, ownerId);
        }
        private void Tick()
        {
            if (_tickDiff == 0 || !_active)
                return;
            
            if (_controller.GetState(NetworkManager.Singleton.LocalTime.Tick - _tickDiff, out NetworkTransformState state))
            {
                transform.position = state.Position;
                transform.rotation = Quaternion.Euler(0, state.LookRotation.y, 0);
            }
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            _active = active;
        }
    }
}