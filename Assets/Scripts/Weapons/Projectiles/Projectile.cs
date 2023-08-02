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
        private void Initialize(ProjectileInfo info, Vector3 position, Vector3 direction, ulong ownerId)
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

            NetworkManager.Singleton.NetworkTickSystem.Tick += Tick;
        }
        private void OnDestroy()
        {
            NetworkManager.Singleton.NetworkTickSystem.Tick -= Tick;
        }
        public static void SpawnProjectile(ProjectileInfo info, Vector3 position, Vector3 direction, ulong ownerId)
        {
            GameObject projectileObj = Instantiate(PrefabHolder.Prefabs[PrefabTypes.Projectile], position, Quaternion.identity);
            projectileObj.GetComponent<Projectile>().Initialize(info, position, direction, ownerId);
        }
        private void Tick()
        {
            Vector3 oldVelocity = _velocity;
            Vector3 movement = _velocity * NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            transform.position += movement;
            _displayObject.transform.position = transform.position;
            _displayObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, _velocity);

            Hitscan(movement, _lastPosition, out float penetrationResistances);
            _velocity *= penetrationResistances;

            // Lifetime
            if ((transform.position - _spawnPosition).sqrMagnitude > _maxDistanceSqr)
                Destroy(gameObject);
            // Physics
            _velocity -= _info.Drag * _velocity * NetworkManager.Singleton.LocalTime.FixedDeltaTime; // Drag
            _velocity += Vector3.down * _info.Dropoff * NetworkManager.Singleton.LocalTime.FixedDeltaTime; // Gravity
            // Check for reversed velocity
            if (_velocity.x / Mathf.Abs(_velocity.x) != oldVelocity.x / Mathf.Abs(oldVelocity.x) ||
                _velocity.z / Mathf.Abs(_velocity.z) != oldVelocity.z / Mathf.Abs(oldVelocity.z))
                Destroy(gameObject);

            _lastPosition = transform.position;
        }
        private void Hitscan(Vector3 movement, Vector3 startPos, out float penetrationResistances)
        {
            penetrationResistances = 1;
            // Main hitscan
            List<RaycastHit> hits = Utility.RaycastAll(startPos, movement, movement.magnitude, ProjectileHitLayer.CanHit);
            foreach (RaycastHit hit in hits)
            {
                if (Hit(hit, movement.normalized, out float penetrationResistance))
                    break;
                else
                    penetrationResistances *= penetrationResistance;
            }
            // Backwards hitscan for bullet holes
            Vector3 backwardsStartPos = startPos + movement;
            Vector3 backwardsMovement = movement *= -1;
            List<RaycastHit> backwardsHits = Utility.RaycastAll(backwardsStartPos, backwardsMovement, backwardsMovement.magnitude, ProjectileHitLayer.Penetrable);
            foreach (RaycastHit hit in backwardsHits)
                BackwardsHit(hit, backwardsMovement.normalized);

            bool Hit(RaycastHit hitInfo, Vector3 direction, out float penetrationResistance)
            {
                penetrationResistance = 1;
                if (hitInfo.transform.TryGetComponent(out IDamagable damagable))
                {
                    if (!damagable.CanHit(_ownerId))
                        return false;
                    
                    damagable.OnHit(_info.Damage, hitInfo.point, _ownerId);
                }
                else if (hitInfo.transform.TryGetComponent(out Penetrable penetrable) && penetrable.Resistance < _info.PenetrationForce)
                {
                    Instantiate(_info.PenetrateMarker, hitInfo.point, Quaternion.identity, hitInfo.transform).Initialize(hitInfo.normal, direction);
                    penetrable.Penetrate();
                    penetrationResistance = 1 - (_info.PenetrationForce - penetrable.Resistance);
                    return false;
                }
                else
                {
                    Instantiate(_info.HitMarker, hitInfo.point, Quaternion.identity, hitInfo.transform).Initialize(hitInfo.normal, _velocity);
                    Destroy(gameObject);
                }
                return true;
            }
            bool BackwardsHit(RaycastHit hitInfo, Vector3 direction)
            {
                if (hitInfo.transform.TryGetComponent(out Penetrable penetrable))
                {
                    Instantiate(_info.ExitMarker, hitInfo.point, Quaternion.identity, hitInfo.transform).Initialize(hitInfo.normal, direction);
                    return true;
                }
                return false;  
            }
        }
    }
}