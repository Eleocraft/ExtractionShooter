using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Projectile : MonoBehaviour
    {
        private Vector3 _spawnPosition;
        private Vector3 _velocity;
        private ProjectileInfo _info;
        private float _sqrMaxDistance;
        private ulong _ownerId;
        private ProjectileGraphic _displayObject;
        private float _sqrMinVelocity;
        private float _distanceTraveled;
        private Dictionary<IProjectileTarget, float> hitObjects = new();

        private int _tickDiff;
        private void Initialize(ProjectileInfo info, Vector3 graphicsSource, Vector3 direction, ulong ownerId, int tickDiff)
        {
            // Graphics
            _displayObject = Instantiate(info.Prefab, transform.position, Quaternion.identity);
            // Physics
            _velocity = direction.normalized * info.MuzzleVelocity;

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
        private void Tick()
        {
            Vector3 movement = _velocity * NetworkManager.Singleton.LocalTime.FixedDeltaTime;

            // Physics
            _velocity -= _velocity * _info.Drag * NetworkManager.Singleton.LocalTime.FixedDeltaTime; // Drag
            _velocity += Vector3.down * _info.Dropoff * NetworkManager.Singleton.LocalTime.FixedDeltaTime; // Gravity

            if (Hitscan(movement, transform.position, _distanceTraveled, ref _velocity))
            {
                EndProjectile();
                return;
            }

            _distanceTraveled += movement.magnitude;
            transform.position += movement;
            _displayObject.SetPositionAndDirection(transform.position, _velocity);

            // Lifetime
            if ((transform.position - _spawnPosition).sqrMagnitude > _sqrMaxDistance)
            {
                EndProjectile();
                return;
            }

            // Check for min velocity
            else if (_velocity.sqrMagnitude <= _sqrMinVelocity)
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
        private bool Hitscan(Vector3 movement, Vector3 startPos, float distanceTraveled, ref Vector3 velocity)
        {
            Vector3 oldVelocity = velocity;
            // Main hitscan
            List<RaycastHit> hits = Utility.RaycastAll(startPos, movement, movement.magnitude, ProjectileHitLayer.CanHit, true);
            foreach (RaycastHit hit in hits)
            {
                if (!hit.transform.TryGetComponent(out IProjectileTarget damagable))
                    continue;

                float totalDistance = (hit.point - startPos).magnitude + distanceTraveled;

                if (!hitObjects.ContainsKey(damagable))
                {
                    if (damagable.OnHit(_info, hit.point, hit.normal, _ownerId, _tickDiff, ref velocity))
                        _displayObject.AddHit(hit.point, oldVelocity.normalized);
                    
                    hitObjects.Add(damagable, totalDistance);
                }
                else
                {
                    damagable.OnExit(_info, hit.point, hit.normal, totalDistance - hitObjects[damagable], ref velocity);
                    hitObjects.Remove(damagable);
                }
                
                if (VelocityReversed(oldVelocity, velocity))
                    return true;
                else
                    oldVelocity = velocity;
            }
            return false;
        }
        private bool VelocityReversed(Vector3 oldVelocity, Vector3 newVelocity) => newVelocity == Vector3.zero ||
                newVelocity.x / Mathf.Abs(newVelocity.x) != oldVelocity.x / Mathf.Abs(oldVelocity.x) ||
                newVelocity.z / Mathf.Abs(newVelocity.z) != oldVelocity.z / Mathf.Abs(oldVelocity.z);
    }
}