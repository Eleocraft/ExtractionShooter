using UnityEngine;
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
        private const float HIT_OFFSET = 0.001f;
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
        }
        public static void SpawnProjectile(ProjectileInfo info, Vector3 position, Vector3 direction, ulong ownerId)
        {
            GameObject projectileObj = Instantiate(PrefabHolder.Prefabs[PrefabTypes.Projectile], position, Quaternion.identity);
            projectileObj.GetComponent<Projectile>().Initialize(info, position, direction, ownerId);
        }
        private void FixedUpdate()
        {
            Vector3 oldVelocity = _velocity;
            Vector3 movement = _velocity * Time.fixedDeltaTime;
            transform.position += movement;
            _displayObject.transform.position = transform.position;
            _displayObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, _velocity);

            Hitscan(movement, _lastPosition, out float penetrationResistances);
            _velocity *= penetrationResistances;

            // Lifetime
            if ((transform.position - _spawnPosition).sqrMagnitude > _maxDistanceSqr)
                Destroy(gameObject);
            // Physics
            _velocity -= _info.Drag * _velocity * Time.fixedDeltaTime; // Drag
            _velocity += Vector3.down * _info.Dropoff * Time.fixedDeltaTime; // Gravity
            // Check for reversed velocity
            if (_velocity.x / Mathf.Abs(_velocity.x) != oldVelocity.x / Mathf.Abs(oldVelocity.x) ||
                _velocity.z / Mathf.Abs(_velocity.z) != oldVelocity.z / Mathf.Abs(oldVelocity.z))
                Destroy(gameObject);

            _lastPosition = transform.position;
        }
        private void Hitscan(Vector3 movement, Vector3 startPos, out float penetrationResistances)
        {
            penetrationResistances = 1;
            Vector3 currentStartPos = startPos;
            Vector3 currentMovement = movement;
            // Main hitscan
            while (true)
            {
                if (Physics.Raycast(currentStartPos, currentMovement, out RaycastHit hitInfo, currentMovement.magnitude, ProjectileHitLayer.CanHit))
                {
                    if (Hit(hitInfo, currentMovement.normalized, out float penetrationResistance))
                        break;
                    else
                    {
                        penetrationResistances *= penetrationResistance;
                        currentStartPos = hitInfo.point + currentMovement.normalized * HIT_OFFSET;
                        currentMovement *= 1 - (hitInfo.distance / currentMovement.magnitude);
                    }
                }
                else
                    break;
            }
            // Backwards hitscan for bullet holes
            currentStartPos = startPos + movement;
            currentMovement = movement *= -1;
            while (true)
            {
                if (Physics.Raycast(currentStartPos, currentMovement, out RaycastHit hitInfo, currentMovement.magnitude, ProjectileHitLayer.Penetrable)
                    && BackwardsHit(hitInfo, currentMovement.normalized))
                {
                    currentStartPos = hitInfo.point + currentMovement.normalized * HIT_OFFSET;
                    currentMovement *= 1 - (hitInfo.distance / currentMovement.magnitude);
                }
                else
                    break;
            }

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