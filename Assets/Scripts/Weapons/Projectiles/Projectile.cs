using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Projectile : MonoBehaviour
    {
        private Vector3 _lastPosition;
        private Vector3 _velocity;
        private ProjectileInfo _info;
        private float _lifeTime;
        private void Initialize(ProjectileInfo info, Vector3 position, Vector3 direction)
        {
            transform.position = position;
            // Graphics
            Instantiate(info.Prefab, transform.position, Quaternion.identity, transform);
            // Physics
            _velocity = direction.normalized * info.MuzzleVelocity;

            _lifeTime = info.LifeTime;
            _lastPosition = transform.position;
            _info = info;
        }
        public static void SpawnProjectile(ProjectileInfo info, Vector3 position, Vector3 direction)
        {
            GameObject projectileObj = new GameObject("Projectile");
            projectileObj.AddComponent<Projectile>().Initialize(info, position, direction);
        }
        private void FixedUpdate()
        {
            Vector3 movement = _velocity * Time.fixedDeltaTime;
            transform.position += movement;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, _velocity);
            // Spherecast
            if (Physics.Raycast(_lastPosition, movement, out RaycastHit hitInfo, movement.magnitude))
            {
                if (hitInfo.transform.TryGetComponent(out IDamagable damagable))
                {
                    damagable.OnHit(_info.Damage, hitInfo.point);
                    Destroy(gameObject);
                }
            }
            // Lifetime
            _lifeTime -= Time.fixedDeltaTime;
            if (_lifeTime < 0)
                Destroy(gameObject);
            // Physics
            _velocity -= _info.Drag * _velocity * Time.fixedDeltaTime; // Drag
            _velocity += Vector3.down * _info.Dropoff * Time.fixedDeltaTime; // Gravity


            _lastPosition = transform.position;
        }
    }
}