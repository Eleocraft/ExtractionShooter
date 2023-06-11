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
        private ulong _ownerId;
        private const float HIT_OFFSET = 0.001f;
        private void Initialize(ProjectileInfo info, Vector3 position, Vector3 direction, ulong ownerId)
        {
            // Graphics
            Instantiate(info.Prefab, transform.position, Quaternion.identity, transform);
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
            transform.rotation = Quaternion.FromToRotation(Vector3.up, _velocity);
            
            Vector3 startPos = _lastPosition;
            Vector3 currentMovement = movement;
            // Main hitscan
            while (true)
            {
                bool ray = Physics.Raycast(startPos, currentMovement, out RaycastHit hitInfo, currentMovement.magnitude, ProjectileHitLayer.CanHit);

                bool penetration = Physics.Raycast(startPos, currentMovement, out RaycastHit penetrableHitInfo, currentMovement.magnitude, ProjectileHitLayer.Penetrable);

                if (ray && penetration)
                {
                    if (hitInfo.distance < penetrableHitInfo.distance)
                    {
                        if (MainHit(hitInfo))
                            break;
                        else
                        {
                            startPos = hitInfo.point + currentMovement.normalized * HIT_OFFSET;
                            currentMovement *= 1 - (hitInfo.distance / currentMovement.magnitude);
                        }
                    }
                    else
                    {
                        if (PenetrationHit(penetrableHitInfo, true, out Penetrable penetrable))
                        {
                            _velocity *= _info.PenetrationForce - penetrable.Resistance;
                            startPos = penetrableHitInfo.point + currentMovement.normalized * HIT_OFFSET;
                            currentMovement *= 1 - (penetrableHitInfo.distance / currentMovement.magnitude);
                        }
                        else
                            break;
                    }
                }
                else if (ray)
                {
                    if (MainHit(hitInfo))
                        break;
                    else
                    {
                        startPos = hitInfo.point + currentMovement.normalized * HIT_OFFSET;
                        currentMovement *= 1 - (hitInfo.distance / currentMovement.magnitude);
                    }
                }
                else if (penetration)
                {
                    if (PenetrationHit(penetrableHitInfo, true, out Penetrable penetrable))
                    {
                        _velocity *= _info.PenetrationForce - penetrable.Resistance;
                        startPos = penetrableHitInfo.point + currentMovement.normalized * HIT_OFFSET;
                        currentMovement *= 1 - (penetrableHitInfo.distance / currentMovement.magnitude);
                    }
                    else
                        break;
                }
                else
                    break;
            }
            // Backwards hitscan for bullet holes
            startPos = transform.position;
            currentMovement = movement *= -1;
            while (true)
            {
                if (Physics.Raycast(startPos, currentMovement, out RaycastHit penetrableHitInfo, currentMovement.magnitude, ProjectileHitLayer.Penetrable)
                    && PenetrationHit(penetrableHitInfo, false, out Penetrable penetrable))
                {
                    startPos = penetrableHitInfo.point + currentMovement.normalized * HIT_OFFSET;
                    currentMovement *= 1 - (penetrableHitInfo.distance / currentMovement.magnitude);
                }
                else
                    break;
            }

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
            
            bool MainHit(RaycastHit hitInfo)
            {
                if (hitInfo.transform.TryGetComponent(out IDamagable damagable))
                {
                    if (!damagable.CanHit(_ownerId))
                        return false;
                    
                    damagable.OnHit(_info.Damage, hitInfo.point, _ownerId);
                }
                else
                    Instantiate(_info.HitMarker, hitInfo.point, Quaternion.identity, hitInfo.transform).Initialize(hitInfo.normal, _velocity);
                Destroy(gameObject);
                return true;
            }
            bool PenetrationHit(RaycastHit penetrableHitInfo, bool frontHit, out Penetrable penetrable)
            {
                if (penetrableHitInfo.transform.TryGetComponent(out penetrable) && penetrable.Resistance < _info.PenetrationForce)
                {
                    Instantiate(frontHit ? _info.PenetrateMarker : _info.ExitMarker, penetrableHitInfo.point, Quaternion.identity, penetrableHitInfo.transform).Initialize(penetrableHitInfo.normal, _velocity);
                    if (frontHit)
                        penetrable.Penetrate();
                    return true;
                }
                if (frontHit)
                {
                    Instantiate(_info.HitMarker, penetrableHitInfo.point, Quaternion.identity, penetrableHitInfo.transform).Initialize(penetrableHitInfo.normal, _velocity);
                    Destroy(gameObject);
                }
                return false;
            }
        }
    }
}