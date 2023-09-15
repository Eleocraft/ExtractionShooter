using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerHitRegion : MonoBehaviour, IProjectileTarget, IDamagable
    {
        [SerializeField] private DamageType DamageType;

        private PlayerBulletHitbox _playerBulletHitbox;
        public void Initialize(PlayerBulletHitbox playerBulletHitbox)
        {
            _playerBulletHitbox = playerBulletHitbox;
        }
        public bool OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ulong ownerId, int tickDiff, ref Vector3 velocity) => 
            _playerBulletHitbox.OnHit(info, point, DamageType, velocity.magnitude, ownerId, tickDiff);
        
        public void Damage(float damage, ulong ownerId, int tickDiff)
        {
            if (DamageType == DamageType.Default)
                _playerBulletHitbox.Damage(damage, ownerId, tickDiff);
        }
    }
}