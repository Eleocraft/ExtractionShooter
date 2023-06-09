using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Dummy : NetworkBehaviour, IDamagable
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
        public void OnHit(float damage, Vector3 point, ulong ownerId)
        {
            Instantiate(HitParticles, point, Quaternion.identity);
            if (!IsServer) return;
            
            _life.Value -= damage;
            if (_life.Value < 0)
                GetComponent<NetworkObject>().Despawn();
        }
        public override void OnNetworkDespawn()
        {
            Instantiate(BreakParticles, transform.position, Quaternion.identity);
        }

    }
}