using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "CustomObjects/Weapons/SemiAuto")]
    public class SemiAutomaticWeapon : Weapon
    {
        [SerializeField] private ProjectileInfo projectileInfo;
        public override void StartMainAction(Vector3 position, Vector3 direction)
        {
            Projectile.SpawnProjectile(projectileInfo, position, direction);
        }
    }
}