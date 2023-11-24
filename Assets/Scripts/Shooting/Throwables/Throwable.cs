using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Throwable : MonoBehaviour
    {
        private Vector3 _spawnPosition;
        private Vector3 _velocity;
        private float _sqrMaxDistance;
        private float _sqrMinVelocity;
        private ProjectileGraphic _displayObject;
        ThrowableInfo _info;
        private int _tickDiff;
        private ulong _ownerId;
        private const float HIT_ERROR = 0.01f;
        private void Initialize(ThrowableInfo info, Vector3 veloctiy, ulong ownerId, int tickDiff)
        {
            // Graphics
            _displayObject = Instantiate(info.Prefab, transform.position, Quaternion.identity);
            // Physics
            _velocity = veloctiy;

            _spawnPosition = transform.position;

            _tickDiff = tickDiff;
            _ownerId = ownerId;

            _info = info;

            _sqrMaxDistance = info.MaxDistance * info.MaxDistance;
            _sqrMinVelocity = info.MinVelocity * info.MinVelocity;

            _displayObject.OnInitialisation(transform.position, _velocity, Vector3.zero);

            NetworkManager.Singleton.NetworkTickSystem.Tick += Tick;
            PlayerBulletHitboxManager.AddBullet(_tickDiff);
        }
        public static void SpawnProjectile(ThrowableInfo info, Vector3 position, Vector3 velocity, ulong ownerId, int tickDiff)
        {
            GameObject projectileObj = Instantiate(PrefabHolder.Prefabs[PrefabTypes.Throwable], position, Quaternion.identity);
            projectileObj.GetComponent<Throwable>().Initialize(info, velocity, ownerId, tickDiff);
        }
        private void Tick()
        {
            Vector3 movement = _velocity * NetworkManager.Singleton.LocalTime.FixedDeltaTime;

            // Physics
            _velocity -= _velocity * _info.Drag * NetworkManager.Singleton.LocalTime.FixedDeltaTime; // Drag
            _velocity += Vector3.down * _info.Dropoff * NetworkManager.Singleton.LocalTime.FixedDeltaTime; // Gravity

            if (Physics.Raycast(transform.position, movement, out RaycastHit hit, movement.magnitude, ProjectileHitLayer.TrowableHit))
            {
                transform.position = hit.point + hit.normal * HIT_ERROR;
                _velocity = Vector3.Reflect(_velocity, hit.normal) * _info.Bouncyness;

                if (_velocity.sqrMagnitude < _sqrMinVelocity)
                {
                    // Explode granade
                    Instantiate(_info.Explosion, transform.position, Quaternion.identity).ExecuteExplosion(_ownerId, _tickDiff);
                    EndProjectile();
                    return;
                }
            }
            else
                transform.position += movement;

            _displayObject.SetPositionAndDirection(transform.position, _velocity);

            // Lifetime
            if ((transform.position - _spawnPosition).sqrMagnitude > _sqrMaxDistance)
            {
                EndProjectile();
                return;
            }

            void EndProjectile()
            {
                _displayObject.EndProjectile();
                NetworkManager.Singleton.NetworkTickSystem.Tick -= Tick;
                PlayerBulletHitboxManager.RemoveBullet(_tickDiff);
                Destroy(gameObject);
            }
        }
    }
}
