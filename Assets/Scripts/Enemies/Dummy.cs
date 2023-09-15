using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Dummy : NetworkBehaviour, IProjectileTarget, IDamagable
    {
        [SerializeField] private float MaxLife;
        [SerializeField] private GameObject BreakParticles;
        [SerializeField] private GameObject HitParticles;

        private NetworkVariable<float> _life = new NetworkVariable<float>();
        private void Start()
        {
            if (IsServer)
                _life.Value = MaxLife;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
        }
        public bool OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ref Vector3 velocity)
        {
            if (!IsSpawned)
                return false;
            
            Instantiate(HitParticles, point, Quaternion.identity);

            if (IsServer)
                Damage(info.GetDamage(DamageType.Default, velocity.magnitude));
            
            return true;
        }
        public void Damage(float damage)
        {
            _life.Value -= damage;
            if (_life.Value <= 0)
                GetComponent<NetworkObject>().Despawn();
        }
        public override void OnNetworkDespawn()
        {
            Instantiate(BreakParticles, transform.position, Quaternion.identity);
        }

    }
}