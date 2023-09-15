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
        private float _sqrMaxDistance;
        private ulong _ownerId;
        private ProjectileGraphic _displayObject;
        private float _sqrMinVelocity;

        private int _tickDiff;
        private void Initialize(ProjectileInfo info, Vector3 graphicsSource, Vector3 direction, ulong ownerId, int tickDiff)
        {
            // Graphics
            _displayObject = Instantiate(info.Prefab, transform.position, Quaternion.identity);
            // Physics
            _velocity = direction.normalized * info.MuzzleVelocity;

            _lastPosition = transform.position;
            _spawnPosition = transform.position;

            _ownerId = ownerId;
            _info = info;
            _tickDiff = tickDiff;

            _sqrMaxDistance = info.MaxDistance * info.MaxDistance;
            _sqrMinVelocity = info.MinVelocity * info.MinVelocity;

            _displayObject.OnInitialisation(graphicsSource, _velocity);

            NetworkManager.Singleton.NetworkTickSystem.Tick += Tick;
            PlayerBulletHitboxManager.AddBullet(_tickDiff);
        }
        public static void SpawnProjectile(ProjectileInfo info, Vector3 graphicsSource, Vector3 position, Vector3 direction, ulong ownerId, int tickDiff)
        {
            GameObject projectileObj = Instantiate(PrefabHolder.Prefabs[PrefabTypes.Projectile], position, Quaternion.identity);
            projectileObj.GetComponent<Projectile>().Initialize(info, graphicsSource, direction, ownerId, tickDiff);
        }
        public static void SpawnProjectile(ProjectileInfo info, Vector3 position, Vector3 direction, ulong ownerId, int tickDiff)
        {
            GameObject projectileObj = Instantiate(PrefabHolder.Prefabs[PrefabTypes.Projectile], position, Quaternion.identity);
            projectileObj.GetComponent<Projectile>().Initialize(info, position, direction, ownerId, tickDiff);
        }
        private void Tick()
        {
            Vector3 movement = _velocity * NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            transform.position += movement;

            if (Hitscan(movement, _lastPosition, ref _velocity))
            {
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

            // Check for min velocity
            if (_velocity.sqrMagnitude <= _sqrMinVelocity)
            {
                EndProjectile();
                return;
            }

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
        private bool Hitscan(Vector3 movement, Vector3 startPos, ref Vector3 velocity)
        {
            bool destroy = false;
            Vector3 oldVelocity = velocity;
            Vector3 backwardsStartPos = startPos + movement;
            Vector3 backwardsMovement = movement * -1;
            // Main hitscan
            List<RaycastHit> hits = Utility.RaycastAll(startPos, movement, movement.magnitude, ProjectileHitLayer.CanHit, true);
            foreach (RaycastHit hit in hits)
            {
                if (!hit.transform.TryGetComponent(out IDamagable damagable))
                    continue;
                
                if (damagable.OnHit(_info, hit.point, hit.normal, _ownerId, _tickDiff, ref velocity))
                    _displayObject.AddHit(hit.point, oldVelocity.normalized);
                    
                if (VelocityReversed(oldVelocity, velocity))
                {
                    backwardsStartPos = hit.point;
                    backwardsMovement *= (hit.point - startPos).magnitude;
                    destroy = true;
                    break;
                }
                else
                    oldVelocity = velocity;
            }
            // Backwards hitscan for bullet holes
            List<RaycastHit> backwardsHits = Utility.RaycastAll(backwardsStartPos, backwardsMovement, backwardsMovement.magnitude, ProjectileHitLayer.CanHit);
            foreach (RaycastHit hit in backwardsHits)
                if (hit.transform.TryGetComponent(out IDamagable damagable))
                    damagable.OnExit(_info, hit.point, hit.normal, velocity);

            return destroy;
        }
        private bool VelocityReversed(Vector3 oldVelocity, Vector3 newVelocity) => newVelocity == Vector3.zero ||
                newVelocity.x / Mathf.Abs(newVelocity.x) != oldVelocity.x / Mathf.Abs(oldVelocity.x) ||
                newVelocity.z / Mathf.Abs(newVelocity.z) != oldVelocity.z / Mathf.Abs(oldVelocity.z);
    }
}