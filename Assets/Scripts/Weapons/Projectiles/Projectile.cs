using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Projectile : MonoBehaviour
    {
        private Vector3 _spawnPosition;
        private Vector3 _lastPosition;
        private Vector3 _velocity;
        private ProjectileInfo _info;
        private float _maxDistanceSqr;
        private void Initialize(ProjectileInfo info, Vector3 position, Vector3 direction)
        {
            // Graphics
            Instantiate(info.Prefab, transform.position, Quaternion.identity, transform);
            // Physics
            _velocity = direction.normalized * info.MuzzleVelocity;

            _lastPosition = transform.position;
            _spawnPosition = transform.position;
            _maxDistanceSqr = info.MaxDistance * info.MaxDistance;
            _info = info;
        }
        public static void SpawnProjectile(ProjectileInfo info, Vector3 position, Vector3 direction)
        {
            GameObject projectileObj = Instantiate(PrefabHolder.Prefabs[PrefabTypes.Projectile], position, Quaternion.identity);
            projectileObj.GetComponent<Projectile>().Initialize(info, position, direction);
        }
        private void Update()
        {
            Vector3 movement = _velocity * Time.deltaTime;
            transform.position += movement;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, _velocity);
            // Spherecast
            if (Physics.Raycast(_lastPosition, movement, out RaycastHit hitInfo, movement.magnitude, _info.CanHit))
            {
                if (hitInfo.transform.TryGetComponent(out IDamagable damagable))
                    damagable.OnHit(_info.Damage);
                GameObject hitMarker = Instantiate(_info.HitMarker, hitInfo.point, Quaternion.identity, hitInfo.transform);
                hitMarker.transform.localScale = new Vector3(1/hitInfo.transform.lossyScale.x, 1/hitInfo.transform.lossyScale.y, 1/hitInfo.transform.lossyScale.z);
                Destroy(gameObject);
            }
            // Lifetime
            if ((transform.position - _spawnPosition).sqrMagnitude > _maxDistanceSqr)
                Destroy(gameObject);
            // Physics
            _velocity -= _info.Drag * _velocity * Time.deltaTime; // Drag
            _velocity += Vector3.down * _info.Dropoff * Time.deltaTime; // Gravity


            _lastPosition = transform.position;
        }
    }
}