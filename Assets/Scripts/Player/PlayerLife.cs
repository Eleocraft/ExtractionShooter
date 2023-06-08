using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerLife : NetworkBehaviour, IDamagable
    {
        [SerializeField] private float MaxLife;
        [SerializeField] private GameObject BreakParticles;
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
        public void OnHit(float damage)
        {
            if (IsServer)
            {
                _life.Value -= damage;
                if (_life.Value < 0)
                {
                    _life.Value = MaxLife;
                    PlayerDeadClientRpc();
                }
            }
        }
        [ClientRpc]
        private void PlayerDeadClientRpc()
        {
            Instantiate(BreakParticles, transform.position, Quaternion.identity);
            if (IsOwner)
                Debug.Log("You Died");
        }
    }
}