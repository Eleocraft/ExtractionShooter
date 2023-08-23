using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Projectile : MonoBehaviour
    {
        private Vector3 _spawnPosition;
        private Vector3 _lastPosition;
        private Vector3 _velocity;
        private ProjectileInfo _info;
        private float _maxDistanceSqr;
        private ulong _ownerId;
        private GameObject _displayObject;
        private float _sqrMinVelocity;

        private int _tickDiff;
        private void Initialize(ProjectileInfo info, int tickDiff, Vector3 direction, ulong ownerId)
        {
            // Graphics
            _displayObject = Instantiate(info.Prefab, transform.position, Quaternion.identity);
            // Physics
            _velocity = direction.normalized * info.MuzzleVelocity;

            _lastPosition = transform.position;
            _spawnPosition = transform.position;
            _maxDistanceSqr = info.MaxDistance * info.MaxDistance;
            _ownerId = ownerId;
            _info = info;

            _sqrMinVelocity = _info.MinVelocity * _info.MinVelocity;

            NetworkManager.Singleton.NetworkTickSystem.Tick += Tick;
            _tickDiff = tickDiff;
        }
        private void OnDestroy()
        {
            NetworkManager.Singleton.NetworkTickSystem.Tick -= Tick;
        }
        public static void SpawnProjectile(ProjectileInfo info, Vector3 position, Vector3 direction, ulong ownerId, int tickDiff)
        {
            GameObject projectileObj = Instantiate(PrefabHolder.Prefabs[PrefabTypes.Projectile], position, Quaternion.identity);
            projectileObj.GetComponent<Projectile>().Initialize(info, tickDiff, direction, ownerId);
        }
        private void Tick()
        {
            Vector3 movement = _velocity * NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            transform.position += movement;

            _displayObject.transform.position = transform.position;
            _displayObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, _velocity);

            Hitscan(movement, _lastPosition, ref _velocity);

            // Lifetime
            if ((transform.position - _spawnPosition).sqrMagnitude > _maxDistanceSqr)
                Destroy(gameObject);
            
            // Physics
            _velocity -= _velocity * _info.Drag * NetworkManager.Singleton.LocalTime.FixedDeltaTime; // Drag
            _velocity += Vector3.down * _info.Dropoff * NetworkManager.Singleton.LocalTime.FixedDeltaTime; // Gravity

            // Check for min velocity
            if (_velocity.sqrMagnitude <= _sqrMinVelocity)
                Destroy(gameObject);

            _lastPosition = transform.position;
        }
        private void Hitscan(Vector3 movement, Vector3 startPos, ref Vector3 velocity)
        {
            Vector3 oldVelocity = velocity;
            Vector3 backwardsStartPos = startPos + movement;
            // Main hitscan
            List<RaycastHit> hits = Utility.RaycastAll(startPos, movement, movement.magnitude, ProjectileHitLayer.CanHit, true);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.TryGetComponent(out IDamagable damagable))
                    damagable.OnHit(_info, hit.point, hit.normal, _ownerId, _tickDiff, ref velocity);

                if (VelocityReversed(oldVelocity, velocity))
                {
                    backwardsStartPos = hit.point; 
                    break;
                }
            }
            // Backwards hitscan for bullet holes
            Vector3 backwardsMovement = movement *= -1;
            List<RaycastHit> backwardsHits = Utility.RaycastAll(backwardsStartPos, backwardsMovement, backwardsMovement.magnitude, ProjectileHitLayer.CanHit);
            foreach (RaycastHit hit in backwardsHits)
                if (hit.transform.TryGetComponent(out IDamagable damagable))
                    damagable.OnExit(_info, hit.point, hit.normal, velocity);  
        }
        private bool VelocityReversed(Vector3 oldVelocity, Vector3 newVelocity) => newVelocity == Vector3.zero ||
                newVelocity.x / Mathf.Abs(newVelocity.x) != oldVelocity.x / Mathf.Abs(oldVelocity.x) ||
                newVelocity.z / Mathf.Abs(newVelocity.z) != oldVelocity.z / Mathf.Abs(oldVelocity.z);
    }
}