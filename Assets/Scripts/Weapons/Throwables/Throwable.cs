using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Throwable : MonoBehaviour
    {
        private Vector3 _spawnPosition;
        private Vector3 _lastPosition;
        private Vector3 _velocity;
        private float _sqrMaxDistance;
        private ProjectileGraphic _displayObject;
        ThrowableInfo _info;
        private int _tickDiff;
        private ulong _ownerId;
        private void Initialize(ThrowableInfo info, Vector3 direction, ulong ownerId, int tickDiff)
        {
            // Graphics
            _displayObject = Instantiate(info.Prefab, transform.position, Quaternion.identity);
            // Physics
            _velocity = direction.normalized * info.InitialVelocity;

            _lastPosition = transform.position;
            _spawnPosition = transform.position;

            _tickDiff = tickDiff;
            _ownerId = ownerId;

            _info = info;

            _sqrMaxDistance = info.MaxDistance * info.MaxDistance;

            _displayObject.OnInitialisation(transform.position, _velocity);

            NetworkManager.Singleton.NetworkTickSystem.Tick += Tick;
            PlayerBulletHitboxManager.AddBullet(_tickDiff);
        }
        public static void SpawnProjectile(ThrowableInfo info, Vector3 position, Vector3 direction, ulong ownerId, int tickDiff)
        {
            GameObject projectileObj = Instantiate(PrefabHolder.Prefabs[PrefabTypes.GranadeProjectile], position, Quaternion.identity);
            projectileObj.GetComponent<Throwable>().Initialize(info, direction, ownerId, tickDiff);
        }
        private void Tick()
        {
            Vector3 movement = _velocity * NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            transform.position += movement;

            if (Physics.Raycast(_lastPosition, movement, out RaycastHit hit, movement.magnitude, ProjectileHitLayer.CanHit))
            {
                Instantiate(_info.Explosion, hit.point, Quaternion.identity).ExecuteExplosion(_ownerId, _tickDiff);
                EndProjectile();
                return;
            }

            // Lifetime
            if ((transform.position - _spawnPosition).sqrMagnitude > _sqrMaxDistance)
            {
                EndProjectile();
                return;
            }
            
            // Physics
            _velocity -= _velocity * _info.Drag * NetworkManager.Singleton.LocalTime.FixedDeltaTime; // Drag
            _velocity += Vector3.down * _info.Dropoff * NetworkManager.Singleton.LocalTime.FixedDeltaTime; // Gravity

            _lastPosition = transform.position;
            _displayObject.SetPositionAndDirection(transform.position, _velocity);

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
