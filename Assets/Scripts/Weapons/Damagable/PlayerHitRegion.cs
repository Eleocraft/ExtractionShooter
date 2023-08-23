using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerHitRegion : MonoBehaviour, IDamagable
    {
        [SerializeField] private bool Headshot;

        private PlayerBulletHitbox _playerBulletHitbox;
        public void Initialize(PlayerBulletHitbox playerBulletHitbox)
        {
            _playerBulletHitbox = playerBulletHitbox;
        }
        public void OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ulong ownerId, int tickDiff, ref Vector3 velocity) => 
            _playerBulletHitbox.OnHit(info, point, Headshot, ownerId, tickDiff);
    }
}